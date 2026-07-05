using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The clearing zone at the bottom of the screen: up to 7 cards, kept grouped
/// by kind. Three identical cards clear automatically.
/// </summary>
public class Tray
{
    public readonly List<Card> cards = new List<Card>();

    /// <summary>Fired once per cleared triple, with the kind that cleared.</summary>
    public Action<int> onTriple;

    const float SlotSpacing = 114f;

    public bool IsFull => cards.Count >= GameConfig.TraySize;

    /// <summary>
    /// Adds a card (already re-parented under the tray root by the caller),
    /// resolves any triple, and returns false when the tray overflowed —
    /// i.e. still holds more than 7 cards after matches resolved.
    /// </summary>
    public bool Add(Card card)
    {
        int insertAt = cards.Count;
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (cards[i].typeIndex == card.typeIndex)
            {
                insertAt = i + 1;
                break;
            }
        }
        cards.Insert(insertAt, card);

        // count how many of this kind we now hold
        var same = new List<Card>();
        foreach (var c in cards)
            if (c.typeIndex == card.typeIndex)
                same.Add(c);

        Relayout();

        if (same.Count >= 3)
        {
            foreach (var c in same)
            {
                c.removed = true;
                cards.Remove(c);
            }
            // let the last card visually land, then pop the triple
            Tween.Delay(0.2f, () =>
            {
                foreach (var c in same)
                    if (c != null) Tween.PopAndDestroy(c.gameObject, 0.22f);
            });
            Tween.Delay(0.45f, Relayout);
            onTriple?.Invoke(card.typeIndex);
        }

        return cards.Count <= GameConfig.TraySize;
    }

    public void Remove(Card card)
    {
        cards.Remove(card);
        Relayout();
    }

    /// <summary>Pops up to n cards from the front of the tray (Remove item).</summary>
    public List<Card> TakeFromFront(int n)
    {
        var taken = new List<Card>();
        while (taken.Count < n && cards.Count > 0)
        {
            taken.Add(cards[0]);
            cards.RemoveAt(0);
        }
        Relayout();
        return taken;
    }

    public Vector2 SlotPosition(int index)
    {
        return new Vector2((index - (GameConfig.TraySize - 1) * 0.5f) * SlotSpacing, 0f);
    }

    public void Relayout()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            if (c == null || c.removed) continue;
            Tween.MoveTo(c.Rect, SlotPosition(i), 0.18f);
        }
    }

    public void Clear()
    {
        foreach (var c in cards)
            if (c != null) UnityEngine.Object.Destroy(c.gameObject);
        cards.Clear();
    }
}
