using UnityEngine;
using UnityEngine.Events;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Unity-side Moralis integration manager.
/// Handles Auth, Token balance, Transfer history, and WebSocket real-time events.
/// Works alongside Web3Manager (which still handles the on-chain tx submission).
///
/// Attach to a GameObject named exactly "MoralisManager".
/// </summary>
public class MoralisManager : MonoBehaviour
{
    public static MoralisManager Instance { get; private set; }

    [Header("Server URLs (from ContractConfig or Inspector)")]
    public string serverUrl = "https://your-game-backend.example.com";
    public string wsUrl     = "wss://your-game-backend.example.com/ws";

    // ── Runtime state ─────────────────────────────────────────────────────────
    public string PlayerAddress { get; private set; } = "";
    public string AuthToken     { get; private set; } = "";
    public bool   IsAuthed      => !string.IsNullOrEmpty(AuthToken);

    // ── Events ────────────────────────────────────────────────────────────────
    public UnityEvent<string>        onAuthVerified    = new(); // address
    public UnityEvent<string>        onAuthError       = new();
    public UnityEvent<BalanceData>   onBalanceReceived = new();
    public UnityEvent<TransferEntry[]> onHistoryReceived = new();
    public UnityEvent<RewardPushData>  onRewardConfirmed = new(); // from WS/Streams
    public UnityEvent<string>        onError           = new();

    // ── JS imports ────────────────────────────────────────────────────────────
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void JS_MoralisAuthRequest(string serverUrl, string address, string chain);
    [DllImport("__Internal")] static extern void JS_MoralisAuthSign(string serverUrl, string message);
    [DllImport("__Internal")] static extern void JS_MoralisGetBalance(string serverUrl, string address);
    [DllImport("__Internal")] static extern void JS_MoralisGetHistory(string serverUrl, string address, int limit);
    [DllImport("__Internal")] static extern void JS_WsConnect(string wsUrl, string address);
    [DllImport("__Internal")] static extern void JS_WsDisconnect();
    [DllImport("__Internal")] static extern void JS_WsSend(string msg);
#else
    static void JS_MoralisAuthRequest(string s, string a, string c)  => Debug.Log($"[Moralis] AuthRequest {a} (stub)");
    static void JS_MoralisAuthSign(string s, string m)               => Debug.Log($"[Moralis] AuthSign (stub)");
    static void JS_MoralisGetBalance(string s, string a)             => Debug.Log($"[Moralis] GetBalance {a} (stub)");
    static void JS_MoralisGetHistory(string s, string a, int l)      => Debug.Log($"[Moralis] GetHistory {a} limit={l} (stub)");
    static void JS_WsConnect(string u, string a)                      => Debug.Log($"[Moralis] WsConnect {a} (stub)");
    static void JS_WsDisconnect()                                     => Debug.Log("[Moralis] WsDisconnect (stub)");
    static void JS_WsSend(string m)                                   => Debug.Log($"[Moralis] WsSend {m} (stub)");
#endif

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var cfg = Resources.Load<TextAsset>("ContractConfig");
        if (cfg != null)
        {
            var data = JsonUtility.FromJson<ContractConfig>(cfg.text);
            if (!string.IsNullOrEmpty(data.backendUrl)) serverUrl = data.backendUrl;
            if (!string.IsNullOrEmpty(data.wsUrl))      wsUrl     = data.wsUrl;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Start Moralis Auth flow for the given wallet address.</summary>
    public void Login(string address)
    {
        PlayerAddress = address;
        JS_MoralisAuthRequest(serverUrl, address, "0x61");
    }

    public void RefreshBalance()
    {
        if (!IsAuthed) return;
        JS_MoralisGetBalance(serverUrl, PlayerAddress);
    }

    public void FetchHistory(int limit = 20)
    {
        if (!IsAuthed) return;
        JS_MoralisGetHistory(serverUrl, PlayerAddress, limit);
    }

    // ── JS → Unity callbacks ──────────────────────────────────────────────────

    // Step 1 result: sign the challenge with MetaMask
    public void OnAuthChallengeReceived(string message)
    {
        JS_MoralisAuthSign(serverUrl, message);
    }

    // Step 3 result: JWT + verified address
    public void OnAuthVerified(string json)
    {
        var data = JsonUtility.FromJson<AuthResult>(json);
        AuthToken     = data.token;
        PlayerAddress = data.address;
        Debug.Log($"[Moralis] Authed as {PlayerAddress}");

        // Connect WebSocket for real-time reward push
        JS_WsConnect(wsUrl, PlayerAddress);

        onAuthVerified.Invoke(PlayerAddress);
        RefreshBalance();
    }

    public void OnAuthError(string msg)
    {
        Debug.LogWarning($"[Moralis] Auth error: {msg}");
        onAuthError.Invoke(msg);
    }

    public void OnBalanceReceived(string json)
    {
        var data = JsonUtility.FromJson<BalanceData>(json);
        onBalanceReceived.Invoke(data);
    }

    public void OnHistoryReceived(string json)
    {
        var entries = JsonHelper.FromJsonArray<TransferEntry>(json);
        onHistoryReceived.Invoke(entries);
    }

    // WebSocket callbacks
    public void OnWsConnected(string _)    => Debug.Log("[WS] Connected to game server.");
    public void OnWsDisconnected(string _) => Debug.Log("[WS] Disconnected from game server.");
    public void OnWsError(string msg)      => Debug.LogWarning($"[WS] Error: {msg}");

    public void OnWsMessage(string json)
    {
        try
        {
            var msg = JsonUtility.FromJson<WsMessage>(json);
            if (msg.type == "reward")
            {
                var reward = JsonUtility.FromJson<RewardPushData>(json);
                Debug.Log($"[WS] Reward confirmed: {reward.tokensEarned} RAGG (stage {reward.stageId})");
                onRewardConfirmed.Invoke(reward);
                RefreshBalance();   // refresh balance now tx is confirmed
            }
        }
        catch (Exception e) { Debug.LogWarning($"[WS] Bad message: {e.Message}"); }
    }

    public void OnMoralisError(string msg)
    {
        Debug.LogWarning($"[Moralis] Error: {msg}");
        onError.Invoke(msg);
    }

    void OnDestroy() => JS_WsDisconnect();

    // ── Data types ────────────────────────────────────────────────────────────

    [Serializable] public class BalanceData
    {
        public string balance;
        public string balanceFormatted;
        public string usdValue;
        public string symbol;
        public int    decimals;
    }

    [Serializable] public class TransferEntry
    {
        public string txHash;
        public string from;
        public string to;
        public string value;
        public string valueFormatted;
        public string blockTimestamp;
    }

    [Serializable] public class RewardPushData
    {
        public string type;
        public string @event;
        public int    stageId;
        public int    stars;
        public string tokensEarned;
        public string txHash;
    }

    [Serializable] private class AuthResult    { public string token; public string address; }
    [Serializable] private class WsMessage     { public string type; }
    [Serializable] private class ContractConfig
    {
        public string backendUrl;
        public string wsUrl;
        public string tokenAddress;
        public string rewardsAddress;
    }
}

/// <summary>Unity's JsonUtility can't deserialise root arrays — this wraps them.</summary>
public static class JsonHelper
{
    public static T[] FromJsonArray<T>(string json)
    {
        string wrapped = "{\"items\":" + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(wrapped).items;
    }
    [Serializable] private class Wrapper<T> { public T[] items; }
}
