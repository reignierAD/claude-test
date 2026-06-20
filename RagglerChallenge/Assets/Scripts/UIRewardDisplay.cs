using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Shows token reward status in the result screen.
/// Wires up wallet connect button and displays RAGG balance.
/// </summary>
public class UIRewardDisplay : MonoBehaviour
{
    public static UIRewardDisplay Instance { get; private set; }

    [Header("Wallet Panel")]
    public Button   connectWalletButton;
    public TextMeshProUGUI walletAddressText;
    public TextMeshProUGUI raggBalanceText;

    [Header("Reward Notification")]
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardTitleText;
    public TextMeshProUGUI rewardBodyText;
    public TextMeshProUGUI txHashText;

    [Header("Claim Button")]
    public Button claimButton;

    void Awake() => Instance = this;

    void Start()
    {
        connectWalletButton?.onClick.AddListener(Web3Manager.Instance.ConnectWallet);

        var w = Web3Manager.Instance;
        w.onWalletConnected.AddListener(addr =>
        {
            if (walletAddressText) walletAddressText.text = $"{addr[..6]}...{addr[^4..]}";
            connectWalletButton?.gameObject.SetActive(false);
        });

        w.onBalanceUpdated.AddListener(bal =>
        {
            if (raggBalanceText) raggBalanceText.text = $"{bal:F2} RAGG";
        });

        w.onTxPending.AddListener(hash =>
        {
            if (txHashText) txHashText.text = $"Tx: {hash[..10]}...";
            if (rewardTitleText) rewardTitleText.text = "Transaction Sent!";
        });

        w.onClaimSuccess.AddListener(_ =>
        {
            if (rewardTitleText) rewardTitleText.text = "Tokens Claimed! ✓";
        });

        w.onClaimFailed.AddListener(reason =>
        {
            if (rewardBodyText) rewardBodyText.text = $"Claim failed:\n{reason}";
        });

        w.onWalletError.AddListener(msg =>
        {
            if (rewardPanel) rewardPanel.SetActive(true);
            if (rewardTitleText) rewardTitleText.text = "Wallet Error";
            if (rewardBodyText)  rewardBodyText.text  = msg;
        });
    }

    // ── Called by RewardManager ───────────────────────────────────────────────

    public void ShowPendingClaim(int stageId, int stars, int tokens, bool endless)
    {
        if (!rewardPanel) return;
        rewardPanel.SetActive(true);

        string context = endless ? "Endless Round" : $"Stage {stageId}";
        if (rewardTitleText) rewardTitleText.text = $"{context} Complete!";
        if (rewardBodyText)  rewardBodyText.text  =
            $"{'★',0}{new string('★', stars)}{new string('☆', 3 - stars)}\n" +
            $"Earning {tokens} RAGG token{(tokens != 1 ? "s" : "")}\n" +
            "Confirm in MetaMask...";
        if (txHashText) txHashText.text = "";
    }

    public void ShowNoReward(int stageId, int stars, int chainBest)
    {
        if (!rewardPanel) return;
        rewardPanel.SetActive(true);

        string bestStr = chainBest == 3 ? "already 3★ (max)" : $"best on-chain: {chainBest}★";
        if (rewardTitleText) rewardTitleText.text = $"Stage {stageId} — No New Tokens";
        if (rewardBodyText)  rewardBodyText.text  =
            $"You scored {stars}★ but {bestStr}.\n" +
            (chainBest < 3 ? $"Get {3 - stars} more ★ to earn remaining tokens." : "");
    }

    public void ShowNotConnected()
    {
        if (!rewardPanel) return;
        rewardPanel.SetActive(true);
        if (rewardTitleText) rewardTitleText.text = "Wallet Not Connected";
        if (rewardBodyText)  rewardBodyText.text  = "Connect MetaMask to earn RAGG tokens.";
    }

    public void ShowClaimError(string msg)
    {
        if (rewardTitleText) rewardTitleText.text = "Claim Error";
        if (rewardBodyText)  rewardBodyText.text  = msg;
    }
}
