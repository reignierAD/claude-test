using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Card : MonoBehaviour, IPointerClickHandler
{
    public int cardTypeId;       // Which symbol this card represents
    public int layer;            // Stack depth (higher = on top)
    public Sprite faceSprite;

    [HideInInspector] public bool isInClearingZone = false;
    [HideInInspector] public bool isRemoved = false;

    private SpriteRenderer spriteRenderer;
    private List<Card> coveringCards = new List<Card>();

    // Tint to show a card is blocked by cards above it
    private static readonly Color BlockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color NormalColor = Color.white;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (faceSprite != null)
            spriteRenderer.sprite = faceSprite;
    }

    public void SetCoveringCards(List<Card> covers)
    {
        coveringCards = covers;
        RefreshBlockedVisual();
    }

    public void NotifyCoverRemoved(Card removed)
    {
        coveringCards.Remove(removed);
        RefreshBlockedVisual();
    }

    public bool IsAccessible()
    {
        if (isRemoved || isInClearingZone) return false;
        foreach (var c in coveringCards)
            if (!c.isRemoved && !c.isInClearingZone)
                return false;
        return true;
    }

    void RefreshBlockedVisual()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = IsAccessible() ? NormalColor : BlockedColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsAccessible()) return;
        GameManager.Instance.OnCardClicked(this);
    }

    // Called by other cards/systems to force a visual refresh (e.g. after undo)
    public void ForceRefresh()
    {
        RefreshBlockedVisual();
    }
}
