using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Cualquier entidad que recibe daño: protagonista, enemigos, jefes, aliados (retoños) y el monigote de práctica.
    /// Vive en el plano XZ como un círculo de radio <see cref="radius"/>.
    /// </summary>
    public class Combatant : MonoBehaviour
    {
        public static readonly List<Combatant> All = new List<Combatant>();

        public Team team;
        public string displayName = "?";
        public string codexId;
        public float radius = 0.5f;
        public float maxHp = 100, hp = 100;
        public float maxArmor, armor;
        public bool isBoss, isElite, isAlly, isDummy;
        public float knockbackResist;     // 0 = vuela, 1 = inamovible
        public float height = 1.6f;       // altura para números y barras
        public bool targetable = true;
        public bool flying;               // ignora fosas (no implementado en colisión aún: solo estética)

        [NonSerialized] public StatusController status;
        [NonSerialized] public ModelRig rig;
        [NonSerialized] public Vector2 facing = Vector2.up;
        [NonSerialized] public Vector2 knockVel;
        [NonSerialized] public bool dead;
        [NonSerialized] public float invulnUntil;
        [NonSerialized] public bool invulnerable;   // invulnerabilidad sostenida (esprint, fases de jefe)
        [NonSerialized] public float lastHurtTime = -10;
        [NonSerialized] public Combatant lastAttacker;

        public event Action<Hit, float> OnHurt;      // golpe, daño final
        public event Action<Combatant> OnDied;

        public Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.z); }
            set { var p = transform.position; transform.position = new Vector3(value.x, p.y, value.y); }
        }

        public bool Alive => !dead && hp > 0;
        public bool Invulnerable => invulnerable || Time.time < invulnUntil;
        public float HpFrac => maxHp > 0 ? hp / maxHp : 0;

        protected virtual void Awake()
        {
            status = GetComponent<StatusController>() ?? gameObject.AddComponent<StatusController>();
            status.owner = this;
        }

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public void SetMaxHp(float v, bool fill = true)
        {
            maxHp = v;
            if (fill) hp = v; else hp = Mathf.Min(hp, v);
        }

        /// <summary>Movimiento resuelto contra la arena.</summary>
        public void MoveBy(Vector2 delta)
        {
            var arena = Arena.Current;
            Pos = arena != null ? arena.Move(Pos, delta, radius) : Pos + delta;
        }

        public void Teleport(Vector2 p)
        {
            var arena = Arena.Current;
            Pos = arena != null ? arena.Resolve(p, radius) : p;
        }

        public void Knock(Vector2 dir, float force)
        {
            if (armor > 0 || isBoss) force *= 0.15f;
            force *= 1f - Mathf.Clamp01(knockbackResist);
            if (force <= 0.01f) return;
            knockVel += dir.SafeNormal(Vector2.up) * force;
        }

        protected virtual void Update()
        {
            if (knockVel.sqrMagnitude > 0.01f)
            {
                MoveBy(knockVel * Time.deltaTime);
                knockVel = Vector2.Lerp(knockVel, Vector2.zero, 1f - Mathf.Exp(-9f * Time.deltaTime));
            }
            else knockVel = Vector2.zero;
        }

        /// <summary>Aplica daño ya calculado. Devuelve el daño a la vida (después de armadura).</summary>
        public virtual float ReceiveDamage(Hit hit, float amount)
        {
            if (dead) return 0;
            float toHp = amount;
            if (armor > 0 && !hit.ignoreArmor)
            {
                float absorbed = Mathf.Min(armor, amount);
                armor -= absorbed;
                toHp = amount - absorbed;
                if (armor <= 0) { armor = 0; OnArmorBroken(); }
            }
            hp -= toHp;
            if (status != null && hp > 0) status.OnDamaged(hit);
            lastHurtTime = Time.time;
            lastAttacker = hit.source;
            OnHurt?.Invoke(hit, amount);
            if (rig != null && !hit.hidden) rig.Flash(hit.type == DamageType.Physical ? Color.white : Hit.ColorOf(hit.type), isBoss ? 0.3f : (hit.secondary ? 0.3f : 0.55f));
            if (hp <= 0 && !isDummy) Die(hit);
            if (isDummy && hp <= 0) hp = maxHp;
            return toHp;
        }

        protected virtual void OnArmorBroken()
        {
            Fx.Burst(transform.position + Vector3.up * height * 0.6f, new Color(0.8f, 0.8f, 0.9f), 18, 7f, 0.25f, 0.5f);
            Sfx.Play(SfxId.ArmorBreak, transform.position);
        }

        public virtual void Die(Hit killingBlow)
        {
            if (dead) return;
            dead = true;
            hp = 0;
            OnDied?.Invoke(this);
            Combat.RaiseDeath(this, killingBlow);
        }

        public void Heal(float amount)
        {
            if (dead || amount <= 0) return;
            float before = hp;
            hp = Mathf.Min(maxHp, hp + amount);
            float healed = hp - before;
            if (healed >= 1f) UIFloat.Text(transform.position + Vector3.up * height, "+" + Mathf.RoundToInt(healed), new Color(0.5f, 1f, 0.55f), 1f);
        }

        public static IEnumerable<Combatant> Enemies(Team of)
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                if (i >= All.Count) continue;
                var c = All[i];
                if (c != null && c.Alive && c.targetable && c.team != of) yield return c;
            }
        }
    }
}
