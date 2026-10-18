using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Fábrica y caché de materiales. Todo el arte del juego se genera por código.</summary>
    public static class Mats
    {
        static Shader toon, unlit, voidShader;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Shader ToonShader => toon ??= Find("Umbral/Toon");
        public static Shader UnlitShader => unlit ??= Find("Umbral/Unlit");
        public static Shader VoidShader => voidShader ??= Find("Umbral/Void");

        static Shader Find(string name)
        {
            var s = Shader.Find(name);
            if (s == null)
            {
                Debug.LogWarning($"[Umbral] No se encontró el shader {name}; usando respaldo.");
                s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            }
            return s;
        }

        static string Key(Color c) => ((Color32)c).GetHashCode() + "_" + c.r.ToString("F2") + c.g.ToString("F2") + c.b.ToString("F2") + c.a.ToString("F2");

        /// <summary>Material "cel" con contorno. shade multiplica el color en las zonas de sombra.</summary>
        public static Material Toon(Color baseColor, Color? emission = null, float outline = 1f, Color? rim = null, Color? shade = null)
        {
            Color e = emission ?? Color.black;
            Color r = rim ?? new Color(0.55f, 0.45f, 0.9f);
            Color s = shade ?? new Color(0.36f, 0.27f, 0.5f);
            string k = "toon" + Key(baseColor) + Key(e) + outline.ToString("F2") + Key(r) + Key(s);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(ToonShader) { name = "Toon" };
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_EmissionColor", e);
            m.SetFloat("_OutlineWidth", outline);
            m.SetColor("_RimColor", r * 0.6f);
            m.SetColor("_ShadeColor", s);
            m.enableInstancing = true;
            cache[k] = m;
            return m;
        }

        /// <summary>Material "cel" con textura (suelos, paredes).</summary>
        public static Material ToonTextured(string key, Texture2D tex, Color tint, Vector2 tiling, Texture2D emissionMap = null, Color? emission = null, float outline = 0f)
        {
            string k = "toontex" + key;
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(ToonShader) { name = "ToonTex_" + key };
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_OutlineWidth", outline);
            m.SetColor("_RimColor", new Color(0.1f, 0.08f, 0.15f));
            m.SetFloat("_RimPower", 6f);
            m.SetColor("_ShadeColor", new Color(0.45f, 0.38f, 0.6f));
            m.SetFloat("_ShadeThreshold", -0.2f);
            if (emissionMap != null)
            {
                m.SetTexture("_EmissionMap", emissionMap);
                m.SetTextureScale("_EmissionMap", tiling);
                m.SetFloat("_EmissionMapWeight", 1f);
                m.SetColor("_EmissionColor", emission ?? Color.white);
            }
            cache[k] = m;
            return m;
        }

        public enum Blend { Additive, Alpha }

        /// <summary>Material sin iluminación (efectos). intensity &gt; 1 produce resplandor (bloom).</summary>
        public static Material Unlit(Color color, Blend blend = Blend.Additive, Texture2D tex = null, float intensity = 1f, bool vertexColor = true, bool zTestAlways = false)
        {
            string k = "unlit" + Key(color) + blend + (tex != null ? tex.name : "w") + intensity.ToString("F2") + vertexColor + zTestAlways;
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = NewUnlit(color, blend, tex, intensity, vertexColor, zTestAlways);
            cache[k] = m;
            return m;
        }

        /// <summary>Material sin caché, para efectos que animan su propio color.</summary>
        public static Material NewUnlit(Color color, Blend blend = Blend.Additive, Texture2D tex = null, float intensity = 1f, bool vertexColor = true, bool zTestAlways = false)
        {
            var m = new Material(UnlitShader) { name = "Unlit" };
            m.SetColor("_BaseColor", color);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_VertexColorWeight", vertexColor ? 1f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", blend == Blend.Additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_ZTest", zTestAlways ? (float)UnityEngine.Rendering.CompareFunction.Always : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            m.renderQueue = 3000 + (zTestAlways ? 100 : 0);
            return m;
        }

        public static Material Void(Color a, Color b, Color c, bool globes = true)
        {
            var m = new Material(VoidShader) { name = "Void" };
            m.SetColor("_ColorA", a);
            m.SetColor("_ColorB", b);
            m.SetColor("_ColorC", c);
            m.SetFloat("_Globes", globes ? 1f : 0f);
            return m;
        }

        // Propiedades para destello de golpe
        public static readonly int FlashId = Shader.PropertyToID("_Flash");
        public static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    }
}
