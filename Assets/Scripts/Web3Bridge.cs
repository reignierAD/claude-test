using System;
using System.Globalization;
using System.Text;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Bridge between the game and the browser Web3 layer (MetaMask + Moralis +
/// BSC Testnet). In WebGL builds it talks to raggler-web3.js through a jslib
/// plugin; everywhere else (editor, standalone) it runs a local simulation
/// backed by PlayerPrefs so the whole token flow can be tested in Play mode.
///
/// The GameObject MUST be named "Web3Bridge" — the JavaScript side sends
/// messages to it by name.
/// </summary>
public class Web3Bridge : MonoBehaviour
{
    public static Web3Bridge Instance { get; private set; }

    public bool Connected { get; private set; }
    public string Address { get; private set; } = "";
    public string Balance { get; private set; } = "0";
    public bool ClaimPending { get; private set; }

    /// <summary>True when running the local (non-blockchain) simulation.</summary>
    public bool Simulated
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }

    bool[] _claimedLevels = new bool[1];

    public Action onStateChanged;        // wallet/balance/claimed-set updated
    public Action<string> onClaimOk;     // claim confirmed ("level 3|0xTX..." / "endless|0xTX...")
    public Action<string> onClaimError;  // claim failed / rejected
    public Action<string> onError;       // wallet / read errors

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void JS_Web3_Connect();
    [DllImport("__Internal")] static extern void JS_Web3_ClaimLevel(int level, int stars);
    [DllImport("__Internal")] static extern void JS_Web3_ClaimEndless(int stars);
    [DllImport("__Internal")] static extern void JS_Web3_RefreshState(int levelCount);
#endif

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _claimedLevels = new bool[GameConfig.S.LevelCount + 1];
    }

    public bool IsLevelClaimed(int levelOneBased)
    {
        return levelOneBased > 0 && levelOneBased < _claimedLevels.Length && _claimedLevels[levelOneBased];
    }

    public string ShortAddress
    {
        get
        {
            if (string.IsNullOrEmpty(Address)) return "";
            return Address.Length > 12 ? Address.Substring(0, 6) + "..." + Address.Substring(Address.Length - 4) : Address;
        }
    }

    // =====================================================================
    // calls from the game
    // =====================================================================

    public void Connect()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_Web3_Connect();
#else
        Connected = true;
        Address = "0xDEMO00000000000000000000000000000DEMO";
        SimLoad();
        onStateChanged?.Invoke();
#endif
    }

    public void ClaimLevel(int levelOneBased, int stars)
    {
        stars = Mathf.Clamp(stars, 1, 3);
#if UNITY_WEBGL && !UNITY_EDITOR
        ClaimPending = true;
        JS_Web3_ClaimLevel(levelOneBased, stars);
#else
        if (IsLevelClaimed(levelOneBased))
        {
            onClaimError?.Invoke("Already claimed for this level.");
            return;
        }
        SimSetClaimed(levelOneBased);
        SimAddBalance(stars);
        onClaimOk?.Invoke("level " + levelOneBased + "|simulated");
        onStateChanged?.Invoke();
#endif
    }

    public void ClaimEndless(int stars)
    {
        stars = Mathf.Clamp(stars, 1, 3);
#if UNITY_WEBGL && !UNITY_EDITOR
        ClaimPending = true;
        JS_Web3_ClaimEndless(stars);
#else
        SimAddBalance(stars);
        onClaimOk?.Invoke("endless|simulated");
        onStateChanged?.Invoke();
#endif
    }

    public void RequestRefresh()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Connected) JS_Web3_RefreshState(GameConfig.S.LevelCount);
#endif
    }

    // =====================================================================
    // callbacks from raggler-web3.js (via SendMessage) — names must match
    // =====================================================================

    public void OnWalletConnected(string address)
    {
        Connected = true;
        Address = address ?? "";
        onStateChanged?.Invoke();
    }

    public void OnWalletError(string message)
    {
        onError?.Invoke(message);
    }

    /// <summary>Payload: "balanceFormatted|claimedBits" (e.g. "12.5|1100000000").</summary>
    public void OnState(string payload)
    {
        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split('|');
            if (parts.Length > 0) Balance = FormatBalance(parts[0]);
            if (parts.Length > 1)
            {
                string bits = parts[1];
                _claimedLevels = new bool[bits.Length + 1];
                for (int i = 0; i < bits.Length; i++)
                    _claimedLevels[i + 1] = bits[i] == '1';
            }
        }
        onStateChanged?.Invoke();
    }

    public void OnClaimPending(string txHash)
    {
        ClaimPending = true;
    }

    public void OnClaimOk(string payload)
    {
        ClaimPending = false;
        onClaimOk?.Invoke(payload);
    }

    public void OnClaimError(string message)
    {
        ClaimPending = false;
        onClaimError?.Invoke(message);
    }

    static string FormatBalance(string raw)
    {
        double value;
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        return raw;
    }

    // =====================================================================
    // local simulation (editor / standalone)
    // =====================================================================

    void SimLoad()
    {
        Balance = PlayerPrefs.GetInt("sim_rst_balance", 0).ToString();
        string bits = PlayerPrefs.GetString("sim_claimed_bits", "");
        _claimedLevels = new bool[GameConfig.S.LevelCount + 1];
        for (int i = 0; i < bits.Length && i + 1 < _claimedLevels.Length; i++)
            _claimedLevels[i + 1] = bits[i] == '1';
    }

    void SimSetClaimed(int levelOneBased)
    {
        if (levelOneBased >= _claimedLevels.Length) return;
        _claimedLevels[levelOneBased] = true;
        var sb = new StringBuilder();
        for (int i = 1; i < _claimedLevels.Length; i++)
            sb.Append(_claimedLevels[i] ? '1' : '0');
        PlayerPrefs.SetString("sim_claimed_bits", sb.ToString());
        PlayerPrefs.Save();
    }

    void SimAddBalance(int stars)
    {
        int balance = PlayerPrefs.GetInt("sim_rst_balance", 0) + stars;
        PlayerPrefs.SetInt("sim_rst_balance", balance);
        PlayerPrefs.Save();
        Balance = balance.ToString();
    }
}
