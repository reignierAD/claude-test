using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu gag: every so often a random image (assign your favorite memes
/// in GameSettings > Menu Peekaboo) slides partway in from a random screen
/// edge, hangs out for a moment, then ducks back down. Attach to the menu
/// screen — it dies with it, so it never runs during gameplay.
/// </summary>
public class Peekaboo : MonoBehaviour
{
    IEnumerator Start()
    {
        var s = GameConfig.S;
        // first appearance comes a bit sooner so the gag gets noticed
        yield return new WaitForSeconds(Random.Range(2.5f, 5f));
        while (true)
        {
            if (s.peekabooSprites != null && s.peekabooSprites.Length > 0)
                yield return Show(s);
            float min = Mathf.Max(1f, Mathf.Min(s.peekabooMinInterval, s.peekabooMaxInterval));
            float max = Mathf.Max(min, Mathf.Max(s.peekabooMinInterval, s.peekabooMaxInterval));
            yield return new WaitForSeconds(Random.Range(min, max));
        }
    }

    IEnumerator Show(GameSettings s)
    {
        var sprite = s.peekabooSprites[Random.Range(0, s.peekabooSprites.Length)];
        if (sprite == null) yield break;

        int side = Random.Range(0, 4); // 0 bottom, 1 top, 2 left, 3 right
        float size = Mathf.Max(60f, s.peekabooSize);

        // the image's BOTTOM always faces the edge it pops out of:
        // bottom = upright, top = upside down, left/right = lying sideways
        Vector2 anchor, hidden, shown;
        float baseAngle;
        switch (side)
        {
            case 0:
                anchor = new Vector2(0.5f, 0f);
                baseAngle = 0f;
                float bx = Random.Range(-600f, 600f);
                hidden = new Vector2(bx, -size * 0.75f);
                shown = new Vector2(bx, size * 0.32f);
                break;
            case 1:
                anchor = new Vector2(0.5f, 1f);
                baseAngle = 180f;
                float tx = Random.Range(-600f, 600f);
                hidden = new Vector2(tx, size * 0.75f);
                shown = new Vector2(tx, -size * 0.32f);
                break;
            case 2:
                anchor = new Vector2(0f, 0.5f);
                baseAngle = -90f; // bottom points at the left edge
                float ly = Random.Range(-300f, 300f);
                hidden = new Vector2(-size * 0.75f, ly);
                shown = new Vector2(size * 0.32f, ly);
                break;
            default:
                anchor = new Vector2(1f, 0.5f);
                baseAngle = 90f; // bottom points at the right edge
                float ry = Random.Range(-300f, 300f);
                hidden = new Vector2(size * 0.75f, ry);
                shown = new Vector2(-size * 0.32f, ry);
                break;
        }

        var rt = Ui.Rect("Peekaboo", transform, new Vector2(size, size), hidden, anchor);
        rt.SetSiblingIndex(0); // behind the menu buttons and labels
        rt.localEulerAngles = new Vector3(0f, 0f, baseAngle + Random.Range(-14f, 14f));
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        Tween.MoveTo(rt, shown, 0.45f);
        yield return new WaitForSeconds(0.45f + Mathf.Max(0.5f, s.peekabooShowTime));
        if (rt == null) yield break;
        Tween.MoveTo(rt, hidden, 0.4f);
        yield return new WaitForSeconds(0.45f);
        if (rt != null) Destroy(rt.gameObject);
    }
}
