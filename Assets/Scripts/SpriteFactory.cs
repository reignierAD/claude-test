using UnityEngine;

/// <summary>
/// Generates the few sprites the game needs (rounded rectangle, circle) at
/// runtime so the project requires no imported art assets.
/// </summary>
public static class SpriteFactory
{
    static Sprite _rounded;
    static Sprite _roundedMid;
    static Sprite _roundedInner;
    static Sprite _roundedFrame;
    static Sprite _circle;
    static Sprite _star;
    static Sprite _arrow;
    static Sprite _gear;
    static Sprite _sunburst;

    /// <summary>White rounded-rect, 9-sliced so it scales to any size.</summary>
    public static Sprite RoundedRect
    {
        get
        {
            if (_rounded == null) _rounded = BuildRoundedRect(64, 16f);
            return _rounded;
        }
    }

    /// <summary>
    /// Rounded-rect with a mid corner radius, for the colored ring that sits
    /// between the dark cartoon ink outline and the inner fill so all three
    /// curves stay concentric.
    /// </summary>
    public static Sprite RoundedRectMid
    {
        get
        {
            if (_roundedMid == null) _roundedMid = BuildRoundedRect(64, 13f);
            return _roundedMid;
        }
    }

    /// <summary>
    /// Rounded-rect with a smaller corner radius, for fills nested inside a
    /// border so the inner curve stays concentric with the outer one.
    /// </summary>
    public static Sprite RoundedRectInner
    {
        get
        {
            if (_roundedInner == null) _roundedInner = BuildRoundedRect(64, 10f);
            return _roundedInner;
        }
    }

    /// <summary>
    /// Rounded-rect outline only — a stroke of constant thickness with a fully
    /// transparent centre. 9-sliced, so a zone drawn with it shows nothing but
    /// its cartoon border and lets the background show straight through.
    /// </summary>
    public static Sprite RoundedRectFrame
    {
        get
        {
            if (_roundedFrame == null) _roundedFrame = BuildRoundedRectFrame(64, 16f, 6f);
            return _roundedFrame;
        }
    }

    static Sprite BuildRoundedRectFrame(int size, float radius, float thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float dx = Mathf.Max(Mathf.Abs(px) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(py) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius; // <0 inside, 0 at edge
                float outer = Mathf.Clamp01(0.5f - dist);              // fades past the outer edge
                float inner = Mathf.Clamp01(dist + thickness + 0.5f);  // fades past the stroke width
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Min(outer, inner));
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        float border = radius + thickness + 2f;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    /// <summary>White radial sunburst (alternating transparent rays).</summary>
    public static Sprite Sunburst
    {
        get
        {
            if (_sunburst == null) _sunburst = BuildSunburst(512, 24);
            return _sunburst;
        }
    }

    static Sprite BuildSunburst(int size, int rays)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        float half = size * 0.5f;
        float wedge = Mathf.PI * 2f / rays;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 2; sy++)
                {
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float px = x + 0.25f + sx * 0.5f - half;
                        float py = y + 0.25f + sy * 0.5f - half;
                        if (Mathf.Sqrt(px * px + py * py) > half - 1f) continue;
                        float ang = Mathf.Atan2(py, px) + Mathf.PI;
                        if ((int)(ang / wedge) % 2 == 0) hits++;
                    }
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, hits / 4f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>White filled circle.</summary>
    public static Sprite Circle
    {
        get
        {
            if (_circle == null) _circle = BuildCircle(64);
            return _circle;
        }
    }

    /// <summary>White five-pointed star.</summary>
    public static Sprite Star
    {
        get
        {
            if (_star == null) _star = BuildStar(64);
            return _star;
        }
    }

    /// <summary>White left-pointing chevron (used for the back button).</summary>
    public static Sprite Arrow
    {
        get
        {
            if (_arrow == null) _arrow = BuildArrow(64);
            return _arrow;
        }
    }

    /// <summary>White gear/cog with a hollow centre (settings icon).</summary>
    public static Sprite Gear
    {
        get
        {
            if (_gear == null) _gear = BuildGear(64, 8);
            return _gear;
        }
    }

    static Sprite BuildGear(int size, int teeth)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        float wedge = Mathf.PI * 2f / teeth;
        float rTip = half * 0.94f, rRoot = half * 0.72f, rHole = half * 0.30f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 2; sy++)
                {
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float px = x + 0.25f + sx * 0.5f - half;
                        float py = y + 0.25f + sy * 0.5f - half;
                        float rr = Mathf.Sqrt(px * px + py * py);
                        if (rr < rHole) continue;
                        float ang = Mathf.Atan2(py, px) + Mathf.PI;
                        float frac = (ang % wedge) / wedge; // 0..1 within a tooth
                        float outer = frac < 0.5f ? rTip : rRoot;
                        if (rr <= outer) hits++;
                    }
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, hits / 4f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildStar(int size)
    {
        // 10 alternating outer/inner vertices, point at the top
        var verts = new Vector2[10];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outer = size * 0.48f, inner = size * 0.20f;
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? outer : inner;
            verts[i] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
        }
        return BuildPolygon(size, verts);
    }

    static Sprite BuildArrow(int size)
    {
        // thick "<" chevron
        var f = new[]
        {
            new Vector2(0.64f, 0.88f),
            new Vector2(0.26f, 0.50f),
            new Vector2(0.64f, 0.12f),
            new Vector2(0.80f, 0.26f),
            new Vector2(0.56f, 0.50f),
            new Vector2(0.80f, 0.74f),
        };
        var verts = new Vector2[f.Length];
        for (int i = 0; i < f.Length; i++)
            verts[i] = new Vector2(f[i].x * size, f[i].y * size);
        return BuildPolygon(size, verts);
    }

    static Sprite BuildPolygon(int size, Vector2[] verts)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 2x2 supersampling for soft edges
                int hits = 0;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (PointInPolygon(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f), verts))
                            hits++;
                pixels[y * size + x] = new Color(1f, 1f, 1f, hits / 4f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    static Sprite BuildRoundedRect(int size, float radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float dx = Mathf.Max(Mathf.Abs(px) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(py) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float alpha = Mathf.Clamp01(0.5f - dist); // ~1px anti-aliasing
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        float border = radius + 6f;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    static Sprite BuildCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float dist = Mathf.Sqrt(px * px + py * py) - (half - 1f);
                float alpha = Mathf.Clamp01(0.5f - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
