using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A pile of face-down cards beside the clearing zone (like the original's
/// side rows). Only the innermost card is face-up and tappable; taking it
/// slides the pile inward and flips the next card up.
/// </summary>
public class SideStack
{
    public readonly List<Card> cards = new List<Card>(); // [0] = front (face-up)

    RectTransform _root;
    float _dirOut;           // -1 = pile extends left, +1 = pile extends right
    const float Step = 34f;
    const float StackScale = 0.82f;

    public void Init(RectTransform root, float dirOut)
    {
        _root = root;
        _dirOut = dirOut;
    }

    public int Count => cards.Count;
    public Card Front => cards.Count > 0 ? cards[0] : null;

    public void AddInitial(Card card)
    {
        cards.Add(card);
    }

    public void Take(Card card)
    {
        cards.Remove(card);
        Refresh(true);
    }

    /// <summary>Puts a card back at the front (Undo item).</summary>
    public void PushFront(Card card)
    {
        card.inTray = false;
        card.inHold = false;
        card.transform.SetParent(_root, true);
        cards.Insert(0, card);
        Refresh(true);
    }

    public void Refresh(bool animate)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null) continue;
            card.transform.SetSiblingIndex(cards.Count - 1 - i); // front drawn on top
            card.transform.localScale = new Vector3(StackScale, StackScale, 1f);
            card.SetFaceDown(i != 0);
            var target = new Vector2(_dirOut * Step * i, 0f);
            if (animate) Tween.MoveTo(card.Rect, target, 0.18f);
            else card.Rect.anchoredPosition = target;
        }
    }

    public void Clear()
    {
        foreach (var c in cards)
            if (c != null) Object.Destroy(c.gameObject);
        cards.Clear();
    }
}
