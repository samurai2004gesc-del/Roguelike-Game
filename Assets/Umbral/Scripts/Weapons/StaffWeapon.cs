using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>Báculo de la Llave de Plata — equilibrado, como el Báculo de Bruja de Hades II.</summary>
    public class StaffWeapon : Weapon
    {
        static readonly Color Silver = new Color(0.78f, 0.86f, 1f);
        static readonly Color Irid = new Color(0.75f, 0.5f, 1f);
        float specialReadyAt;

        protected override void BuildModel(Transform pivot)
        {
            model = Kit.Empty(pivot, "Baculo", new Vector3(0.45f, 1.0f, 0.15f), new Vector3(70, 0, 0));
            Kit.Part(model, MeshGen.Cylinder, Mats.Toon(new Color(0.12f, 0.08f, 0.15f), null, 0.8f), new Vector3(0, 0, 0), new Vector3(0.08f, 1.05f, 0.08f));
            // la Llave de Plata en la punta
            var key = Kit.Empty(model, "Llave", new Vector3(0, 1.1f, 0));
            var silver = Mats.Toon(Silver, new Color(0.25f, 0.3f, 0.45f), 0.8f, new Color(0.8f, 0.9f, 1f));
            Kit.Part(key, MeshGen.Cylinder, silver, Vector3.zero, new Vector3(0.05f, 0.25f, 0.05f));
            Kit.Part(key, MeshGen.Ring(0.7f), silver, new Vector3(0, 0.33f, 0), new Vector3(0.22f, 0.22f, 0.22f), new Vector3(90, 0, 0));
            Kit.Part(key, MeshGen.Cube, silver, new Vector3(0.08f, -0.15f, 0), new Vector3(0.12f, 0.05f, 0.04f));
            Kit.Part(key, MeshGen.Cube, silver, new Vector3(0.06f, -0.06f, 0), new Vector3(0.08f, 0.04f, 0.04f));
            Kit.Glow(key, Irid, 0.16f, new Vector3(0, 0.33f, 0), 3f);
        }

        float Reach => M.Flag("st_largo") ? 1.35f : 1f;

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
            float windup = (step == 2 ? 0.12f : 0.06f) / spd;
            float recover = (step == 2 ? 0.28f : 0.16f) / spd;
            busyUntil = Time.time + windup + recover;
            moveMult = 0.4f;
            P.rig.Swing(step == 1 ? -80 : 100, step == 1 ? 100 : -80, windup + 0.1f);
            yield return new WaitForSeconds(windup);
            float emp = P.ConsumeEmpowered();
            Sfx.Play(step == 2 ? SfxId.SwingHeavy : SfxId.Swing, P.transform.position);
            P.Lunge(dir, step == 2 ? 1.1f : 0.5f);
            var h = MakeHit(step == 2 ? 40 : 20, MoveSlot.Attack, false, step == 2 ? 7f : 2.5f, dir);
            if (step < 2)
            {
                SlashFx(dir, 2.7f * Reach, 150, Silver, step == 1);
                MeleeArc(P.Pos, dir, 2.7f * Reach, 150, h);
            }
            else
            {
                Fx.Slash(P.transform.position, dir, 3.5f * Reach, 50, Irid, 0.2f, 0.9f);
                MeleeArc(P.Pos, dir, 3.5f * Reach, 60, h);
                if (M.Flag("st_onda"))
                {
                    var w = MakeHit(30, MoveSlot.Attack, false, 3f, dir);
                    var pr = Shoot(dir, 18f, w, Silver, 0.5f, 9f);
                    pr.pierce = 99; pr.reflectable = false;
                }
            }
            yield return new WaitForSeconds(recover * 0.5f);
            moveMult = 1f;
            comboUntil = Time.time + 0.45f;
            current = null;
        }

        public override void Special()
        {
            if (Time.time < specialReadyAt) return;
            Run(SpecialCo());
        }

        IEnumerator SpecialCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            float cd = 0.32f / SpSpd;
            busyUntil = Time.time + 0.12f;
            specialReadyAt = Time.time + cd;
            moveMult = 0.6f;
            P.rig.Swing(30, -20, 0.1f);
            yield return new WaitForSeconds(0.05f);
            Sfx.Play(SfxId.Projectile, P.transform.position);
            int shots = M.Flag("st_doble") ? 2 : 1;
            for (int i = 0; i < shots; i++)
            {
                Vector2 d = shots > 1 ? dir.Rotate(i == 0 ? -6 : 6) : dir;
                var h = MakeHit(25, MoveSlot.Special, false, 3f, d, DamageType.Void);
                var pr = Shoot(d, 24f, h, Irid, 0.32f, 13f);
                if (M.Flag("staff_pierce")) pr.pierce = 99;
            }
            yield return new WaitForSeconds(0.08f);
            moveMult = 1f;
            current = null;
        }

        protected override void OmegaAttack() => Run(OmegaAttackCo());

        IEnumerator OmegaAttackCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.45f;
            moveMult = 0.2f;
            float len = M.Flag("st_rayo") ? 13.5f : 9f;
            float dmg = M.Flag("st_rayo") ? 91f : 70f;
            Vector2 end = P.Pos + dir * len;
            Sfx.Play(SfxId.Magic, P.transform.position, 1f, 0.7f);
            yield return new WaitForSeconds(0.06f);
            Fx.Beam(P.Pos.X0Z(1f) + dir.X0Z() * 0.6f, end.X0Z(1f), Silver, 0.9f, 0.3f);
            Fx.Beam(P.Pos.X0Z(1f) + dir.X0Z() * 0.6f, end.X0Z(1f), Color.white, 0.35f, 0.25f);
            Fx.Light(P.transform.position + Vector3.up + dir.X0Z() * 3, Irid, 10, 6, 0.3f);
            CameraRig.Shake(0.25f);
            var h = MakeHit(dmg, MoveSlot.Attack, true, 8f, dir, DamageType.Void);
            foreach (var t in Combat.InLine(P.Pos, end, 0.8f, Team.Player))
            {
                var hh = h; hh.dir = dir; Combat.Deal(t, hh);
            }
            for (int i = 0; i < 12; i++) Fx.Emit(FxKind.Spark, Vector3.Lerp(P.Pos.X0Z(1f), end.X0Z(1f), i / 12f), Random.insideUnitSphere * 4f, Irid, 0.25f, 0.4f);
            yield return new WaitForSeconds(0.2f);
            moveMult = 1f;
            current = null;
        }

        protected override void OmegaSpecial() => Run(OmegaSpecialCo());

        IEnumerator OmegaSpecialCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.3f;
            moveMult = 0.4f;
            Sfx.Play(SfxId.Cast, P.transform.position, 1f, 0.6f);
            var h = MakeHit(30, MoveSlot.Special, true, 4f, dir, DamageType.Void);
            var orb = Shoot(dir, 10f, h, Irid, 0.75f, 11f);
            orb.onHit = (pr, c) => pr.End();
            orb.onEnd = pr =>
            {
                Vector2 c = pr.pos;
                var big = MakeHit(90, MoveSlot.Special, true, 9f, dir, DamageType.Void);
                Explode(c, 3.2f, big);
                if (M.Flag("st_esfera"))
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 d = U.Dir(i * 90 + 45);
                        var small = MakeHit(30, MoveSlot.Special, true, 3f, d, DamageType.Void);
                        var s = Projectile.Fire(P, Team.Player, c, d, 9f, small, Irid, 0.4f, 4f);
                        s.onEnd = sp => Explode(sp.pos, 1.8f, small);
                    }
            };
            yield return new WaitForSeconds(0.2f);
            moveMult = 1f;
            current = null;
        }

        void Explode(Vector2 c, float r, Hit h)
        {
            Fx.Ring(c.X0Z(), r, Irid, 0.4f);
            Fx.GroundFlash(c.X0Z(), r, Irid, 0.35f);
            Fx.Burst(c.X0Z(1f), Irid, 24, 8f, 0.35f, 0.5f);
            Fx.Light(c.X0Z(1.5f), Irid, 9, 6, 0.4f);
            Sfx.Play(SfxId.Explosion, c.X0Z(), 0.8f);
            CameraRig.Shake(0.3f);
            Combat.Explode(c, r, h, Team.Player, h.knockback);
        }
    }
}
