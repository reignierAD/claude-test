using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight UI confetti: colored strips that fall, sway, spin and fade.
/// Purely cosmetic, cleans itself up.
/// </summary>
public static class Confetti
{
    static readonly Color[] Palette =
    {
        new Color(0.95f, 0.45f, 0.40f),
        new Color(1.00f, 0.78f, 0.15f),
        new Color(0.45f, 0.80f, 0.35f),
        new Color(0.35f, 0.60f, 0.95f),
        new Color(0.80f, 0.50f, 0.95f),
        new Color(1.00f, 0.62f, 0.25f),
    };

    public static void Burst(RectTransform parent, int count)
    {
        if (parent == null) return;
        for (int i = 0; i < count; i++)
            Tween.Run(Piece(parent));
    }

    static IEnumerator Piece(RectTransform parent)
    {
        if (parent == null) yield break;

        // small random delay so the burst feels organic
        yield return new WaitForSeconds(Random.Range(0f, 0.35f));
        if (parent == null) yield break;

        var rt = Ui.Rect("Confetti", parent,
            new Vector2(Random.Range(9f, 16f), Random.Range(14f, 24f)), Vector2.zero);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Palette[Random.Range(0, Palette.Length)];
        img.raycastTarget = false;

        float x0 = Random.Range(-360f, 360f);
        float y0 = Random.Range(330f, 430f);
        float fall = Random.Range(190f, 300f);
        float sway = Random.Range(25f, 80f);
        float phase = Random.Range(0f, 6.28f);
        float spin = Random.Range(-420f, 420f);
        float life = Random.Range(2.2f, 3.2f);

        float t = 0f;
        while (t < life)
        {
            if (rt == null) yield break;
            t += Time.deltaTime;
            rt.anchoredPosition = new Vector2(x0 + Mathf.Sin(phase + t * 3f) * sway, y0 - fall * t);
            rt.localEulerAngles = new Vector3(0f, 0f, spin * t);
            if (t > life - 0.5f)
            {
                var c = img.color;
                c.a = Mathf.Clamp01((life - t) * 2f);
                img.color = c;
            }
            yield return null;
        }
        if (rt != null) Object.Destroy(rt.gameObject);
    }
}
