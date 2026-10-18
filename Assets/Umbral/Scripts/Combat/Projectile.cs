using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Proyectil genérico (jugador o enemigo): recto, teledirigido, perforante, con estela y luz.</summary>
    public class Projectile : MonoBehaviour
    {
        public static readonly List<Projectile> Active = new List<Projectile>();

        public Team team;
        public Combatant owner;
        public Hit hit;
        public Vector2 pos, vel;
        public float speed, radius = 0.35f, range = 12f, height = 1.1f;
        public int pierce;
        public float homing;            // grados por segundo
        public float accel;
        public bool reflectable = true;
        public bool ignoreWalls;
        public Action<Projectile, Combatant> onHit;
        public Action<Projectile> onEnd;
        public Color color;

        float traveled;
        readonly HashSet<Combatant> hitSet = new HashSet<Combatant>();
        bool dead;

        public static Projectile Fire(Combatant owner, Team team, Vector2 from, Vector2 dir, float speed, Hit hit, Color color, float size = 0.35f, float range = 12f)
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(World.Root, false);
            var p = go.AddComponent<Projectile>();
            p.owner = owner; p.team = team; p.pos = from; p.speed = speed;
            p.vel = dir.SafeNormal(Vector2.up) * speed; p.hit = hit; p.color = color; p.radius = size; p.range = range;
            p.hit.dir = dir.SafeNormal(Vector2.up);
            go.transform.position = from.X0Z(p.height);
            p.BuildVisual(size);
            Active.Add(p);
            return p;
        }

        void BuildVisual(float size)
        {
            Kit.Glow(transform, color, size * 2.2f, Vector3.zero, team == Team.Enemy ? 2.2f : 3f);
            Kit.Glow(transform, Color.white, size * 1.1f, Vector3.zero, 3f);
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = Mats.Unlit(color, Mats.Blend.Additive, null, 2f);
            trail.time = 0.18f;
            trail.widthMultiplier = size * 1.6f;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            trail.startColor = color; trail.endColor = color.WithA(0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.minVertexDistance = 0.1f;
        }

        void OnDestroy() { Active.Remove(this); }

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            if (homing > 0)
            {
                var target = Combat.Nearest(pos, team, 9f, null, hitSet);
                if (target != null)
                {
                    Vector2 want = (target.Pos - pos).normalized;
                    float ang = Vector2.SignedAngle(vel, want);
                    float turn = Mathf.Clamp(ang, -homing * dt, homing * dt);
                    vel = vel.Rotate(turn);
                }
            }
            if (accel != 0) { speed = Mathf.Max(1f, speed + accel * dt); vel = vel.normalized * speed; }
            Vector2 step = vel * dt;
            pos += step;
            traveled += step.magnitude;
            transform.position = pos.X0Z(height);
            hit.dir = vel.normalized;

            foreach (var c in Combatant.All.ToArray())
            {
                if (c == null || !c.Alive || !c.targetable || c.team == team || hitSet.Contains(c)) continue;
                if ((c.Pos - pos).sqrMagnitude > (c.radius + radius) * (c.radius + radius)) continue;
                hitSet.Add(c);
                var h = hit;
                h.origin = pos - vel.normalized * 2f;
                if (onHit != null) onHit(this, c);
                else Combat.Deal(c, h);
                if (pierce-- <= 0) { End(); return; }
            }

            // los proyectiles enemigos pueden ser bloqueados por la guardia de la Hoz o destruidos por el Signo
            if (traveled >= range || (!ignoreWalls && Arena.Current != null && Arena.Current.BlocksProjectile(pos))) End();
        }

        public void Reflect(Team newTeam, Combatant newOwner)
        {
            team = newTeam; owner = newOwner;
            vel = -vel; traveled = 0; hitSet.Clear();
            hit.source = newOwner;
            color = new Color(0.9f, 0.8f, 1f);
        }

        public void End()
        {
            if (dead) return;
            dead = true;
            onEnd?.Invoke(this);
            Fx.Burst(transform.position, color, 6, 3f, 0.25f, 0.3f);
            Destroy(gameObject);
        }
    }

    /// <summary>Zonas de suelo dañinas (rastros de esprint, charcos, llamas, telarañas, tinta).</summary>
    public class Hazards : MonoBehaviour
    {
        public Team team = Team.Player;
        public string kind;
        public Vector2 center;
        public float radius, until, tickEvery = 0.5f, damage;
        public bool curse;
        public StatusType? status;
        public int statusStacks = 1;
        float acc;
        Material mat;
        Color color;

        /// <summary>Zona del jugador asociada a un dios (aplica su maldición).</summary>
        public static Hazards Spawn(string god, Vector2 at, float radius, float duration, float damage, bool curse)
        {
            var g = Gods.Get(god);
            var h = Make(at, radius, duration, g != null ? g.color : Color.white, Team.Player);
            h.kind = god; h.damage = damage; h.curse = curse;
            return h;
        }

        /// <summary>Zona enemiga (veneno, tinta, telaraña...).</summary>
        public static Hazards Enemy(Vector2 at, float radius, float duration, float damage, Color color, StatusType? status = null, float tick = 0.5f)
        {
            var h = Make(at, radius, duration, color, Team.Enemy);
            h.damage = damage; h.status = status; h.tickEvery = tick;
            return h;
        }

        static Hazards Make(Vector2 at, float radius, float duration, Color color, Team team)
        {
            var go = new GameObject("Hazard");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z(0.06f);
            var h = go.AddComponent<Hazards>();
            h.center = at; h.radius = radius; h.until = Time.time + duration; h.team = team; h.color = color;
            h.mat = Mats.NewUnlit(color.WithA(0.55f), Mats.Blend.Additive, Tex.Swirl, 1.6f, false);
            var q = Fx.MeshObj("disc", MeshGen.Quad, h.mat, go.transform.position, Quaternion.Euler(90, UnityEngine.Random.Range(0, 360f), 0), Vector3.one * radius * 2.2f);
            q.transform.SetParent(go.transform, true);
            return h;
        }

        void Update()
        {
            float left = until - Time.time;
            if (left <= 0) { Destroy(gameObject); return; }
            if (mat != null) mat.SetColor(Mats.BaseColorId, color.WithA(Mathf.Clamp01(left * 2f) * 0.55f));
            transform.Rotate(0, 40f * Time.deltaTime, 0);
            acc += Time.deltaTime;
            if (acc < tickEvery) return;
            acc = 0;
            if (team == Team.Player)
            {
                foreach (var t in Combat.InCircle(center, radius, Team.Player))
                {
                    if (kind == "cthugha") t.status.Apply(StatusType.Llama, Mathf.RoundToInt(damage), 6f);
                    else
                    {
                        Combat.Deal(t, Hit.Proc(damage, PlayerController.Instance, BoonFx.TypeOf(kind), "rastro"));
                        if (curse) BoonFx.Curse(kind, t, default);
                    }
                }
            }
            else
            {
                var p = PlayerController.Instance;
                if (p != null && p.Alive && (p.Pos - center).sqrMagnitude < (radius + p.radius * 0.5f) * (radius + p.radius * 0.5f) && !p.IsDashing)
                {
                    if (damage > 0 && !status.HasValue) Combat.Deal(p, Hit.Proc(damage, null, DamageType.Earth, "charco"));
                    if (status.HasValue) p.status.Apply(status.Value, statusStacks, 1.2f, damage);
                }
            }
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }
    }
}
