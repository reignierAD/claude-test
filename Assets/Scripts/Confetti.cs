using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Explosive UI confetti: pieces blast outward from the center with real
/// velocity, arc under gravity, spin and fade. Purely cosmetic, cleans
/// itself up.
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
        new Color(0.30f, 0.85f, 0.80f),
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

        // slight stagger so the blast feels like a real explosion, not a frame
        yield return new WaitForSeconds(Random.Range(0f, 0.18f));
        if (parent == null) yield break;

        // mix of strips and squares
        Vector2 pieceSize = Random.Range(0, 2) == 0
            ? new Vector2(Random.Range(8f, 14f), Random.Range(18f, 30f))
            : new Vector2(Random.Range(10f, 17f), Random.Range(10f, 17f));

        var rt = Ui.Rect("Confetti", parent, pieceSize,
            new Vector2(Random.Range(-70f, 70f), Random.Range(-30f, 90f)));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Palette[Random.Range(0, Palette.Length)];
        img.raycastTarget = false;

        // blast outward: strong sideways spread, upward bias, gravity pulls back
        float vx = Random.Range(-850f, 850f);
        float vy = Random.Range(150f, 950f);
        const float gravity = 1500f;
        float spin = Random.Range(-720f, 720f);
        float life = Random.Range(1.4f, 2.4f);

        float t = 0f;
        Vector2 pos = rt.anchoredPosition;
        while (t < life)
        {
            if (rt == null) yield break;
            float dt = Time.deltaTime;
            t += dt;
            vy -= gravity * dt;
            vx *= 1f - 0.6f * dt; // air drag on the sideways burst
            pos = new Vector2(pos.x + vx * dt, pos.y + vy * dt);
            rt.anchoredPosition = pos;
            rt.localEulerAngles = new Vector3(0f, 0f, spin * t);
            if (t > life - 0.4f)
            {
                var c = img.color;
                c.a = Mathf.Clamp01((life - t) / 0.4f);
                img.color = c;
            }
            yield return null;
        }
        if (rt != null) Object.Destroy(rt.gameObject);
    }
}
