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
        /// Si hay una imagen de personaje para este id, construye una figura pintada frente a la cámara
        /// (como los sprites de Hades) con el mismo esqueleto de animación que el arte procedural.
        /// Devuelve null si no hay imagen.
        /// </summary>
        public static ModelRig SpriteRig(Transform parent, string id, float height, float shadowRadius = 0.5f)
        {
            var t = Tex(Personajes, id);
            if (t == null) return null;
            var rig = CharacterArt.Root(parent, shadowRadius, out var body);
            var r = Bounds(id, t);
            float aspect = (float)r.width / Mathf.Max(1, r.height);
            float h = height * 1.2f;

            var mesh = new Mesh { name = "sprite_" + id };
            float u0 = (float)r.x / t.width, u1 = (float)(r.x + r.width) / t.width;
            float v0 = (float)r.y / t.height, v1 = (float)(r.y + r.height) / t.height;
            mesh.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(-0.5f, 1, 0) };
            mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();

            if (!spriteMats.TryGetValue(id, out var mat) || mat == null)
            {
                mat = new Material(Shader.Find("Umbral/Sprite")) { name = "Sprite_" + id };
                mat.SetTexture("_BaseMap", t);
                spriteMats[id] = mat;
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
            rig.bobAmount = 0.05f;
            rig.leanAmount = 4f;
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
