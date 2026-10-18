using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    public enum FxKind { Glow, Spark, Smoke, Star }

    /// <summary>
    /// Efectos visuales: partículas compartidas (Emit), arcos de tajo, anillos expansivos, rayos, avisos de ataque y luces.
    /// </summary>
    public static class Fx
    {
        static ParticleSystem glow, spark, smoke, star;
        static Transform root;

        public static void Init(Transform parent)
        {
            root = parent.Child("FX");
            Object.DontDestroyOnLoad(root.gameObject);
            glow = MakeSystem("Glow", Mats.Unlit(Color.white, Mats.Blend.Additive, Tex.SoftCircle, 2.2f), false);
            spark = MakeSystem("Spark", Mats.Unlit(Color.white, Mats.Blend.Additive, Tex.Spark, 3f), true);
            smoke = MakeSystem("Smoke", Mats.Unlit(Color.white, Mats.Blend.Alpha, Tex.Smoke, 1f), false);
            star = MakeSystem("Star", Mats.Unlit(Color.white, Mats.Blend.Additive, Tex.Star4, 2.5f), false);
        }

        static ParticleSystem MakeSystem(string name, Material mat, bool stretched)
        {
            var go = new GameObject("PS_" + name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.duration = 1f;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, stretched ? 0.2f : 0.5f));
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.08f;
            limit.limit = 0.5f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            if (stretched)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.06f;
                r.lengthScale = 2f;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static ParticleSystem Sys(FxKind k) => k == FxKind.Spark ? spark : k == FxKind.Smoke ? smoke : k == FxKind.Star ? star : glow;

        public static void Emit(FxKind kind, Vector3 pos, Vector3 vel, Color color, float size, float life)
        {
            var ps = Sys(kind);
            if (ps == null) return;
            var p = new ParticleSystem.EmitParams
            {
                position = pos, velocity = vel, startColor = color, startSize = size, startLifetime = life,
                rotation = Random.Range(0f, 360f)
            };
            ps.Emit(p, 1);
        }

        public static void Burst(Vector3 pos, Color color, int count, float speed, float size, float life, FxKind kind = FxKind.Glow)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.6f;
                Emit(kind, pos, d * speed * Random.Range(0.4f, 1f), color, size * Random.Range(0.6f, 1.3f), life * Random.Range(0.6f, 1.2f));
            }
        }

        public static void HitSpark(Vector3 pos, Vector2 dir, Color color)
        {
            Vector3 d3 = dir.X0Z();
            for (int i = 0; i < 7; i++)
            {
                Vector3 v = (d3 + Random.insideUnitSphere * 0.9f).normalized * Random.Range(6f, 13f);
                Emit(FxKind.Spark, pos, v, color, Random.Range(0.12f, 0.25f), Random.Range(0.15f, 0.3f));
            }
            Emit(FxKind.Glow, pos, Vector3.zero, color, 1.2f, 0.12f);
        }

        public static void Smoke(Vector3 pos, Color color, int count = 8, float size = 1f)
        {
            for (int i = 0; i < count; i++)
                Emit(FxKind.Smoke, pos + Random.insideUnitSphere * 0.4f, Random.insideUnitSphere * 1.2f + Vector3.up * 0.6f, color, size * Random.Range(0.7f, 1.4f), Random.Range(0.6f, 1.1f));
        }

        // ---------------- Objetos efímeros ----------------

        static Transform Parent => World.Root != null ? World.Root : root;

        public static GameObject MeshObj(string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        /// <summary>Arco de tajo brillante, el sello visual de los ataques cuerpo a cuerpo de Hades.</summary>
        public static void Slash(Vector3 pos, Vector2 dir, float radius, float angle, Color color, float duration = 0.18f, float height = 0.9f, bool flip = false)
        {
            var mat = Mats.NewUnlit(color, Mats.Blend.Additive, Tex.SlashGradient, 2.6f, false);
            var rot = U.LookFlat(dir) * Quaternion.Euler(0, 0, flip ? 180 : 0);
            var go = MeshObj("Slash", MeshGen.SlashArc(angle, 0.55f), mat, pos + Vector3.up * height, rot, Vector3.one * radius);
            if (flip) go.transform.localScale = new Vector3(-radius, radius, radius);
            Runner.Run(AnimateFade(go, mat, duration, 1.12f, 0.15f));
        }

        public static void Ring(Vector3 pos, float radius, Color color, float duration = 0.4f, float startFrac = 0.2f, float intensity = 2f)
        {
            var mat = Mats.NewUnlit(color, Mats.Blend.Additive, Tex.Ring, intensity, false);
            var go = MeshObj("Ring", MeshGen.Quad, mat, pos + Vector3.up * 0.06f, Quaternion.Euler(90, 0, 0), Vector3.one * radius * 2f * startFrac);
            Runner.Run(AnimateRing(go, mat, duration, radius * 2f * startFrac, radius * 2f));
        }

        public static void GroundFlash(Vector3 pos, float radius, Color color, float duration = 0.3f)
        {
            var mat = Mats.NewUnlit(color.WithA(0.7f), Mats.Blend.Additive, Tex.SoftCircle, 1.6f, false);
            var go = MeshObj("Flash", MeshGen.Quad, mat, pos + Vector3.up * 0.05f, Quaternion.Euler(90, 0, 0), Vector3.one * radius * 2f);
            Runner.Run(AnimateFade(go, mat, duration, 1.15f, 0f));
        }

        static IEnumerator AnimateFade(GameObject go, Material mat, float duration, float growTo, float delayFade)
        {
            float t = 0; Vector3 s0 = go.transform.localScale;
            Color c0 = mat.GetColor(Mats.BaseColorId);
            while (t < duration && go != null)
            {
                t += Time.deltaTime;
                float k = t / duration;
                go.transform.localScale = s0 * Mathf.Lerp(1f, growTo, U.EaseOutCubic(k));
                float a = k < delayFade ? 1 : 1f - (k - delayFade) / (1f - delayFade);
                mat.SetColor(Mats.BaseColorId, c0.WithA(c0.a * a));
                yield return null;
            }
            if (go != null) Object.Destroy(go);
            Object.Destroy(mat);
        }

        static IEnumerator AnimateRing(GameObject go, Material mat, float duration, float from, float to)
        {
            float t = 0; Color c0 = mat.GetColor(Mats.BaseColorId);
            while (t < duration && go != null)
            {
                t += Time.deltaTime;
                float k = t / duration;
                go.transform.localScale = Vector3.one * Mathf.Lerp(from, to, U.EaseOutCubic(k));
                mat.SetColor(Mats.BaseColorId, c0.WithA(1f - k));
                yield return null;
            }
            if (go != null) Object.Destroy(go);
            Object.Destroy(mat);
        }

        /// <summary>Rayo quebrado entre dos puntos.</summary>
        public static void Lightning(Vector3 from, Vector3 to, Color color, float width = 0.25f, float duration = 0.22f)
        {
            var go = new GameObject("Bolt");
            go.transform.SetParent(Parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.Unlit(color, Mats.Blend.Additive, null, 3f);
            int n = 9;
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < n - 1) p += Random.insideUnitSphere * 0.6f;
                lr.SetPosition(i, p);
            }
            lr.widthMultiplier = width;
            lr.startColor = Color.white; lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Burst(to, color, 10, 6f, 0.35f, 0.35f, FxKind.Spark);
            Light(to + Vector3.up, color, 6f, 5f, duration * 1.5f);
            Runner.Run(KillLine(lr, duration));
        }

        static IEnumerator KillLine(LineRenderer lr, float duration)
        {
            float t = 0; float w = lr.widthMultiplier;
            while (t < duration && lr != null)
            {
                t += Time.deltaTime;
                lr.widthMultiplier = w * (1f - t / duration);
                yield return null;
            }
            if (lr != null) Object.Destroy(lr.gameObject);
        }

        /// <summary>Línea recta brillante (rayos de jefes, rayos del báculo).</summary>
        public static void Beam(Vector3 from, Vector3 to, Color color, float width, float duration)
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(Parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.Unlit(color, Mats.Blend.Additive, null, 3f);
            lr.positionCount = 2;
            lr.SetPosition(0, from); lr.SetPosition(1, to);
            lr.widthMultiplier = width;
            lr.startColor = Color.white; lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Runner.Run(KillLine(lr, duration));
        }

        public static Light Light(Vector3 pos, Color color, float range, float intensity, float duration)
        {
            var go = new GameObject("FxLight");
            go.transform.SetParent(Parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color; l.range = range; l.intensity = intensity;
            l.shadows = LightShadows.None;
            Runner.Run(FadeLight(l, duration));
            return l;
        }

        static IEnumerator FadeLight(Light l, float duration)
        {
            float t = 0; float i0 = l.intensity;
            while (t < duration && l != null)
            {
                t += Time.deltaTime;
                l.intensity = i0 * (1f - t / duration);
                yield return null;
            }
            if (l != null) Object.Destroy(l.gameObject);
        }
    }

    /// <summary>Ejecutor global de corrutinas (para efectos que no pertenecen a ningún objeto).</summary>
    public class Runner : MonoBehaviour
    {
        static Runner inst;
        public static Runner Instance
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("CoroutineRunner");
                    DontDestroyOnLoad(go);
                    inst = go.AddComponent<Runner>();
                }
                return inst;
            }
        }
        public static Coroutine Run(IEnumerator e) => Instance.StartCoroutine(e);
        public static void Delay(float seconds, System.Action a) => Instance.StartCoroutine(DelayCo(seconds, a));
        static IEnumerator DelayCo(float s, System.Action a) { yield return new WaitForSeconds(s); a?.Invoke(); }
        public static void StopAll() { if (inst != null) inst.StopAllCoroutines(); }
    }
}
