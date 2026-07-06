using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A star with a dark border star behind it and a little sparkle on top,
/// so lit stars stand out. Used by the HUD, menu and result popups.
/// </summary>
public class StarIcon
{
    public RectTransform Root;

    Image _border;
    Image _fill;
    GameObject _shine;

    static readonly Color Gold = new Color(1.00f, 0.78f, 0.15f);
    static readonly Color GoldBorder = new Color(0.74f, 0.46f, 0.08f);
    static readonly Color Dim = new Color(0.62f, 0.56f, 0.50f, 0.55f);
    static readonly Color DimBorder = new Color(0.45f, 0.41f, 0.37f, 0.45f);

    public static StarIcon Create(Transform parent, float size, Vector2 pos, Vector2? anchor = null, bool lit = true)
    {
        var icon = new StarIcon();
        icon.Root = Ui.Rect("Star", parent, new Vector2(size, size), pos, anchor);

        var borderRt = Ui.Rect("Border", icon.Root, new Vector2(size * 1.28f, size * 1.28f), Vector2.zero);
        icon._border = borderRt.gameObject.AddComponent<Image>();
        icon._border.sprite = SpriteFactory.Star;
        icon._border.raycastTarget = false;

        var fillRt = Ui.Rect("Fill", icon.Root, new Vector2(size, size), Vector2.zero);
        icon._fill = fillRt.gameObject.AddComponent<Image>();
        icon._fill.sprite = SpriteFactory.Star;
        icon._fill.raycastTarget = false;

        // sparkle dot near the top-left point
        var shineRt = Ui.Rect("Shine", icon.Root, new Vector2(size * 0.24f, size * 0.24f),
            new Vector2(-size * 0.13f, size * 0.08f));
        var shine = shineRt.gameObject.AddComponent<Image>();
        shine.sprite = SpriteFactory.Circle;
        shine.color = new Color(1f, 1f, 1f, 0.85f);
        shine.raycastTarget = false;
        icon._shine = shineRt.gameObject;

        icon.SetLit(lit);
        return icon;
    }

    public void SetLit(bool lit)
    {
        _border.color = lit ? GoldBorder : DimBorder;
        _fill.color = lit ? Gold : Dim;
        _shine.SetActive(lit);
    }
}
