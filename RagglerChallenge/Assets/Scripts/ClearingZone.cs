using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the 7-slot clearing zone at the bottom of the screen.
/// Handles card placement, match-3 detection, and overflow (game over).
/// </summary>
public class ClearingZone : MonoBehaviour
{
    public static ClearingZone Instance { get; private set; }

    public int maxSlots = 7;

    // Slot anchor transforms set up in the scene
    public Transform[] slotTransforms;

    private List<Card> slots = new List<Card>();   // ordered list of cards in zone

    // Tracks the last cleared group for undo support
    private List<Card> lastClearedGroup = new List<Card>();
    private int lastClearedInsertIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    public bool IsFull() => slots.Count >= maxSlots;
    public int Count => slots.Count;

    /// <summary>Adds a card to the clearing zone. Returns false if overflow (game over).</summary>
    public bool AddCard(Card card)
    {
        // Insert next to same-type cards for visual grouping
        int insertIndex = FindInsertIndex(card.cardTypeId);
        slots.Insert(insertIndex, card);

        card.isInClearingZone = true;
        card.transform.SetParent(transform, false);

        RefreshSlotPositions();

        // Check for match-3
        if (CheckAndClearMatch(card.cardTypeId))
        {
            if (slots.Count == 0 && BoardGenerator.Instance.AllCardsCleared())
                GameManager.Instance.OnStageClear();
            return true;
        }

        // Overflow check AFTER placing (7 cards and no match = game over)
        if (slots.Count >= maxSlots)
        {
            GameManager.Instance.OnGameOver();
            return false;
        }

        return true;
    }

    int FindInsertIndex(int typeId)
    {
        // Place adjacent to existing cards of the same type (rightmost of that group)
        for (int i = slots.Count - 1; i >= 0; i--)
            if (slots[i].cardTypeId == typeId)
                return i + 1;
        return slots.Count;
    }

    bool CheckAndClearMatch(int typeId)
    {
        var matches = slots.Where(c => c.cardTypeId == typeId).ToList();
        if (matches.Count >= 3)
        {
            lastClearedGroup = matches.Take(3).ToList();
            lastClearedInsertIndex = slots.IndexOf(lastClearedGroup[0]);

            foreach (var c in lastClearedGroup)
            {
                slots.Remove(c);
                c.isRemoved = true;
                Destroy(c.gameObject);
            }
            RefreshSlotPositions();

            // Notify board cards that may now be unblocked
            BoardGenerator.Instance.RefreshAllCardAccessibility();

            // Check win: all board cards gone AND clearing zone empty
            if (slots.Count == 0 && BoardGenerator.Instance.AllCardsCleared())
                GameManager.Instance.OnStageClear();

            return true;
        }
        return false;
    }

    void RefreshSlotPositions()
    {
        float cardWidth = 1.1f;
        float startX = -(slots.Count - 1) * cardWidth * 0.5f;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slotTransforms != null && i < slotTransforms.Length)
                slots[i].transform.position = slotTransforms[i].position;
            else
                slots[i].transform.localPosition = new Vector3(startX + i * cardWidth, 0f, 0f);

            slots[i].transform.localScale = Vector3.one;
        }
    }

    // ── Power-ups ────────────────────────────────────────────────────────────

    /// <summary>Remove: pulls the last card out of the clearing zone back to a holding area.</summary>
    public Card RemoveLastCard()
    {
        if (slots.Count == 0) return null;
        Card c = slots[slots.Count - 1];
        slots.RemoveAt(slots.Count - 1);
        c.isInClearingZone = false;
        RefreshSlotPositions();
        return c;
    }

    /// <summary>Undo: restores the last cleared match back into the zone.</summary>
    public bool UndoLastClear(List<Card> restoredCards)
    {
        if (lastClearedGroup.Count == 0 || lastClearedInsertIndex < 0) return false;
        // Cards were destroyed — undo is only viable if GameManager rebuilt them
        for (int i = 0; i < restoredCards.Count; i++)
        {
            int idx = Mathf.Min(lastClearedInsertIndex + i, slots.Count);
            slots.Insert(idx, restoredCards[i]);
            restoredCards[i].isInClearingZone = true;
            restoredCards[i].isRemoved = false;
            restoredCards[i].transform.SetParent(transform, false);
        }
        RefreshSlotPositions();
        lastClearedGroup.Clear();
        lastClearedInsertIndex = -1;
        return true;
    }

    public List<Card> GetSlots() => new List<Card>(slots);

    public void Clear()
    {
        foreach (var c in slots)
            if (c != null) Destroy(c.gameObject);
        slots.Clear();
        lastClearedGroup.Clear();
    }
}
