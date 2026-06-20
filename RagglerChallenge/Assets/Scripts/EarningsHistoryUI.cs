using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Renders the player's RAGG transfer history fetched from Moralis Wallet API.
/// Attach to the EarningsHistoryPanel in the UI canvas.
/// </summary>
public class EarningsHistoryUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    public Button     openButton;
    public Button     closeButton;
    public Button     refreshButton;

    [Header("Balance Header")]
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI usdValueText;

    [Header("History List")]
    public Transform       listContainer;
    public GameObject      entryPrefab;       // prefab with HistoryEntryRow component
    public TextMeshProUGUI emptyStateText;

    [Header("Real-time Confirmation Banner")]
    public GameObject confirmBanner;
    public TextMeshProUGUI confirmBannerText;

    private readonly List<GameObject> spawnedRows = new();

    void Start()
    {
        openButton?.onClick.AddListener(() => panel.SetActive(true));
        closeButton?.onClick.AddListener(() => panel.SetActive(false));
        refreshButton?.onClick.AddListener(() => MoralisManager.Instance.FetchHistory(20));

        var m = MoralisManager.Instance;
        m.onBalanceReceived.AddListener(OnBalance);
        m.onHistoryReceived.AddListener(OnHistory);
        m.onRewardConfirmed.AddListener(OnRewardConfirmed);
        m.onAuthVerified.AddListener(_ => m.FetchHistory(20));
    }

    // ── Balance display ───────────────────────────────────────────────────────

    void OnBalance(MoralisManager.BalanceData data)
    {
        if (balanceText)
        {
            float bal = float.TryParse(data.balanceFormatted, out float f) ? f : 0f;
            balanceText.text = $"{bal:F2} RAGG";
        }

        if (usdValueText)
        {
            usdValueText.text = string.IsNullOrEmpty(data.usdValue)
                ? "" : $"≈ ${float.Parse(data.usdValue):F4} USD";
        }

        // Also push to UIRewardDisplay for in-game HUD
        UIRewardDisplay.Instance?.UpdateBalanceDisplay(data.balanceFormatted, data.usdValue);
    }

    // ── History list ──────────────────────────────────────────────────────────

    void OnHistory(MoralisManager.TransferEntry[] transfers)
    {
        foreach (var go in spawnedRows) Destroy(go);
        spawnedRows.Clear();

        if (transfers == null || transfers.Length == 0)
        {
            if (emptyStateText) emptyStateText.gameObject.SetActive(true);
            return;
        }
        if (emptyStateText) emptyStateText.gameObject.SetActive(false);

        foreach (var tx in transfers)
        {
            var row = Instantiate(entryPrefab, listContainer);
            var entry = row.GetComponent<HistoryEntryRow>();
            entry?.Populate(tx);
            spawnedRows.Add(row);
        }
    }

    // ── Real-time reward banner ───────────────────────────────────────────────

    void OnRewardConfirmed(MoralisManager.RewardPushData data)
    {
        if (!confirmBanner) return;

        // Parse tokensEarned from wei string
        decimal wei = decimal.TryParse(data.tokensEarned, out decimal w) ? w : 0m;
        decimal ragg = wei / 1_000_000_000_000_000_000m;

        string context = data.stageId > 0 ? $"Stage {data.stageId}" : "Endless";
        confirmBannerText.text = $"✓ {ragg:F0} RAGG earned from {context}!";

        confirmBanner.SetActive(true);
        CancelInvoke(nameof(HideBanner));
        Invoke(nameof(HideBanner), 4f);

        // Refresh history list to show the new entry
        MoralisManager.Instance.FetchHistory(20);
    }

    void HideBanner() { if (confirmBanner) confirmBanner.SetActive(false); }
}

/// <summary>Single row in the history list — set up in the prefab.</summary>
public class HistoryEntryRow : MonoBehaviour
{
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI txHashText;

    public void Populate(MoralisManager.TransferEntry tx)
    {
        if (amountText)
        {
            float val = float.TryParse(tx.valueFormatted, out float f) ? f : 0f;
            amountText.text = $"+{val:F0} RAGG";
        }

        if (dateText && !string.IsNullOrEmpty(tx.blockTimestamp))
        {
            if (DateTime.TryParse(tx.blockTimestamp, out DateTime dt))
                dateText.text = dt.ToLocalTime().ToString("MMM dd  HH:mm");
        }

        if (txHashText && !string.IsNullOrEmpty(tx.txHash))
            txHashText.text = tx.txHash[..10] + "...";
    }
}
