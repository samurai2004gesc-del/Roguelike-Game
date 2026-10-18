using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Enemigo genérico dirigido por datos. Aparece con un aviso, persigue/rodea/flota según su estilo,
    /// elige ataques con avisos en el suelo (ventana para esquivar con el esprint) y muere con estilo.
    /// </summary>
    public class Enemy : Combatant
    {
        public static readonly List<Enemy> Alive_ = new List<Enemy>();
        public EnemyDef def;
        public float damageMult = 1f, speedMult = 1f;
        protected bool spawning, attacking;
        protected readonly Dictionary<AttackDef, float> cds = new Dictionary<AttackDef, float>();
        protected float nextAttackAt;
        protected Vector2 vel;
        protected float strafeSign = 1f;
        protected float thinkTimer;
        protected Coroutine attackCo;
        public bool countsForClear = true;

        public static Enemy Spawn(EnemyDef def, Vector2 at, bool elite, float delay = 0.9f)
        {
            var go = new GameObject(def.id);
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var e = go.AddComponent<Enemy>();
            e.Setup(def, elite);
            e.StartCoroutine(e.SpawnCo(delay));
            return e;
        }

        public virtual void Setup(EnemyDef d, bool elite)
        {
            def = d;
            team = Team.Enemy;
            codexId = d.id;
            displayName = elite ? d.name + " (Élite)" : d.name;
            radius = d.radius * (elite ? 1.25f : 1f);
            height = d.height * (elite ? 1.25f : 1f);
            knockbackResist = d.knockResist + (elite ? 0.3f : 0f);
            isElite = elite || d.eliteOnly;
            flying = d.flying;
            var run = RunState.Current;
            float hpMult = run != null ? run.vowEnemyHpMult : 1f;
            float depthMult = run != null ? 1f + run.depth * 0.012f : 1f;
            SetMaxHp(Mathf.Round(d.hp * hpMult * depthMult * (elite ? 1.7f : 1f)));
            maxArmor = d.armor * hpMult * (elite ? 1.5f : 1f);
            if (elite && maxArmor <= 0) maxArmor = Mathf.Round(maxHp * 0.5f);
            if (run != null && maxArmor <= 0 && run.vowArmorChance > 0 && run.rng.Chance(run.vowArmorChance)) maxArmor = Mathf.Round(maxHp * 0.4f);
            armor = maxArmor;
            damageMult = elite ? 1.25f : 1f;
            rig = ArtIA.SpriteRig(transform, d.id == "boss_wilbur" ? "wilbur" : d.id, d.height, d.radius) ?? (d.model != null ? d.model(transform) : CharacterArt.Zoog(transform, 1f));
            if (elite) rig.transform.localScale *= 1.25f;
            rig.speedRef = d.speed;
            rig.Init();
            nextAttackAt = Time.time + Random.Range(0.6f, 1.4f);
            strafeSign = Random.value < 0.5f ? -1 : 1;
            Save.Data.Codex(d.id);
        }

        protected override void OnEnable() { base.OnEnable(); Alive_.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Alive_.Remove(this); }

        protected virtual IEnumerator SpawnCo(float delay)
        {
            spawning = true;
            targetable = false;
            rig.gameObject.SetActive(false);
            var tele = Telegraph.Circle(Pos, radius + 0.6f, delay, new Color(0.6f, 0.3f, 1f));
            for (float t = 0; t < delay; t += Time.deltaTime)
            {
                if (Random.value < 0.4f) Fx.Emit(FxKind.Glow, transform.position + Random.insideUnitSphere * radius, Vector3.up * 3f, new Color(0.5f, 0.2f, 0.9f), 0.3f, 0.5f);
                yield return null;
            }
            rig.gameObject.SetActive(true);
            targetable = true;
            Fx.Smoke(transform.position + Vector3.up * 0.4f, new Color(0.12f, 0.05f, 0.18f, 0.8f), 8, 1.2f);
            Fx.Burst(transform.position + Vector3.up, new Color(0.6f, 0.3f, 1f), 12, 4f, 0.3f, 0.4f);
            float t2 = 0; Vector3 full = rig.transform.localScale;
            while (t2 < 0.25f) { t2 += Time.deltaTime; rig.transform.localScale = full * U.EaseOutBack(Mathf.Clamp01(t2 / 0.25f)); yield return null; }
            rig.transform.localScale = full;
            spawning = false;
            UIFloat.Track(this);
        }

        public void AttackDelay(float seconds) { nextAttackAt = Time.time + seconds; }

        protected Combatant Target
        {
            get
            {
                if (status.Has(StatusType.Traidor))
                {
                    var other = Combatant.All.Where(c => c != null && c != this && c.Alive && c.team == Team.Enemy && !c.isDummy).OrderBy(c => (c.Pos - Pos).sqrMagnitude).FirstOrDefault();
                    if (other != null) return other;
                }
                var p = PlayerController.Instance;
                return p != null && p.Alive ? p : null;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (dead || spawning) return;
            var tgt = Target;
            float dt = Time.deltaTime;
            if (!status.CanAct)
            {
                if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; attacking = false; ClearTelegraphs(); }
                rig.velocity = Vector2.zero;
                return;
            }
            if (tgt == null) { rig.velocity = Vector2.zero; return; }

            if (!attacking)
            {
                Think(tgt, dt);
                TryAttack(tgt);
            }
            if (!isBoss) Separate(dt);
            rig.velocity = vel;
        }

        protected virtual void Think(Combatant tgt, float dt)
        {
            Vector2 to = tgt.Pos - Pos;
            float dist = to.magnitude;
            Vector2 dir = to / Mathf.Max(0.01f, dist);
            float spd = def.speed * speedMult * status.SpeedMult();
            Vector2 want = Vector2.zero;
            thinkTimer -= dt;
            if (thinkTimer <= 0) { thinkTimer = Random.Range(1.2f, 2.6f); if (Random.value < 0.4f) strafeSign = -strafeSign; }
            switch (def.move)
            {
                case MoveStyle.Chase:
                    if (dist > def.preferredRange + tgt.radius) want = dir * spd;
                    else want = new Vector2(-dir.y, dir.x) * strafeSign * spd * 0.3f;
                    break;
                case MoveStyle.Kite:
                    if (dist < def.preferredRange * 0.7f) want = -dir * spd;
                    else if (dist > def.preferredRange * 1.25f) want = dir * spd;
                    else want = new Vector2(-dir.y, dir.x) * strafeSign * spd * 0.6f;
                    break;
                case MoveStyle.Hover:
                    {
                        float r = def.preferredRange;
                        Vector2 tangent = new Vector2(-dir.y, dir.x) * strafeSign;
                        want = (tangent * 0.8f + dir * Mathf.Clamp((dist - r) * 0.5f, -1f, 1f)).normalized * spd;
                    }
                    break;
                case MoveStyle.Ambush:
                    if (dist > def.preferredRange) want = dir * spd * 0.6f;
                    break;
            }
            vel = Vector2.Lerp(vel, want, 1f - Mathf.Exp(-8f * dt));
            MoveBy(vel * dt);
            if (to.sqrMagnitude > 0.01f) FaceTowards(dir, dt * 8f);
        }

        protected void FaceTowards(Vector2 dir, float k)
        {
            facing = Vector2.Lerp(facing, dir, Mathf.Clamp01(k)).SafeNormal(dir);
            transform.rotation = U.LookFlat(facing);
        }

        protected void FaceNow(Vector2 dir)
        {
            facing = dir.SafeNormal(facing);
            transform.rotation = U.LookFlat(facing);
        }

        void Separate(float dt)
        {
            foreach (var e in Alive_)
            {
                if (e == this || e == null || e.dead) continue;
                Vector2 d = Pos - e.Pos;
                float min = radius + e.radius;
                float m = d.magnitude;
                if (m < min && m > 0.001f) MoveBy(d / m * (min - m) * 0.5f * Mathf.Min(1f, dt * 20f));
            }
        }

        protected virtual void TryAttack(Combatant tgt)
        {
            if (Time.time < nextAttackAt || def.attacks.Count == 0) return;
            float dist = Vector2.Distance(Pos, tgt.Pos) - tgt.radius;
            var options = def.attacks.Where(a => dist <= a.range && dist >= a.minRange && (!cds.TryGetValue(a, out var cd) || Time.time >= cd)).ToList();
            if (options.Count == 0) return;
            float total = options.Sum(a => a.weight);
            float r = Random.value * total;
            AttackDef pick = options[0];
            foreach (var a in options) { r -= a.weight; if (r <= 0) { pick = a; break; } }
            attackCo = StartCoroutine(DoAttack(pick, tgt));
        }

        protected IEnumerator DoAttack(AttackDef a, Combatant tgt)
        {
            attacking = true;
            vel = Vector2.zero;
            cds[a] = Time.time + a.cooldown;
            yield return AttackRunner.Run(this, a, tgt);
            attacking = false;
            attackCo = null;
            nextAttackAt = Time.time + Random.Range(0.35f, 0.9f) / Mathf.Max(0.2f, def.aggression);
        }

        // --------------- avisos activos para cancelarlos si se congela/aturde ---------------
        public readonly List<Telegraph> telegraphs = new List<Telegraph>();
        public void ClearTelegraphs() { foreach (var t in telegraphs) if (t != null) t.Kill(); telegraphs.Clear(); }

        public override void Die(Hit killingBlow)
        {
            if (dead) return;
            base.Die(killingBlow);
            ClearTelegraphs();
            StopAllCoroutines();
            Sfx.Play(SfxId.EnemyDeath, transform.position, isElite ? 1f : 0.8f, isElite ? 0.7f : 1f);
            Fx.Burst(transform.position + Vector3.up * height * 0.5f, def.accent, isElite ? 30 : 16, 7f, 0.35f, 0.6f);
            Fx.Smoke(transform.position + Vector3.up * 0.5f, new Color(0.08f, 0.04f, 0.1f, 0.85f), isElite ? 14 : 8, 1.2f);
            if (isElite) CameraRig.Shake(0.35f);
            if (!string.IsNullOrEmpty(def.deathSpawn))
            {
                var sd = EnemyDB.Get(def.deathSpawn);
                for (int i = 0; i < def.deathSpawnCount; i++)
                {
                    var p = Pos + Random.insideUnitCircle * 1.2f;
                    if (Arena.Current != null) p = Arena.Current.Resolve(p, 0.5f);
                    RoomDirector.Register(Enemy.Spawn(sd, p, false, 0.3f));
                }
            }
            targetable = false;
            Runner.Run(DeathCo());
        }

        IEnumerator DeathCo()
        {
            if (rig != null) yield return rig.DeathAnim(0.4f);
            if (this != null) Destroy(gameObject);
        }
    }
}
