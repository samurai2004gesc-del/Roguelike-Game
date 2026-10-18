using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>Colmillos de Tindalos — rápidas, letales por la espalda (como las Hermanas Cuchillas de Hades II).</summary>
    public class DaggerWeapon : Weapon
    {
        static readonly Color Steel = new Color(0.45f, 0.95f, 0.9f);
        float specialReadyAt;
        bool cornerReady;

        public override float AttackOmegaCost => 15f;
        public override float SpecialOmegaCost => 25f;

        protected override void BuildModel(Transform pivot)
        {
            model = Kit.Empty(pivot, "Dagas", new Vector3(0, 0.9f, 0));
            var blade = Mats.Toon(new Color(0.25f, 0.5f, 0.55f), Steel * 0.5f, 0.8f, Steel);
            var hilt = Mats.Toon(new Color(0.1f, 0.06f, 0.1f), null, 0.8f);
            for (int s = -1; s <= 1; s += 2)
            {
                var d = Kit.Empty(model, "Daga", new Vector3(0.45f * s, 0, 0.2f), new Vector3(80, 0, 15 * s));
                Kit.Part(d, MeshGen.Cylinder, hilt, Vector3.zero, new Vector3(0.06f, 0.12f, 0.06f));
                Kit.Part(d, MeshGen.Crystal(4, 0.4f), blade, new Vector3(0, 0.1f, 0), new Vector3(0.1f, 0.6f, 0.04f));
            }
        }

        public override void Attack()
        {
            if (Time.time > comboUntil) combo = 0;
            int step = combo;
            combo = (combo + 1) % 4;
            Run(AttackCo(step));
        }

        IEnumerator AttackCo(int step)
        {
            FaceAim();
            Vector2 dir = Aim;
            float spd = AtkSpd;
            float windup = 0.035f / spd, recover = (step == 3 ? 0.2f : 0.09f) / spd;
            busyUntil = Time.time + windup + recover;
            moveMult = 0.65f;
            P.rig.Swing(step % 2 == 0 ? 70 : -70, step % 2 == 0 ? -70 : 70, 0.09f);
            yield return new WaitForSeconds(windup);
            P.ConsumeEmpowered();
            Sfx.Play(SfxId.Swing, P.transform.position, 0.8f, 1.3f);
            P.Lunge(dir, 0.35f);
            int hits = step == 3 ? 2 : 1;
            for (int i = 0; i < hits; i++)
            {
                var h = MakeHit(step == 3 ? 14 : 12, MoveSlot.Attack, false, 1.5f, dir);
                if (cornerReady) { h.extraCrit += 1f; }
                SlashFx(dir, 2.1f, 110, Steel, (step + i) % 2 == 1, 0.12f);
                MeleeArc(P.Pos, dir, 2.1f, 110, h, 0.4f);
                if (hits > 1) yield return new WaitForSeconds(0.06f / spd);
            }
            cornerReady = false;
            yield return new WaitForSeconds(recover * 0.6f);
            moveMult = 1f;
            comboUntil = Time.time + 0.35f;
            current = null;
        }

        /// <summary>Llamado por el jugador al esprintar (Desde la Esquina).</summary>
        public void OnDash() { if (M.Flag("dg_esquina")) cornerReady = true; }

        int ExtraDaggers()
        {
            int n = M.Flag("dg_colmillo") ? 2 : 0;
            for (int l = 1; l <= 3; l++) if (M.Flag("dg_extra" + l)) n = Mathf.Max(n, (M.Flag("dg_colmillo") ? 2 : 0) + l);
            return n;
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
            specialReadyAt = Time.time + 0.38f / SpSpd;
            busyUntil = Time.time + 0.1f;
            P.rig.Swing(-40, 60, 0.1f);
            yield return new WaitForSeconds(0.04f);
            Sfx.Play(SfxId.Projectile, P.transform.position, 0.8f, 1.5f);
            int n = 3 + ExtraDaggers();
            for (int i = 0; i < n; i++)
            {
                float a = (i - (n - 1) / 2f) * 10f;
                ThrowDagger(dir.Rotate(a), 15f, false, 0);
            }
            current = null;
        }

        void ThrowDagger(Vector2 d, float dmg, bool omega, int pierce)
        {
            var h = MakeHit(dmg, MoveSlot.Special, omega, 1.5f, d);
            var pr = Shoot(d, 26f, h, Steel, 0.22f, 10f);
            pr.pierce = pierce;
            bool keziah = M.Flag("dg_keziah");
            bool bounce = M.Flag("dg_rebote");
            pr.onHit = (p, c) =>
            {
                var hh = p.hit;
                if (Combat.IsBackstab(c, P.Pos)) hh.extraCrit += 0.4f;
                Combat.Deal(c, hh);
                if (keziah) c.status.Apply(StatusType.Vulnerable, 1, 4f, 0.15f);
                if (bounce && p.hit.tag != "rebote")
                {
                    var n = Combat.Nearest(c.Pos, Team.Player, 6f, c);
                    if (n != null)
                    {
                        var bh = p.hit; bh.tag = "rebote";
                        var b = Projectile.Fire(P, Team.Player, c.Pos, n.Pos - c.Pos, 24f, bh, Steel, 0.2f, 7f);
                    }
                }
            };
        }

        protected override void OmegaAttack() => Run(OmegaAttackCo());

        IEnumerator OmegaAttackCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.4f;
            // Ángulo Agudo: atravesar el espacio hasta la espalda del enemigo más cercano en la dirección apuntada
            Combatant target = null; float best = 9f;
            foreach (var c in Combat.InArc(P.Pos, dir, 8f, 70, Team.Player))
            {
                float d = Vector2.Distance(c.Pos, P.Pos);
                if (d < best) { best = d; target = c; }
            }
            Fx.Burst(P.transform.position + Vector3.up, Steel, 16, 5f, 0.3f, 0.4f, FxKind.Spark);
            Sfx.Play(SfxId.Dash, P.transform.position, 1f, 1.4f);
            P.SetInvulnerable(0.35f);
            if (target != null)
            {
                Vector2 behind = target.Pos - target.facing.SafeNormal(-dir) * (target.radius + 0.9f);
                if (Vector2.Dot(target.facing, (P.Pos - target.Pos)) < 0) behind = target.Pos + dir * (target.radius + 0.9f);
                Fx.Beam(P.Pos.X0Z(1f), behind.X0Z(1f), Steel, 0.3f, 0.2f);
                P.Teleport(behind);
                Vector2 toT = (target.Pos - P.Pos).SafeNormal(dir);
                P.Face(toT);
                yield return new WaitForSeconds(0.06f);
                int strikes = M.Flag("dg_doble") ? 2 : 1;
                for (int i = 0; i < strikes; i++)
                {
                    var h = MakeHit(50, MoveSlot.Attack, true, 4f, toT);
                    h.extraCrit += 0.6f;
                    h.origin = target.Pos - target.facing * 2f; // siempre cuenta como por la espalda
                    SlashFx(toT, 2.2f, 90, Color.white, i == 1, 0.15f);
                    if (target != null && target.Alive) Combat.Deal(target, h);
                    MeleeArc(P.Pos, toT, 2.2f, 90, MakeHit(20, MoveSlot.Attack, true, 2f, toT));
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                Vector2 to = Arena.Current != null ? Arena.Current.Raycast(P.Pos, dir, 6f, P.radius) : P.Pos + dir * 6f;
                Fx.Beam(P.Pos.X0Z(1f), to.X0Z(1f), Steel, 0.3f, 0.2f);
                foreach (var c in Combat.InLine(P.Pos, to, 0.9f, Team.Player)) Combat.Deal(c, MakeHit(35, MoveSlot.Attack, true, 3f, dir));
                P.Teleport(to);
            }
            current = null;
        }

        protected override void OmegaSpecial() => Run(OmegaSpecialCo());

        IEnumerator OmegaSpecialCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.3f;
            Sfx.Play(SfxId.Projectile, P.transform.position, 1f, 1.1f);
            int n = 8 + ExtraDaggers();
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(-60, 60, n > 1 ? i / (float)(n - 1) : 0.5f);
                ThrowDagger(dir.Rotate(a), 20f, true, 3);
            }
            yield return new WaitForSeconds(0.15f);
            current = null;
        }
    }
}
