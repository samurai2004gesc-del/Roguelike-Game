using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>Hoz de Leng — pesada, con guardia y contraataque (como el Hacha de Piedra Lunar de Hades II).</summary>
    public class ScytheWeapon : Weapon
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        bool guarding;
        float guardStart;
        bool blockedSomething;
        bool spinning;
        float spinTick;

        public override float AttackOmegaCost => M.Flag("hz_eterno") ? 3f : 6f;
        public override float SpecialOmegaCost => 30f;
        public override float ChargeTime => base.ChargeTime * 0.8f;

        protected override void BuildModel(Transform pivot)
        {
            model = Kit.Empty(pivot, "Hoz", new Vector3(0.5f, 1.0f, 0.1f), new Vector3(60, 0, 0));
            Kit.Part(model, MeshGen.Cylinder, Mats.Toon(new Color(0.2f, 0.12f, 0.08f), null, 0.8f), new Vector3(0, 0.3f, 0), new Vector3(0.09f, 1.3f, 0.09f));
            var blade = Mats.Toon(new Color(0.75f, 0.7f, 0.55f), Gold * 0.25f, 0.9f, Gold);
            var b = Kit.Part(model, MeshGen.SlashArc(110, 0.55f), blade, new Vector3(0, 1.55f, -0.2f), new Vector3(1.1f, 1f, 1.1f), new Vector3(0, 0, 90));
            Kit.Part(model, MeshGen.Sphere, Mats.Toon(new Color(0.9f, 0.75f, 0.25f), Gold * 0.4f, 0.6f), new Vector3(0, 1.58f, 0), Vector3.one * 0.18f);
        }

        float Reach => M.Flag("hz_filo") ? 1.25f : 1f;

        public override bool CanStart => base.CanStart && !guarding && !spinning;

        public override void Attack()
        {
            if (Time.time > comboUntil) combo = 0;
            int step = combo;
            combo = (combo + 1) % 3;
            Run(AttackCo(step));
        }

        IEnumerator AttackCo(int step)
        {
            FaceAim();
            Vector2 dir = Aim;
            float spd = AtkSpd;
            float windup = (step == 2 ? 0.26f : 0.18f) / spd, recover = (step == 2 ? 0.42f : 0.3f) / spd;
            busyUntil = Time.time + windup + recover;
            moveMult = 0.25f;
            P.rig.Swing(step == 1 ? -130 : 130, step == 1 ? 130 : -130, windup + 0.12f);
            yield return new WaitForSeconds(windup);
            P.ConsumeEmpowered();
            Sfx.Play(SfxId.SwingHeavy, P.transform.position, 1f, 0.8f);
            P.Lunge(dir, 0.7f);
            if (step < 2)
            {
                var h = MakeHit(35, MoveSlot.Attack, false, 8f, dir);
                SlashFx(dir, 3.3f * Reach, 200, Gold, step == 1, 0.22f);
                MeleeArc(P.Pos, dir, 3.3f * Reach, 200, h);
                CameraRig.Shake(0.12f);
            }
            else
            {
                Vector2 c = P.Pos + dir * 1.8f * Reach;
                var h = MakeHit(70, MoveSlot.Attack, false, 10f, dir);
                Fx.Ring(c.X0Z(), 2.6f * Reach, Gold, 0.35f);
                Fx.GroundFlash(c.X0Z(), 2.4f * Reach, Gold, 0.3f);
                Fx.Burst(c.X0Z(0.3f), new Color(0.6f, 0.5f, 0.4f), 14, 6f, 0.4f, 0.6f, FxKind.Smoke);
                Combat.Explode(c, 2.5f * Reach, h, Team.Player, 10f);
                CameraRig.Shake(0.35f);
                Sfx.Play(SfxId.Explosion, c.X0Z(), 0.6f, 1.4f);
                if (M.Flag("hz_grieta")) Runner.Run(Fissure(c, dir));
            }
            yield return new WaitForSeconds(recover * 0.6f);
            moveMult = 1f;
            comboUntil = Time.time + 0.5f;
            current = null;
        }

        IEnumerator Fissure(Vector2 from, Vector2 dir)
        {
            for (int i = 1; i <= 5; i++)
            {
                Vector2 p = from + dir * i * 1.5f;
                if (Arena.Current != null && !Arena.Current.InsidePolygon(p)) yield break;
                Fx.GroundFlash(p.X0Z(), 1.2f, Gold, 0.3f);
                Fx.Burst(p.X0Z(0.2f), new Color(0.6f, 0.5f, 0.4f), 6, 4f, 0.3f, 0.4f, FxKind.Smoke);
                Combat.Explode(p, 1.3f, MakeHit(25, MoveSlot.Attack, false, 4f, dir), Team.Player);
                yield return new WaitForSeconds(0.06f);
            }
        }

        // ---------------- Especial: guardia ----------------
        public override void Special()
        {
            if (guarding) return;
            guarding = true; guardStart = Time.time; blockedSomething = false;
            P.blocking = true;
            moveMult = 0.35f;
            P.rig.Swing(0, 90, 0.08f);
            Sfx.Play(SfxId.HitArmor, P.transform.position, 0.4f, 0.8f);
        }

        public override void SpecialHeld(float heldTime)
        {
            if (guarding)
            {
                P.blockDir = P.aimDir;
                P.Face(P.aimDir);
                // reflejar proyectiles con Guardia de Hierro
                foreach (var pr in Projectile.Active.ToArray())
                {
                    if (pr == null || pr.team != Team.Enemy) continue;
                    Vector2 d = pr.pos - P.Pos;
                    if (d.magnitude < 1.6f && Vector2.Angle(P.blockDir, d) < 80)
                    {
                        if (M.Flag("hz_hierro")) pr.Reflect(Team.Player, P); else pr.End();
                        blockedSomething = true;
                        Fx.HitSpark(pr.transform.position, -d.normalized, Gold);
                    }
                }
                if (Random.value < 0.3f) Fx.Emit(FxKind.Glow, P.transform.position + P.blockDir.X0Z(1f) + Vector3.up, Vector3.up, Gold, 0.25f, 0.3f);
                // la guardia mantenida carga el Salto (Omega Especial)
                if (heldTime > 0.6f && CanAffordOmega(MoveSlot.Special) && !charging) { StartCharge(MoveSlot.Special); }
                return;
            }
        }

        public void OnBlocked() { blockedSomething = true; }

        public override void SpecialReleased()
        {
            if (!guarding) return;
            guarding = false;
            P.blocking = false;
            moveMult = 1f;
            if (charging) return; // la liberación la gestiona la carga
            Run(CounterCo());
        }

        public override void TickCharge(bool stillHeld)
        {
            if (chargeSlot == MoveSlot.Special && guarding) SpecialHeld(Time.time - guardStart);
            base.TickCharge(stillHeld);
            if (!stillHeld && guarding) { guarding = false; P.blocking = false; }
        }

        IEnumerator CounterCo()
        {
            Vector2 dir = Aim;
            FaceAim();
            busyUntil = Time.time + 0.3f;
            float dmg = blockedSomething ? 60f : 25f;
            if (M.Flag("hz_contra")) dmg *= 2f;
            P.rig.Swing(-100, 120, 0.15f);
            Sfx.Play(SfxId.SwingHeavy, P.transform.position);
            SlashFx(dir, 3f, 160, blockedSomething ? Color.white : Gold, false, 0.2f);
            MeleeArc(P.Pos, dir, 3f, 160, MakeHit(dmg, MoveSlot.Special, false, 9f, dir));
            yield return new WaitForSeconds(0.25f);
            current = null;
        }

        // ---------------- Omega Ataque: torbellino ----------------
        public override void AttackHeld(float heldTime)
        {
            if (spinning)
            {
                spinTick -= Time.deltaTime;
                P.rig.weaponPivot.Rotate(0, 1400f * Time.deltaTime, 0);
                if (spinTick <= 0)
                {
                    if (!P.SpendMagick(OmegaCost(MoveSlot.Attack))) { StopSpin(); return; }
                    spinTick = 0.22f / AtkSpd;
                    var h = MakeHit(22, MoveSlot.Attack, true, 5f, P.aimDir);
                    Fx.Slash(P.transform.position, U.Dir(Random.Range(0, 360)), 2.9f * Reach, 300, Gold, 0.18f, 0.8f);
                    Sfx.Play(SfxId.Swing, P.transform.position, 0.7f, 0.9f);
                    Combat.Explode(P.Pos, 2.9f * Reach, h, Team.Player, 5f);
                }
                return;
            }
            if (heldTime < 0.22f || Busy) return;
            if (CanAffordOmega(MoveSlot.Attack))
            {
                spinning = true; spinTick = 0; moveMult = 0.55f;
                busyUntil = float.MaxValue;
            }
            else Attack();
        }

        public override void AttackReleased() { if (spinning) StopSpin(); }

        void StopSpin()
        {
            spinning = false;
            moveMult = 1f;
            busyUntil = Time.time + 0.15f;
        }

        public override void Cancel()
        {
            base.Cancel();
            if (spinning) StopSpin();
            busyUntil = 0;
            guarding = false; P.blocking = false;
        }

        protected override void OmegaAttack() { }

        // ---------------- Omega Especial: salto ----------------
        protected override void OmegaSpecial() => Run(LeapCo());

        IEnumerator LeapCo()
        {
            Vector2 from = P.Pos;
            Vector2 to = P.ClampedAimPoint(7f);
            if (Arena.Current != null) to = Arena.Current.Resolve(to, P.radius);
            float r = 3.5f * (M.Flag("hz_caida") ? 1.5f : 1f);
            busyUntil = Time.time + 0.75f;
            moveMult = 0f;
            P.SetInvulnerable(0.6f);
            var tele = Telegraph.Circle(to, r, 0.45f, Gold);
            float t = 0, dur = 0.45f;
            P.Face((to - from).SafeNormal(P.aimDir));
            Sfx.Play(SfxId.Dash, P.transform.position, 1f, 0.7f);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                P.Pos = Vector2.Lerp(from, to, U.Smooth01(k));
                P.rig.transform.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.5f;
                yield return null;
            }
            P.rig.transform.localPosition = Vector3.zero;
            var h = MakeHit(100, MoveSlot.Special, true, 12f, Vector2.up);
            Fx.Ring(to.X0Z(), r, Gold, 0.45f);
            Fx.GroundFlash(to.X0Z(), r, Gold, 0.4f);
            Fx.Burst(to.X0Z(0.3f), new Color(0.6f, 0.5f, 0.4f), 26, 9f, 0.5f, 0.7f, FxKind.Smoke);
            Fx.Light(to.X0Z(1f), Gold, 10, 7, 0.4f);
            Sfx.Play(SfxId.Explosion, to.X0Z());
            CameraRig.Shake(0.6f);
            foreach (var c in Combat.InCircle(to, r, Team.Player))
            {
                var hh = h; hh.dir = (c.Pos - to).SafeNormal(Vector2.up);
                Combat.Deal(c, hh);
                c.status.Apply(StatusType.Aturdido, 1, 1f);
            }
            yield return new WaitForSeconds(0.2f);
            moveMult = 1f;
            current = null;
        }
    }
}
