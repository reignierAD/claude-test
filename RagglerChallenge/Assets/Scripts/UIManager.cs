using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Controls all HUD elements: timer, star display, power-up counts,
/// stage-select panel, and result overlays.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("In-Game HUD")]
    public TextMeshProUGUI timerText;
    public Image[] starImages;          // 3 star images; filled/unfilled based on time
    public Sprite starFilled;
    public Sprite starEmpty;

    [Header("Power-Up HUD")]
    public TextMeshProUGUI removeCountText;
    public TextMeshProUGUI undoCountText;
    public TextMeshProUGUI refreshCountText;
    public TextMeshProUGUI removeUsedText;
    public TextMeshProUGUI undoUsedText;
    public TextMeshProUGUI refreshUsedText;

    [Header("Clearing Zone")]
    public TextMeshProUGUI slotCountText;   // e.g. "4/7"

    [Header("Panels")]
    public GameObject stageSelectPanel;
    public GameObject resultPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultStarText;  // "⭐⭐⭐" etc.
    public TextMeshProUGUI stageLabel;      // "Stage 1"

    [Header("Stage Select Buttons")]
    public Button[] stageButtons;           // One per stage
    public Image[] stageButtonStars;        // Star icon on each button

    void Awake() => Instance = this;

    // ── Timer display ─────────────────────────────────────────────────────────

    public void UpdateTimer(float elapsed)
    {
        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);
        if (timerText) timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void ShowStars(int count)
    {
        for (int i = 0; i < starImages.Length; i++)
            starImages[i].sprite = i < count ? starFilled : starEmpty;
    }

    // ── Power-up display ─────────────────────────────────────────────────────

    public void UpdatePowerUpDisplay()
    {
        var p = PowerUpManager.Instance;
        if (removeCountText)  removeCountText.text  = p.removeCharges.ToString();
        if (undoCountText)    undoCountText.text    = p.undoCharges.ToString();
        if (refreshCountText) refreshCountText.text = p.refreshCharges.ToString();

        if (removeUsedText)  removeUsedText.text  = $"{p.removeUsed}/{p.maxUses}";
        if (undoUsedText)    undoUsedText.text    = $"{p.undoUsed}/{p.maxUses}";
        if (refreshUsedText) refreshUsedText.text = $"{p.refreshUsed}/{p.maxUses}";
    }

    // ── Slot count display ────────────────────────────────────────────────────

    public void UpdateSlotCount(int used, int max)
    {
        if (slotCountText) slotCountText.text = $"{used}/{max}";
    }

    // ── Stage label ───────────────────────────────────────────────────────────

    public void SetStageLabel(int stageIndex, bool endless = false)
    {
        if (stageLabel)
            stageLabel.text = endless ? "Endless" : $"Stage {stageIndex + 1}";
    }

    // ── Panels ────────────────────────────────────────────────────────────────

    public void ShowStageSelect(bool show)
    {
        if (stageSelectPanel) stageSelectPanel.SetActive(show);
    }

    public void ShowResult(int stars)
    {
        if (resultPanel) resultPanel.SetActive(true);
        if (resultStarText) resultStarText.text = new string('★', stars) + new string('☆', 3 - stars);
    }

    public void ShowGameOver(bool show)
    {
        if (gameOverPanel) gameOverPanel.SetActive(show);
    }

    public void HideAllOverlays()
    {
        if (resultPanel)   resultPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    // ── Stage select buttons ──────────────────────────────────────────────────

    public void RefreshStageButtons(int[] stageStars)
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            bool unlocked = i == 0 || (i < stageStars.Length && stageStars[i - 1] > 0);
            stageButtons[i].interactable = unlocked;
        }
    }
}
