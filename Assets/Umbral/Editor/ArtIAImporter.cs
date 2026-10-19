using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Importa automáticamente las imágenes de Assets/Umbral/Resources/ArtIA:
    ///  · retratos, personajes e iconos: elimina el fondo plano (magenta) conectado a los bordes,
    ///    suaviza el contorno y quita el halo de color.
    ///  · suelos: textura repetible con mipmaps.
    ///  · fondos: imagen completa sin recortar.
    /// </summary>
    public class ArtIAImporter : AssetPostprocessor
    {
        static bool IsArt(string path) => path.Replace('\\', '/').Contains("/Resources/ArtIA/");
        static string Folder(string path)
        {
            var p = path.Replace('\\', '/');
            int i = p.IndexOf("/Resources/ArtIA/");
            var rest = p.Substring(i + "/Resources/ArtIA/".Length);
            int s = rest.IndexOf('/');
            return s > 0 ? rest.Substring(0, s) : "";
        }
        static bool NeedsKey(string folder) => folder == "retratos" || folder == "personajes" || folder == "iconos";

        void OnPreprocessTexture()
        {
            if (!IsArt(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            string folder = Folder(assetPath);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = NeedsKey(folder);
            ti.mipmapEnabled = folder == "suelos" || folder == "personajes";
            ti.wrapMode = folder == "suelos" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.isReadable = folder == "personajes";
            ti.maxTextureSize = folder == "fondos" ? 2048 : 1024;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.filterMode = FilterMode.Bilinear;
            if (NeedsKey(folder))
            {
                // las imágenes suelen venir sin canal alfa: forzar un formato con alfa para conservar el recorte
                foreach (var platform in new[] { "Standalone", "DefaultTexturePlatform" })
                {
                    var ps = ti.GetPlatformTextureSettings(platform);
                    ps.overridden = platform == "Standalone";
                    ps.format = platform == "Standalone" ? TextureImporterFormat.BC7 : TextureImporterFormat.Automatic;
                    ps.maxTextureSize = ti.maxTextureSize;
                    ps.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.SetPlatformTextureSettings(ps);
                }
                // crea siempre un canal alfa (aunque la PNG no lo tenga); el recorte lo rellena después
                ti.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            }
        }

        void OnPostprocessTexture(Texture2D tex)
        {
            if (!IsArt(assetPath) || !NeedsKey(Folder(assetPath))) return;
            RemoveBackground(tex);
        }

        /// <summary>Relleno por inundación desde los bordes: borra el color de fondo y su halo.</summary>
        public static void RemoveBackground(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels();
            Color bg = BorderMode(px, w, h);
            const float tol = 0.32f;
            var removed = new bool[w * h];
            var q = new Queue<int>();
            void Seed(int x, int y) { int i = y * w + x; if (!removed[i] && Dist(px[i], bg) < tol) { removed[i] = true; q.Enqueue(i); } }
            for (int x = 0; x < w; x++) { Seed(x, 0); Seed(x, h - 1); }
            for (int y = 0; y < h; y++) { Seed(0, y); Seed(w - 1, y); }
            // huecos cerrados (entre alas, brazos...): el color casi exacto del fondo también se elimina
            var key = new Color(1f, 0f, 1f);
            for (int i = 0; i < px.Length; i++)
                if (!removed[i] && (Dist(px[i], bg) < 0.2f || Dist(px[i], key) < 0.3f)) { removed[i] = true; q.Enqueue(i); }
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int x = i % w, y = i / w;
                if (x > 0) Seed(x - 1, y);
                if (x < w - 1) Seed(x + 1, y);
                if (y > 0) Seed(x, y - 1);
                if (y < h - 1) Seed(x, y + 1);
            }
            for (int i = 0; i < px.Length; i++)
            {
                if (removed[i]) { px[i] = new Color(0, 0, 0, 0); continue; }
                int x = i % w, y = i / w;
                bool edge = (x > 0 && removed[i - 1]) || (x < w - 1 && removed[i + 1]) || (y > 0 && removed[i - w]) || (y < h - 1 && removed[i + w]);
                if (!edge) { px[i].a = 1f; continue; }
                // borde: semitransparente según parecido al fondo y sin tinte del fondo
                var c = px[i];
                float d = Dist(c, bg);
                float a = Mathf.Clamp01((d - tol * 0.6f) / (tol * 1.2f));
                c = Despill(c, bg);
                c.a = Mathf.Max(0.5f, a);
                px[i] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true);
        }

        /// <summary>Color de fondo = el más frecuente del borde de la imagen (aunque la figura toque los bordes).</summary>
        static Color BorderMode(Color[] px, int w, int h)
        {
            var buckets = new Dictionary<int, (int n, Color sum)>();
            void Add(Color c)
            {
                int k = (Mathf.RoundToInt(c.r * 15) << 8) | (Mathf.RoundToInt(c.g * 15) << 4) | Mathf.RoundToInt(c.b * 15);
                buckets.TryGetValue(k, out var e);
                buckets[k] = (e.n + 1, e.sum + c);
            }
            for (int x = 0; x < w; x += 2) { Add(px[x]); Add(px[(h - 1) * w + x]); }
            for (int y = 0; y < h; y += 2) { Add(px[y * w]); Add(px[y * w + w - 1]); }
            var best = default((int n, Color sum));
            foreach (var e in buckets.Values) if (e.n > best.n) best = e;
            return best.n > 0 ? best.sum / best.n : Color.magenta;
        }

        static float Dist(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        static Color Despill(Color c, Color bg)
        {
            // si el fondo es magenta (r y b altos), limitar r y b al nivel de g + un margen
            if (bg.r > 0.6f && bg.b > 0.6f && bg.g < 0.4f)
            {
                float lim = c.g + 0.15f;
                c.r = Mathf.Min(c.r, Mathf.Max(lim, c.r * 0.7f));
                c.b = Mathf.Min(c.b, Mathf.Max(lim, c.b * 0.7f));
            }
            else if (bg.g > 0.6f && bg.r < 0.4f && bg.b < 0.4f)
                c.g = Mathf.Min(c.g, Mathf.Max(c.r, c.b) + 0.1f);
            return c;
        }

        /// <summary>Informe de formatos (modo batch): comprueba que las imágenes recortadas conservan el alfa.</summary>
        public static void Report()
        {
            AssetDatabase.ImportAsset("Assets/Umbral/Resources/ArtIA", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Umbral/Resources/ArtIA" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(t, rt);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var probe = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
                probe.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0); probe.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                float corner = probe.GetPixel(3, 3).a, center = probe.GetPixel(t.width / 2, (int)(t.height * 0.6f)).a;
                Debug.Log($"[ArtIA] {path} -> {t.format} {t.width}x{t.height} alfa esquina={corner:F2} centro={center:F2}");
                var dump = System.Environment.GetEnvironmentVariable("UMBRAL_ART_DUMP");
                if (!string.IsNullOrEmpty(dump) && !path.Contains("/fondos/") && !path.Contains("/suelos/"))
                {
                    System.IO.Directory.CreateDirectory(dump);
                    var folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dump, folder + "_" + System.IO.Path.GetFileNameWithoutExtension(path) + ".png"), probe.EncodeToPNG());
                }
            }
            EditorApplication.Exit(0);
        }

        [MenuItem("Umbral/Reimportar arte IA", priority = 30)]
        static void Reimport()
        {
            AssetDatabase.ImportAsset("Assets/Umbral/Resources/ArtIA", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[Umbral] Arte IA reimportado.");
        }
    }
}
