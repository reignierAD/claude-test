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

    /// <summary>-1 = board card; 0 = left side stack; 1 = right side stack.</summary>
    public int stackSide = -1;

    // small per-card pixel offset inside its grid cell, for an organic look
    public float jitterX;
    public float jitterY;

    public Button button;
    public Action<Card> onClicked;

    Image _icon;
    Image _iconRim;
    Image _highlight;
    GameObject _shade;
    GameObject _back;

    public RectTransform Rect => (RectTransform)transform;

    public Vector2 BoardPosition => new Vector2(col * GameConfig.HalfStep + jitterX, row * GameConfig.HalfStep + jitterY);

    public static Card Create(Transform parent, int typeIndex)
    {
        float s = GameConfig.CardSize;
        var rt = Ui.Rect("Card", parent, new Vector2(s, s), Vector2.zero);
        var go = rt.gameObject;

        var card = go.AddComponent<Card>();
        card.typeIndex = typeIndex;

        // border frame, slightly smaller than the logical grid cell so the
        // random per-card spacing can never make neighbors overlap
        var frameRt = Ui.Rect("Frame", rt, new Vector2(s - 8f, s - 8f), Vector2.zero);
        var frame = frameRt.gameObject.AddComponent<Image>();
        frame.sprite = SpriteFactory.RoundedRect;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.95f, 0.70f, 0.38f);

        // the face doubles as a stencil mask, so pictures get the same
        // rounded corners as the card and never show white side bars
        var faceRt = Ui.Rect("Face", rt, new Vector2(s - 20f, s - 20f), Vector2.zero);
        var face = faceRt.gameObject.AddComponent<Image>();
        face.sprite = SpriteFactory.RoundedRectInner;
        face.type = Image.Type.Sliced;
        face.color = new Color(1f, 0.99f, 0.95f);
        face.raycastTarget = false;
        var faceMask = faceRt.gameObject.AddComponent<Mask>();
        faceMask.showMaskGraphic = true;

        card.button = go.AddComponent<Button>();
        card.button.targetGraphic = frame;
        card.button.onClick.AddListener(() => card.onClicked?.Invoke(card));

        // placeholder art (used when the kind has no sprite): rim + circle + shine
        var rimRt = Ui.Rect("IconRim", faceRt, new Vector2(80, 80), Vector2.zero);
        card._iconRim = rimRt.gameObject.AddComponent<Image>();
        card._iconRim.sprite = SpriteFactory.Circle;
        card._iconRim.raycastTarget = false;

        var iconRt = Ui.Rect("Icon", faceRt, new Vector2(72, 72), Vector2.zero);
        card._icon = iconRt.gameObject.AddComponent<Image>();
        card._icon.sprite = SpriteFactory.Circle;
        card._icon.raycastTarget = false;

        var hlRt = Ui.Rect("Highlight", faceRt, new Vector2(18, 18), new Vector2(-16, 18));
        card._highlight = hlRt.gameObject.AddComponent<Image>();
        card._highlight.sprite = SpriteFactory.Circle;
        card._highlight.color = new Color(1f, 1f, 1f, 0.55f);
        card._highlight.raycastTarget = false;

        // card back, shown while face-down in a side pile. A custom picture
        // can be assigned in GameSettings > Card Back; otherwise a plain
        // generated back is used.
        var backRt = Ui.Rect("Back", rt, new Vector2(s - 8f, s - 8f), Vector2.zero);
        var backImg = backRt.gameObject.AddComponent<Image>();
        backImg.sprite = SpriteFactory.RoundedRect;
        backImg.type = Image.Type.Sliced;
        backImg.color = new Color(0.62f, 0.42f, 0.28f);
        backImg.raycastTarget = false;
        var backMask = backRt.gameObject.AddComponent<Mask>();
        backMask.showMaskGraphic = true;
        var customBack = GameConfig.S.cardBackSprite;
        if (customBack != null)
        {
            // custom picture fills the back edge-to-edge, corners masked round
            var backPicRt = Ui.Stretch("BackPicture", backRt);
            var backPic = backPicRt.gameObject.AddComponent<Image>();
            backPic.sprite = customBack;
            backPic.raycastTarget = false;
        }
        else
        {
            var emblemRt = Ui.Rect("Emblem", backRt, new Vector2(44, 44), Vector2.zero);
            var emblem = emblemRt.gameObject.AddComponent<Image>();
            emblem.sprite = SpriteFactory.Circle;
            emblem.color = new Color(0.78f, 0.58f, 0.40f);
            emblem.raycastTarget = false;
        }
        card._back = backRt.gameObject;
        card._back.SetActive(false);

        // shade overlay shown while the card is covered by a higher layer
        var shadeRt = Ui.Rect("Shade", rt, new Vector2(s - 8f, s - 8f), Vector2.zero);
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

    /// <summary>
    /// Re-applies the kind's picture (or placeholder color). Called on
    /// creation and after a Refresh item shuffles types.
    /// </summary>
    public void RefreshVisual()
    {
        var kind = GameConfig.S.cardKinds[typeIndex];
        bool hasSprite = kind.sprite != null;
        var iconRt = (RectTransform)_icon.transform;

        _iconRim.gameObject.SetActive(!hasSprite);
        _highlight.gameObject.SetActive(!hasSprite);

        if (hasSprite)
        {
            // the picture fills the masked face edge-to-edge (no white bars),
            // and the mask rounds its corners
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            _icon.sprite = kind.sprite;
            _icon.color = Color.white;
            _icon.preserveAspect = false;
        }
        else
        {
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(72, 72);
            iconRt.anchoredPosition = Vector2.zero;
            _icon.sprite = SpriteFactory.Circle;
            _icon.color = kind.color;
            _icon.preserveAspect = false;

            Color rim = Color.Lerp(kind.color, Color.black, 0.38f);
            rim.a = 1f;
            _iconRim.color = rim;
        }
    }

    public void SetBlocked(bool blocked)
    {
        button.interactable = !blocked;
        _shade.SetActive(blocked);
    }

    /// <summary>Face-down cards (side stacks) show their back and can't be tapped.</summary>
    public void SetFaceDown(bool down)
    {
        _back.SetActive(down);
        button.interactable = !down;
    }
}
