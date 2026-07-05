using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single card on the board / in the clearing zone. Purely visual + click
/// forwarding; all rules live in GameController / Board / Tray.
/// </summary>
public class Card : MonoBehaviour
{
    public int typeIndex;

    // board placement (half-grid coordinates)
    public int layer;
    public int col;
    public int row;

    public bool inTray;
    public bool inHold;
    public bool removed;

    public Button button;
    public Action<Card> onClicked;

    Image _icon;
    Image _iconRim;
    Text _label;
    GameObject _shade;

    public RectTransform Rect => (RectTransform)transform;

    public Vector2 BoardPosition => new Vector2(col * GameConfig.HalfStep, row * GameConfig.HalfStep);

    public static Card Create(Transform parent, int typeIndex)
    {
        float s = GameConfig.CardSize;
        var rt = Ui.Rect("Card", parent, new Vector2(s, s), Vector2.zero);
        var go = rt.gameObject;

        var card = go.AddComponent<Card>();
        card.typeIndex = typeIndex;

        // face
        var face = go.AddComponent<Image>();
        face.sprite = SpriteFactory.RoundedRect;
        face.type = Image.Type.Sliced;
        face.color = new Color(1f, 0.99f, 0.95f);

        card.button = go.AddComponent<Button>();
        card.button.targetGraphic = face;
        card.button.onClick.AddListener(() => card.onClicked?.Invoke(card));

        // icon: darker rim circle behind a full-color circle, plus a highlight dot
        var rimRt = Ui.Rect("IconRim", rt, new Vector2(66, 66), new Vector2(0, 12));
        card._iconRim = rimRt.gameObject.AddComponent<Image>();
        card._iconRim.sprite = SpriteFactory.Circle;
        card._iconRim.raycastTarget = false;

        var iconRt = Ui.Rect("Icon", rt, new Vector2(58, 58), new Vector2(0, 12));
        card._icon = iconRt.gameObject.AddComponent<Image>();
        card._icon.sprite = SpriteFactory.Circle;
        card._icon.raycastTarget = false;

        var hlRt = Ui.Rect("Highlight", rt, new Vector2(16, 16), new Vector2(-12, 26));
        var hl = hlRt.gameObject.AddComponent<Image>();
        hl.sprite = SpriteFactory.Circle;
        hl.color = new Color(1f, 1f, 1f, 0.55f);
        hl.raycastTarget = false;

        card._label = Ui.Label("Name", rt, "", 17, new Color(0.45f, 0.30f, 0.15f),
            new Vector2(0, -36), new Vector2(s, 24));

        // shade overlay shown while the card is covered by a higher layer
        var shadeRt = Ui.Rect("Shade", rt, new Vector2(s, s), Vector2.zero);
        var shadeImg = shadeRt.gameObject.AddComponent<Image>();
        shadeImg.sprite = SpriteFactory.RoundedRect;
        shadeImg.type = Image.Type.Sliced;
        shadeImg.color = new Color(0.15f, 0.10f, 0.08f, 0.62f);
        shadeImg.raycastTarget = false;
        card._shade = shadeRt.gameObject;
        card._shade.SetActive(false);

        card.RefreshVisual();
        return card;
    }

    /// <summary>Re-applies kind color and name (used after a Refresh item shuffles types).</summary>
    public void RefreshVisual()
    {
        var kind = GameConfig.Kinds[typeIndex];
        _icon.color = kind.color;
        Color rim = Color.Lerp(kind.color, Color.black, 0.38f);
        rim.a = 1f;
        _iconRim.color = rim;
        _label.text = kind.name;
    }

    public void SetBlocked(bool blocked)
    {
        button.interactable = !blocked;
        _shade.SetActive(blocked);
    }
}
