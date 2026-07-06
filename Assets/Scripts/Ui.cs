using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helpers for building uGUI elements from code.
/// </summary>
public static class Ui
{
    static Font _font;

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
    /// Rounded panel with a contrasting border: the root image is the border
    /// color and a stretched child, inset by the border width, is the fill.
    /// Children added afterwards draw above the fill.
    /// </summary>
    public static Image BorderPanel(RectTransform rt, Color fill, Color border, float borderWidth = 6f)
    {
        Panel(rt, border);
        var fillRt = Stretch("Fill", rt);
        fillRt.offsetMin = new Vector2(borderWidth, borderWidth);
        fillRt.offsetMax = new Vector2(-borderWidth, -borderWidth);
        var img = Panel(fillRt, fill);
        img.raycastTarget = false;
        return img;
    }

    public static Text Label(string name, Transform parent, string content, int fontSize, Color color,
        Vector2 pos, Vector2 size, TextAnchor align = TextAnchor.MiddleCenter,
        FontStyle style = FontStyle.Bold, Vector2? anchor = null, bool wrap = false)
    {
        var rt = Rect(name, parent, size, pos, anchor);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = DefaultFont;
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

    public static Button MakeButton(string name, Transform parent, string label, Vector2 size, Vector2 pos,
        Color bg, Color textColor, int fontSize, Action onClick, Vector2? anchor = null)
    {
        var rt = Rect(name, parent, size, pos, anchor);
        var img = Panel(rt, bg);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        if (!string.IsNullOrEmpty(label))
            Label("Label", rt, label, fontSize, textColor, Vector2.zero, size);
        if (onClick != null)
            btn.onClick.AddListener(() => onClick());
        return btn;
    }
}
