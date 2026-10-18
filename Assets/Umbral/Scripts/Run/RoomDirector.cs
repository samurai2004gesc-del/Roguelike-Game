using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Director de sala: construye la sala, gestiona oleadas de enemigos, detecta cuándo se ha limpiado,
    /// hace aparecer la recompensa y abre las puertas hacia la siguiente sala (con su recompensa visible).
    /// </summary>
    public class RoomDirector : MonoBehaviour
    {
        public static RoomDirector Current;
        public static bool InCombat;
        public static float EncounterTime;
        static readonly List<Enemy> spawned = new List<Enemy>();
        public static int AliveCount => spawned.Count(e => e != null && !e.dead && e.countsForClear);

        public RoomPlan plan;
        public RoomBuilder.Layout layout;
        public BiomeDef biome;
        public bool cleared, doorsOpen;
        readonly List<Door> doors = new List<Door>();
        float tindalosTimer;

        public static void Register(Enemy e) { if (e != null) spawned.Add(e); }

        public static RoomDirector Build(RoomPlan plan, RunState run)
        {
            World.Reset();
            spawned.Clear();
            InCombat = false;
            Combatant.All.RemoveAll(c => c == null);
            var go = new GameObject("RoomDirector");
            go.transform.SetParent(World.Root, false);
            var d = go.AddComponent<RoomDirector>();
            Current = d;
            d.plan = plan;
            d.biome = Biomes.Get(run.biome);
            var rng = run.rng;

            float w = 25f, h = 21f; int shape = rng.Next(0, 5); int obstacles = rng.Next(2, 6); bool pits = run.biome >= 1 && rng.Chance(0.5f);
            switch (plan.kind)
            {
                case RoomKind.Boss: w = 32; h = 27; shape = 2; obstacles = run.biome == 3 ? 0 : 4; pits = false; break;
                case RoomKind.Shop: case RoomKind.Fountain: case RoomKind.Antesala: case RoomKind.Chaos: w = 19; h = 16; obstacles = 0; pits = false; shape = 4; break;
                case RoomKind.Elite: w = 28; h = 23; break;
                case RoomKind.Wilbur: w = 26; h = 22; obstacles = 2; pits = false; break;
            }
            d.layout = RoomBuilder.Generate(rng, w, h, shape, obstacles, pits);
            Arena.Current = d.layout.arena;
            RoomBuilder.ApplyLighting(d.biome);
            RoomBuilder.BuildVoid(d.biome, Vector2.zero, run.biome == 3 || plan.kind == RoomKind.Chaos);
            RoomBuilder.BuildFloor(d.biome, d.layout.arena, rng.Next());
            RoomBuilder.Dress(d.biome, d.layout.arena, rng);
            RoomBuilder.BuildMotes(d.biome, Vector2.zero, Mathf.Max(w, h));
            return d;
        }

        public void Begin()
        {
            var p = PlayerController.Instance;
            p.Teleport(layout.entry);
            p.Face(layout.up);
            p.OnRoomStart();
            CameraRig.Instance.SetTarget(p.transform, true);
            CameraRig.Instance.SetZoom(plan.kind == RoomKind.Boss ? 1.12f : 1f);
            var run = RunState.Current;
            foreach (var f in run.mods.onRoomStart.ToArray()) f();
            Music.Play(plan.kind == RoomKind.Boss || plan.kind == RoomKind.Wilbur ? "boss" : (plan.kind == RoomKind.Shop || plan.kind == RoomKind.Fountain || plan.kind == RoomKind.Antesala ? "calm" : biome.music));
            // portal de entrada (decorativo)
            Fx.Ring(layout.entry.X0Z(), 2.5f, biome.veins, 0.8f);

            switch (plan.kind)
            {
                case RoomKind.Combat: StartCoroutine(Encounter(false)); break;
                case RoomKind.Elite: StartCoroutine(Encounter(true)); break;
                case RoomKind.Boss: StartCoroutine(BossFight()); break;
                case RoomKind.Wilbur: StartCoroutine(WilburFight()); break;
                case RoomKind.Shop:
                    Shop.Build(this, layout, run);
                    if (Save.Data.HasRitual("r_manantial") && run.rng.Chance(0.3f)) Fountain.Build(layout.center + Perp(4f), run);
                    Clear(false);
                    break;
                case RoomKind.Fountain:
                    Fountain.Build(layout.center, run);
                    Clear(false);
                    break;
                case RoomKind.Antesala:
                    if (Save.Data.HasRitual("r_mercaderes")) Shop.Build(this, layout, run, small: true);
                    Fountain.Build(layout.center - Perp(4.5f), run);
                    Clear(false);
                    break;
                case RoomKind.Chaos:
                    InCombat = false;
                    cleared = true;
                    Props.Emblem(World.Root, RewardType.Chaos, null, (layout.center + layout.up * 6).X0Z(4f), 6f);
                    RewardPickup.Spawn(layout.center, RewardType.Chaos, null, OpenDoors);
                    break;
            }
            Game.Instance.ShowRoomTitle(plan, run);
        }

        Vector2 Perp(float d) => new Vector2(layout.up.y, -layout.up.x) * d;

        void Update()
        {
            if (InCombat) EncounterTime += Time.deltaTime;
            var run = RunState.Current;
            if (run == null) return;
            run.time += Time.deltaTime;
            run.biomeTime += Time.deltaTime;
            if (run.vowTimer && run.biomeTime > 540f && plan.kind != RoomKind.Boss)
            {
                tindalosTimer -= Time.deltaTime;
                if (tindalosTimer <= 0)
                {
                    tindalosTimer = 9f;
                    var p = PlayerController.Instance;
                    if (p != null)
                    {
                        UIRoot.Instance?.Toast("¡Los Sabuesos de Tindalos te han encontrado!", new Color(0.3f, 0.8f, 1f));
                        var pos = layout.arena.RandomPoint(run.rng, 0.6f, 4f, p.Pos);
                        var e = Enemy.Spawn(EnemyDB.Get("tindalos"), pos, false, 0.8f);
                        e.countsForClear = false;
                    }
                }
            }
        }

        // ---------------------------------------------------------------- combate
        IEnumerator Encounter(bool elite)
        {
            var run = RunState.Current;
            var rng = run.rng;
            InCombat = true;
            EncounterTime = 0;
            yield return new WaitForSeconds(0.6f);
            int waves = elite ? 2 : (run.room <= 1 && run.biome == 0 ? 1 : rng.Next(2, 4));
            var pool = EnemyDB.OfBiome(run.biome).ToList();
            var elites = EnemyDB.ElitesOf(run.biome).ToList();
            for (int w = 0; w < waves; w++)
            {
                float budget = (2.5f + run.biome * 1.6f + run.room * 0.45f + w * 0.8f) * run.vowEnemyCountMult;
                if (elite && w == 0 && elites.Count > 0) { SpawnOne(elites.Pick(rng), false); budget -= 3f; }
                if (elite && w == waves - 1) budget *= 1.2f;
                int guard = 0;
                while (budget > 0.5f && guard++ < 20)
                {
                    var choices = pool.Where(e => e.threat <= budget + 0.5f).ToList();
                    if (choices.Count == 0) break;
                    var d = choices.Pick(rng);
                    bool makeElite = run.biome >= 1 && rng.Chance(0.05f + run.biome * 0.04f) && d.threat >= 2;
                    SpawnOne(d, makeElite);
                    budget -= d.threat * (makeElite ? 2 : 1);
                }
                yield return new WaitForSeconds(1.0f);
                // siguiente oleada cuando quedan pocos
                int threshold = w == waves - 1 ? 0 : 1;
                while (AliveCount > threshold) yield return new WaitForSeconds(0.25f);
                if (w < waves - 1) yield return new WaitForSeconds(0.6f);
            }
            while (AliveCount > 0) yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(0.4f);
            Clear(true);
        }

        void SpawnOne(EnemyDef d, bool elite)
        {
            var run = RunState.Current;
            var p = PlayerController.Instance;
            var pos = layout.arena.RandomPoint(run.rng, d.radius + 0.4f, 5f, p != null ? p.Pos : layout.entry);
            Register(Enemy.Spawn(d, pos, elite, Random.Range(0.6f, 1.2f)));
        }

        IEnumerator BossFight()
        {
            var run = RunState.Current;
            InCombat = false;
            yield return new WaitForSeconds(0.5f);
            Vector2 at = layout.center + layout.up * 4f;
            var bosses = new List<Boss>();
            switch (run.biome)
            {
                case 0: bosses.Add(Boss.Create<PickmanBoss>("boss_pickman", at, biome.bossTitle)); break;
                case 1:
                    {
                        var dg = Boss.Create<DagonBoss>("boss_dagon", at + Perp(3.5f), "Señor de los Profundos");
                        var hy = Boss.Create<HydraBoss>("boss_hydra", at - Perp(3.5f), "Madre de los Profundos");
                        dg.partner = hy; hy.partner = dg;
                        dg.OnDefeated += b => { if (hy != null && hy.Alive) hy.OnPartnerDeath(); };
                        hy.OnDefeated += b => { if (dg != null && dg.Alive) dg.OnPartnerDeath(); };
                        bosses.Add(dg); bosses.Add(hy);
                        break;
                    }
                case 2: bosses.Add(Boss.Create<PriestBoss>("boss_sacerdote", at, biome.bossTitle)); break;
                default: bosses.Add(Boss.Create<NyarlathotepBoss>("boss_nyarlathotep", at, "El Faraón Negro")); break;
            }
            foreach (var b in bosses) { Register(b); b.AttackDelay(2.5f); }
            Save.Data.Codex(bosses[0].def.id);
            Sfx.Play(SfxId.BossRoar);
            CameraRig.Shake(0.5f);
            // diálogo de presentación
            bool done = false;
            UIRoot.Instance.ShowBossIntro(bosses[0], () => done = true);
            while (!done) yield return null;
            InCombat = true;
            EncounterTime = 0;
            foreach (var b in bosses) if (b != null) b.AttackDelay(0.8f);
            while (AliveCount > 0) yield return new WaitForSeconds(0.2f);
            InCombat = false;
            yield return new WaitForSeconds(1.8f);
            if (run.biome == 3) { Game.Instance.OnRunVictory(); yield break; }
            UIRoot.Instance.ShowBossOutro(bosses[0].def.id, () => { });
            Clear(true);
        }

        IEnumerator WilburFight()
        {
            var run = RunState.Current;
            run.wilburMet = true;
            yield return new WaitForSeconds(0.5f);
            var w = Boss.Create<WilburBoss>("boss_wilbur", layout.center + layout.up * 4f, "Tu hermano mayor");
            w.SetMaxHp(Mathf.Round(900f * (1f + run.biome * 0.5f) * run.vowEnemyHpMult));
            Register(w);
            w.AttackDelay(2f);
            bool done = false;
            UIRoot.Instance.ShowBossIntro(w, () => done = true);
            while (!done) yield return null;
            InCombat = true;
            EncounterTime = 0;
            while (AliveCount > 0) yield return new WaitForSeconds(0.2f);
            Save.Data.Inc("wilbur_defeated");
            Save.Data.AddAffinity("wilbur", 0);
            yield return new WaitForSeconds(1f);
            Clear(true);
        }

        // ---------------------------------------------------------------- final de sala
        void Clear(bool combat)
        {
            if (cleared) return;
            cleared = true;
            InCombat = false;
            var run = RunState.Current;
            var p = PlayerController.Instance;
            foreach (var pr in Projectile.Active.ToArray()) if (pr != null && pr.team == Team.Enemy) pr.End();
            if (combat)
            {
                run.roomsCleared++;
                p.OnRoomCleared();
                BoonManager.OnRoomCleared();
                if (p.familiar != null) p.familiar.OnRoomCleared();
                Sfx.Play(SfxId.RoomClear);
                UIRoot.Instance?.Toast("Sala despejada", new Color(0.85f, 0.8f, 1f));
                Fx.Ring(p.transform.position, 12f, new Color(0.7f, 0.6f, 1f), 1f, 0.05f, 1f);
            }
            Save.Write();
            RewardType r = plan.reward;
            if (plan.kind == RoomKind.Boss)
            {
                bool first = Save.Data.Count(Biomes.Get(run.biome).bossId) <= 1;
                r = first ? RewardType.Lagrima : RewardType.Ensueno;
            }
            if (plan.kind == RoomKind.Wilbur) r = RewardType.Licor;
            if (plan.kind == RoomKind.Shop || plan.kind == RoomKind.Fountain || plan.kind == RoomKind.Antesala) r = RewardType.None;
            if (combat && Save.Data.HasRitual("r_azathoth") && plan.kind == RoomKind.Combat && run.rng.Chance(0.14f)) ChaosGate.Spawn(this, layout.center - Perp(6f));

            if (r != RewardType.None)
                StartCoroutine(SpawnRewardDelayed(r));
            else OpenDoors();
        }

        IEnumerator SpawnRewardDelayed(RewardType r)
        {
            yield return new WaitForSeconds(0.5f);
            var p = PlayerController.Instance;
            Vector2 at = layout.arena.IsWalkable(layout.center, 1f) ? layout.center : layout.arena.Resolve(layout.center, 1f);
            if (p != null && Vector2.Distance(p.Pos, at) < 1.5f) at += layout.up * 2.5f;
            RewardPickup.Spawn(at, r, plan.god, OpenDoors);
        }

        public void OpenDoors()
        {
            if (doorsOpen) return;
            doorsOpen = true;
            var run = RunState.Current;
            var plans = Rewards.NextDoors(run);
            for (int i = 0; i < plans.Count; i++)
            {
                int slot = plans.Count == 1 ? 1 : plans.Count == 2 ? (i == 0 ? 0 : 2) : i;
                var door = Door.Create(layout.doorSlots[slot], layout.up, plans[i], biome);
                doors.Add(door);
                door.Unlock();
            }
            Sfx.Play(SfxId.DoorOpen);
        }
    }

    /// <summary>Puerta-portal hacia la siguiente sala, con el emblema de su recompensa.</summary>
    public class Door : MonoBehaviour
    {
        public RoomPlan plan;
        bool open;
        Material portalMat;
        Light glow;

        public static Door Create(Vector2 at, Vector2 up, RoomPlan plan, BiomeDef b)
        {
            var go = new GameObject("Puerta");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            go.transform.rotation = U.LookFlat(-up);
            var d = go.AddComponent<Door>();
            d.plan = plan;
            d.BuildArch(b, true);
            Props.Emblem(go.transform, plan.reward, plan.god, new Vector3(0, 4.4f, 0), 1.1f);
            if (plan.kind == RoomKind.Elite)
                Kit.Part(go.transform, MeshGen.Sphere, Mats.Unlit(new Color(1f, 0.2f, 0.2f), Mats.Blend.Additive, null, 3f, false), new Vector3(0, 5.6f, 0), Vector3.one * 0.35f, default, "elite", false);
            var it = Interactable.Add(go, "Entrar: " + Rewards.Name(plan.reward, plan.god) + (plan.kind == RoomKind.Elite ? " (Sala de Élite)" : ""), d.Enter, 2.4f, 5.6f);
            it.usable = () => d.open;
            return d;
        }

        public static void EntranceArch(Vector2 at, Vector2 up, BiomeDef b)
        {
            var go = new GameObject("Entrada");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            go.transform.rotation = U.LookFlat(up);
            go.AddComponent<Door>().BuildArch(b, false);
        }

        void BuildArch(BiomeDef b, bool portal)
        {
            var stone = Mats.Toon(Color.Lerp(b.floorA, Color.black, 0.2f), null, 1f);
            Kit.Part(transform, MeshGen.Cube, stone, new Vector3(1.5f, 1.6f, 0), new Vector3(0.6f, 3.2f, 0.6f));
            Kit.Part(transform, MeshGen.Cube, stone, new Vector3(-1.5f, 1.6f, 0), new Vector3(0.6f, 3.2f, 0.6f));
            Kit.Part(transform, MeshGen.Cube, stone, new Vector3(0, 3.35f, 0), new Vector3(3.8f, 0.5f, 0.7f));
            Kit.Part(transform, MeshGen.Crystal(4, 0.5f), stone, new Vector3(0, 3.6f, 0), new Vector3(0.6f, 0.8f, 0.6f));
            if (!portal) return;
            portalMat = Mats.NewUnlit(b.veins.WithA(0.15f), Mats.Blend.Additive, Tex.Swirl, 1.5f, false);
            var q = Kit.Part(transform, MeshGen.Quad, portalMat, new Vector3(0, 1.6f, 0), new Vector3(2.4f, 3.1f, 1f), new Vector3(0, 180, 0), "portal", false);
            q.gameObject.AddComponent<Spin>().speed = 0;
            glow = Kit.PointLight(transform, b.veins, 5f, 0.3f, new Vector3(0, 1.8f, 0.8f));
        }

        public void Unlock()
        {
            open = true;
            if (portalMat != null) portalMat.SetColor(Mats.BaseColorId, Rewards.ColorOf(plan.reward, plan.god).WithA(0.9f));
            if (glow != null) { glow.intensity = 3f; glow.color = Rewards.ColorOf(plan.reward, plan.god); }
            Fx.Burst(transform.position + Vector3.up * 1.6f, Rewards.ColorOf(plan.reward, plan.god), 20, 5f, 0.3f, 0.6f);
        }

        void Update()
        {
            if (portalMat != null && open) portalMat.SetTextureOffset("_BaseMap", new Vector2(0, -Time.time * 0.3f));
        }

        void Enter()
        {
            if (!open) return;
            open = false;
            Sfx.Play(SfxId.Portal, transform.position);
            Game.Instance.EnterRoom(plan);
        }

        void OnDestroy() { if (portalMat != null) Destroy(portalMat); }
    }

    /// <summary>Grieta hacia la corte de Azathoth (como las Puertas del Caos). Cuesta vida entrar.</summary>
    public class ChaosGate : MonoBehaviour
    {
        public static void Spawn(RoomDirector d, Vector2 at)
        {
            if (!d.layout.arena.IsWalkable(at, 1f)) at = d.layout.arena.Resolve(at, 1f);
            var go = new GameObject("GrietaAzathoth");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var mat = Mats.NewUnlit(new Color(1f, 0.3f, 0.85f, 0.9f), Mats.Blend.Additive, Tex.Swirl, 2.5f, false);
            var q = Fx.MeshObj("grieta", MeshGen.Quad, mat, at.X0Z(0.08f), Quaternion.Euler(90, 0, 0), Vector3.one * 2.6f);
            q.transform.SetParent(go.transform, true);
            q.AddComponent<Spin>().speed = 120f;
            Kit.PointLight(go.transform, new Color(1f, 0.3f, 0.85f), 5f, 2.5f, Vector3.up);
            Props.Emblem(go.transform, RewardType.Chaos, null, new Vector3(0, 1.6f, 0), 0.8f);
            var it = Interactable.Add(go, "Entrar en la Grieta de Azathoth (cuesta 15% de vida)", () =>
            {
                var p = PlayerController.Instance;
                float cost = Mathf.Round(p.maxHp * 0.15f);
                if (p.hp <= cost + 1) { UIRoot.Instance.Toast("No te queda suficiente vida", new Color(1f, 0.4f, 0.4f)); return; }
                p.hp -= cost;
                UIFloat.Number(p.transform.position + Vector3.up * 2, "-" + cost, new Color(1f, 0.3f, 0.6f), 1.2f);
                Sfx.Play(SfxId.Portal);
                Game.Instance.EnterRoom(new RoomPlan { kind = RoomKind.Chaos, reward = RewardType.Chaos }, sameRoomIndex: true);
            }, 2f, 2.6f);
        }
    }

    /// <summary>Manantial de Kadath: cura una vez.</summary>
    public class Fountain : MonoBehaviour
    {
        bool used;
        public static void Build(Vector2 at, RunState run)
        {
            var go = new GameObject("Manantial");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var stone = Mats.Toon(new Color(0.55f, 0.55f, 0.6f), null, 1f);
            Kit.Part(go.transform, MeshGen.Cylinder, stone, new Vector3(0, 0.3f, 0), new Vector3(2.4f, 0.3f, 2.4f));
            var water = Kit.Part(go.transform, MeshGen.Cylinder, Mats.Unlit(new Color(0.3f, 0.75f, 1f, 0.85f), Mats.Blend.Alpha, null, 1.6f, false), new Vector3(0, 0.62f, 0), new Vector3(2.1f, 0.02f, 2.1f), default, "agua", false);
            Kit.Part(go.transform, MeshGen.Crystal(6, 0.3f), Mats.Toon(new Color(0.4f, 0.8f, 1f), new Color(0.2f, 0.5f, 0.8f), 1f), new Vector3(0, 0.6f, 0), new Vector3(0.5f, 1.6f, 0.5f));
            Kit.PointLight(go.transform, new Color(0.4f, 0.8f, 1f), 6f, 2.5f, new Vector3(0, 1.5f, 0));
            var f = go.AddComponent<Fountain>();
            var it = Interactable.Add(go, "Beber del Manantial de Kadath (cura 40%)", f.Use, 2.4f, 2.6f);
            it.usable = () => !f.used;
        }

        void Use()
        {
            if (used) return;
            used = true;
            var p = PlayerController.Instance;
            p.HealScaled(p.maxHp * 0.4f);
            Sfx.Play(SfxId.Heal, transform.position);
            Fx.Burst(p.transform.position + Vector3.up, new Color(0.4f, 0.9f, 1f), 30, 5f, 0.3f, 0.8f, FxKind.Star);
            foreach (Transform c in transform) if (c.name == "agua") c.gameObject.SetActive(false);
        }
    }
}
