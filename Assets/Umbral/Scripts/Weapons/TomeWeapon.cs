using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>Necronomicón — arma a distancia de llamas fatuas y glifos (como las Llamas Umbrías de Hades II).</summary>
    public class TomeWeapon : Weapon
    {
        static readonly Color Wisp = new Color(0.5f, 1f, 0.45f);
        static readonly Color Glyph = new Color(0.35f, 1f, 0.6f);
        float specialReadyAt;
        bool alt;

        public override float AttackOmegaCost => 20f;
        public override float SpecialOmegaCost => 35f;

        protected override void BuildModel(Transform pivot)
        {
            model = Kit.Empty(pivot, "Tomo", new Vector3(0.5f, 1.2f, 0.35f), new Vector3(-20, 30, 0));
            var cover = Mats.Toon(new Color(0.35f, 0.2f, 0.16f), null, 0.9f);
            var pages = Mats.Toon(new Color(0.88f, 0.82f, 0.66f), null, 0.6f);
            Kit.Part(model, MeshGen.Cube, cover, Vector3.zero, new Vector3(0.5f, 0.1f, 0.62f));
            Kit.Part(model, MeshGen.Cube, pages, new Vector3(0.02f, 0.02f, 0), new Vector3(0.46f, 0.1f, 0.58f));
            // ojo en la portada
            Kit.Glow(model, Wisp, 0.12f, new Vector3(0, 0.07f, 0), 4f);
            Kit.Part(model, MeshGen.Sphere, Mats.Toon(new Color(0.9f, 0.85f, 0.7f), null, 0.6f), new Vector3(0, 0.05f, 0), new Vector3(0.2f, 0.06f, 0.12f));
            // llamas que flotan alrededor
            for (int i = 0; i < 3; i++) P.rig.orbiters.Add(Kit.Glow(P.transform, Wisp, 0.18f, Vector3.zero, 3f));
            P.rig.orbitRadius = 0.85f; P.rig.orbitHeight = 1.5f; P.rig.orbitSpeed = 140f;
        }

        public override void Attack() => Run(AttackCo());

        IEnumerator AttackCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            float spd = AtkSpd;
            busyUntil = Time.time + 0.16f / spd;
            moveMult = 0.75f;
            P.ConsumeEmpowered();
            int n = M.Flag("tm_doble") ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                alt = !alt;
                Vector2 side = new Vector2(-dir.y, dir.x) * (alt ? 0.4f : -0.4f);
                FireWisp(dir.Rotate(alt ? 4 : -4), 9f, false, side);
            }
            Sfx.Play(SfxId.Fire, P.transform.position, 0.35f, 1.6f);
            yield return new WaitForSeconds(0.1f / spd);
            moveMult = 1f;
            current = null;
        }

        void FireWisp(Vector2 dir, float dmg, bool omega, Vector2 offset)
        {
            var h = MakeHit(dmg, MoveSlot.Attack, omega, 0.8f, dir, DamageType.Fire);
            var pr = Projectile.Fire(P, Team.Player, P.Pos + dir * 0.8f + offset, dir, omega ? 15f : 17f, h, Wisp, omega ? 0.32f : 0.24f, 13f);
            pr.homing = omega ? 360f : 220f;
            bool ink = M.Flag("tm_tinta");
            if (ink) pr.onHit = (p, c) => { Combat.Deal(c, p.hit); c.status.Apply(StatusType.Vulnerable, 1, 3f, 0.1f); };
        }

        float GlyphRadius => M.Flag("tm_azif") ? 3.35f : 2.4f;

        public override void Special()
        {
            if (Time.time < specialReadyAt) return;
            Run(SpecialCo());
        }

        IEnumerator SpecialCo()
        {
            FaceAim();
            specialReadyAt = Time.time + 0.75f / SpSpd;
            busyUntil = Time.time + 0.15f;
            moveMult = 0.5f;
            Vector2 target = P.ClampedAimPoint(8f);
            Sfx.Play(SfxId.Cast, P.transform.position, 0.6f, 1.3f);
            ThrowGlyph(target, 45f, GlyphRadius, false);
            yield return new WaitForSeconds(0.12f);
            moveMult = 1f;
            current = null;
        }

        void ThrowGlyph(Vector2 at, float dmg, float radius, bool omega)
        {
            var go = Fx.MeshObj("Glifo", MeshGen.Quad, Mats.NewUnlit(Glyph.WithA(0.9f), Mats.Blend.Additive, Tex.ElderSign, 2.5f, false), at.X0Z(0.08f), Quaternion.Euler(90, 0, 0), Vector3.one * radius * 2f);
            Runner.Run(GlyphCo(go, at, dmg, radius, omega));
        }

        IEnumerator GlyphCo(GameObject go, Vector2 at, float dmg, float radius, bool omega)
        {
            float t = 0, delay = 0.55f;
            while (t < delay && go != null)
            {
                t += Time.deltaTime;
                go.transform.Rotate(0, 0, 220f * Time.deltaTime, Space.Self);
                go.transform.localScale = Vector3.one * radius * 2f * (0.6f + 0.4f * t / delay);
                yield return null;
            }
            if (go != null) Destroy(go);
            if (World.Root == null) yield break;
            Boom(at, radius, dmg, omega);
            if (M.Flag("tm_persistente")) Hazards.Spawn("cthugha", at, radius * 0.8f, 3f, 1, false);
            if (M.Flag("tm_dee"))
            {
                float frac = M.Flag("tm_dee3") ? 0.8f : M.Flag("tm_dee2") ? 0.6f : 0.4f;
                yield return new WaitForSeconds(0.4f);
                if (World.Root != null) Boom(at, radius, dmg * frac, omega);
            }
        }

        void Boom(Vector2 at, float radius, float dmg, bool omega)
        {
            Fx.Ring(at.X0Z(), radius, Glyph, 0.35f);
            Fx.GroundFlash(at.X0Z(), radius, Glyph, 0.3f);
            Fx.Burst(at.X0Z(0.6f), Glyph, 18, 7f, 0.3f, 0.5f);
            Fx.Light(at.X0Z(1.2f), Glyph, 8, 5, 0.3f);
            Sfx.Play(SfxId.Explosion, at.X0Z(), 0.6f, 1.3f);
            CameraRig.Shake(0.15f);
            var h = MakeHit(dmg, MoveSlot.Special, omega, 6f, Vector2.up, DamageType.Fire);
            Combat.Explode(at, radius, h, Team.Player, 6f);
        }

        protected override void OmegaAttack() => Run(OmegaAttackCo());

        IEnumerator OmegaAttackCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.35f;
            moveMult = 0.3f;
            int n = M.Flag("tm_paginas") ? 10 : 6;
            Sfx.Play(SfxId.Magic, P.transform.position, 1f, 0.8f);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(-50, 50, i / (float)(n - 1));
                FireWisp(dir.Rotate(a), 18f, true, Vector2.zero);
                yield return new WaitForSeconds(0.025f);
            }
            moveMult = 1f;
            current = null;
        }

        protected override void OmegaSpecial() => Run(OmegaSpecialCo());

        IEnumerator OmegaSpecialCo()
        {
            busyUntil = Time.time + 0.3f;
            Vector2 at = P.ClampedAimPoint(8f);
            float r = 3.6f * (M.Flag("tm_azif") ? 1.3f : 1f);
            var mat = Mats.NewUnlit(Glyph.WithA(0.9f), Mats.Blend.Additive, Tex.ElderSign, 3f, false);
            var go = Fx.MeshObj("Circulo", MeshGen.Quad, mat, at.X0Z(0.08f), Quaternion.Euler(90, 0, 0), Vector3.one * r * 2f);
            Sfx.Play(SfxId.Portal, at.X0Z());
            Runner.Run(CircleCo(go, mat, at, r));
            yield return null;
            current = null;
        }

        IEnumerator CircleCo(GameObject go, Material mat, Vector2 at, float r)
        {
            float t = 0, dur = 1.3f;
            while (t < dur && go != null)
            {
                t += Time.deltaTime;
                go.transform.Rotate(0, 0, -300f * Time.deltaTime, Space.Self);
                foreach (var c in Combat.InCircle(at, r * 1.4f, Team.Player))
                    if (!c.isBoss) c.MoveBy((at - c.Pos).SafeNormal(Vector2.zero) * 6f * Time.deltaTime);
                if (Random.value < 0.5f) Fx.Emit(FxKind.Glow, (at + Random.insideUnitCircle * r).X0Z(0.3f), Vector3.up * 3f, Glyph, 0.3f, 0.6f);
                yield return null;
            }
            if (go != null) Destroy(go);
            Destroy(mat);
            if (World.Root == null) yield break;
            Boom(at, r, 100f, true);
            CameraRig.Shake(0.4f);
        }
    }
}
