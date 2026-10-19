using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Esqueleto visual procedural: animación por código (balanceo, inclinación, estirar/aplastar, tentáculos que ondulan,
    /// esferas orbitando), destello al recibir daño y escarcha al congelarse.
    /// </summary>
    public class ModelRig : MonoBehaviour
    {
        public Transform body;          // se inclina y rebota
        public Transform head;
        public Transform weaponPivot;   // gira en los tajos
        public readonly List<Transform> tentacles = new List<Transform>();
        public readonly List<Transform> orbiters = new List<Transform>();
        public readonly List<Transform> wings = new List<Transform>();
        public readonly List<Renderer> renderers = new List<Renderer>();
        public float bobAmount = 0.06f, bobSpeed = 7f, leanAmount = 10f;
        public float orbitRadius = 0.9f, orbitSpeed = 70f, orbitHeight = 1.3f;
        public bool floats;
        public float floatHeight = 0.0f;

        [System.NonSerialized] public Vector2 velocity;
        [System.NonSerialized] public float speedRef = 5f;
        /// <summary>Figura pintada: la anima <see cref="SpriteAnimator"/> en el shader (no se escala el cuerpo).</summary>
        [System.NonSerialized] public bool spriteMode;
        [System.NonSerialized] public float lastSwingTime = -10f, lastHitTime = -10f;
        public float Squash => squash;
        public bool Frozen => iceShell != null;

        MaterialPropertyBlock mpb;
        float flash;
        Color flashColor = Color.white;
        float squash, squashVel;
        float swingT = 1f, swingDur = 0.2f, swingFrom, swingTo;
        Vector3 bodyBasePos;
        Quaternion bodyBaseRot;
        GameObject iceShell;
        float time;
        Quaternion[] tentBase;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public void Init()
        {
            if (body != null) { bodyBasePos = body.localPosition; bodyBaseRot = body.localRotation; }
            renderers.Clear();
            GetComponentsInChildren(true, renderers);
            tentBase = new Quaternion[tentacles.Count];
            for (int i = 0; i < tentacles.Count; i++) tentBase[i] = tentacles[i].localRotation;
            time = Random.Range(0f, 10f);
        }

        public void Flash(Color c, float amount = 0.55f)
        {
            flash = Mathf.Max(flash, amount);
            flashColor = c;
            lastHitTime = Time.time;
            Kick(0.18f);
        }

        /// <summary>Impulso de estirar (+) o aplastar (-).</summary>
        public void Kick(float amount) { squashVel += amount * 12f; }

        /// <summary>Animación de tajo del arma: gira el pivote del arma de un ángulo a otro.</summary>
        public void Swing(float fromDeg, float toDeg, float duration)
        {
            swingFrom = fromDeg; swingTo = toDeg; swingDur = Mathf.Max(0.05f, duration); swingT = 0;
            lastSwingTime = Time.time;
            Kick(0.25f);
        }

        public void SetFrozen(bool frozen)
        {
            if (frozen && iceShell == null)
            {
                iceShell = new GameObject("Hielo");
                iceShell.transform.SetParent(transform, false);
                var owner = GetComponentInParent<Combatant>();
                float rr = owner != null ? owner.radius : 0.5f, h = owner != null ? owner.height : 1.6f;
                float inv = 1f / Mathf.Max(0.01f, transform.localScale.x);
                iceShell.transform.localPosition = Vector3.up * h * 0.45f * inv;
                iceShell.transform.localScale = new Vector3(rr * 2.4f, h * 0.6f, rr * 2.4f) * inv;
                iceShell.AddComponent<MeshFilter>().sharedMesh = MeshGen.Rock(7, 0.3f, 1);
                var r = iceShell.AddComponent<MeshRenderer>();
                r.sharedMaterial = Mats.Unlit(new Color(0.6f, 0.9f, 1f, 0.45f), Mats.Blend.Alpha, null, 1.3f, false);
            }
            else if (!frozen && iceShell != null)
            {
                Fx.Burst(iceShell.transform.position, new Color(0.7f, 0.95f, 1f), 14, 6f, 0.25f, 0.5f, FxKind.Spark);
                Destroy(iceShell);
                iceShell = null;
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            time += dt;
            float speed = velocity.magnitude;
            float moveK = Mathf.Clamp01(speed / Mathf.Max(0.1f, speedRef));
            bool frozen = iceShell != null;
            float animDt = frozen ? 0f : dt;

            // muelle de estirar/aplastar
            float k = 180f, damp = 14f;
            squashVel += (-k * squash - damp * squashVel) * dt;
            squash += squashVel * dt;
            squash = Mathf.Clamp(squash, -0.35f, 0.35f);

            if (body != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(time * bobSpeed)) * bobAmount * (0.3f + moveK);
                if (floats) bob = Mathf.Sin(time * 2.2f) * 0.12f + floatHeight;
                body.localPosition = bodyBasePos + Vector3.up * bob;
                // inclinación en la dirección de movimiento (en espacio local del personaje)
                Vector3 localVel = transform.InverseTransformDirection(new Vector3(velocity.x, 0, velocity.y));
                Quaternion lean = Quaternion.Euler(localVel.z / Mathf.Max(1f, speedRef) * leanAmount, 0, -localVel.x / Mathf.Max(1f, speedRef) * leanAmount);
                if (!spriteMode)
                {
                    body.localRotation = bodyBaseRot * lean;
                    float s = 1f + squash;
                    body.localScale = new Vector3(1f / Mathf.Sqrt(s), s, 1f / Mathf.Sqrt(s));
                }
            }

            if (weaponPivot != null && swingT < 1f)
            {
                swingT += dt / swingDur;
                float e = U.EaseOutCubic(Mathf.Clamp01(swingT));
                weaponPivot.localRotation = Quaternion.Euler(0, Mathf.Lerp(swingFrom, swingTo, e), 0);
                if (swingT >= 1f) swingT = 1f;
            }
            else if (weaponPivot != null)
            {
                weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, Quaternion.Euler(0, 35f, 0), dt * 6f);
            }

            for (int i = 0; i < tentacles.Count; i++)
            {
                if (tentacles[i] == null) continue;
                float ph = time * 3f + i * 1.3f;
                tentacles[i].localRotation = tentBase[i] * Quaternion.Euler(Mathf.Sin(ph) * 14f + moveK * 18f, Mathf.Cos(ph * 0.7f) * 10f, Mathf.Sin(ph * 1.3f) * 9f);
            }
            for (int i = 0; i < wings.Count; i++)
            {
                if (wings[i] == null) continue;
                float sign = (i % 2 == 0) ? 1 : -1;
                wings[i].localRotation = Quaternion.Euler(0, 0, sign * (20f + Mathf.Sin(time * 9f) * 28f));
            }
            for (int i = 0; i < orbiters.Count; i++)
            {
                if (orbiters[i] == null) continue;
                float a = (time * orbitSpeed + i * 360f / orbiters.Count) * Mathf.Deg2Rad;
                orbiters[i].localPosition = new Vector3(Mathf.Cos(a) * orbitRadius, orbitHeight + Mathf.Sin(time * 2f + i) * 0.15f, Mathf.Sin(a) * orbitRadius);
            }

            if (flash > 0 || animDt == 0)
            {
                flash = Mathf.Max(0, flash - dt * 9f);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetFloat(Mats.FlashId, flash);
                    mpb.SetColor(Mats.FlashColorId, flashColor);
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        /// <summary>Animación de muerte: se aplasta, se hunde en el suelo y desaparece.</summary>
        public System.Collections.IEnumerator DeathAnim(float duration = 0.45f)
        {
            float t = 0;
            Vector3 s0 = transform.localScale;
            Vector3 p0 = transform.localPosition;
            while (t < duration && this != null)
            {
                t += Time.deltaTime;
                float k = t / duration;
                transform.localScale = new Vector3(s0.x * (1f + k * 0.4f), s0.y * (1f - k), s0.z * (1f + k * 0.4f));
                transform.localPosition = p0 + Vector3.down * k * 0.3f;
                yield return null;
            }
        }
    }

    /// <summary>Utilidades para ensamblar modelos con primitivas y mallas procedurales.</summary>
    public static class Kit
    {
        public static Transform Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Vector3 euler = default, string name = "part", bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public static Transform Empty(Transform parent, string name, Vector3 pos, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            return go.transform;
        }

        public static Light PointLight(Transform parent, Color c, float range, float intensity, Vector3 pos)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c; l.range = range; l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>Brillo billboard sin luz real (barato), para ojos y orbes.</summary>
        public static Transform Glow(Transform parent, Color c, float size, Vector3 pos, float intensity = 2.5f)
        {
            var t = Part(parent, MeshGen.Sphere, Mats.Unlit(c, Mats.Blend.Additive, null, intensity, false), pos, Vector3.one * size, default, "glow", false);
            return t;
        }

        /// <summary>Par de ojos brillantes.</summary>
        public static void Eyes(Transform head, Color c, float spacing, float size, float forward, float y = 0f, int count = 2)
        {
            for (int i = 0; i < count; i++)
            {
                float x = count == 1 ? 0 : Mathf.Lerp(-spacing, spacing, (float)i / (count - 1));
                Part(head, MeshGen.Sphere, Mats.Unlit(c, Mats.Blend.Additive, null, 4f, false), new Vector3(x, y, forward), Vector3.one * size, default, "eye", false);
            }
        }

        /// <summary>Corona de tentáculos alrededor de un punto.</summary>
        public static void TentacleRing(ModelRig rig, Transform parent, Material mat, int count, float radius, float length, float tilt, Vector3 offset, float thickness = 1f, float curl = 0.5f)
        {
            var mesh = MeshGen.Tentacle(12, 7, 0.18f * thickness, curl);
            for (int i = 0; i < count; i++)
            {
                float a = i * 360f / count;
                var pivot = Empty(parent, "tent", offset + Quaternion.Euler(0, a, 0) * new Vector3(0, 0, radius), new Vector3(0, a, 0));
                var tent = Part(pivot, mesh, mat, Vector3.zero, Vector3.one * length, new Vector3(tilt, 0, 0), "tentacle");
                rig.tentacles.Add(pivot);
            }
        }

        public static void Shadow(Transform parent, float radius)
        {
            var t = Part(parent, MeshGen.Quad, Mats.Unlit(new Color(0, 0, 0, 0.45f), Mats.Blend.Alpha, Tex.SoftCircle, 1f, false), new Vector3(0, 0.03f, 0), Vector3.one * radius * 2.6f, new Vector3(90, 0, 0), "blob", false);
        }
    }
}
