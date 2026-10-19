using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Signo Arcano (el "Signo de los Primigenios"): equivalente al lanzamiento de Hades II.
    /// Lanza un círculo ritual que Ata a los enemigos dentro (no pueden moverse). Mantén pulsado para el Signo Omega,
    /// más grande y dañino. Los boones de Signo añaden efectos (mareas, rayos, raíces, ventisca, explosión solar...).
    /// </summary>
    public class CastController : MonoBehaviour
    {
        public PlayerController P;
        static PlayerModifiers M => RunState.Current?.mods ?? PlayerModifiers.Default;

        int used;
        readonly List<float> rechargeAt = new List<float>();
        readonly List<ElderSign> active = new List<ElderSign>();
        bool charging; float chargeStart; bool readySignaled;
        public const float OmegaCost = 30f;
        public bool Busy => false;
        public float MoveMult => charging ? 0.5f : 1f;

        public int MaxCharges => Mathf.Max(1, M.castCharges);
        public int Available
        {
            get
            {
                for (int i = rechargeAt.Count - 1; i >= 0; i--) if (Time.time >= rechargeAt[i]) { rechargeAt.RemoveAt(i); used = Mathf.Max(0, used - 1); }
                return Mathf.Max(0, MaxCharges - used);
            }
        }

        public float RechargeFrac
        {
            get
            {
                if (Available > 0) return 1f;
                if (rechargeAt.Count == 0) return 0f;
                float next = float.MaxValue; foreach (var r in rechargeAt) next = Mathf.Min(next, r);
                return Mathf.Clamp01(1f - (next - Time.time) / 2f);
            }
        }

        public void Pressed() { }

        public void Held(float t)
        {
            if (charging)
            {
                float k = Mathf.Clamp01((Time.time - chargeStart) / 0.55f);
                if (k >= 1f && !readySignaled) { readySignaled = true; Sfx.Play(SfxId.ChargeReady, P.transform.position); P.rig.Flash(new Color(0.8f, 0.6f, 1f), 0.4f); }
                if (Random.value < 0.5f)
                {
                    Vector2 at = P.ClampedAimPoint(8f);
                    Fx.Emit(FxKind.Glow, (at + Random.insideUnitCircle * 3.5f).X0Z(0.2f), Vector3.up * 2f, new Color(0.8f, 0.6f, 1f), 0.25f, 0.4f);
                }
                return;
            }
            if (t > 0.22f && Available > 0 && P.magick >= OmegaCost * M.omegaCostMult)
            {
                charging = true; chargeStart = Time.time; readySignaled = false;
                Sfx.Play(SfxId.Charge, P.transform.position, 0.5f, 1.2f);
            }
        }

        public void Released(float t)
        {
            if (Available <= 0) { Sfx.Play(SfxId.UiBack, P.transform.position, 0.6f); charging = false; return; }
            bool omega = false;
            if (charging)
            {
                bool ready = Time.time - chargeStart >= 0.55f;
                charging = false;
                if (ready && P.SpendMagick(OmegaCost * M.omegaCostMult)) omega = true;
                else if (!ready) return;
            }
            Throw(omega);
        }

        public void CancelCharge() { charging = false; }

        void Throw(bool omega)
        {
            used++;
            Vector2 target = P.ClampedAimPoint(7.5f);
            P.Face((target - P.Pos).SafeNormal(P.aimDir));
            P.rig.Kick(0.15f);
            Sfx.Play(SfxId.Cast, P.transform.position);
            float radius = (omega ? 3.6f : 2.6f) * M.castRadiusMult;
            float dur = (omega ? 5f : 3.5f) * M.castDurationMult;
            var sign = ElderSign.Create(this, P.Pos, target, radius, dur, omega);
            active.Add(sign);
            if (omega) foreach (var f in M.onOmega) f(MoveSlot.Cast);
        }

        public void OnSignExpired(ElderSign s)
        {
            active.Remove(s);
            rechargeAt.Add(Time.time + 1.2f);
        }

        public void ResetAll()
        {
            foreach (var s in active.ToArray()) if (s != null) Destroy(s.gameObject);
            active.Clear();
            rechargeAt.Clear();
            used = 0;
            charging = false;
        }
    }

    public class ElderSign : MonoBehaviour
    {
        CastController owner;
        Vector2 center;
        float radius, until;
        bool omega, formed;
        Material mat, glowMat;
        Transform disc, glow;
        Light lightC;
        static PlayerModifiers M => RunState.Current?.mods ?? PlayerModifiers.Default;
        static readonly Color SignColor = new Color(0.75f, 0.5f, 1f);

        public static ElderSign Create(CastController owner, Vector2 from, Vector2 to, float radius, float duration, bool omega)
        {
            var go = new GameObject("SignoArcano");
            go.transform.SetParent(World.Root, false);
            var s = go.AddComponent<ElderSign>();
            s.owner = owner; s.center = to; s.radius = radius; s.omega = omega;
            s.until = Time.time + duration + 0.2f;
            go.transform.position = to.X0Z(0.07f);
            s.StartCoroutine(s.Fly(from, to, duration));
            return s;
        }

        IEnumerator Fly(Vector2 from, Vector2 to, float duration)
        {
            // el signo vuela como un disco brillante y se despliega al llegar
            var orb = Kit.Glow(World.Root, SignColor, 0.5f, from.X0Z(1.2f), 3f);
            float t = 0, dur = 0.18f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                if (orb != null) orb.position = Vector3.Lerp(from.X0Z(1.2f), to.X0Z(0.4f), k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                yield return null;
            }
            if (orb != null) Destroy(orb.gameObject);
            Form(duration);
        }

        void Form(float duration)
        {
            formed = true;
            var painted = ArtIA.Tex("efectos", "circulo");
            mat = Mats.NewUnlit(painted != null ? Color.white : SignColor.WithA(0.95f), Mats.Blend.Additive, painted != null ? painted : Tex.ElderSign, painted != null ? (omega ? 2.2f : 1.6f) : (omega ? 3.2f : 2.4f), false);
            disc = Fx.MeshObj("Disc", MeshGen.Quad, mat, center.X0Z(0.07f), Quaternion.Euler(90, 0, 0), Vector3.one * radius * 2f).transform;
            disc.SetParent(transform, true);
            glowMat = Mats.NewUnlit(SignColor.WithA(0.25f), Mats.Blend.Additive, Tex.SoftCircle, 1.5f, false);
            glow = Fx.MeshObj("Glow", MeshGen.Quad, glowMat, center.X0Z(0.05f), Quaternion.Euler(90, 0, 0), Vector3.one * radius * 2.6f).transform;
            glow.SetParent(transform, true);
            lightC = Kit.PointLight(transform, SignColor, radius * 2.2f, omega ? 4f : 2.5f, Vector3.up * 1.2f);
            Fx.Ring(center.X0Z(), radius, Color.white, 0.3f);
            Sfx.Play(SfxId.Bind, center.X0Z());
            until = Time.time + duration;

            foreach (var c in Combat.InCircle(center, radius, Team.Player)) Bind(c);
            if (omega)
            {
                var h = Hit.Make(50, PlayerController.Instance, MoveSlot.Cast, Vector2.up, 0);
                h.omega = true; h.type = DamageType.Void;
                Combat.Explode(center, radius, h, Team.Player, 0);
                CameraRig.Shake(0.25f);
            }
            foreach (var f in M.onCastPlaced.ToArray()) f(center, radius);
        }

        void Bind(Combatant c)
        {
            float left = Mathf.Max(0.3f, until - Time.time);
            c.status.Apply(StatusType.Atado, 1, left);
        }

        void Update()
        {
            if (!formed) return;
            float dt = Time.deltaTime;
            if (disc != null) disc.Rotate(0, 0, 25f * dt, Space.Self);
            float left = until - Time.time;
            if (mat != null) mat.SetColor(Mats.BaseColorId, (ArtIA.Has("efectos", "circulo") ? Color.white : SignColor).WithA(Mathf.Clamp01(left * 2f) * 0.95f));
            if (lightC != null) lightC.intensity = (omega ? 4f : 2.5f) * Mathf.Clamp01(left * 2f) * (0.85f + 0.15f * Mathf.Sin(Time.time * 8f));
            // nuevos enemigos que entren quedan atados
            foreach (var c in Combat.InCircle(center, radius, Team.Player)) if (!c.status.Has(StatusType.Atado)) Bind(c);
            foreach (var f in M.onCastTick.ToArray()) f(center, radius, dt);
            if (left <= 0)
            {
                owner?.OnSignExpired(this);
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (mat != null) Destroy(mat);
            if (glowMat != null) Destroy(glowMat);
        }
    }
}
