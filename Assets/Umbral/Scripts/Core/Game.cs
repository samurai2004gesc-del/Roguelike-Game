using System.Collections;
using UnityEngine;

namespace Umbral
{
    public enum GameState { Boot, MainMenu, Hub, Run, Transition }

    /// <summary>
    /// Bucle principal estilo Hades: Menú → Umbral (santuario) → Incursión (salas) → Muerte/Victoria → Umbral...
    /// Todo el juego se construye por código desde aquí; basta con pulsar Play en cualquier escena.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game Instance;
        public GameState state = GameState.Boot;
        Transform persistent;
        Transform menuTarget;
        bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            if (FindAnyObjectByType<Game>() != null) return;
            var go = new GameObject("UMBRAL");
            go.AddComponent<Game>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            persistent = transform;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;

            // desactivar cámaras/luces de la escena por defecto
            foreach (var c in FindObjectsByType<Camera>()) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional) l.gameObject.SetActive(false);

            Save.Load();
            UIKit.Init();
            CameraRig.Create(persistent);
            Fx.Init(persistent);
            Sfx.Init(persistent);
            Sfx.volume = 0.8f * GameSettings.SfxVolume;
            Music.Init(persistent);
            UIFloat.Create(persistent);
            UIRoot.Create(persistent);
            BoonManager.HookEvents();
            World.Reset();
        }

        void Start()
        {
            if (TestHooks.SkipMenu) return;
            ToMainMenu();
        }

        // =====================================================================
        // MENÚ PRINCIPAL
        // =====================================================================
        public void ToMainMenu()
        {
            StopAllCoroutines();
            busy = false;
            HitStop.Clear();
            RunState.Current = null;
            DestroyPlayer();
            World.Reset();
            state = GameState.MainMenu;
            var hub = new BiomeDef
            {
                id = "menu", floorA = new Color(0.25f, 0.2f, 0.35f), floorB = new Color(0.3f, 0.24f, 0.4f), crack = new Color(0.05f, 0.02f, 0.08f), veins = new Color(0.7f, 0.6f, 1f),
                voidA = new Color(0.01f, 0.0f, 0.04f), voidB = new Color(0.2f, 0.06f, 0.35f), voidC = new Color(0.4f, 0.85f, 1f),
                sun = new Color(0.85f, 0.8f, 1f), ambient = new Color(0.25f, 0.2f, 0.35f), vignette = new Color(0.05f, 0, 0.12f), motes = new Color(0.8f, 0.85f, 1f), sunIntensity = 1f
            };
            RoomBuilder.ApplyLighting(hub);
            RoomBuilder.BuildVoid(hub, Vector2.zero, true);
            RoomBuilder.BuildMotes(hub, Vector2.zero, 30);
            var cam = CameraRig.Instance;
            Vector2 right = Vector2.right.Rotate(-cam.yaw);
            menuTarget = World.Child("MenuFocus");
            menuTarget.position = Vector3.zero;
            // isla con Sothis contemplando el abismo
            var island = new Arena();
            var pts = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < 10; i++) pts.Add(right * 5f + U.Dir(i * 36f) * (3.2f + (i % 2) * 0.5f));
            island.SetPolygon(pts);
            RoomBuilder.BuildFloor(hub, island, 3);
            var model = new GameObject("SothisMenu");
            model.transform.SetParent(World.Root, false);
            model.transform.position = (right * 5f).X0Z();
            model.transform.rotation = U.LookFlat(-right.Rotate(-30));
            CharacterArt.Protagonist(model.transform, Save.Data.female);
            for (int i = 0; i < 7; i++)
            {
                var g = Kit.Glow(World.Root, Color.HSVToRGB(i / 7f, 0.5f, 1f), Random.Range(0.6f, 2f), (right * 5f + Random.insideUnitCircle * 9f).X0Z(Random.Range(1f, 6f)), 2.2f);
                g.gameObject.AddComponent<Floater>().amp = 0.6f;
            }
            cam.SetTarget(menuTarget, true);
            cam.SetZoom(0.75f);
            cam.SetOffset(right * 1.5f);
            Music.Play("menu");
            UIRoot.Instance.OpenMainMenu();
        }

        // =====================================================================
        // SANTUARIO
        // =====================================================================
        public void EnterHub(bool fromMenu) { if (!busy) StartCoroutine(EnterHubCo(fromMenu)); }

        IEnumerator EnterHubCo(bool fromMenu)
        {
            busy = true;
            yield return UIRoot.Instance.FadeOut(0.4f);
            UIRoot.Instance.CloseAll();
            HitStop.Clear();
            RunState.Current = null;
            StatusEvents.Clear();
            DestroyPlayer();
            Vector2 spawn = HubBuilder.Build();
            var p = PlayerController.Spawn(persistent, Save.Data.lastWeapon, Save.Data.female);
            p.inHub = true;
            p.Teleport(spawn);
            p.Face(Vector2.up.Rotate(-CameraRig.Instance.yaw));
            RefreshFamiliar();
            var cam = CameraRig.Instance;
            cam.SetOffset(Vector2.zero);
            cam.SetZoom(1.05f);
            cam.SetTarget(p.transform, true);
            state = GameState.Hub;
            Save.Write();
            yield return UIRoot.Instance.FadeIn(0.6f);
            busy = false;
            if (!Save.Data.seenDialogue.Contains("umr_intro")) UIRoot.Instance.TalkTo("umr");
            else if (fromMenu) UIRoot.Instance.Banner("El Umbral", "El espacio entre los espacios", new Color(0.8f, 0.75f, 1f), 2.5f);
        }

        public void OnRitualDone(string id)
        {
            var s = Save.Data;
            if (id == "r_fam_gato" && !s.familiars.Contains("fam_gato")) { s.familiars.Add("fam_gato"); if (string.IsNullOrEmpty(s.equippedFamiliar)) s.equippedFamiliar = "fam_gato"; }
            if (id == "r_fam_zoog" && !s.familiars.Contains("fam_zoog")) { s.familiars.Add("fam_zoog"); if (string.IsNullOrEmpty(s.equippedFamiliar)) s.equippedFamiliar = "fam_zoog"; }
            if (id == "r_epilogo") UIRoot.Instance.Toast("Habla con 'Umr at-Tawil...", new Color(0.85f, 0.8f, 1f), 4f);
            Save.Write();
            if (state == GameState.Hub) StartCoroutine(RebuildHubCo());
        }

        IEnumerator RebuildHubCo()
        {
            var p = PlayerController.Instance;
            Vector2 pos = p != null ? p.Pos : Vector2.zero;
            yield return null;
            HubBuilder.Build();
            if (p != null) p.Teleport(pos);
            RefreshFamiliar();
        }

        public void RefreshFamiliar()
        {
            var p = PlayerController.Instance;
            if (p == null) return;
            if (p.familiar != null) Destroy(p.familiar.gameObject);
            p.familiar = null;
            var f = Save.Data.equippedFamiliar;
            if (!string.IsNullOrEmpty(f) && Save.Data.familiars.Contains(f)) p.familiar = Familiar.Spawn(p, f);
        }

        void DestroyPlayer()
        {
            var p = PlayerController.Instance;
            if (p != null)
            {
                if (p.familiar != null) Destroy(p.familiar.gameObject);
                Destroy(p.gameObject);
            }
            PlayerController.Instance = null;
        }

        // =====================================================================
        // INCURSIÓN
        // =====================================================================
        public void StartRun() { if (!busy) StartCoroutine(StartRunCo()); }

        IEnumerator StartRunCo()
        {
            busy = true;
            Sfx.Play(SfxId.Portal);
            yield return UIRoot.Instance.FadeOut(0.6f);
            DestroyPlayer();
            var s = Save.Data;
            var run = RunState.Begin(s.lastWeapon);
            var p = PlayerController.Spawn(persistent, s.lastWeapon, s.female);
            p.inHub = false;
            BoonManager.Recompute();
            p.hp = p.maxHp; p.magick = p.MaxMagick;
            RefreshFamiliar();
            state = GameState.Run;
            s.Inc("reached_bosque");
            var first = new RoomPlan { kind = RoomKind.Combat, reward = RewardType.Boon, god = BoonManager.PickGod(run.rng) };
            run.room = 0;
            yield return EnterRoomCo(first, false, true);
            UIRoot.Instance.Banner(Biomes.Get(0).name, Biomes.Get(0).subtitle, new Color(0.6f, 1f, 0.7f), 3f);
            if (run.pendingStartBoon)
            {
                run.pendingStartBoon = false;
                string god = BoonManager.PickGod(run.rng);
                UIRoot.Instance.OpenBoonChoice(god, BoonManager.RollGodOffer(god), null);
            }
            busy = false;
        }

        public void EnterRoom(RoomPlan plan, bool sameRoomIndex = false)
        {
            if (busy) return;
            StartCoroutine(EnterRoomWrapper(plan, sameRoomIndex));
        }

        IEnumerator EnterRoomWrapper(RoomPlan plan, bool same)
        {
            busy = true;
            if (plan.kind == RoomKind.Exit) yield return NextBiomeCo();
            else yield return EnterRoomCo(plan, same, false);
            busy = false;
        }

        IEnumerator EnterRoomCo(RoomPlan plan, bool sameIndex, bool alreadyFaded)
        {
            var run = RunState.Current;
            if (!alreadyFaded) yield return UIRoot.Instance.FadeOut(0.3f);
            UIRoot.Instance.CloseAll();
            HitStop.Clear();
            Projectile.Active.Clear();
            Boss.Active.Clear();
            UIFloat.ClearAll();
            if (!sameIndex) run.room++;
            run.depth++;
            var d = RoomDirector.Build(plan, run);
            d.Begin();
            if (PlayerController.Instance != null && PlayerController.Instance.familiar != null)
                PlayerController.Instance.familiar.transform.position = PlayerController.Instance.transform.position;
            yield return UIRoot.Instance.FadeIn(0.35f);
        }

        IEnumerator NextBiomeCo()
        {
            var run = RunState.Current;
            yield return UIRoot.Instance.FadeOut(0.6f);
            run.biome++;
            run.room = 0;
            run.biomeTime = 0;
            run.rescueUsed = false;
            var b = Biomes.Get(run.biome);
            Save.Data.Inc("reached_" + b.id);
            var p = PlayerController.Instance;
            p.HealScaled(p.maxHp * 0.25f);
            var first = new RoomPlan { kind = RoomKind.Combat, reward = RewardType.Boon, god = BoonManager.PickGod(run.rng, run.lastGodOffered) };
            yield return EnterRoomCo(first, false, true);
            UIRoot.Instance.Banner(b.name, b.subtitle, b.veins, 3.5f);
        }

        public void ShowRoomTitle(RoomPlan plan, RunState run)
        {
            if (plan.kind == RoomKind.Elite) UIRoot.Instance.Toast("Sala de Élite: un horror poderoso te espera", new Color(1f, 0.45f, 0.4f));
            if (plan.kind == RoomKind.Shop) UIRoot.Instance.Toast("Una galera negra de Leng ha atracado aquí", new Color(1f, 0.75f, 0.4f));
            if (plan.kind == RoomKind.Antesala) UIRoot.Instance.Toast("Antesala del Guardián", new Color(0.85f, 0.8f, 1f));
            if (plan.kind == RoomKind.Chaos) UIRoot.Instance.Toast("La Corte del Sultán Ciego...", new Color(1f, 0.3f, 0.8f));
        }

        public void SetDarkness(bool dark)
        {
            var run = RunState.Current;
            if (run == null) return;
            RoomBuilder.ApplyLighting(Biomes.Get(run.biome), dark);
        }

        // =====================================================================
        // MUERTE / VICTORIA
        // =====================================================================
        public void OnPlayerDeath(Hit killingBlow)
        {
            if (state != GameState.Run) return;
            state = GameState.Transition;
            string killer = killingBlow.source != null ? killingBlow.source.displayName : (killingBlow.tag == "veneno" ? "Veneno" : "");
            StartCoroutine(DeathCo(killer));
        }

        IEnumerator DeathCo(string killer)
        {
            var run = RunState.Current;
            var s = Save.Data;
            s.Inc("deaths");
            if (GameSettings.GodMode) s.Inc("godmode_deaths");
            if (!string.IsNullOrEmpty(killer)) s.Inc("killed_by_" + killer);
            Save.Write();
            Sfx.Play(SfxId.PlayerDeath);
            HitStop.Slow(0.2f, 1.6f);
            CameraRig.Shake(0.8f);
            var p = PlayerController.Instance;
            if (p != null)
            {
                Fx.Burst(p.transform.position + Vector3.up, PlayerController.IridColor(), 60, 8f, 0.4f, 1.2f, FxKind.Star);
                p.rig.gameObject.SetActive(false);
            }
            Music.Stop();
            yield return new WaitForSecondsRealtime(2.0f);
            bool done = false;
            UIRoot.Instance.OpenDeathScreen(run, killer, () => done = true);
            while (!done) yield return null;
            EnterHub(false);
        }

        public void AbandonRun()
        {
            if (state != GameState.Run) return;
            state = GameState.Transition;
            Save.Data.Inc("deaths");
            Save.Write();
            EnterHub(false);
        }

        public void OnRunVictory()
        {
            StartCoroutine(VictoryCo());
        }

        IEnumerator VictoryCo()
        {
            var run = RunState.Current;
            var s = Save.Data;
            state = GameState.Transition;
            bool first = s.Count("wins") == 0;
            s.Inc("wins");
            s.Inc("win_" + run.weaponId);
            s.bestFear = Mathf.Max(s.bestFear, run.fear);
            if (s.bestTime <= 0 || run.time < s.bestTime) s.bestTime = run.time;
            if (run.fear > 0) { run.AddResource(Res.Lagrima, 1 + run.fear / 5); }
            Save.Write();
            Sfx.Play(SfxId.Bell);
            Music.Play("calm");
            yield return UIRoot.Instance.FadeOut(1.2f);
            yield return UIRoot.Instance.FadeIn(0.4f);
            bool done = false;
            UIRoot.Instance.OpenVictoryScreen(run, first, () => done = true);
            while (!done) yield return null;
            EnterHub(false);
        }
    }

    /// <summary>Ganchos para pruebas automáticas.</summary>
    public static class TestHooks
    {
        public static bool SkipMenu;
    }
}
