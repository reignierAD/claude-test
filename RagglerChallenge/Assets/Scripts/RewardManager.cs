using UnityEngine;
using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Bridges game results to on-chain claims.
/// Uses System.Net.Http.HttpClient (standard .NET) instead of
/// UnityWebRequest to avoid the CS1069 assembly-forwarding error in Unity 2022.
/// </summary>
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [Header("Backend Signer URL")]
    [Tooltip("Your game server that holds the GAME_SIGNER_PRIVATE_KEY and returns signatures.")]
    public string backendSignerUrl = "https://your-game-backend.example.com/sign-reward";

    private static readonly HttpClient http = new HttpClient();

    // On-chain best stars cached from the blockchain (source of truth)
    private int[] chainBestStars = new int[11];   // index 1-10

    private struct PendingSignatureRequest
    {
        public bool   isEndless;
        public int    stageId;
        public int    stars;
        public ulong  nonce;
        public string player;
    }
    private PendingSignatureRequest pendingRequest;

    void Awake() => Instance = this;

    // ── Called by GameManager ─────────────────────────────────────────────────

    public void OnStageComplete(int stageId, int stars)
    {
        if (!Web3Manager.Instance.IsConnected)
        {
            UIRewardDisplay.Instance?.ShowNotConnected();
            return;
        }

        int chainBest = chainBestStars[stageId];
        int delta = stars - chainBest;

        if (delta <= 0)
        {
            UIRewardDisplay.Instance?.ShowNoReward(stageId, stars, chainBest);
            return;
        }

        UIRewardDisplay.Instance?.ShowPendingClaim(stageId, stars, delta * 10, endless: false);
        Web3Manager.Instance.ClaimStageReward(stageId, stars);
    }

    public void OnEndlessRoundComplete(int stars)
    {
        if (!Web3Manager.Instance.IsConnected)
        {
            UIRewardDisplay.Instance?.ShowNotConnected();
            return;
        }

        UIRewardDisplay.Instance?.ShowPendingClaim(0, stars, stars * 10, endless: true);
        Web3Manager.Instance.ClaimEndlessReward(stars);
    }

    // ── Signature request ─────────────────────────────────────────────────────

    public void RequestBackendSignature(bool isEndless, int stageId, int stars, ulong nonce, string player)
    {
        pendingRequest = new PendingSignatureRequest
        {
            isEndless = isEndless,
            stageId   = stageId,
            stars     = stars,
            nonce     = nonce,
            player    = player,
        };
        StartCoroutine(FetchSignatureCoroutine());
    }

    IEnumerator FetchSignatureCoroutine()
    {
        string body = JsonUtility.ToJson(new SignRequest
        {
            player    = pendingRequest.player,
            stageId   = pendingRequest.stageId,
            stars     = pendingRequest.stars,
            nonce     = pendingRequest.nonce.ToString(),
            isEndless = pendingRequest.isEndless,
            chainId   = 97
        });

        // Run async HttpClient call and wait for it inside coroutine
        Task<string> task = PostJsonAsync(backendSignerUrl, body);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError($"[Reward] Backend error: {task.Exception?.GetBaseException().Message}");
            UIRewardDisplay.Instance?.ShowClaimError("Could not reach game server.");
            yield break;
        }

        var resp = JsonUtility.FromJson<SignResponse>(task.Result);
        if (string.IsNullOrEmpty(resp?.sig))
        {
            UIRewardDisplay.Instance?.ShowClaimError("Invalid signature from server.");
            yield break;
        }

        Web3Manager.Instance.SubmitClaimWithSig(
            pendingRequest.isEndless,
            pendingRequest.stageId,
            pendingRequest.stars,
            pendingRequest.nonce,
            resp.sig
        );
    }

    static async Task<string> PostJsonAsync(string url, string json)
    {
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // ── Chain data callbacks ───────────────────────────────────────────────────

    public void OnChainBestStarsReceived(int stageId, int stars)
    {
        if (stageId >= 1 && stageId <= 10)
            chainBestStars[stageId] = stars;
        GameManager.Instance?.SyncBestStarsFromChain(stageId, stars);
    }

    public int GetChainBestStars(int stageId) =>
        stageId >= 1 && stageId <= 10 ? chainBestStars[stageId] : 0;

    // ── Serialisation helpers ─────────────────────────────────────────────────

    [Serializable] private class SignRequest
    {
        public string player;
        public int    stageId;
        public int    stars;
        public string nonce;
        public bool   isEndless;
        public int    chainId;
    }

    [Serializable] private class SignResponse { public string sig; }
}
