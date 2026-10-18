using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Base de los Guardianes de cada región. Cada jefe tiene fases (umbral de vida), un repertorio de movimientos
    /// con pesos/enfriamientos por fase y una transición de fase con rugido e invulnerabilidad breve.
    /// </summary>
    public abstract class Boss : Enemy
    {
        public static readonly List<Boss> Active = new List<Boss>();
        public string title;
        public int phase;
        protected float[] thresholds = { 0.5f };
        protected bool transitioning;
        protected readonly List<Move> moves = new List<Move>();
        public event Action<Boss> OnDefeated;

        protected class Move
        {
            public string name;
            public int minPhase, maxPhase = 9;
            public float weight = 1f, cooldown = 3f, range = 99f, minRange;
            public Func<Combatant, IEnumerator> run;
            public float readyAt;
        }

        protected void M(string name, Func<Combatant, IEnumerator> run, float weight = 1f, float cooldown = 3f, int minPhase = 0, int maxPhase = 9, float range = 99f, float minRange = 0f)
            => moves.Add(new Move { name = name, run = run, weight = weight, cooldown = cooldown, minPhase = minPhase, maxPhase = maxPhase, range = range, minRange = minRange });

        public static T Create<T>(string defId, Vector2 at, string title) where T : Boss
        {
            var def = EnemyDB.Get(defId);
            var go = new GameObject(def.id);
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var b = go.AddComponent<T>();
            b.isBoss = true;
            b.title = title;
            b.Setup(def, false);
            b.isBoss = true;
            var run = RunState.Current;
            float mult = run != null ? run.vowBossHpMult * run.vowEnemyHpMult : 1f;
            b.SetMaxHp(Mathf.Round(def.hp * mult));
            b.armor = b.maxArmor = 0;
            b.displayName = def.name;
            b.Build();
            b.targetable = true;
            b.nextAttackAt = Time.time + 1.5f;
            return b;
        }

        protected abstract void Build();

        protected override void OnEnable() { base.OnEnable(); Active.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Active.Remove(this); }

        protected override IEnumerator SpawnCo(float delay) { yield break; }

        protected override void Update()
        {
            base.Update();
            if (dead || transitioning) return;
            if (phase < thresholds.Length && HpFrac <= thresholds[phase])
            {
                phase++;
                if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
                ClearTelegraphs();
                attacking = false;
                StartCoroutine(PhaseCo());
            }
        }

        IEnumerator PhaseCo()
        {
            transitioning = true;
            attacking = true;
            invulnerable = true;
            Sfx.Play(SfxId.BossRoar, transform.position);
            CameraRig.Shake(0.6f);
            Fx.Ring(transform.position, 7f, def.accent, 0.8f);
            rig.Flash(Color.white, 1f);
            yield return OnPhaseChange(phase);
            invulnerable = false;
            transitioning = false;
            attacking = false;
            nextAttackAt = Time.time + 0.6f;
        }

        protected virtual IEnumerator OnPhaseChange(int newPhase) { yield return new WaitForSeconds(1.2f); }

        protected override void TryAttack(Combatant tgt)
        {
            if (Time.time < nextAttackAt || transitioning) return;
            float dist = Vector2.Distance(Pos, tgt.Pos);
            var opts = moves.Where(m => phase >= m.minPhase && phase <= m.maxPhase && Time.time >= m.readyAt && dist <= m.range && dist >= m.minRange).ToList();
            if (opts.Count == 0) return;
            float total = opts.Sum(o => o.weight), r = UnityEngine.Random.value * total;
            Move pick = opts[0];
            foreach (var o in opts) { r -= o.weight; if (r <= 0) { pick = o; break; } }
            pick.readyAt = Time.time + pick.cooldown;
            attackCo = StartCoroutine(RunMove(pick, tgt));
        }

        IEnumerator RunMove(Move m, Combatant tgt)
        {
            attacking = true;
            vel = Vector2.zero;
            yield return m.run(tgt);
            attacking = false;
            attackCo = null;
            float speed = RunState.Current != null ? RunState.Current.vowBossSpeed : 1f;
            nextAttackAt = Time.time + UnityEngine.Random.Range(0.3f, 0.8f) / speed / (1f + phase * 0.25f);
        }

        // ---------- utilidades para patrones ----------
        protected IEnumerator Do(AttackDef a, Combatant tgt) { yield return AttackRunner.Run(this, a, tgt); }

        protected static AttackDef A(AtkKind k, float dmg, float windup, Action<AttackDef> cfg = null)
        {
            var a = new AttackDef { kind = k, damage = dmg, windup = windup, recover = 0.3f };
            cfg?.Invoke(a);
            return a;
        }

        protected IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        protected IEnumerator MoveTo(Vector2 p, float speed)
        {
            float t = 0;
            while ((Pos - p).magnitude > 0.3f && t < 3f)
            {
                t += Time.deltaTime;
                Vector2 d = (p - Pos).normalized;
                MoveBy(d * speed * Time.deltaTime);
                vel = d * speed;
                FaceTowards(d, Time.deltaTime * 8f);
                rig.velocity = vel;
                yield return null;
            }
            vel = Vector2.zero;
        }

        protected IEnumerator Vanish(Vector2 to, float hidden = 0.6f)
        {
            Fx.Smoke(transform.position + Vector3.up, new Color(0.1f, 0.05f, 0.15f, 0.9f), 16, 2f);
            Sfx.Play(SfxId.Portal, transform.position, 0.7f);
            rig.gameObject.SetActive(false);
            targetable = false;
            yield return new WaitForSeconds(hidden);
            Teleport(to);
            rig.gameObject.SetActive(true);
            targetable = true;
            Fx.Smoke(transform.position + Vector3.up, new Color(0.1f, 0.05f, 0.15f, 0.9f), 16, 2f);
            Fx.Burst(transform.position + Vector3.up * 2, def.accent, 20, 6f, 0.35f, 0.5f);
        }

        protected IEnumerator Summon(string id, int count, float radius = 4f)
        {
            var sd = EnemyDB.Get(id);
            if (sd == null || RoomDirector.AliveCount > 10) yield break;
            rig.Flash(new Color(0.6f, 0.3f, 1f), 0.6f);
            Sfx.Play(SfxId.Whisper, transform.position);
            for (int i = 0; i < count; i++)
            {
                var p = Arena.Current != null ? Arena.Current.RandomPoint(new System.Random(UnityEngine.Random.Range(0, 99999)), 0.6f, 3f, PlayerController.Instance != null ? PlayerController.Instance.Pos : Pos) : Pos + UnityEngine.Random.insideUnitCircle * radius;
                RoomDirector.Register(Enemy.Spawn(sd, p, false, 0.8f));
            }
            yield return new WaitForSeconds(0.6f);
        }

        /// <summary>Haces giratorios (patrón de jefe): n rayos desde el centro que barren el área.</summary>
        protected IEnumerator RotatingBeams(int n, float length, float duration, float degPerSec, float dmg, Color c)
        {
            float baseAng = UnityEngine.Random.Range(0, 360f);
            var teles = new List<Telegraph>();
            for (int i = 0; i < n; i++) { var t = Telegraph.Line(Pos, U.Dir(baseAng + i * 360f / n), length, 1.2f, 0.9f, c); teles.Add(t); telegraphs.Add(t); }
            yield return new WaitForSeconds(0.9f);
            var lines = new List<LineRenderer>();
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("RotBeam"); go.transform.SetParent(World.Root, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = Mats.Unlit(c, Mats.Blend.Additive, null, 3f);
                lr.positionCount = 2; lr.widthMultiplier = 0.9f;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lines.Add(lr);
            }
            var hitCd = new Dictionary<Combatant, float>();
            var atk = new AttackDef { damage = dmg, knock = 6 };
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (dead || !status.CanAct) break;
                for (int i = 0; i < n; i++)
                {
                    float ang = baseAng + i * 360f / n + t * degPerSec;
                    Vector2 end = Pos + U.Dir(ang) * length;
                    lines[i].SetPosition(0, Pos.X0Z(1.2f)); lines[i].SetPosition(1, end.X0Z(1.2f));
                    var p = PlayerController.Instance;
                    if (p != null && p.Alive && U.DistToSegment(p.Pos, Pos, end, out _) < 0.6f + p.radius * 0.5f)
                    {
                        if (!hitCd.TryGetValue(p, out var cd) || Time.time > cd) { hitCd[p] = Time.time + 0.6f; AttackRunner.Strike(this, atk, p, (p.Pos - Pos).normalized); }
                    }
                }
                yield return null;
            }
            foreach (var l in lines) if (l != null) Destroy(l.gameObject);
        }

        /// <summary>Lluvia de erupciones (tentáculos/columnas) alrededor del jugador.</summary>
        protected IEnumerator Eruptions(int count, float radius, float spreadR, float dmg, float windup, string visual)
        {
            var p = PlayerController.Instance;
            if (p == null) yield break;
            var pts = new List<Vector2>();
            pts.Add(p.Pos);
            for (int i = 1; i < count; i++)
            {
                var q = p.Pos + UnityEngine.Random.insideUnitCircle * spreadR;
                if (Arena.Current != null) q = Arena.Current.Resolve(q, 0.2f);
                pts.Add(q);
            }
            foreach (var q in pts) telegraphs.Add(Telegraph.Circle(q, radius, windup, Telegraph.BossColor));
            yield return new WaitForSeconds(windup);
            var atk = new AttackDef { damage = dmg, knock = 8 };
            foreach (var q in pts)
            {
                Props.SpawnEruptionVisual(visual, q, radius);
                Fx.GroundFlash(q.X0Z(), radius, def.accent, 0.3f);
                AttackRunner.HitArea(this, atk, q, radius);
            }
            Sfx.Play(SfxId.Explosion, p.transform.position, 0.6f, 0.8f);
            CameraRig.Shake(0.3f);
        }

        public override void Die(Hit killingBlow)
        {
            if (dead) return;
            Save.Data.Inc(def.id.Replace("boss_", "boss_"));
            OnDefeated?.Invoke(this);
            HitStop.Slow(0.15f, 1.4f);
            Sfx.Play(SfxId.BossRoar, transform.position, 1f, 0.7f);
            Fx.Burst(transform.position + Vector3.up * 2, def.accent, 60, 12f, 0.5f, 1.2f, FxKind.Star);
            Fx.Light(transform.position + Vector3.up * 2, def.accent, 20, 10, 1.5f);
            CameraRig.Shake(1f);
            base.Die(killingBlow);
        }
    }

    // =====================================================================
    // I. RICHARD UPTON PICKMAN
    // =====================================================================
    public class PickmanBoss : Boss
    {
        protected override void Build()
        {
            thresholds = new[] { 0.5f };
            var claw = A(AtkKind.Melee, 13, 0.55f, a => { a.radius = 3.2f; a.angle = 130; a.knock = 7; a.repeats = 3; a.repeatDelay = 0.18f; });
            var lunge = A(AtkKind.Lunge, 15, 0.6f, a => { a.length = 7f; a.width = 1f; a.knock = 9; });
            var paint = A(AtkKind.Lob, 10, 0.6f, a => { a.radius = 2.4f; a.status = StatusType.Lento; a.statusPower = 1; a.repeats = 3; a.repeatDelay = 0.25f; });
            var splatter = A(AtkKind.Projectile, 8, 0.6f, a => { a.count = 7; a.spread = 80; a.speed = 12; a.length = 14; });
            M("garras", t => Do(claw, t), 2f, 2f, range: 4f);
            M("acometida", t => Do(lunge, t), 1.2f, 3f, minRange: 3f);
            M("pintura", t => Do(paint, t), 1f, 5f);
            M("salpicadura", t => Do(splatter, t), 1f, 3.5f);
            M("excavar", Burrow, 1.2f, 8f);
            M("lienzos", t => Summon("ghoul", 2), 0.8f, 14f);
            M("retrato", Portrait, 1.2f, 7f, minPhase: 1);
            M("lienzos2", t => Summon("zoog", 4), 0.8f, 12f, minPhase: 1);
        }

        IEnumerator Burrow(Combatant t)
        {
            int times = phase >= 1 ? 3 : 2;
            Fx.Smoke(transform.position, new Color(0.3f, 0.25f, 0.2f, 0.9f), 20, 2f);
            Sfx.Play(SfxId.Growl, transform.position);
            rig.gameObject.SetActive(false); targetable = false;
            for (int i = 0; i < times; i++)
            {
                if (t == null) break;
                Vector2 at = t.Pos;
                telegraphs.Add(Telegraph.Circle(at, 2.6f, 0.9f, Telegraph.BossColor));
                yield return new WaitForSeconds(0.9f);
                Teleport(at);
                rig.gameObject.SetActive(true); targetable = true;
                Fx.Smoke(transform.position, new Color(0.3f, 0.25f, 0.2f, 0.9f), 20, 2f);
                CameraRig.Shake(0.4f);
                AttackRunner.HitArea(this, new AttackDef { damage = 16, knock = 12 }, at, 2.6f);
                Sfx.Play(SfxId.Explosion, transform.position, 0.7f, 0.7f);
                if (i < times - 1) { yield return new WaitForSeconds(0.35f); rig.gameObject.SetActive(false); targetable = false; }
            }
            rig.gameObject.SetActive(true); targetable = true;
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Portrait(Combatant t)
        {
            // pinta un "retrato" del natural: anillos de pintura en todos los colores
            rig.Flash(new Color(1f, 0.5f, 0.3f), 0.6f);
            yield return new WaitForSeconds(0.6f);
            for (int r = 0; r < 4; r++)
            {
                int n = 14;
                for (int i = 0; i < n; i++)
                {
                    var a = new AttackDef { damage = 8, speed = 8f + r, knock = 2, length = 14 };
                    var pr = AttackRunner.FireEnemyProjectile(this, a, U.Dir(i * 360f / n + r * 12));
                    pr.color = Color.HSVToRGB((i / (float)n + r * 0.2f) % 1f, 0.8f, 1f);
                }
                Sfx.Play(SfxId.Projectile, transform.position, 0.6f);
                yield return new WaitForSeconds(0.35f);
            }
            yield return new WaitForSeconds(0.4f);
        }

        protected override IEnumerator OnPhaseChange(int p)
        {
            UIRoot.Instance?.ShowBossLine(this, "¿Crees que pinto de memoria? ¡Pinto DEL NATURAL!");
            yield return Summon("ghoul", 3);
            yield return new WaitForSeconds(0.8f);
        }
    }

    // =====================================================================
    // II. PADRE DAGÓN y MADRE HIDRA (combate doble)
    // =====================================================================
    public class DagonBoss : Boss
    {
        public Boss partner;
        bool enraged;

        protected override void Build()
        {
            thresholds = new[] { 0.5f };
            var stomp = A(AtkKind.Slam, 18, 1.0f, a => { a.radius = 4.5f; a.knock = 14; });
            var charge = A(AtkKind.Charge, 18, 0.9f, a => { a.length = 13f; a.width = 1.6f; a.speed = 20f; a.knock = 14; });
            var trident = A(AtkKind.Melee, 15, 0.7f, a => { a.radius = 4.5f; a.angle = 60; a.knock = 10; a.repeats = 2; a.repeatDelay = 0.25f; });
            M("pisotón", t => Do(stomp, t), 1f, 4f, range: 5f);
            M("embestida", t => Do(charge, t), 1.2f, 4f, minRange: 4f);
            M("tridente", t => Do(trident, t), 1.5f, 2.5f, range: 5.5f);
            M("olas", Waves, 1f, 6f);
            M("marea", t => Eruptions(4 + phase * 2, 2.2f, 6f, 16, 1f, "cthulhu"), 0.8f, 7f, minPhase: 1);
        }

        IEnumerator Waves(Combatant t)
        {
            int n = enraged ? 5 : 3;
            for (int i = 0; i < n; i++)
            {
                Vector2 d = (t.Pos - Pos).SafeNormal(facing).Rotate(UnityEngine.Random.Range(-25f, 25f));
                var a = A(AtkKind.Beam, 14, 0.7f, x => { x.length = 16f; x.width = 1.2f; x.knock = 10; x.tracking = false; });
                FaceNow(d);
                yield return Do(a, t);
            }
        }

        public void OnPartnerDeath()
        {
            enraged = true;
            speedMult = 1.35f; damageMult = 1.3f;
            phase = Mathf.Max(phase, 1);
            UIRoot.Instance?.ShowBossLine(this, "¡HIDRA! ¡Las profundidades te reclamarán, criatura!");
            Sfx.Play(SfxId.BossRoar, transform.position);
        }
    }

    public class HydraBoss : Boss
    {
        public Boss partner;
        bool enraged;

        protected override void Build()
        {
            thresholds = new[] { 0.5f };
            def.move = MoveStyle.Kite;
            def.preferredRange = 8f;
            var song = A(AtkKind.Radial, 9, 0.8f, a => { a.count = 16; a.speed = 8f; a.spread = 11; a.repeats = 3; a.repeatDelay = 0.4f; a.length = 16; });
            var lob = A(AtkKind.Lob, 11, 0.7f, a => { a.radius = 2.4f; a.status = StatusType.Lento; a.statusPower = 1; a.repeats = 3; a.repeatDelay = 0.3f; });
            M("canto", t => Do(song, t), 1.5f, 4f);
            M("chorro", t => Do(lob, t), 1f, 4f);
            M("profundos", t => Summon("deepone", enraged ? 3 : 2), 0.8f, 12f);
            M("zambullida", t => Vanish(Arena.Current != null ? Arena.Current.RandomPoint(new System.Random(), 1f, 6f, t.Pos) : Pos, 0.8f), 0.8f, 6f);
            M("espiral", Spiral, 1f, 7f, minPhase: 1);
        }

        IEnumerator Spiral(Combatant t)
        {
            rig.Flash(def.accent, 0.5f);
            yield return new WaitForSeconds(0.6f);
            float ang = 0;
            for (int i = 0; i < 36; i++)
            {
                ang += 23f;
                for (int k = 0; k < 3; k++) AttackRunner.FireEnemyProjectile(this, new AttackDef { damage = 8, speed = 9f, knock = 2, length = 16 }, U.Dir(ang + k * 120));
                if (i % 4 == 0) Sfx.Play(SfxId.Projectile, transform.position, 0.4f, 1.3f);
                yield return new WaitForSeconds(0.07f);
            }
        }

        public void OnPartnerDeath()
        {
            enraged = true;
            speedMult = 1.35f; damageMult = 1.3f;
            phase = Mathf.Max(phase, 1);
            UIRoot.Instance?.ShowBossLine(this, "Mi Dagón... Tu sangre cantará por él.");
            Sfx.Play(SfxId.BossRoar, transform.position, 1f, 1.3f);
        }
    }

    // =====================================================================
    // III. EL SUMO SACERDOTE QUE NO DEBE SER DESCRITO
    // =====================================================================
    public class PriestBoss : Boss
    {
        protected override void Build()
        {
            thresholds = new[] { 0.6f, 0.25f };
            def.move = MoveStyle.Kite; def.preferredRange = 7f;
            var sign = A(AtkKind.Radial, 10, 0.8f, a => { a.count = 20; a.speed = 7.5f; a.spread = 9; a.repeats = 3; a.repeatDelay = 0.45f; a.length = 16; });
            var bolts = A(AtkKind.Projectile, 11, 0.5f, a => { a.count = 3; a.spread = 25; a.speed = 15; a.repeats = 3; a.repeatDelay = 0.25f; a.length = 15; });
            M("signo amarillo", t => Do(sign, t), 1.2f, 5f);
            M("rayos dorados", t => Do(bolts, t), 1.5f, 3f);
            M("haces", t => RotatingBeams(3 + phase, 14f, 3.5f, 45f + phase * 15f, 14, new Color(1f, 0.85f, 0.25f)), 1.2f, 8f);
            M("teletransporte", t => Vanish(Arena.Current != null ? Arena.Current.RandomPoint(new System.Random(), 1f, 6f, t.Pos) : Pos, 0.5f), 1f, 5f);
            M("hombres de leng", t => Summon("lengman", 2 + phase), 0.8f, 13f);
            M("locura", MadnessPulse, 0.8f, 10f, minPhase: 1);
            M("arañas", t => Summon("lengspider", 3), 0.7f, 14f, minPhase: 2);
            M("columnas", t => Eruptions(6 + phase * 2, 2f, 7f, 15, 1f, "hastur"), 1f, 6f, minPhase: 1);
        }

        IEnumerator MadnessPulse(Combatant t)
        {
            rig.Flash(new Color(1f, 0.9f, 0.3f), 0.8f);
            Sfx.Play(SfxId.Whisper, transform.position, 1f, 0.7f);
            telegraphs.Add(Telegraph.Circle(Pos, 9f, 1.2f, new Color(1f, 0.85f, 0.2f)));
            yield return new WaitForSeconds(1.2f);
            Fx.Ring(transform.position, 9f, new Color(1f, 0.85f, 0.2f), 0.6f);
            var p = PlayerController.Instance;
            if (p != null && (p.Pos - Pos).magnitude < 9f && !p.Invulnerable)
            {
                p.status.Apply(StatusType.Locura, 1, 2.5f);
                UIFloat.Text(p.transform.position + Vector3.up * 2.4f, "¡Has visto el Signo Amarillo!", new Color(1f, 0.85f, 0.2f), 1.1f);
            }
        }

        protected override IEnumerator OnPhaseChange(int p)
        {
            UIRoot.Instance?.ShowBossLine(this, p == 1 ? "Bajo la seda no hay rostro. Hay una puerta. Mírala." : "LOS OTROS DIOSES TE CONTEMPLAN.");
            yield return Vanish(Arena.Current != null ? Arena.Current.bounds.Center : Pos, 0.8f);
            yield return RotatingBeams(4 + p, 15f, 2.5f, 70f, 14, new Color(1f, 0.85f, 0.25f));
        }
    }

    // =====================================================================
    // IV. NYARLATHOTEP — el Caos Reptante (tres formas)
    // =====================================================================
    public class NyarlathotepBoss : Boss
    {
        protected override void Build()
        {
            thresholds = new[] { 0.66f, 0.33f };
            def.move = MoveStyle.Chase; def.preferredRange = 3f;
            // Forma 1: el Faraón Negro
            var spears = A(AtkKind.Beam, 15, 0.8f, a => { a.length = 18f; a.width = 0.8f; a.knock = 8; a.repeats = 3; a.repeatDelay = 0.2f; });
            var scarabs = A(AtkKind.Radial, 9, 0.7f, a => { a.count = 18; a.speed = 9f; a.spread = 10; a.repeats = 3; a.repeatDelay = 0.35f; a.length = 18; });
            var ankh = A(AtkKind.SlamAtTarget, 16, 0.85f, a => { a.radius = 2.6f; a.knock = 10; a.repeats = 3; a.repeatDelay = 0.25f; });
            M("lanzas de arena", t => Do(spears, t), 1.3f, 3.5f, 0, 0);
            M("escarabajos", t => Do(scarabs, t), 1.2f, 4f, 0, 0);
            M("anj", t => Do(ankh, t), 1f, 4f, 0, 0);
            M("sabuesos", t => Summon("tindalos", 2), 0.7f, 14f, 0, 0);
            // Forma 2: el Morador de las Tinieblas
            var wing = A(AtkKind.Melee, 18, 0.8f, a => { a.radius = 6f; a.angle = 220; a.knock = 14; });
            var eye = A(AtkKind.Beam, 17, 0.9f, a => { a.length = 20f; a.width = 1.2f; a.knock = 9; a.repeats = 3; a.repeatDelay = 0.3f; });
            var dive = A(AtkKind.Charge, 20, 0.9f, a => { a.length = 16f; a.width = 2f; a.speed = 26f; a.knock = 15; a.repeats = 2; a.repeatDelay = 0.4f; });
            M("alas", t => Do(wing, t), 1.2f, 3f, 1, 1, range: 7f);
            M("ojo trilobulado", t => Do(eye, t), 1.4f, 4f, 1, 1);
            M("picado", t => Do(dive, t), 1f, 5f, 1, 1);
            M("mi-go", t => Summon("migo", 2), 0.6f, 14f, 1, 1);
            M("haces negros", t => RotatingBeams(4, 16f, 3f, 60f, 16, new Color(1f, 0.25f, 0.15f)), 0.8f, 9f, 1, 1);
            // Forma 3: el Dios de las Mil Formas
            M("tentáculos", t => Eruptions(8, 2.4f, 7f, 18, 0.9f, "nyar"), 1.4f, 4f, 2, 2);
            M("espirales", Spirals, 1.2f, 6f, 2, 2);
            M("haces del caos", t => RotatingBeams(6, 18f, 3.5f, 55f, 16, new Color(1f, 0.2f, 0.3f)), 1f, 8f, 2, 2);
            M("masas", t => Summon("shoggoth_blob", 4), 0.7f, 12f, 2, 2);
            M("caos", t => Do(A(AtkKind.Radial, 10, 0.6f, a => { a.count = 24; a.speed = 10f; a.spread = 7.5f; a.repeats = 4; a.repeatDelay = 0.25f; a.length = 20; }), t), 1f, 5f, 2, 2);
        }

        IEnumerator Spirals(Combatant t)
        {
            rig.Flash(def.accent, 0.6f);
            yield return new WaitForSeconds(0.6f);
            float ang = UnityEngine.Random.Range(0, 360f);
            for (int i = 0; i < 50; i++)
            {
                ang += 17f;
                for (int k = 0; k < 4; k++) AttackRunner.FireEnemyProjectile(this, new AttackDef { damage = 9, speed = 8.5f, knock = 2, length = 18 }, U.Dir(ang + k * 90) * (k % 2 == 0 ? 1 : 1));
                if (i % 5 == 0) Sfx.Play(SfxId.Projectile, transform.position, 0.4f, 0.7f);
                yield return new WaitForSeconds(0.06f);
            }
        }

        protected override IEnumerator OnPhaseChange(int p)
        {
            if (p == 1)
            {
                UIRoot.Instance?.ShowBossLine(this, "Ah, sobrino. ¿Te gusta este rostro? Tengo mil más.");
                yield return ChangeForm(EnemyArt.Haunter, "El Morador de las Tinieblas", "nyar_morador");
                Game.Instance.SetDarkness(true);
                def.move = MoveStyle.Hover; def.preferredRange = 6f;
            }
            else
            {
                UIRoot.Instance?.ShowBossLine(this, "BASTA DE MÁSCARAS. CONTEMPLA AL CAOS QUE REPTA.");
                Game.Instance.SetDarkness(false);
                yield return ChangeForm(EnemyArt.CrawlingChaos, "El Dios de las Mil Formas", "nyar_caos");
                def.move = MoveStyle.Stationary;
                Teleport(Arena.Current != null ? Arena.Current.bounds.Center : Pos);
            }
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator ChangeForm(Func<Transform, ModelRig> builder, string newTitle, string artId)
        {
            Fx.Smoke(transform.position + Vector3.up * 2, new Color(0.05f, 0.0f, 0.05f, 0.95f), 40, 4f);
            Fx.Burst(transform.position + Vector3.up * 2, def.accent, 40, 10f, 0.5f, 0.8f, FxKind.Star);
            yield return new WaitForSeconds(0.5f);
            if (rig != null) Destroy(rig.gameObject);
            rig = ArtIA.SpriteRig(transform, artId, artId == "nyar_caos" ? 7f : 5f, 1.4f) ?? builder(transform);
            rig.Init();
            title = newTitle;
            Sfx.Play(SfxId.BossRoar, transform.position, 1f, 0.6f);
            CameraRig.Shake(0.8f);
        }

        public override void Die(Hit killingBlow)
        {
            if (Game.Instance != null) Game.Instance.SetDarkness(false);
            base.Die(killingBlow);
        }
    }

    // =====================================================================
    // RIVAL: EL ECO DE WILBUR WHATELEY
    // =====================================================================
    public class WilburBoss : Boss
    {
        protected override void Build()
        {
            thresholds = new[] { 0.5f };
            var lash = A(AtkKind.Melee, 12, 0.6f, a => { a.radius = 3.6f; a.angle = 160; a.knock = 9; a.repeats = 2; a.repeatDelay = 0.2f; });
            var bolts = A(AtkKind.Projectile, 9, 0.5f, a => { a.count = 5; a.spread = 50; a.speed = 13; a.length = 14; a.repeats = 2; a.repeatDelay = 0.3f; });
            var grab = A(AtkKind.Lunge, 14, 0.6f, a => { a.length = 6f; a.width = 1f; a.knock = 10; });
            M("tentáculos", t => Do(lash, t), 1.5f, 2.2f, range: 4.5f);
            M("conjuro", t => Do(bolts, t), 1.2f, 3f);
            M("agarre", t => Do(grab, t), 1f, 3.5f, minRange: 3f);
            M("gemelo", t => Eruptions(4, 2f, 5f, 12, 0.9f, "nyar"), 0.9f, 7f, minPhase: 1);
            M("salto", t => Vanish(t.Pos + UnityEngine.Random.insideUnitCircle.normalized * 4f, 0.4f), 0.8f, 5f);
        }

        protected override IEnumerator OnPhaseChange(int p)
        {
            UIRoot.Instance?.ShowBossLine(this, "¡Yog-Sothoth! ¡Padre! ¡AYÚDAME!");
            yield return new WaitForSeconds(0.8f);
        }
    }
}
