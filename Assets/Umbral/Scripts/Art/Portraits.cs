using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Retratos pintados por código con campos de distancia (SDF): formas planas con sombreado interior,
    /// luz de borde y contorno de tinta, al estilo de los retratos de diálogo de Hades.
    /// </summary>
    public static class Portraits
    {
        const int W = 256, H = 320;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string id)
        {
            bool f = Save.Data.female;
            string key = id == "sothis" ? (f ? "sothis_f" : "sothis_m") : id;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var ai = ArtIA.Sprite(ArtIA.Retratos, key, true) ?? (id == "boss_wilbur" ? ArtIA.Sprite(ArtIA.Retratos, "wilbur", true) : null);
            if (ai != null) { cache[key] = ai; return ai; }
            var p = new Painter(W, H);
            Paint(p, key);
            var tex = p.ToTexture(key);
            s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100);
            cache[key] = s;
            return s;
        }

        // ---------------- pintor SDF ----------------
        public class Painter
        {
            public readonly int w, h;
            public readonly Color[] px;
            public Painter(int w, int h) { this.w = w; this.h = h; px = new Color[w * h]; }

            public Texture2D ToTexture(string name)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "portrait_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                t.SetPixels(px); t.Apply();
                return t;
            }

            public void Background(Color top, Color bottom, Color glow)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (float)x / w, v = (float)y / h;
                        Color c = Color.Lerp(bottom, top, v);
                        float g = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.62f)) * 1.8f);
                        c += glow * g * g * 0.8f;
                        float n = Tex.Fbm(u * 6, v * 6, 3);
                        c *= 0.85f + n * 0.3f;
                        // marco: viñeta y desvanecido en los bordes laterales
                        float edge = Mathf.Clamp01(Mathf.Min(u, 1 - u) * 6f) * Mathf.Clamp01((1f - v) * 5f);
                        edge *= Mathf.Clamp01(1.25f - Vector2.Distance(new Vector2(u, v * 0.8f), new Vector2(0.5f, 0.45f)) * 1.4f);
                        c.a = Mathf.Clamp01(edge * 1.2f);
                        px[y * w + x] = c;
                    }
            }

            /// <summary>Rellena una forma SDF (coordenadas normalizadas, y hacia arriba) con sombreado y tinta.</summary>
            public void Fill(Func<Vector2, float> sdf, Color c, float ink = 2.2f, float shade = 0.35f, Color? rim = null, float glow = 0f)
            {
                Color inkC = new Color(0.04f, 0.02f, 0.06f, 1f);
                float px1 = 1f / w;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Vector2 p = new Vector2((x + 0.5f) / w, (y + 0.5f) / w);
                        float d = sdf(p) * w; // en píxeles
                        if (d > ink + 1f) continue;
                        int i = y * w + x;
                        Color dst = px[i];
                        if (glow > 0 && d > 0)
                        {
                            continue;
                        }
                        Color col;
                        if (d > 0)
                        {
                            float a = Mathf.Clamp01(1f - (d - ink) );
                            col = Color.Lerp(dst, inkC, a);
                            col.a = Mathf.Max(dst.a, a);
                            px[i] = col;
                            continue;
                        }
                        // interior: sombreado hacia el borde y luz superior-izquierda
                        float inner = Mathf.Clamp01(-d / 14f);
                        float gx = sdf(p + new Vector2(px1, 0)) - sdf(p - new Vector2(px1, 0));
                        float gy = sdf(p + new Vector2(0, px1)) - sdf(p - new Vector2(0, px1));
                        Vector2 n = new Vector2(gx, gy).normalized;
                        float light = Vector2.Dot(n, new Vector2(-0.6f, 0.8f));
                        col = c * Mathf.Lerp(1f - shade, 1f, inner);
                        if (-d < 5f && light > 0.2f) col = Color.Lerp(col, rim ?? Color.white, (light - 0.2f) * 0.6f * (1f - -d / 5f));
                        if (-d < 3f && light < -0.3f) col *= 0.7f;
                        if (d > -ink) col = Color.Lerp(col, inkC, Mathf.Clamp01((d + ink) / 1.2f));
                        col.a = 1f;
                        px[i] = col;
                    }
            }

            /// <summary>Brillo aditivo (ojos, orbes, auras).</summary>
            public void Glow(Vector2 center, float radius, Color c, float intensity = 1f)
            {
                int x0 = Mathf.Max(0, (int)((center.x - radius * 2) * w)), x1 = Mathf.Min(w - 1, (int)((center.x + radius * 2) * w));
                int y0 = Mathf.Max(0, (int)((center.y - radius * 2) * w)), y1 = Mathf.Min(h - 1, (int)((center.y + radius * 2) * w));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        Vector2 p = new Vector2((x + 0.5f) / w, (y + 0.5f) / w);
                        float d = Vector2.Distance(p, center) / radius;
                        float a = Mathf.Clamp01(1f - d * 0.5f);
                        a = a * a * intensity;
                        if (d < 0.5f) a = Mathf.Max(a, intensity);
                        int i = y * w + x;
                        Color dst = px[i];
                        Color add = c * a;
                        px[i] = new Color(Mathf.Min(1, dst.r + add.r), Mathf.Min(1, dst.g + add.g), Mathf.Min(1, dst.b + add.b), Mathf.Max(dst.a, Mathf.Clamp01(a)));
                    }
            }
        }

        // ---------------- primitivas SDF (espacio: x 0..1, y 0..1.25) ----------------
        static Func<Vector2, float> Circle(float x, float y, float r) => p => Vector2.Distance(p, new Vector2(x, y)) - r;
        static Func<Vector2, float> Ellipse(float x, float y, float rx, float ry) => p =>
        {
            Vector2 q = new Vector2((p.x - x) / rx, (p.y - y) / ry);
            float k = q.magnitude;
            return (k - 1f) * Mathf.Min(rx, ry);
        };
        static Func<Vector2, float> Capsule(float ax, float ay, float bx, float by, float r) => p => U.DistToSegment(p, new Vector2(ax, ay), new Vector2(bx, by), out _) - r;
        static Func<Vector2, float> Poly(params float[] xy)
        {
            var pts = new Vector2[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return p =>
            {
                float d = float.MaxValue; bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    d = Mathf.Min(d, U.DistToSegment(p, pts[i], pts[j], out _));
                    if (((pts[i].y > p.y) != (pts[j].y > p.y)) && (p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)) inside = !inside;
                }
                return inside ? -d : d;
            };
        }
        static Func<Vector2, float> Union(params Func<Vector2, float>[] fs) => p => { float d = float.MaxValue; foreach (var f in fs) d = Mathf.Min(d, f(p)); return d; };
        static Func<Vector2, float> Sub(Func<Vector2, float> a, Func<Vector2, float> b) => p => Mathf.Max(a(p), -b(p));

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        static void Shoulders(Painter p, Color c, float width = 0.42f, float top = 0.42f)
            => p.Fill(Union(Ellipse(0.5f, 0.12f, width, top - 0.05f), Capsule(0.5f - width * 0.6f, 0.3f, 0.5f + width * 0.6f, 0.3f, 0.1f)), c, 2.4f, 0.4f);

        static void Eyes(Painter p, float y, float sep, Color c, float r = 0.022f, float x = 0.5f)
        {
            p.Glow(new Vector2(x - sep, y), r, c, 1f);
            p.Glow(new Vector2(x + sep, y), r, c, 1f);
        }

        // ---------------- retratos ----------------
        static void Paint(Painter p, string id)
        {
            switch (id)
            {
                case "sothis_f":
                case "sothis_m":
                    {
                        bool f = id == "sothis_f";
                        p.Background(C(0.18f, 0.08f, 0.3f), C(0.03f, 0.02f, 0.08f), C(0.25f, 0.6f, 0.8f));
                        for (int i = 0; i < 5; i++) p.Glow(new Vector2(0.15f + i * 0.18f, 1.05f - (i % 2) * 0.1f), 0.03f, Color.HSVToRGB(i * 0.2f, 0.5f, 1f), 0.8f);
                        if (f) p.Fill(Union(Ellipse(0.5f, 0.62f, 0.2f, 0.33f), Capsule(0.36f, 0.55f, 0.32f, 0.3f, 0.07f), Capsule(0.64f, 0.55f, 0.68f, 0.3f, 0.07f)), C(0.82f, 0.84f, 0.96f), 2.2f, 0.35f, C(0.7f, 0.95f, 1f));
                        Shoulders(p, C(0.24f, 0.12f, 0.36f), f ? 0.38f : 0.44f);
                        p.Fill(Poly(0.38f, 0.42f, 0.62f, 0.42f, 0.5f, 0.28f), C(0.13f, 0.07f, 0.2f));
                        p.Fill(Capsule(0.5f, 0.44f, 0.5f, 0.5f, 0.05f), C(0.8f, 0.74f, 0.86f));
                        p.Fill(Ellipse(0.5f, 0.66f, 0.13f, 0.16f), C(0.86f, 0.8f, 0.9f), 2.2f, 0.3f, C(0.75f, 0.65f, 1f));
                        // cuernos
                        p.Fill(Poly(0.4f, 0.8f, 0.36f, 0.95f, 0.32f, 0.99f, 0.38f, 0.86f, 0.43f, 0.79f), C(0.12f, 0.08f, 0.16f));
                        p.Fill(Poly(0.6f, 0.8f, 0.64f, 0.95f, 0.68f, 0.99f, 0.62f, 0.86f, 0.57f, 0.79f), C(0.12f, 0.08f, 0.16f));
                        if (f) p.Fill(Union(Ellipse(0.5f, 0.78f, 0.15f, 0.07f), Ellipse(0.42f, 0.74f, 0.07f, 0.06f)), C(0.86f, 0.87f, 0.98f), 2.2f, 0.3f, C(0.7f, 0.95f, 1f));
                        else p.Fill(Union(Ellipse(0.5f, 0.79f, 0.15f, 0.06f), Poly(0.36f, 0.78f, 0.4f, 0.9f, 0.46f, 0.8f, 0.52f, 0.92f, 0.56f, 0.8f, 0.62f, 0.9f, 0.64f, 0.76f)), C(0.86f, 0.87f, 0.98f), 2.2f, 0.3f, C(0.7f, 0.95f, 1f));
                        Eyes(p, 0.66f, 0.05f, C(0.4f, 0.95f, 1f));
                        p.Glow(new Vector2(0.5f, 0.74f), 0.016f, C(1f, 0.45f, 0.95f), 1f);
                        p.Fill(Capsule(0.47f, 0.585f, 0.53f, 0.585f, 0.004f), C(0.5f, 0.3f, 0.45f), 0.8f);
                        break;
                    }
                case "umr":
                    p.Background(C(0.25f, 0.18f, 0.4f), C(0.05f, 0.03f, 0.1f), C(0.6f, 0.5f, 0.9f));
                    p.Fill(Poly(0.12f, 0f, 0.88f, 0f, 0.72f, 0.6f, 0.62f, 0.9f, 0.5f, 0.98f, 0.38f, 0.9f, 0.28f, 0.6f), C(0.3f, 0.22f, 0.45f), 2.4f, 0.45f, C(0.9f, 0.8f, 1f));
                    p.Fill(Ellipse(0.5f, 0.7f, 0.11f, 0.15f), C(0.08f, 0.05f, 0.12f));
                    p.Glow(new Vector2(0.5f, 0.7f), 0.08f, C(0.85f, 0.75f, 1f), 0.9f);
                    for (int i = 0; i < 6; i++) p.Glow(new Vector2(0.5f + Mathf.Cos(i * 1.05f) * 0.32f, 0.65f + Mathf.Sin(i * 1.05f) * 0.25f), 0.03f, Color.HSVToRGB(i / 6f, 0.5f, 1f), 0.9f);
                    break;
                case "carter":
                    p.Background(C(0.12f, 0.18f, 0.3f), C(0.03f, 0.04f, 0.08f), C(0.4f, 0.6f, 0.9f));
                    Shoulders(p, C(0.25f, 0.24f, 0.28f));
                    p.Fill(Poly(0.42f, 0.42f, 0.58f, 0.42f, 0.5f, 0.2f), C(0.9f, 0.9f, 0.88f));
                    p.Fill(Poly(0.48f, 0.4f, 0.52f, 0.4f, 0.53f, 0.25f, 0.5f, 0.2f, 0.47f, 0.25f), C(0.6f, 0.12f, 0.15f));
                    p.Fill(Capsule(0.5f, 0.44f, 0.5f, 0.5f, 0.05f), C(0.88f, 0.76f, 0.66f));
                    p.Fill(Ellipse(0.5f, 0.66f, 0.12f, 0.15f), C(0.9f, 0.78f, 0.68f));
                    p.Fill(Ellipse(0.5f, 0.79f, 0.13f, 0.06f), C(0.82f, 0.82f, 0.85f));
                    p.Fill(Capsule(0.45f, 0.6f, 0.55f, 0.6f, 0.012f), C(0.82f, 0.82f, 0.85f));
                    Eyes(p, 0.67f, 0.045f, C(0.4f, 0.6f, 0.9f), 0.012f);
                    p.Glow(new Vector2(0.72f, 0.22f), 0.03f, C(0.85f, 0.9f, 1f), 1f);
                    break;
                case "keziah":
                    p.Background(C(0.15f, 0.25f, 0.1f), C(0.03f, 0.05f, 0.02f), C(0.5f, 0.9f, 0.3f));
                    Shoulders(p, C(0.18f, 0.2f, 0.12f), 0.36f);
                    p.Fill(Ellipse(0.5f, 0.62f, 0.12f, 0.15f), C(0.75f, 0.72f, 0.58f), 2.2f, 0.45f);
                    p.Fill(Poly(0.5f, 0.62f, 0.56f, 0.52f, 0.5f, 0.56f), C(0.7f, 0.66f, 0.52f));
                    p.Fill(Union(Capsule(0.38f, 0.66f, 0.33f, 0.42f, 0.03f), Capsule(0.62f, 0.66f, 0.67f, 0.42f, 0.03f)), C(0.8f, 0.8f, 0.78f));
                    p.Fill(Union(Poly(0.32f, 0.76f, 0.68f, 0.76f, 0.56f, 1.15f, 0.62f, 1.2f, 0.5f, 1.1f), Ellipse(0.5f, 0.76f, 0.26f, 0.03f)), C(0.1f, 0.08f, 0.1f), 2.4f, 0.3f, C(0.4f, 0.6f, 0.3f));
                    Eyes(p, 0.65f, 0.045f, C(0.6f, 1f, 0.3f), 0.016f);
                    break;
                case "jenkin":
                    p.Background(C(0.3f, 0.2f, 0.1f), C(0.05f, 0.03f, 0.02f), C(0.9f, 0.7f, 0.3f));
                    p.Fill(Ellipse(0.5f, 0.25f, 0.32f, 0.3f), C(0.35f, 0.25f, 0.18f), 2.4f, 0.4f);
                    p.Fill(Union(Circle(0.32f, 0.78f, 0.09f), Circle(0.68f, 0.78f, 0.09f)), C(0.4f, 0.28f, 0.2f));
                    p.Fill(Ellipse(0.5f, 0.6f, 0.17f, 0.17f), C(0.85f, 0.72f, 0.65f), 2.2f, 0.35f);
                    p.Fill(Ellipse(0.5f, 0.52f, 0.06f, 0.04f), C(0.95f, 0.6f, 0.6f));
                    for (int i = -1; i <= 1; i += 2) for (int k = 0; k < 3; k++) p.Fill(Capsule(0.5f + i * 0.08f, 0.52f, 0.5f + i * 0.28f, 0.5f + k * 0.04f, 0.002f), C(0.9f, 0.9f, 0.85f), 0.6f);
                    Eyes(p, 0.63f, 0.065f, C(1f, 0.85f, 0.3f), 0.02f);
                    break;
                case "zadok":
                    p.Background(C(0.15f, 0.2f, 0.25f), C(0.03f, 0.04f, 0.05f), C(0.5f, 0.6f, 0.7f));
                    Shoulders(p, C(0.2f, 0.25f, 0.3f));
                    p.Fill(Ellipse(0.5f, 0.66f, 0.13f, 0.15f), C(0.8f, 0.66f, 0.58f));
                    p.Fill(Union(Ellipse(0.5f, 0.52f, 0.15f, 0.13f), Poly(0.36f, 0.58f, 0.64f, 0.58f, 0.5f, 0.36f)), C(0.92f, 0.92f, 0.9f));
                    p.Fill(Circle(0.5f, 0.62f, 0.025f), C(0.9f, 0.4f, 0.4f));
                    p.Fill(Union(Ellipse(0.5f, 0.8f, 0.16f, 0.05f), Poly(0.36f, 0.8f, 0.64f, 0.8f, 0.6f, 0.9f, 0.4f, 0.9f)), C(0.15f, 0.15f, 0.22f));
                    Eyes(p, 0.68f, 0.045f, C(0.8f, 0.85f, 0.6f), 0.012f);
                    p.Fill(Union(Capsule(0.78f, 0.15f, 0.78f, 0.35f, 0.045f), Capsule(0.78f, 0.35f, 0.78f, 0.43f, 0.018f)), C(0.45f, 0.28f, 0.12f), 2f, 0.3f, C(1f, 0.8f, 0.5f));
                    break;
                case "west":
                    p.Background(C(0.1f, 0.25f, 0.12f), C(0.02f, 0.05f, 0.03f), C(0.4f, 1f, 0.4f));
                    Shoulders(p, C(0.88f, 0.88f, 0.85f));
                    p.Fill(Poly(0.44f, 0.42f, 0.56f, 0.42f, 0.5f, 0.3f), C(0.25f, 0.25f, 0.28f));
                    p.Fill(Capsule(0.5f, 0.44f, 0.5f, 0.5f, 0.045f), C(0.9f, 0.8f, 0.72f));
                    p.Fill(Ellipse(0.5f, 0.66f, 0.12f, 0.15f), C(0.92f, 0.82f, 0.74f));
                    p.Fill(Ellipse(0.5f, 0.79f, 0.13f, 0.06f), C(0.92f, 0.82f, 0.45f));
                    p.Fill(Sub(Circle(0.455f, 0.665f, 0.035f), Circle(0.455f, 0.665f, 0.026f)), C(0.1f, 0.1f, 0.1f), 1f);
                    p.Fill(Sub(Circle(0.545f, 0.665f, 0.035f), Circle(0.545f, 0.665f, 0.026f)), C(0.1f, 0.1f, 0.1f), 1f);
                    Eyes(p, 0.665f, 0.045f, C(0.5f, 0.8f, 0.6f), 0.01f);
                    p.Fill(Capsule(0.74f, 0.18f, 0.8f, 0.34f, 0.025f), C(0.3f, 0.8f, 0.3f));
                    p.Glow(new Vector2(0.77f, 0.26f), 0.05f, C(0.4f, 1f, 0.3f), 0.8f);
                    break;
                case "wilbur":
                case "boss_wilbur":
                    p.Background(C(0.3f, 0.15f, 0.08f), C(0.05f, 0.02f, 0.02f), C(1f, 0.55f, 0.3f));
                    Shoulders(p, C(0.28f, 0.2f, 0.14f), 0.46f);
                    p.Fill(Capsule(0.5f, 0.44f, 0.5f, 0.52f, 0.05f), C(0.62f, 0.58f, 0.48f));
                    p.Fill(Union(Ellipse(0.5f, 0.7f, 0.12f, 0.17f), Ellipse(0.5f, 0.57f, 0.08f, 0.08f)), C(0.65f, 0.6f, 0.5f));
                    p.Fill(Poly(0.42f, 0.83f, 0.36f, 0.98f, 0.45f, 0.86f), C(0.15f, 0.12f, 0.1f));
                    p.Fill(Poly(0.58f, 0.83f, 0.64f, 0.98f, 0.55f, 0.86f), C(0.15f, 0.12f, 0.1f));
                    Eyes(p, 0.72f, 0.045f, id == "boss_wilbur" ? C(1f, 0.3f, 0.2f) : C(1f, 0.85f, 0.4f), 0.016f);
                    for (int i = 0; i < 4; i++) p.Fill(Capsule(0.3f + i * 0.13f, 0.05f, 0.25f + i * 0.15f, 0.25f, 0.025f), C(0.3f, 0.4f, 0.3f));
                    break;
                case "cthulhu":
                    p.Background(C(0.05f, 0.25f, 0.25f), C(0.0f, 0.04f, 0.05f), C(0.2f, 0.9f, 0.8f));
                    p.Fill(Union(Poly(0.05f, 0.5f, 0.35f, 0.75f, 0.3f, 0.95f), Poly(0.95f, 0.5f, 0.65f, 0.75f, 0.7f, 0.95f)), C(0.1f, 0.3f, 0.28f));
                    Shoulders(p, C(0.15f, 0.42f, 0.38f), 0.46f);
                    p.Fill(Ellipse(0.5f, 0.72f, 0.2f, 0.24f), C(0.2f, 0.5f, 0.45f), 2.4f, 0.45f, C(0.5f, 1f, 0.9f));
                    for (int i = 0; i < 6; i++) p.Fill(Capsule(0.38f + i * 0.048f, 0.58f, 0.36f + i * 0.055f + Mathf.Sin(i) * 0.03f, 0.3f, 0.02f), C(0.18f, 0.45f, 0.4f), 1.8f, 0.3f, C(0.5f, 1f, 0.9f));
                    Eyes(p, 0.74f, 0.08f, C(1f, 0.3f, 0.2f), 0.02f);
                    break;
                case "hastur":
                    p.Background(C(0.35f, 0.28f, 0.05f), C(0.05f, 0.04f, 0.0f), C(1f, 0.85f, 0.2f));
                    p.Fill(Poly(0.08f, 0f, 0.92f, 0f, 0.75f, 0.55f, 0.65f, 0.95f, 0.5f, 1.05f, 0.35f, 0.95f, 0.25f, 0.55f), C(0.95f, 0.8f, 0.2f), 2.4f, 0.5f, C(1f, 1f, 0.7f));
                    p.Fill(Ellipse(0.5f, 0.72f, 0.11f, 0.15f), C(0.96f, 0.94f, 0.82f), 2.2f, 0.25f);
                    p.Fill(Union(Capsule(0.5f, 0.3f, 0.5f, 0.48f, 0.012f), Capsule(0.42f, 0.4f, 0.58f, 0.36f, 0.012f), Capsule(0.44f, 0.3f, 0.56f, 0.46f, 0.012f)), C(1f, 0.9f, 0.3f), 1f);
                    p.Glow(new Vector2(0.5f, 0.38f), 0.08f, C(1f, 0.85f, 0.2f), 0.6f);
                    Eyes(p, 0.74f, 0.04f, C(0.1f, 0.08f, 0.0f), 0.012f);
                    break;
                case "shub":
                    p.Background(C(0.1f, 0.2f, 0.05f), C(0.02f, 0.03f, 0.01f), C(0.45f, 0.9f, 0.25f));
                    p.Fill(Union(Ellipse(0.5f, 0.45f, 0.38f, 0.4f), Circle(0.3f, 0.75f, 0.15f), Circle(0.7f, 0.72f, 0.16f)), C(0.06f, 0.08f, 0.04f), 2.4f, 0.4f, C(0.4f, 0.8f, 0.3f));
                    for (int i = 0; i < 7; i++) p.Glow(new Vector2(0.3f + (i * 37 % 40) / 100f, 0.3f + (i * 53 % 50) / 100f), 0.02f, C(0.5f, 1f, 0.3f), 1f);
                    for (int i = 0; i < 3; i++) p.Fill(Ellipse(0.38f + i * 0.12f, 0.35f + (i % 2) * 0.12f, 0.04f, 0.02f), C(0.6f, 0.1f, 0.15f), 1f);
                    p.Fill(Union(Poly(0.32f, 0.85f, 0.18f, 1.1f, 0.3f, 0.9f), Poly(0.68f, 0.85f, 0.82f, 1.1f, 0.7f, 0.9f)), C(0.15f, 0.12f, 0.1f));
                    break;
                case "nodens":
                    p.Background(C(0.15f, 0.12f, 0.35f), C(0.02f, 0.02f, 0.06f), C(0.7f, 0.6f, 1f));
                    Shoulders(p, C(0.3f, 0.3f, 0.45f));
                    p.Fill(Ellipse(0.5f, 0.68f, 0.12f, 0.15f), C(0.88f, 0.84f, 0.8f));
                    p.Fill(Union(Ellipse(0.5f, 0.5f, 0.16f, 0.16f), Poly(0.36f, 0.6f, 0.64f, 0.6f, 0.5f, 0.25f)), C(0.88f, 0.9f, 0.95f), 2.2f, 0.3f);
                    p.Fill(Ellipse(0.5f, 0.8f, 0.15f, 0.07f), C(0.88f, 0.9f, 0.95f));
                    Eyes(p, 0.69f, 0.045f, C(0.75f, 0.65f, 1f), 0.016f);
                    p.Fill(Union(Capsule(0.8f, 0.05f, 0.8f, 0.95f, 0.012f), Capsule(0.74f, 0.95f, 0.74f, 1.05f, 0.01f), Capsule(0.86f, 0.95f, 0.86f, 1.05f, 0.01f), Capsule(0.74f, 0.95f, 0.86f, 0.95f, 0.01f)), C(0.85f, 0.85f, 0.95f), 1.5f);
                    p.Glow(new Vector2(0.8f, 1.02f), 0.06f, C(0.75f, 0.65f, 1f), 0.8f);
                    break;
                case "ithaqua":
                    p.Background(C(0.35f, 0.5f, 0.6f), C(0.05f, 0.08f, 0.12f), C(0.8f, 0.95f, 1f));
                    p.Fill(Union(Ellipse(0.5f, 0.35f, 0.3f, 0.45f), Ellipse(0.5f, 0.8f, 0.14f, 0.18f)), C(0.75f, 0.85f, 0.92f), 2.4f, 0.45f, C(1f, 1f, 1f));
                    Eyes(p, 0.82f, 0.05f, C(1f, 0.15f, 0.1f), 0.022f);
                    for (int i = 0; i < 10; i++) p.Glow(new Vector2((i * 0.11f) % 1f, 0.2f + (i * 0.13f) % 1f), 0.008f, Color.white, 1f);
                    break;
                case "cthugha":
                    p.Background(C(0.4f, 0.12f, 0.02f), C(0.06f, 0.01f, 0.0f), C(1f, 0.5f, 0.1f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 1.05f;
                        p.Fill(Poly(0.5f + Mathf.Cos(a) * 0.12f, 0.55f + Mathf.Sin(a) * 0.12f, 0.5f + Mathf.Cos(a + 0.4f) * 0.38f, 0.55f + Mathf.Sin(a + 0.4f) * 0.38f, 0.5f + Mathf.Cos(a + 0.5f) * 0.12f, 0.55f + Mathf.Sin(a + 0.5f) * 0.12f), C(1f, 0.45f, 0.08f), 1.5f, 0.2f, C(1f, 0.9f, 0.5f));
                    }
                    p.Fill(Circle(0.5f, 0.55f, 0.17f), C(1f, 0.7f, 0.2f), 2f, 0.25f, C(1f, 1f, 0.8f));
                    p.Glow(new Vector2(0.5f, 0.55f), 0.18f, C(1f, 0.8f, 0.3f), 0.9f);
                    break;
                case "azathoth":
                    p.Background(C(0.3f, 0.02f, 0.25f), C(0.03f, 0.0f, 0.03f), C(1f, 0.3f, 0.8f));
                    for (int i = 0; i < 9; i++) p.Fill(Circle(0.5f + Mathf.Cos(i * 2.4f) * 0.2f, 0.6f + Mathf.Sin(i * 2.4f) * 0.25f, 0.08f + (i % 3) * 0.03f), Color.HSVToRGB(0.8f + i * 0.03f, 0.7f, 0.6f), 2f, 0.4f);
                    for (int i = 0; i < 5; i++) p.Glow(new Vector2(0.4f + i * 0.05f, 0.5f + (i % 2) * 0.15f), 0.02f, C(1f, 0.9f, 0.3f), 1f);
                    break;
                case "gatos":
                    p.Background(C(0.15f, 0.3f, 0.1f), C(0.02f, 0.04f, 0.02f), C(0.6f, 1f, 0.4f));
                    p.Fill(Union(Ellipse(0.5f, 0.28f, 0.3f, 0.28f), Ellipse(0.5f, 0.66f, 0.2f, 0.17f), Poly(0.33f, 0.72f, 0.36f, 0.92f, 0.45f, 0.79f), Poly(0.67f, 0.72f, 0.64f, 0.92f, 0.55f, 0.79f)), C(0.07f, 0.06f, 0.09f), 2.4f, 0.3f, C(0.5f, 0.5f, 0.7f));
                    Eyes(p, 0.67f, 0.07f, C(0.6f, 1f, 0.3f), 0.025f);
                    break;
                case "yog":
                    p.Background(C(0.1f, 0.1f, 0.2f), C(0.0f, 0.0f, 0.03f), C(0.8f, 0.8f, 1f));
                    for (int i = 0; i < 14; i++) p.Glow(new Vector2(0.5f + Mathf.Cos(i * 2.4f) * (0.1f + i * 0.018f), 0.62f + Mathf.Sin(i * 2.4f) * (0.1f + i * 0.02f)), 0.03f + (i % 3) * 0.015f, Color.HSVToRGB(i / 14f, 0.5f, 1f), 1f);
                    break;
                case "boss_pickman":
                    p.Background(C(0.3f, 0.15f, 0.08f), C(0.04f, 0.02f, 0.01f), C(1f, 0.55f, 0.3f));
                    Shoulders(p, C(0.82f, 0.78f, 0.68f), 0.44f);
                    p.Fill(Union(Ellipse(0.5f, 0.68f, 0.15f, 0.15f), Ellipse(0.5f, 0.58f, 0.1f, 0.08f)), C(0.45f, 0.52f, 0.42f), 2.2f, 0.45f);
                    p.Fill(Union(Poly(0.38f, 0.76f, 0.32f, 0.9f, 0.42f, 0.8f), Poly(0.62f, 0.76f, 0.68f, 0.9f, 0.58f, 0.8f)), C(0.45f, 0.52f, 0.42f));
                    p.Fill(Ellipse(0.52f, 0.82f, 0.17f, 0.05f), C(0.5f, 0.1f, 0.15f));
                    Eyes(p, 0.7f, 0.05f, C(1f, 0.85f, 0.2f), 0.016f);
                    for (int i = 0; i < 5; i++) p.Glow(new Vector2(0.3f + i * 0.1f, 0.15f + (i % 2) * 0.08f), 0.02f, Color.HSVToRGB(i * 0.2f, 0.8f, 1f), 0.8f);
                    break;
                case "boss_dagon":
                    p.Background(C(0.05f, 0.25f, 0.2f), C(0.0f, 0.03f, 0.03f), C(0.3f, 1f, 0.8f));
                    Shoulders(p, C(0.25f, 0.45f, 0.4f), 0.48f);
                    p.Fill(Ellipse(0.5f, 0.68f, 0.2f, 0.17f), C(0.25f, 0.45f, 0.4f), 2.4f, 0.45f);
                    p.Fill(Ellipse(0.5f, 0.6f, 0.13f, 0.03f), C(0.05f, 0.08f, 0.08f));
                    Eyes(p, 0.73f, 0.11f, C(0.9f, 1f, 0.6f), 0.03f);
                    for (int i = 0; i < 5; i++) p.Fill(Poly(0.38f + i * 0.06f, 0.84f, 0.4f + i * 0.06f, 0.97f, 0.42f + i * 0.06f, 0.84f), C(0.9f, 0.45f, 0.4f));
                    break;
                case "boss_hydra":
                    p.Background(C(0.1f, 0.3f, 0.32f), C(0.0f, 0.04f, 0.05f), C(0.5f, 1f, 1f));
                    Shoulders(p, C(0.45f, 0.6f, 0.55f), 0.38f);
                    p.Fill(Ellipse(0.5f, 0.68f, 0.15f, 0.16f), C(0.48f, 0.62f, 0.58f), 2.2f, 0.4f);
                    for (int i = 0; i < 5; i++) p.Fill(Poly(0.5f, 0.78f, 0.25f + i * 0.12f, 1.05f, 0.32f + i * 0.12f, 1.02f), C(0.3f, 0.7f, 0.7f), 1.8f, 0.3f);
                    Eyes(p, 0.7f, 0.06f, C(0.6f, 1f, 1f), 0.02f);
                    p.Glow(new Vector2(0.5f, 0.32f), 0.04f, C(0.4f, 1f, 0.9f), 0.9f);
                    break;
                case "boss_sacerdote":
                    p.Background(C(0.3f, 0.22f, 0.02f), C(0.04f, 0.03f, 0.0f), C(1f, 0.85f, 0.25f));
                    p.Fill(Poly(0.1f, 0f, 0.9f, 0f, 0.72f, 0.6f, 0.6f, 0.95f, 0.5f, 1f, 0.4f, 0.95f, 0.28f, 0.6f), C(0.9f, 0.75f, 0.15f), 2.4f, 0.5f);
                    p.Fill(Ellipse(0.5f, 0.7f, 0.12f, 0.17f), C(1f, 0.95f, 0.75f), 2.2f, 0.2f);
                    p.Glow(new Vector2(0.5f, 0.7f), 0.04f, C(1f, 0.85f, 0.2f), 0.5f);
                    break;
                case "boss_nyarlathotep":
                    p.Background(C(0.25f, 0.05f, 0.05f), C(0.02f, 0.0f, 0.0f), C(1f, 0.3f, 0.2f));
                    p.Fill(Union(Poly(0.26f, 0.45f, 0.74f, 0.45f, 0.66f, 0.9f, 0.5f, 0.95f, 0.34f, 0.9f), Ellipse(0.5f, 0.15f, 0.42f, 0.25f)), C(0.12f, 0.22f, 0.5f), 2.4f, 0.4f, C(0.9f, 0.75f, 0.3f));
                    for (int i = 0; i < 4; i++) p.Fill(Capsule(0.3f + i * 0.02f, 0.5f + i * 0.1f, 0.7f - i * 0.02f, 0.5f + i * 0.1f, 0.012f), C(0.95f, 0.75f, 0.3f), 1f);
                    p.Fill(Ellipse(0.5f, 0.66f, 0.11f, 0.15f), C(0.06f, 0.05f, 0.07f), 2.2f, 0.3f, C(0.8f, 0.3f, 0.2f));
                    Eyes(p, 0.68f, 0.045f, C(1f, 0.4f, 0.1f), 0.02f);
                    p.Fill(Capsule(0.46f, 0.58f, 0.54f, 0.58f, 0.006f), C(0.8f, 0.2f, 0.2f), 0.8f);
                    break;
                default:
                    p.Background(C(0.2f, 0.2f, 0.25f), C(0.03f, 0.03f, 0.04f), C(0.6f, 0.6f, 0.7f));
                    Shoulders(p, C(0.3f, 0.3f, 0.35f));
                    p.Fill(Ellipse(0.5f, 0.66f, 0.13f, 0.15f), C(0.7f, 0.68f, 0.7f));
                    Eyes(p, 0.67f, 0.045f, Color.white, 0.014f);
                    break;
            }
        }
    }
}
