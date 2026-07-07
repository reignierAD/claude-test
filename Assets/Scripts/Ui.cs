using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helpers for building uGUI elements from code.
/// </summary>
public static class Ui
{
    static Font _font;
    static Font _titleFont;

    public static Font DefaultFont
    {
        get
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return _font;
        }
    }

    /// <summary>
    /// A chunkier "display" font for titles and buttons so the cartoon theme
    /// reads stronger than the plain body font. Tries a list of rounded/bold OS
    /// faces and falls back to the default font when none are installed. (For a
    /// guaranteed look in a WebGL build, import a real TTF and assign it here.)
    /// </summary>
    public static Font TitleFont
    {
        get
        {
            if (_titleFont == null)
            {
                string[] candidates =
                {
                    "Comic Sans MS", "Comic Neue", "Chalkboard SE",
                    "Arial Rounded MT Bold", "Verdana", "Trebuchet MS", "Arial Black",
                };
                foreach (var name in candidates)
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 16);
                    if (f != null && f.name != null && f.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _titleFont = f;
                        break;
                    }
                }
                if (_titleFont == null) _titleFont = DefaultFont;
            }
            return _titleFont;
        }
    }

    /// <summary>Creates an empty RectTransform anchored at a point.</summary>
    public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos, Vector2? anchor = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    /// <summary>Creates a RectTransform stretched to fill its parent.</summary>
    public static RectTransform Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>Adds an Image to a rect. Rounded corners by default.</summary>
    public static Image Panel(RectTransform rt, Color color, bool rounded = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (rounded)
        {
            img.sprite = SpriteFactory.RoundedRect;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    /// <summary>
    /// Rounded panel with a smaller corner radius, for fills nested inside a
    /// border so both curves stay concentric.
    /// </summary>
    public static Image PanelInner(RectTransform rt, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = SpriteFactory.RoundedRectInner;
        img.type = Image.Type.Sliced;
        return img;
    }

    /// <summary>
    /// Cartoon-framed rounded panel: a dark "ink" outline (root), a colored
    /// border ring inside it, and the inner fill. The three concentric radii
    /// (outer / mid / inner) keep every curve aligned. Children added afterwards
    /// draw above the fill. Pass <paramref name="outline"/> to override the
    /// auto-derived ink color.
    /// </summary>
    public static Image BorderPanel(RectTransform rt, Color fill, Color border, float borderWidth = 6f,
        Color? outline = null)
    {
        // layer 1: dark ink outline (the root image)
        Panel(rt, outline ?? CartoonInk(border));

        // layer 2: colored border ring (mid radius, inset by the ink thickness)
        float ink = Mathf.Max(2f, borderWidth * 0.45f);
        var borderRt = Stretch("Border", rt);
        borderRt.offsetMin = new Vector2(ink, ink);
        borderRt.offsetMax = new Vector2(-ink, -ink);
        var borderImg = borderRt.gameObject.AddComponent<Image>();
        borderImg.color = border;
        borderImg.sprite = SpriteFactory.RoundedRectMid;
        borderImg.type = Image.Type.Sliced;
        borderImg.raycastTarget = false;

        // layer 3: inner fill (inner radius), total inset ≈ borderWidth from the edge
        var fillRt = Stretch("Fill", borderRt);
        float rest = Mathf.Max(2f, borderWidth - ink);
        fillRt.offsetMin = new Vector2(rest, rest);
        fillRt.offsetMax = new Vector2(-rest, -rest);
        var img = PanelInner(fillRt, fill);
        img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// A genuinely transparent zone: only a cartoon outline is drawn, the
    /// interior lets the background show straight through (unlike BorderPanel,
    /// whose layers fill the whole area).
    /// </summary>
    public static Image OutlineZone(RectTransform rt, Color outline)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = SpriteFactory.RoundedRectFrame;
        img.type = Image.Type.Sliced;
        img.color = outline;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Dark, cartoonish "ink" tone derived from a border color.</summary>
    public static Color CartoonInk(Color c)
    {
        return new Color(c.r * 0.32f, c.g * 0.28f, c.b * 0.26f, Mathf.Min(1f, c.a + 0.15f));
    }

    public static Text Label(string name, Transform parent, string content, int fontSize, Color color,
        Vector2 pos, Vector2 size, TextAnchor align = TextAnchor.MiddleCenter,
        FontStyle style = FontStyle.Bold, Vector2? anchor = null, bool wrap = false, Font font = null)
    {
        var rt = Rect(name, parent, size, pos, anchor);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font ?? DefaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// Every button gets the raised "bump" look: a dark drop shadow behind a
    /// bordered body. Border and shadow tones are derived from the fill.
    /// </summary>
    public static Button MakeButton(string name, Transform parent, string label, Vector2 size, Vector2 pos,
        Color bg, Color textColor, int fontSize, Action onClick, Vector2? anchor = null)
    {
        var rt = Rect(name, parent, size, pos, anchor);

        var shadowRt = Rect("Shadow", rt, size, new Vector2(0f, -5f));
        var shadowImg = Panel(shadowRt, Darken(bg, 0.50f, 0.85f));
        shadowImg.raycastTarget = false;

        var body = Rect("Body", rt, size, Vector2.zero);
        // dark cartoon ink outline, then the colored bevel ring inside it
        var borderImg = Panel(body, CartoonInk(bg));
        borderImg.raycastTarget = false;
        var bevelRt = Stretch("Bevel", body);
        bevelRt.offsetMin = new Vector2(3f, 3f);
        bevelRt.offsetMax = new Vector2(-3f, -3f);
        var bevelImg = bevelRt.gameObject.AddComponent<Image>();
        bevelImg.color = Darken(bg, 0.74f, 1f);
        bevelImg.sprite = SpriteFactory.RoundedRectMid;
        bevelImg.type = Image.Type.Sliced;
        bevelImg.raycastTarget = false;
        var fillRt = Stretch("Fill", bevelRt);
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        var fillImg = PanelInner(fillRt, bg);

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = fillImg; // disabled/pressed tint covers the body
        if (!string.IsNullOrEmpty(label))
            Label("Label", body, label, fontSize, textColor, Vector2.zero, size, font: TitleFont);
        if (onClick != null)
            btn.onClick.AddListener(() => onClick());
        return btn;
    }

    static Color Darken(Color c, float factor, float alphaFactor)
    {
        return new Color(c.r * factor, c.g * factor, c.b * factor, c.a * alphaFactor);
    }
}
