using UnityEngine;
using System.Collections;

/// <summary>
/// Central game controller. Manages state machine, timing, star rating,
/// and stage progression for all 10 normal stages + Endless mode.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject cardPrefab;   // Must have Card component + SpriteRenderer + Collider2D

    [Header("Stage Config")]
    public int totalStages = 10;

    // Star time thresholds per stage (seconds). Beat time[0] = 3★, [1] = 2★, else 1★.
    // Scales automatically if not set.
    public float[] threeStarTimes;
    public float[] twoStarTimes;

    // Persistent progress (cleared each play session for now; use PlayerPrefs for persistence)
    private int[] stageBestStars;

    // Runtime state
    private int currentStage = 0;
    private bool endless = false;
    private float elapsed = 0f;
    private bool running = false;
    private bool gameOver = false;

    void Awake()
    {
        Instance = this;
        stageBestStars = new int[totalStages];
    }

    void Start()
    {
        LoadProgress();
        UIManager.Instance.ShowStageSelect(true);
        UIManager.Instance.RefreshStageButtons(stageBestStars);
    }

    void Update()
    {
        if (!running || gameOver) return;
        elapsed += Time.deltaTime;
        UIManager.Instance.UpdateTimer(elapsed);
        UIManager.Instance.UpdateSlotCount(ClearingZone.Instance.Count, ClearingZone.Instance.maxSlots);
    }

    // ── Stage entry points ────────────────────────────────────────────────────

    public void StartStage(int stageIndex)
    {
        currentStage = stageIndex;
        endless = false;
        BeginPlay();
    }

    public void StartEndless()
    {
        currentStage = 0;
        endless = true;
        BeginPlay();
    }

    void BeginPlay()
    {
        elapsed = 0f;
        running = true;
        gameOver = false;

        UIManager.Instance.HideAllOverlays();
        UIManager.Instance.ShowStageSelect(false);
        UIManager.Instance.SetStageLabel(currentStage, endless);

        PowerUpManager.Instance.ResetForNewStage();
        ClearingZone.Instance.Clear();
        BoardGenerator.Instance.BuildStage(currentStage);

        UIManager.Instance.ShowStars(3);    // start optimistic
        UIManager.Instance.UpdatePowerUpDisplay();
    }

    // ── Card interaction ──────────────────────────────────────────────────────

    public void OnCardClicked(Card card)
    {
        if (!running || gameOver) return;
        BoardGenerator.Instance.MarkCardRemoved(card);
        ClearingZone.Instance.AddCard(card);
    }

    // ── Outcomes ──────────────────────────────────────────────────────────────

    public void OnStageClear()
    {
        running = false;
        int stars = CalcStars();

        if (!endless && currentStage < totalStages)
        {
            if (stars > stageBestStars[currentStage])
                stageBestStars[currentStage] = stars;
            SaveProgress();
        }

        UIManager.Instance.ShowStars(stars);
        UIManager.Instance.ShowResult(stars);
    }

    public void OnGameOver()
    {
        running = false;
        gameOver = true;
        UIManager.Instance.ShowGameOver(true);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    public void RetryStage()   => BeginPlay();
    public void NextStage()
    {
        if (endless) { currentStage++; BeginPlay(); return; }
        if (currentStage + 1 < totalStages) { currentStage++; BeginPlay(); }
        else GoToStageSelect();
    }
    public void GoToStageSelect()
    {
        ClearingZone.Instance.Clear();
        UIManager.Instance.ShowStageSelect(true);
        UIManager.Instance.HideAllOverlays();
        UIManager.Instance.RefreshStageButtons(stageBestStars);
    }

    // ── Star calculation ──────────────────────────────────────────────────────

    int CalcStars()
    {
        float t3 = threeStarTimes != null && currentStage < threeStarTimes.Length
            ? threeStarTimes[currentStage] : 30f + currentStage * 5f;
        float t2 = twoStarTimes != null && currentStage < twoStarTimes.Length
            ? twoStarTimes[currentStage] : 60f + currentStage * 10f;

        if (elapsed <= t3) return 3;
        if (elapsed <= t2) return 2;
        return 1;
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    void SaveProgress()
    {
        for (int i = 0; i < stageBestStars.Length; i++)
            PlayerPrefs.SetInt($"stage_{i}_stars", stageBestStars[i]);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        for (int i = 0; i < stageBestStars.Length; i++)
            stageBestStars[i] = PlayerPrefs.GetInt($"stage_{i}_stars", 0);
    }
}
