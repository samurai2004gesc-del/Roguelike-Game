using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Base de las armas. Flujo de entrada como en Hades II:
    ///  · Pulsar Ataque/Especial = movimiento normal (con búfer de entrada y combos).
    ///  · Mantener pulsado = cargar la versión Omega si tienes Lucidez suficiente (si no, repite el movimiento).
    ///  · Soltar con la carga completa = movimiento Omega.
    ///  · El esprint cancela cualquier movimiento.
    /// </summary>
    public abstract class Weapon : MonoBehaviour
    {
        public PlayerController P;
        public WeaponDef def;
        protected static PlayerModifiers M => RunState.Current?.mods ?? PlayerModifiers.Default;

        protected float busyUntil;
        protected Coroutine current;
        public float moveMult = 1f;
        public bool Busy => Time.time < busyUntil;
        public virtual bool CanStart => !Busy && !charging;

        // carga Omega
        public bool charging;
        public MoveSlot chargeSlot;
        protected float chargeStart;
        bool chargeReadySignaled;
        Transform chargeRing;
        Material chargeMat;

        protected int combo;
        protected float comboUntil;
        protected Transform model;

        public virtual float AttackOmegaCost => 20f;
        public virtual float SpecialOmegaCost => 30f;
        public virtual float ChargeTime => 0.5f / Mathf.Max(0.1f, M.omegaChargeMult);
        public float ChargeProgress => charging ? Mathf.Clamp01((Time.time - chargeStart) / ChargeTime) : 0;
        protected float AtkSpd => Mathf.Max(0.3f, M.attackSpeedMult) * (P != null ? P.status.ActionSpeedMult() : 1f);
        protected float SpSpd => Mathf.Max(0.3f, M.specialSpeedMult);

        public float OmegaCost(MoveSlot s) => (s == MoveSlot.Attack ? AttackOmegaCost : SpecialOmegaCost) * M.omegaCostMult;
        public bool CanAffordOmega(MoveSlot s) => P.magick >= OmegaCost(s) - 0.01f;

        public virtual void Init(PlayerController p, WeaponDef d)
        {
            P = p; def = d;
            BuildModel(P.rig.weaponPivot);
            OnUpgradesChanged();
        }

        protected abstract void BuildModel(Transform pivot);
        public virtual void OnUpgradesChanged() { }

        // ---------------- entradas ----------------
        public abstract void Attack();
        public abstract void Special();
        protected abstract void OmegaAttack();
        protected abstract void OmegaSpecial();

        public virtual void AttackHeld(float heldTime)
        {
            if (heldTime < 0.18f || Busy) return;
            if (CanAffordOmega(MoveSlot.Attack)) StartCharge(MoveSlot.Attack);
            else Attack();
        }

        public virtual void SpecialHeld(float heldTime)
        {
            if (heldTime < 0.18f || Busy) return;
            if (CanAffordOmega(MoveSlot.Special)) StartCharge(MoveSlot.Special);
            else Special();
        }

        public virtual void AttackReleased() { }
        public virtual void SpecialReleased() { }

        protected void StartCharge(MoveSlot slot)
        {
            charging = true; chargeSlot = slot; chargeStart = Time.time; chargeReadySignaled = false;
            moveMult = 0.45f;
            Sfx.Play(SfxId.Charge, transform.position, 0.6f);
            if (chargeRing == null)
            {
                chargeMat = Mats.NewUnlit(def.color.WithA(0.8f), Mats.Blend.Additive, Tex.Ring, 2.5f, false);
                var go = Fx.MeshObj("ChargeRing", MeshGen.Quad, chargeMat, P.transform.position + Vector3.up * 0.08f, Quaternion.Euler(90, 0, 0), Vector3.one);
                go.transform.SetParent(P.transform, true);
                chargeRing = go.transform;
            }
            chargeRing.gameObject.SetActive(true);
        }

        /// <summary>Llamado cada fotograma mientras se carga. Devuelve true si la carga sigue activa.</summary>
        public virtual void TickCharge(bool stillHeld)
        {
            float k = ChargeProgress;
            if (chargeRing != null)
            {
                chargeRing.localScale = Vector3.one * Mathf.Lerp(3.2f, 1.4f, k);
                chargeMat.SetColor(Mats.BaseColorId, (k >= 1 ? Color.white : def.color).WithA(0.4f + 0.6f * k));
            }
            if (Random.value < 0.6f)
            {
                Vector3 off = Random.onUnitSphere; off.y = Mathf.Abs(off.y);
                Fx.Emit(FxKind.Glow, P.transform.position + Vector3.up + off * 1.6f, -off * 4f, def.color, 0.18f, 0.35f);
            }
            if (k >= 1f && !chargeReadySignaled)
            {
                chargeReadySignaled = true;
                Sfx.Play(SfxId.ChargeReady, transform.position);
                Fx.Burst(P.transform.position + Vector3.up, Color.white, 10, 4f, 0.2f, 0.3f);
                P.rig.Flash(def.color, 0.4f);
            }
            if (!stillHeld) ReleaseCharge();
        }

        protected virtual void ReleaseCharge()
        {
            bool ready = ChargeProgress >= 1f;
            var slot = chargeSlot;
            EndCharge();
            if (!ready) return;
            if (!P.SpendMagick(OmegaCost(slot))) return;
            foreach (var f in M.onOmega) f(slot);
            if (slot == MoveSlot.Attack) OmegaAttack(); else OmegaSpecial();
        }

        protected void EndCharge()
        {
            charging = false;
            moveMult = 1f;
            if (chargeRing != null) chargeRing.gameObject.SetActive(false);
        }

        public virtual void Cancel()
        {
            if (current != null) StopCoroutine(current);
            current = null;
            busyUntil = 0;
            moveMult = 1f;
            if (charging) EndCharge();
        }

        // ---------------- utilidades ----------------
        protected void Run(IEnumerator co)
        {
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(co);
        }

        protected Vector2 Aim => P.aimDir;

        protected void FaceAim() => P.Face(P.aimDir);

        /// <summary>Crea el golpe aplicando el bonus "potenciado" (Paso Inefable de Hastur).</summary>
        protected Hit MakeHit(float dmg, MoveSlot slot, bool omega, float knock, Vector2 dir, DamageType type = DamageType.Physical)
        {
            var h = Hit.Make(dmg, P, slot, dir, knock);
            h.omega = omega;
            h.type = type;
            if (P.empoweredActive > 0 && (slot == MoveSlot.Attack || slot == MoveSlot.Special))
            {
                h.damage *= 1f + P.empoweredActive;
                h.tag = "empowered";
            }
            return h;
        }

        /// <summary>Golpe cuerpo a cuerpo en arco. Devuelve los enemigos alcanzados.</summary>
        protected List<Combatant> MeleeArc(Vector2 origin, Vector2 dir, float radius, float angle, Hit h, float backstabExtra = 0f)
        {
            var targets = Combat.InArc(origin, dir, radius, angle, Team.Player);
            foreach (var t in targets)
            {
                var hh = h;
                hh.dir = (t.Pos - origin).SafeNormal(dir);
                hh.origin = P.Pos;
                if (backstabExtra > 0 && Combat.IsBackstab(t, P.Pos)) hh.extraCrit += backstabExtra;
                Combat.Deal(t, hh);
            }
            // los tajos destruyen proyectiles enemigos frágiles, como en Hades
            foreach (var pr in Projectile.Active.ToArray())
            {
                if (pr == null || pr.team != Team.Enemy || !pr.reflectable) continue;
                Vector2 d = pr.pos - origin;
                if (d.magnitude <= radius && Vector2.Angle(dir, d) <= angle * 0.5f) pr.End();
            }
            return targets;
        }

        protected Projectile Shoot(Vector2 dir, float speed, Hit h, Color color, float size, float range)
        {
            var p = Projectile.Fire(P, Team.Player, P.Pos + dir * 0.8f, dir, speed, h, color, size, range);
            return p;
        }

        protected void SlashFx(Vector2 dir, float radius, float angle, Color c, bool flip = false, float dur = 0.16f)
        {
            Fx.Slash(P.transform.position, dir, radius, angle, c, dur, 0.9f, flip);
        }

        protected virtual void OnDestroy()
        {
            if (chargeMat != null) Destroy(chargeMat);
        }
    }
}
