using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Bridges game results to on-chain claims.
///
/// Flow:
///  1. GameManager calls RewardManager.OnStageComplete / OnEndlessRoundComplete.
///  2. RewardManager checks on-chain best stars to decide if a claim is valid.
///  3. If tokens are earnable, it requests a backend signature via HTTP.
///  4. On signature received, it tells Web3Manager to submit the tx.
///
/// Backend signature endpoint (your own server):
///   POST /sign-reward
///   Body: { player, stageId, stars, nonce, isEndless, chainId }
///   Response: { sig: "0x..." }
///
/// This keeps the private signer key off the client entirely.
/// </summary>
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [Header("Backend Signer URL")]
    [Tooltip("Your game server that holds the GAME_SIGNER_PRIVATE_KEY and returns signatures.")]
    public string backendSignerUrl = "https://your-game-backend.example.com/sign-reward";

    // On-chain best stars cached from the blockchain (source of truth)
    private int[] chainBestStars = new int[11];   // index 1-10

    // Pending claim waiting for backend signature
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

    /// <summary>
    /// Called when a numbered stage is completed.
    /// Stars must exceed the on-chain best to earn tokens.
    /// </summary>
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

        int tokensToEarn = delta * 10;   // 10 RAGG per new star
        UIRewardDisplay.Instance?.ShowPendingClaim(stageId, stars, tokensToEarn, endless: false);
        Web3Manager.Instance.ClaimStageReward(stageId, stars);
    }

    /// <summary>Called after every Endless round. No cap — always earnable.</summary>
    public void OnEndlessRoundComplete(int stars)
    {
        if (!Web3Manager.Instance.IsConnected)
        {
            UIRewardDisplay.Instance?.ShowNotConnected();
            return;
        }

        int tokensToEarn = stars * 10;
        UIRewardDisplay.Instance?.ShowPendingClaim(0, stars, tokensToEarn, endless: true);
        Web3Manager.Instance.ClaimEndlessReward(stars);
    }

    // ── Signature request (called by Web3Manager after nonce fetched) ─────────

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
        StartCoroutine(FetchSignature());
    }

    IEnumerator FetchSignature()
    {
        var body = JsonUtility.ToJson(new SignRequest
        {
            player    = pendingRequest.player,
            stageId   = pendingRequest.stageId,
            stars     = pendingRequest.stars,
            nonce     = pendingRequest.nonce.ToString(),
            isEndless = pendingRequest.isEndless,
            chainId   = 97   // BSC Testnet
        });

        using var req = new UnityWebRequest(backendSignerUrl, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Reward] Backend error: {req.error}");
            UIRewardDisplay.Instance?.ShowClaimError("Could not reach game server.");
            yield break;
        }

        var resp = JsonUtility.FromJson<SignResponse>(req.downloadHandler.text);
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

    // ── Chain data callbacks (called by Web3Manager) ──────────────────────────

    public void OnChainBestStarsReceived(int stageId, int stars)
    {
        if (stageId >= 1 && stageId <= 10)
            chainBestStars[stageId] = stars;

        // Update GameManager's local save to match chain state (chain is truth)
        GameManager.Instance?.SyncBestStarsFromChain(stageId, stars);
    }

    public int GetChainBestStars(int stageId) =>
        stageId >= 1 && stageId <= 10 ? chainBestStars[stageId] : 0;

    // ── Serialisation helpers ─────────────────────────────────────────────────

    [System.Serializable]
    private class SignRequest
    {
        public string player;
        public int    stageId;
        public int    stars;
        public string nonce;
        public bool   isEndless;
        public int    chainId;
    }

    [System.Serializable]
    private class SignResponse { public string sig; }
}
