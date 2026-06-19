using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Handles the three power-ups: Remove, Undo, Refresh.
/// Each can be used up to 10 times per run (matching the game screenshots).
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Use Counts (shown in UI)")]
    public int removeUsed = 0;
    public int undoUsed   = 0;
    public int refreshUsed = 0;
    public int maxUses = 10;

    [Header("Charges (currency from outside game)")]
    public int removeCharges  = 37;
    public int undoCharges    = 0;
    public int refreshCharges = 23;

    [Header("UI Buttons")]
    public Button removeButton;
    public Button undoButton;
    public Button refreshButton;

    // Undo state — stores snapshot of what the ClearingZone held before the last clear
    private struct UndoSnapshot
    {
        public int[] slotTypeIds;
        public int clearedTypeId;
    }
    private Stack<UndoSnapshot> undoStack = new Stack<UndoSnapshot>();

    // Cards parked by Remove (held off-screen)
    private List<Card> removedHoldCards = new List<Card>();
    private Transform holdArea;

    void Awake()
    {
        Instance = this;
        holdArea = new GameObject("HoldArea").transform;
        holdArea.position = new Vector3(100f, 100f, 0f); // off-screen
    }

    void Start() => RefreshButtonStates();

    // ── Remove ────────────────────────────────────────────────────────────────

    public void UseRemove()
    {
        if (removeCharges <= 0 || removeUsed >= maxUses) return;

        Card removed = ClearingZone.Instance.RemoveLastCard();
        if (removed == null) return;

        removed.transform.SetParent(holdArea, false);
        removed.transform.position = holdArea.position;
        removedHoldCards.Add(removed);

        removeUsed++;
        removeCharges--;
        RefreshButtonStates();
        UIManager.Instance.UpdatePowerUpDisplay();
    }

    // ── Undo ─────────────────────────────────────────────────────────────────

    /// <summary>Called by ClearingZone just before it destroys a matched triple.</summary>
    public void RecordUndoSnapshot(List<Card> aboutToBeCleared, List<Card> currentSlots)
    {
        var snap = new UndoSnapshot
        {
            slotTypeIds = new int[currentSlots.Count],
            clearedTypeId = aboutToBeCleared[0].cardTypeId
        };
        for (int i = 0; i < currentSlots.Count; i++)
            snap.slotTypeIds[i] = currentSlots[i].cardTypeId;
        undoStack.Push(snap);
    }

    public void UseUndo()
    {
        if (undoCharges <= 0 || undoUsed >= maxUses) return;
        if (undoStack.Count == 0) return;

        // Rebuild the three cleared cards and re-insert them
        var snap = undoStack.Pop();
        var boardGen = BoardGenerator.Instance;
        var restoredCards = new List<Card>();

        for (int i = 0; i < 3; i++)
        {
            var go = Instantiate(GameManager.Instance.cardPrefab);
            var card = go.GetComponent<Card>();
            card.cardTypeId = snap.clearedTypeId;
            card.faceSprite = boardGen.cardSprites[snap.clearedTypeId % boardGen.cardSprites.Length];
            restoredCards.Add(card);
        }

        ClearingZone.Instance.UndoLastClear(restoredCards);

        undoUsed++;
        undoCharges--;
        RefreshButtonStates();
        UIManager.Instance.UpdatePowerUpDisplay();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    /// <summary>Shuffles the positions of all remaining board cards (not in zone).</summary>
    public void UseRefresh()
    {
        if (refreshCharges <= 0 || refreshUsed >= maxUses) return;

        BoardGenerator.Instance.ShuffleBoardPositions();

        refreshUsed++;
        refreshCharges--;
        RefreshButtonStates();
        UIManager.Instance.UpdatePowerUpDisplay();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void RefreshButtonStates()
    {
        if (removeButton)  removeButton.interactable  = removeCharges > 0  && removeUsed  < maxUses;
        if (undoButton)    undoButton.interactable    = undoCharges > 0    && undoUsed    < maxUses && undoStack.Count > 0;
        if (refreshButton) refreshButton.interactable = refreshCharges > 0 && refreshUsed < maxUses;
    }

    public void ResetForNewStage()
    {
        removeUsed = undoUsed = refreshUsed = 0;
        undoStack.Clear();
        foreach (var c in removedHoldCards)
            if (c != null) Destroy(c.gameObject);
        removedHoldCards.Clear();
        RefreshButtonStates();
    }
}
