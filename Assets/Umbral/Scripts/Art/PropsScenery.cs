using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Escenografía al estilo de Hades II: siluetas oscuras en primer plano que enmarcan la pantalla
    /// y detalles pintados sobre el suelo que rompen la repetición de la textura.
    /// Usa las imágenes de ArtIA/decorado si existen (biome_frente_N, biome_suelo_N); si no, formas procedurales.
    /// </summary>
    public static partial class Props
    {
        static readonly Dictionary<string, List<Texture2D>> decorCache = new Dictionary<string, List<Texture2D>>();

        static List<Texture2D> Decor(string biome, string kind)
        {
            string key = biome + "_" + kind;
            if (decorCache.TryGetValue(key, out var l)) return l;
            l = new List<Texture2D>();
            for (int i = 1; i <= 8; i++)
            {
                var t = ArtIA.Tex("decorado", key + "_" + i);
                if (t == null) break;
                l.Add(t);
            }
            decorCache[key] = l;
            return l;
        }

        /// <summary>Silueta grande y oscura entre la cámara y el suelo (borde inferior y laterales).</summary>
        public static void Foreground(Transform parent, BiomeDef b, Vector2 at, System.Random rng)
        {
            var root = Kit.Empty(parent, "PrimerPlano", at.X0Z(rng.Range(-0.5f, 1.5f)));
            var painted = Decor(b.id, "frente");
            Color dark = Color.Lerp(b.voidA, Color.black, 0.5f);
            if (painted.Count > 0)
            {
                var tex = painted[rng.Next(painted.Count)];
                float h = rng.Range(5f, 8f);
                float aspect = (float)tex.width / tex.height;
                var q = Kit.Part(root, MeshGen.Quad, Mats.Unlit(new Color(0.35f, 0.32f, 0.42f), Mats.Blend.Alpha, tex, 1f, false), new Vector3(0, h * 0.4f, 0), new Vector3(h * aspect * (rng.NextDouble() < 0.5 ? 1 : -1), h, 1), default, "silueta", false);
                q.gameObject.AddComponent<Billboard>();
                q.gameObject.AddComponent<Sway>().amount = 2.5f;
                return;
            }
            // procedural: ramas y tentáculos casi negros, con un borde de luz del color de la región
            var mat = Mats.Toon(dark, null, 1.5f, b.rim * 0.5f);
            int n = rng.Next(2, 5);
            for (int i = 0; i < n; i++)
            {
                var piv = Kit.Empty(root, "rama", new Vector3(rng.Range(-1.5f, 1.5f), -0.5f, rng.Range(-1f, 1f)), new Vector3(rng.Range(-35f, 35f), rng.Range(0, 360f), rng.Range(-35f, 35f)));
                Kit.Part(piv, MeshGen.Tentacle(10, 7, 0.32f, rng.Range(0.6f, 1.4f)), mat, Vector3.zero, new Vector3(2.2f, rng.Range(4f, 7f), 2.2f), default, "rama", false);
                piv.gameObject.AddComponent<Sway>().amount = 3f;
            }
            if (b.id == "bosque" || b.id == "umbral")
                for (int i = 0; i < 3; i++)
                    Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.4f, 1), mat, new Vector3(rng.Range(-2f, 2f), rng.Range(2f, 4f), rng.Range(-1f, 1f)), Vector3.one * rng.Range(1.2f, 2.2f), default, "follaje", false);
        }

        /// <summary>Detalle plano pintado en el suelo: musgo, grietas, círculos tallados, charcos, hojas.</summary>
        public static void GroundDecal(Transform parent, BiomeDef b, Vector2 at, System.Random rng)
        {
            var painted = Decor(b.id, "suelo");
            if (painted.Count > 0 && rng.NextDouble() < 0.75)
            {
                var tex = painted[rng.Next(painted.Count)];
                float s = rng.Range(2.2f, 4.5f);
                float aspect = (float)tex.width / tex.height;
                var go = Fx.MeshObj("DetalleSuelo", MeshGen.Quad, Mats.Unlit(new Color(0.85f, 0.85f, 0.85f, 1f), Mats.Blend.Alpha, tex, 1f, false), at.X0Z(0.03f + (float)rng.NextDouble() * 0.01f), Quaternion.Euler(90, rng.Range(0, 360f), 0), new Vector3(s * aspect, s, 1));
                go.transform.SetParent(parent, true);
                return;
            }
            int pick = rng.Next(0, 3);
            Material mat; Vector3 scale; Texture2D t;
            switch (pick)
            {
                case 0: // mancha de musgo / sombra de color
                    t = Tex.SoftCircle;
                    mat = Mats.Unlit(Color.Lerp(b.floorA, b.veins, 0.35f).WithA(0.35f), Mats.Blend.Alpha, t, 1f, false);
                    scale = new Vector3(rng.Range(2f, 4f), rng.Range(1.5f, 3f), 1);
                    break;
                case 1: // salpicadura de tinta antigua
                    t = Tex.InkSplat;
                    mat = Mats.Unlit(new Color(0.02f, 0.01f, 0.03f, 0.45f), Mats.Blend.Alpha, t, 1f, false);
                    scale = Vector3.one * rng.Range(1.2f, 2.4f);
                    break;
                default: // círculo tallado tenue
                    t = Tex.ElderSign;
                    mat = Mats.Unlit(b.veins.WithA(0.18f), Mats.Blend.Additive, t, 1f, false);
                    scale = Vector3.one * rng.Range(2.2f, 3.4f);
                    break;
            }
            var g = Fx.MeshObj("DetalleSuelo", MeshGen.Quad, mat, at.X0Z(0.03f), Quaternion.Euler(90, rng.Range(0, 360f), 0), scale);
            g.transform.SetParent(parent, true);
        }
    }
}
