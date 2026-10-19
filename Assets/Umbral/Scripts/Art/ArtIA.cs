using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Arte pintado opcional (por ejemplo, generado con Nano Banana Pro).
    /// Si existe una imagen en Resources/ArtIA/&lt;carpeta&gt;/&lt;id&gt; se usa en lugar del arte procedural;
    /// si no existe, el juego sigue usando el arte generado por código. Ver docs/ARTE_IA.md.
    /// </summary>
    public static class ArtIA
    {
        public const string Retratos = "retratos", Personajes = "personajes", Iconos = "iconos", Fondos = "fondos", Suelos = "suelos";

        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, RectInt> bounds = new Dictionary<string, RectInt>();
        static readonly Dictionary<string, Material> spriteMats = new Dictionary<string, Material>();

        public static Texture2D Tex(string folder, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string key = folder + "/" + id;
            if (textures.TryGetValue(key, out var t)) return t;
            t = Resources.Load<Texture2D>("ArtIA/" + key);
            textures[key] = t;
            return t;
        }

        public static bool Has(string folder, string id) => Tex(folder, id) != null;

        /// <summary>Sprite de UI (retratos, iconos, fondos). pivotBottom para retratos.</summary>
        public static Sprite Sprite(string folder, string id, bool pivotBottom = false)
        {
            var t = Tex(folder, id);
            if (t == null) return null;
            string key = folder + "/" + id + pivotBottom;
            if (sprites.TryGetValue(key, out var s) && s != null) return s;
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivotBottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f), 100);
            sprites[key] = s;
            return s;
        }

        /// <summary>Rectángulo con contenido (alfa) de una imagen de personaje, para recortar márgenes vacíos.</summary>
        static RectInt Bounds(string id, Texture2D t)
        {
            if (bounds.TryGetValue(id, out var r)) return r;
            r = new RectInt(0, 0, t.width, t.height);
            if (t.isReadable)
            {
                var px = t.GetPixels32();
                int minX = t.width, minY = t.height, maxX = -1, maxY = -1;
                for (int y = 0; y < t.height; y += 2)
                    for (int x = 0; x < t.width; x += 2)
                        if (px[y * t.width + x].a > 110)
                        {
                            if (x < minX) minX = x; if (x > maxX) maxX = x;
                            if (y < minY) minY = y; if (y > maxY) maxY = y;
                        }
                if (maxX > minX && maxY > minY) r = new RectInt(minX, minY, maxX - minX + 2, maxY - minY + 2);
            }
            bounds[id] = r;
            return r;
        }

        /// <summary>
        /// Hoja de poses (ArtIA/animaciones/id): rejilla de 3 columnas x 2 filas.
        /// Fila de arriba: reposo, carrera A, carrera B. Fila de abajo: preparación del golpe, golpe, impacto/esprint.
        /// </summary>
        public class Frames
        {
            public Rect[] uv = new Rect[6];
            public Vector2[] size = new Vector2[6];   // tamaño en el mundo (ancho, alto)
        }

        static readonly Dictionary<string, Frames> framesCache = new Dictionary<string, Frames>();

        public static Frames PoseSheet(string id, float height)
        {
            if (framesCache.TryGetValue(id + height, out var f)) return f;
            var t = Tex("animaciones", id);
            if (t == null || !t.isReadable) { framesCache[id + height] = null; return null; }
            var px = t.GetPixels32();
            int cw = t.width / 3, ch = t.height / 2;
            var rects = new RectInt[6];
            for (int i = 0; i < 6; i++)
            {
                int cx = (i % 3) * cw, cy = (i < 3 ? 1 : 0) * ch;   // fila de arriba = y alta en la textura
                int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
                for (int y = cy + 2; y < cy + ch - 2; y += 2)
                    for (int x = cx + 2; x < cx + cw - 2; x += 2)
                        if (px[y * t.width + x].a > 110)
                        {
                            if (x < minX) minX = x; if (x > maxX) maxX = x;
                            if (y < minY) minY = y; if (y > maxY) maxY = y;
                        }
                rects[i] = maxX > minX ? new RectInt(minX, minY, maxX - minX + 2, maxY - minY + 2) : new RectInt(-1, -1, 0, 0);
            }
            if (rects[0].width <= 0) { framesCache[id + height] = null; return null; }
            for (int i = 1; i < 6; i++) if (rects[i].width <= 0) rects[i] = rects[0];
            f = new Frames();
            float worldPerPx = height / rects[0].height;   // el reposo define la escala
            for (int i = 0; i < 6; i++)
            {
                var r = rects[i];
                f.uv[i] = new Rect((float)r.x / t.width, (float)r.y / t.height, (float)r.width / t.width, (float)r.height / t.height);
                f.size[i] = new Vector2(r.width * worldPerPx, r.height * worldPerPx);
            }
            framesCache[id + height] = f;
            return f;
        }

        /// <summary>Cambia la región de la textura que muestra una malla en rejilla.</summary>
        public static void ApplyUV(Mesh m, Rect uv, int cols = 4, int rows = 10)
        {
            var uvs = new Vector2[(cols + 1) * (rows + 1)];
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= cols; x++)
                    uvs[y * (cols + 1) + x] = new Vector2(Mathf.Lerp(uv.xMin, uv.xMax, (float)x / cols), Mathf.Lerp(uv.yMin, uv.yMax, (float)y / rows));
            m.uv = uvs;
        }

        /// <summary>Rejilla subdividida (para poder doblar la figura) con x en [-0.5, 0.5] e y en [0, 1].</summary>
        static Mesh GridMesh(string name, float u0, float u1, float v0, float v1, int cols = 4, int rows = 10)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= cols; x++)
                {
                    float fx = (float)x / cols, fy = (float)y / rows;
                    verts.Add(new Vector3(fx - 0.5f, fy, 0));
                    uvs.Add(new Vector2(Mathf.Lerp(u0, u1, fx), Mathf.Lerp(v0, v1, fy)));
                }
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    int i = y * (cols + 1) + x;
                    tris.AddRange(new[] { i, i + cols + 1, i + 1, i + 1, i + cols + 1, i + cols + 2 });
                }
            var m = new Mesh { name = name };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.bounds = new Bounds(new Vector3(0, 0.5f, 0), new Vector3(2.5f, 2f, 1f));
            return m;
        }

        /// <summary>
        /// Si hay una imagen de personaje para este id, construye una figura pintada frente a la cámara
        /// (como los sprites de Hades) con el mismo esqueleto de animación que el arte procedural.
        /// Devuelve null si no hay imagen.
        /// </summary>
        public static ModelRig SpriteRig(Transform parent, string id, float height, float shadowRadius = 0.5f)
        {
            float h = height * 1.2f;
            var frames = PoseSheet(id, h);
            var t = frames != null ? Tex("animaciones", id) : Tex(Personajes, id);
            if (t == null) return null;
            var rig = CharacterArt.Root(parent, shadowRadius, out var body);
            Mesh mesh;
            float aspect;
            if (frames != null)
            {
                mesh = GridMesh("poses_" + id, 0, 1, 0, 1);
                ApplyUV(mesh, frames.uv[0]);
                aspect = frames.size[0].x / frames.size[0].y;
            }
            else
            {
                var r = Bounds(id, t);
                aspect = (float)r.width / Mathf.Max(1, r.height);
                float u0 = (float)r.x / t.width, u1 = (float)(r.x + r.width) / t.width;
                float v0 = (float)r.y / t.height, v1 = (float)(r.y + r.height) / t.height;
                mesh = GridMesh("sprite_" + id, u0, u1, v0, v1);
            }
            string matKey = (frames != null ? "poses_" : "") + id;
            if (!spriteMats.TryGetValue(matKey, out var mat) || mat == null)
            {
                mat = new Material(Shader.Find("Umbral/Sprite")) { name = "Sprite_" + matKey };
                mat.SetTexture("_BaseMap", t);
                spriteMats[matKey] = mat;
            }
            var holder = Kit.Empty(body, "Sprite", Vector3.zero);
            var go = new GameObject("Figura");
            go.transform.SetParent(holder, false);
            go.transform.localScale = new Vector3(h * aspect, h, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            holder.gameObject.AddComponent<FaceCamera>();
            rig.weaponPivot = Kit.Empty(body, "WeaponPivot", Vector3.zero);
            rig.bobAmount = 0f;
            rig.spriteMode = true;
            var anim = go.AddComponent<SpriteAnimator>();
            anim.Setup(rig, mr, SpriteAnimator.ProfileFor(id), t);
            if (frames != null) anim.SetFrames(frames, mesh);
            if (anim.profile.flies) { rig.floats = true; rig.floatHeight = anim.profile.flyHeight; }
            rig.Init();
            return rig;
        }
    }

    /// <summary>Mantiene la figura pintada mirando a la cámara y la voltea según hacia dónde mira el personaje.</summary>
    [DefaultExecutionOrder(1000)]
    public class FaceCamera : MonoBehaviour
    {
        Transform owner;
        float flip = 1f;
        void Start()
        {
            var rig = GetComponentInParent<ModelRig>();
            owner = rig != null && rig.transform.parent != null ? rig.transform.parent : transform.root;
        }
        void LateUpdate()
        {
            var cam = CameraRig.Instance != null ? CameraRig.Instance.cam : Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            if (owner != null)
            {
                float side = Vector3.Dot(owner.forward, cam.transform.right);
                if (Mathf.Abs(side) > 0.15f) flip = side >= 0 ? 1f : -1f;
            }
            var s = transform.localScale;
            float target = flip;
            s.x = Mathf.MoveTowards(s.x, target, Time.deltaTime * 12f);
            if (Mathf.Abs(s.x) < 0.05f) s.x = 0.05f * Mathf.Sign(target);
            transform.localScale = new Vector3(s.x, 1, 1);
        }
    }
}
