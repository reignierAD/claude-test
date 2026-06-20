using UnityEngine;
using UnityEngine.Events;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Unity-side controller for all blockchain interactions.
/// Calls JavaScript functions in Web3Bridge.jslib and receives
/// results via SendMessage callbacks (Unity's WebGL inter-op mechanism).
///
/// Attach to a GameObject named exactly "Web3Manager" in the scene.
/// </summary>
public class Web3Manager : MonoBehaviour
{
    public static Web3Manager Instance { get; private set; }

    // ── Contract addresses loaded from ContractConfig.json ───────────────────
    private string tokenAddress;
    private string rewardsAddress;

    // ── Runtime state ─────────────────────────────────────────────────────────
    public string PlayerAddress { get; private set; } = "";
    public bool   IsConnected   => !string.IsNullOrEmpty(PlayerAddress);

    // Pending nonce retrieved before building a claim
    private ulong pendingNonce;
    private PendingClaim pendingClaim;

    private struct PendingClaim
    {
        public bool    isEndless;
        public int     stageId;
        public int     stars;
    }

    // ── Events (subscribe in UI) ──────────────────────────────────────────────
    public UnityEvent<string>  onWalletConnected   = new();
    public UnityEvent<string>  onWalletError       = new();
    public UnityEvent<decimal> onBalanceUpdated    = new();
    public UnityEvent<string>  onTxPending         = new();
    public UnityEvent<string>  onClaimSuccess      = new();
    public UnityEvent<string>  onClaimFailed       = new();
    public UnityEvent<string>  onGenericError      = new();

    // ── JS imports ────────────────────────────────────────────────────────────
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void JS_ConnectWallet();
    [DllImport("__Internal")] static extern void JS_GetTokenBalance(string tokenAddr, string playerAddr);
    [DllImport("__Internal")] static extern void JS_GetPlayerNonce(string rewardsAddr, string playerAddr);
    [DllImport("__Internal")] static extern void JS_ClaimStageReward(string rewardsAddr, string payloadJson);
    [DllImport("__Internal")] static extern void JS_ClaimEndlessReward(string rewardsAddr, string payloadJson);
    [DllImport("__Internal")] static extern void JS_GetBestStars(string rewardsAddr, string playerAddr, int stageId);
    [DllImport("__Internal")] static extern void JS_ListenAccountChanges();
#else
    // Stubs for Editor — logs calls without crashing
    static void JS_ConnectWallet()                                              => Debug.Log("[Web3] ConnectWallet (editor stub)");
    static void JS_GetTokenBalance(string t, string p)                          => Debug.Log($"[Web3] GetBalance {p} (editor stub)");
    static void JS_GetPlayerNonce(string r, string p)                           => Debug.Log($"[Web3] GetNonce {p} (editor stub)");
    static void JS_ClaimStageReward(string r, string json)                      => Debug.Log($"[Web3] ClaimStage {json} (editor stub)");
    static void JS_ClaimEndlessReward(string r, string json)                    => Debug.Log($"[Web3] ClaimEndless {json} (editor stub)");
    static void JS_GetBestStars(string r, string p, int id)                     => Debug.Log($"[Web3] GetBestStars stage={id} (editor stub)");
    static void JS_ListenAccountChanges()                                       => Debug.Log("[Web3] ListenAccountChanges (editor stub)");
#endif

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadContractConfig();
    }

    void Start() => JS_ListenAccountChanges();

    void LoadContractConfig()
    {
        var asset = Resources.Load<TextAsset>("ContractConfig");
        if (asset == null) { Debug.LogError("[Web3] ContractConfig.json not found in Resources/"); return; }

        var cfg = JsonUtility.FromJson<ContractConfig>(asset.text);
        tokenAddress   = cfg.tokenAddress;
        rewardsAddress = cfg.rewardsAddress;
        Debug.Log($"[Web3] Token={tokenAddress}  Rewards={rewardsAddress}");
    }

    [Serializable]
    private class ContractConfig
    {
        public string tokenAddress;
        public string rewardsAddress;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ConnectWallet()             => JS_ConnectWallet();
    public void RefreshBalance()            => JS_GetTokenBalance(tokenAddress, PlayerAddress);
    public void FetchBestStars(int stageId) => JS_GetBestStars(rewardsAddress, PlayerAddress, stageId);

    /// <summary>
    /// Begin the claim flow for a numbered stage.
    /// Step 1: fetch nonce → Step 2 (callback): JS calls backend → Step 3: submit tx.
    /// </summary>
    public void ClaimStageReward(int stageId, int stars)
    {
        if (!IsConnected) { Debug.LogWarning("[Web3] Wallet not connected"); return; }
        pendingClaim = new PendingClaim { isEndless = false, stageId = stageId, stars = stars };
        JS_GetPlayerNonce(rewardsAddress, PlayerAddress);   // triggers OnNonceReceived
    }

    public void ClaimEndlessReward(int stars)
    {
        if (!IsConnected) { Debug.LogWarning("[Web3] Wallet not connected"); return; }
        pendingClaim = new PendingClaim { isEndless = true, stars = stars };
        JS_GetPlayerNonce(rewardsAddress, PlayerAddress);
    }

    // ── JS → Unity callbacks (must be public, called via SendMessage) ─────────

    public void OnWalletConnected(string address)
    {
        PlayerAddress = address;
        Debug.Log($"[Web3] Connected: {address}");
        onWalletConnected.Invoke(address);
        RefreshBalance();
        // Pre-load best stars for all 10 stages
        for (int i = 1; i <= 10; i++) FetchBestStars(i);
    }

    public void OnWalletError(string msg)
    {
        Debug.LogWarning($"[Web3] Wallet error: {msg}");
        onWalletError.Invoke(msg);
    }

    public void OnBalanceReceived(string rawWei)
    {
        // rawWei is 10^18 units — convert to human-readable
        if (decimal.TryParse(rawWei, out decimal wei))
            onBalanceUpdated.Invoke(wei / 1_000_000_000_000_000_000m);
    }

    public void OnNonceReceived(string nonceStr)
    {
        if (!ulong.TryParse(nonceStr, out pendingNonce)) return;
        // Now request a signature from the game backend, then proceed
        RewardManager.Instance.RequestBackendSignature(pendingClaim.isEndless,
            pendingClaim.stageId, pendingClaim.stars, pendingNonce, PlayerAddress);
    }

    /// <summary>Called by RewardManager after backend returns a signature.</summary>
    public void SubmitClaimWithSig(bool isEndless, int stageId, int stars, ulong nonce, string sig)
    {
        if (isEndless)
        {
            string json = $"{{\"stars\":{stars},\"nonce\":{nonce},\"sig\":\"{sig}\"}}";
            JS_ClaimEndlessReward(rewardsAddress, json);
        }
        else
        {
            string json = $"{{\"stageId\":{stageId},\"stars\":{stars},\"nonce\":{nonce},\"sig\":\"{sig}\"}}";
            JS_ClaimStageReward(rewardsAddress, json);
        }
    }

    public void OnTxPending(string txHash)
    {
        Debug.Log($"[Web3] Tx pending: {txHash}");
        onTxPending.Invoke(txHash);
    }

    public void OnClaimSuccess(string resultJson)
    {
        Debug.Log($"[Web3] Claim success: {resultJson}");
        onClaimSuccess.Invoke(resultJson);
        RefreshBalance();
    }

    public void OnClaimFailed(string reason)
    {
        Debug.LogWarning($"[Web3] Claim failed: {reason}");
        onClaimFailed.Invoke(reason);
    }

    public void OnBestStarsReceived(string json)
    {
        var data = JsonUtility.FromJson<BestStarsPayload>(json);
        RewardManager.Instance.OnChainBestStarsReceived(data.stageId, data.stars);
    }

    public void OnAccountChanged(string newAddress)
    {
        PlayerAddress = newAddress;
        if (IsConnected) RefreshBalance();
    }

    public void OnChainChanged(string chainId)
    {
        Debug.Log($"[Web3] Chain changed to {chainId}");
        // Re-connect to validate it's still BSC testnet (97 = 0x61)
        if (chainId != "0x61" && chainId != "97")
            onWalletError.Invoke("Please switch to BSC Testnet (chain 97).");
    }

    public void OnWeb3Error(string msg)
    {
        Debug.LogWarning($"[Web3] Error: {msg}");
        onGenericError.Invoke(msg);
    }

    [Serializable] private class BestStarsPayload { public int stageId; public int stars; }
}
