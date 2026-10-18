using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Texturas procedurales: brillos, anillos, chispas, suelos pintados y runas.</summary>
    public static class Tex
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static Texture2D Make(string name, int w, int h, System.Func<float, float, Color> f, bool clamp = true)
        {
            if (cache.TryGetValue(name, out var t) && t != null) return t;
            t = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
            t.SetPixels(px);
            t.Apply(true);
            cache[name] = t;
            return t;
        }

        public static Texture2D SoftCircle => Make("softcircle", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float a = Mathf.Clamp01(1f - d);
            return new Color(1, 1, 1, a * a);
        });

        public static Texture2D HardCircle => Make("hardcircle", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            return new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 20f));
        });

        public static Texture2D Ring => Make("ring", 128, 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) * 14f);
            return new Color(1, 1, 1, a);
        });

        public static Texture2D Spark => Make("spark", 64, 64, (u, v) =>
        {
            float x = Mathf.Abs(u - 0.5f) * 2f, y = Mathf.Abs(v - 0.5f) * 2f;
            float a = Mathf.Clamp01(1f - (x * 0.25f + y)) * Mathf.Clamp01(1f - x);
            return new Color(1, 1, 1, a * a);
        });

        public static Texture2D Star4 => Make("star4", 64, 64, (u, v) =>
        {
            float x = Mathf.Abs(u - 0.5f) * 2f, y = Mathf.Abs(v - 0.5f) * 2f;
            float a = Mathf.Clamp01(1f - x * 6f * y - (x + y) * 0.6f);
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2f);
            return new Color(1, 1, 1, Mathf.Clamp01(a + core));
        });

        /// <summary>Gradiente horizontal para arcos de tajo: denso en el borde exterior.</summary>
        public static Texture2D SlashGradient => Make("slash", 64, 32, (u, v) =>
        {
            float a = Mathf.Pow(v, 2.2f) * Mathf.Sin(u * Mathf.PI);
            return new Color(1, 1, 1, Mathf.Clamp01(a * 1.4f));
        });

        /// <summary>Llenado de aviso (telegraph) con borde marcado.</summary>
        public static Texture2D TelegraphFill => Make("telefill", 64, 64, (u, v) => new Color(1, 1, 1, 1));

        public static Texture2D Smoke => Make("smoke", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float n = Mathf.PerlinNoise(u * 5f, v * 5f);
            float a = Mathf.Clamp01(1f - d) * (0.5f + 0.5f * n);
            return new Color(1, 1, 1, a * a);
        });

        public static float Fbm(float x, float y, int oct = 4)
        {
            float v = 0, a = 0.5f;
            for (int i = 0; i < oct; i++) { v += a * Mathf.PerlinNoise(x, y); x = x * 2.03f + 11.3f; y = y * 2.03f + 7.7f; a *= 0.5f; }
            return v;
        }

        /// <summary>
        /// Suelo pintado: losas irregulares con grietas, manchas y variación de color,
        /// en la paleta del bioma. Se usa en mosaico.
        /// </summary>
        public static Texture2D Floor(string key, Color a, Color b, Color crack, float tileCount = 4f, int seed = 0, bool organic = false)
        {
            string name = "floor_" + key;
            if (cache.TryGetValue(name, out var t) && t != null) return t;
            int size = 256;
            var rng = new System.Random(seed);
            // centros de celdas Voronoi para losas
            int cells = Mathf.RoundToInt(tileCount * tileCount * 1.4f);
            var pts = new Vector2[cells];
            var tints = new float[cells];
            for (int i = 0; i < cells; i++) { pts[i] = new Vector2(rng.Range(0, 1), rng.Range(0, 1)); tints[i] = rng.Range(0, 1); }
            float ox = rng.Range(0, 100), oy = rng.Range(0, 100);
            return Make(name, size, size, (u, v) =>
            {
                float d1 = 9, d2 = 9; int best = 0;
                for (int i = 0; i < cells; i++)
                {
                    for (int sx = -1; sx <= 1; sx++)
                        for (int sy = -1; sy <= 1; sy++)
                        {
                            float dx = u - (pts[i].x + sx), dy = v - (pts[i].y + sy);
                            float d = dx * dx + dy * dy;
                            if (d < d1) { d2 = d1; d1 = d; best = i; }
                            else if (d < d2) d2 = d;
                        }
                }
                float edge = Mathf.Sqrt(d2) - Mathf.Sqrt(d1);
                float n = Fbm(u * 8 + ox, v * 8 + oy);
                float n2 = Fbm(u * 30 + oy, v * 30 + ox, 2);
                Color c = Color.Lerp(a, b, Mathf.Clamp01(tints[best] * 0.7f + n * 0.6f - 0.15f));
                c *= 0.85f + n2 * 0.3f;
                float groove = organic ? 0.035f : 0.02f;
                if (edge < groove) c = Color.Lerp(crack, c, edge / groove * 0.6f);
                // pequeñas grietas
                float cr = Mathf.Abs(Fbm(u * 6 + 3, v * 6 + 9) - 0.5f);
                if (cr < 0.012f) c = Color.Lerp(crack, c, cr / 0.012f);
                c.a = 1;
                return c;
            }, false);
        }

        /// <summary>Mapa de emisión con runas/venas brillantes dispersas (se tiñe con el color de emisión).</summary>
        public static Texture2D RuneVeins(string key, int seed, float density = 0.5f)
        {
            string name = "veins_" + key;
            if (cache.TryGetValue(name, out var t) && t != null) return t;
            var rng = new System.Random(seed);
            float ox = rng.Range(0, 50), oy = rng.Range(0, 50);
            return Make(name, 256, 256, (u, v) =>
            {
                float n = Fbm(u * 4 + ox, v * 4 + oy);
                float vein = Mathf.Abs(n - 0.5f);
                float a = Mathf.Clamp01(1f - vein * 40f) * Mathf.Clamp01((Fbm(u * 2 + oy, v * 2 + ox) - (1f - density)) * 4f);
                return new Color(a, a, a, 1);
            }, false);
        }

        /// <summary>Glifo de reloj/ojo para la textura de la Puerta y del Signo Arcano.</summary>
        public static Texture2D ElderSign => Make("eldersign", 256, 256, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            float r = p.magnitude;
            float ang = Mathf.Atan2(p.y, p.x);
            float a = 0;
            // anillo exterior
            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.92f) * 40f);
            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) * 60f) * 0.7f;
            // estrella de cinco puntas
            for (int i = 0; i < 5; i++)
            {
                float a0 = Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                float a1 = Mathf.PI / 2 + (i + 2) * Mathf.PI * 2 / 5;
                Vector2 s = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 0.8f;
                Vector2 e = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 0.8f;
                float d = U.DistToSegment(p, s, e, out _);
                a += Mathf.Clamp01(1f - d * 45f);
            }
            // el ojo llameante del centro
            Vector2 q = new Vector2(p.x, p.y * 2.2f);
            float eye = Mathf.Clamp01(1f - Mathf.Abs(q.magnitude - 0.22f) * 40f);
            a += eye + Mathf.Clamp01(1f - r * 14f);
            // runas en el anillo
            float runes = (Mathf.Sin(ang * 24f) > 0.6f ? 1 : 0) * Mathf.Clamp01(1f - Mathf.Abs(r - 0.87f) * 60f);
            a += runes * 0.8f;
            return new Color(1, 1, 1, Mathf.Clamp01(a));
        });

        /// <summary>Remolino para portales.</summary>
        public static Texture2D Swirl => Make("swirl", 128, 128, (u, v) =>
        {
            Vector2 p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            float r = p.magnitude;
            float ang = Mathf.Atan2(p.y, p.x);
            float s = Mathf.Sin(ang * 3f + r * 9f) * 0.5f + 0.5f;
            float a = Mathf.Clamp01(1f - r) * (0.35f + 0.65f * s);
            return new Color(1, 1, 1, a);
        });

        /// <summary>Gradiente vertical para columnas de luz.</summary>
        public static Texture2D BeamGradient => Make("beam", 32, 64, (u, v) =>
        {
            float a = Mathf.Sin(u * Mathf.PI) * (1f - v);
            return new Color(1, 1, 1, a * a);
        });
    }
}
