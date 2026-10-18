using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    /// <summary>
    /// Prueba automática de humo (solo se activa con el argumento -umbral-smoke).
    /// Juega sola: menú → santuario → incursión → recompensas → los cuatro guardianes, sacando capturas
    /// y registrando cualquier excepción. Útil para verificar builds sin intervención.
    /// </summary>
    public class SmokeTest : MonoBehaviour
    {
        string outDir;
        int errors;
        StreamWriter log;
        bool autopilot;
        float nextAtk;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-umbral-smoke")) return;
            int i = Array.IndexOf(args, "-umbral-out");
            pendingDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "smoke");
            var go = new GameObject("SmokeTest");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>();
        }

        static string pendingDir;

        void Awake()
        {
            outDir = pendingDir;
            Directory.CreateDirectory(outDir);
            log = new StreamWriter(Path.Combine(outDir, "smoke_log.txt")) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Save.slot = 9;
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                errors++;
                log.WriteLine($"[{type}] {msg}\n{stack}");
            }
        }

        void Note(string s) { log.WriteLine($"[{Time.realtimeSinceStartup:F1}] {s}"); }

        IEnumerator Shot(string name, float wait = 0.6f)
        {
            yield return new WaitForSecondsRealtime(wait);
            ScreenCapture.CaptureScreenshot(Path.Combine(outDir, name + ".png"));
            Note("captura " + name);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("01_menu");

            // nueva partida (hija)
            Save.Delete();
            Save.Data.female = true;
            Save.Data.started = true;
            foreach (var c in MetaDB.Cards) if (c.unlock.Values.All(v => v == 0)) { Save.Data.cards.Add(c.id); Save.Data.activeCards.Add(c.id); }
            UIRoot.Instance.CloseAll();
            Game.Instance.EnterHub(true);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("02_dialogo_intro", 0.3f);
            UIDialogue_Force(true);
            yield return new WaitForSecondsRealtime(1.5f);
            UIDialogue_Force(false);

            // desbloquear todo para ver el santuario completo
            foreach (var r in MetaDB.Rituals) if (!Save.Data.rituals.Contains(r.id)) Save.Data.rituals.Add(r.id);
            Save.Data.familiars.Add("fam_gato"); Save.Data.equippedFamiliar = "fam_gato";
            Save.Data.Inc("wilbur_defeated");
            foreach (var id in Res.AllIds) Save.Data.AddRes(id, 200);
            Game.Instance.EnterHub(false);
            yield return new WaitForSecondsRealtime(2.5f);
            UIDialogue_Force(true);
            yield return new WaitForSecondsRealtime(1f);
            UIDialogue_Force(false);
            yield return Shot("03_santuario");
            UIRoot.Instance.OpenGrimorio(); yield return Shot("04_grimorio"); UIRoot.Instance.CloseAll();
            UIRoot.Instance.OpenCaldero(); yield return Shot("05_caldero"); UIRoot.Instance.CloseAll();
            UIRoot.Instance.OpenWeaponAltar(); yield return Shot("06_armas"); UIRoot.Instance.CloseAll();
            UIRoot.Instance.TalkTo("keziah"); yield return Shot("07_keziah", 1.2f); UIDialogue_Force(true); yield return new WaitForSecondsRealtime(1f); UIDialogue_Force(false);

            // incursión con cada arma: probar movimientos
            foreach (var w in Weapons.All)
            {
                Save.Data.lastWeapon = w.id;
                Game.Instance.StartRun();
                yield return new WaitForSecondsRealtime(3f);
                UIRoot.Instance.CloseAll();
                autopilot = true;
                yield return new WaitForSecondsRealtime(6f);
                yield return Shot("10_combate_" + w.id, 0f);
                yield return FinishRoom(25f);
                autopilot = false;
                if (w.id == "baculo")
                {
                    // recompensa → elección de boon
                    var pick = FindAnyObjectByType<RewardPickup>();
                    if (pick != null) { pick.GetComponent<Interactable>().onUse(); yield return Shot("11_eleccion_boon", 0.8f); ClickFirstCard(); }
                    yield return new WaitForSecondsRealtime(1.5f);
                    yield return Shot("12_puertas");
                    // tienda y pantalla de boones
                    Game.Instance.EnterRoom(new RoomPlan { kind = RoomKind.Shop, reward = RewardType.Shop });
                    yield return new WaitForSecondsRealtime(2f);
                    yield return Shot("13_tienda");
                }
                Game.Instance.AbandonRun();
                yield return new WaitForSecondsRealtime(2.5f);
            }

            // guardianes de cada región
            Save.Data.lastWeapon = "baculo";
            for (int b = 0; b < 4; b++)
            {
                Game.Instance.StartRun();
                yield return new WaitForSecondsRealtime(3f);
                UIRoot.Instance.CloseAll();
                var run = RunState.Current;
                // regalar boones para ver efectos
                BoonManager.Grant(BoonDB.Get(new[] { "cth_attack", "cthg_attack", "nod_attack", "ith_attack" }[b]), Rarity.Epic);
                BoonManager.Grant(BoonDB.Get(new[] { "has_cast", "cthg_cast", "nod_cast", "ith_cast" }[b]), Rarity.Rare);
                run.biome = b;
                run.room = Biomes.Get(b).rooms - 2;
                Game.Instance.EnterRoom(new RoomPlan { kind = RoomKind.Combat, reward = RewardType.Gold });
                yield return new WaitForSecondsRealtime(3f);
                yield return Shot($"20_region{b}_sala", 0.5f);
                autopilot = true;
                yield return new WaitForSecondsRealtime(4f);
                yield return Shot($"21_region{b}_combate", 0f);
                autopilot = false;
                Game.Instance.EnterRoom(new RoomPlan { kind = RoomKind.Boss, reward = RewardType.Boss });
                yield return new WaitForSecondsRealtime(2.5f);
                yield return Shot($"22_jefe{b}_intro", 0.2f);
                UIDialogue_Force(true);
                yield return new WaitForSecondsRealtime(2f);
                UIDialogue_Force(false);
                autopilot = true;
                yield return new WaitForSecondsRealtime(6f);
                yield return Shot($"23_jefe{b}_combate", 0f);
                // forzar fases
                foreach (var boss in Boss.Active.ToArray())
                {
                    if (boss == null) continue;
                    boss.hp = boss.maxHp * 0.3f;
                }
                yield return new WaitForSecondsRealtime(5f);
                yield return Shot($"24_jefe{b}_fase", 0f);
                foreach (var boss in Boss.Active.ToArray()) if (boss != null) { boss.hp = boss.maxHp * 0.2f; }
                yield return new WaitForSecondsRealtime(5f);
                yield return Shot($"25_jefe{b}_fase2", 0f);
                foreach (var boss in Boss.Active.ToArray()) if (boss != null) Combat.Deal(boss, Hit.Proc(99999, PlayerController.Instance, DamageType.Void));
                yield return new WaitForSecondsRealtime(5f);
                UIDialogue_Force(true);
                yield return Shot($"26_jefe{b}_derrotado", 0f);
                autopilot = false;
                yield return new WaitForSecondsRealtime(2f);
                UIDialogue_Force(false);
                if (Game.Instance.state == GameState.Run) Game.Instance.AbandonRun();
                else ClickAnyButton();
                yield return new WaitForSecondsRealtime(3f);
                UIRoot.Instance.CloseAll();
            }

            // muerte
            Game.Instance.StartRun();
            yield return new WaitForSecondsRealtime(3f);
            var p = PlayerController.Instance;
            RunState.Current.deathDefiance = 0;
            p.invulnUntil = 0;
            p.Die(Hit.Proc(999, null, DamageType.Physical));
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("30_muerte");

            Note($"FIN — errores/excepciones: {errors}");
            log.Close();
            Application.Quit();
        }

        IEnumerator FinishRoom(float maxTime)
        {
            float t = 0;
            while (t < maxTime && RoomDirector.Current != null && !RoomDirector.Current.cleared)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (RoomDirector.Current != null && !RoomDirector.Current.cleared)
                foreach (var e in Enemy.Alive_.ToArray()) if (e != null && !e.isDummy) Combat.Deal(e, Hit.Proc(99999, PlayerController.Instance, DamageType.Void));
            yield return new WaitForSecondsRealtime(2f);
        }

        static void UIDialogue_Force(bool v) => UIRoot.ForceAdvance = v;

        void ClickFirstCard()
        {
            var b = UIRoot.Instance.menus.GetComponentsInChildren<Button>().FirstOrDefault();
            if (b != null) b.onClick.Invoke();
        }

        void ClickAnyButton()
        {
            var b = UIRoot.Instance.menus.GetComponentsInChildren<Button>().FirstOrDefault();
            if (b != null) b.onClick.Invoke();
        }

        void Update()
        {
            var p = PlayerController.Instance;
            if (p == null) return;
            // nunca morir durante la prueba (salvo al final)
            if (autopilot) { p.hp = Mathf.Max(p.hp, p.maxHp * 0.6f); p.magick = p.MaxMagick; }
            if (!autopilot || UIRoot.MenuOpen) return;
            var target = Combat.Nearest(p.Pos, Team.Player, 40f);
            if (target == null) return;
            Vector2 d = target.Pos - p.Pos;
            p.aimDir = d.normalized;
            p.aimPoint = target.Pos;
            if (d.magnitude > 2.2f) p.MoveBy(d.normalized * 6f * Time.deltaTime);
            p.Face(d);
            if (Time.time > nextAtk && p.weapon.CanStart)
            {
                nextAtk = Time.time + 0.25f;
                float r = UnityEngine.Random.value;
                if (r < 0.6f) p.weapon.Attack();
                else if (r < 0.85f) p.weapon.Special();
                else p.cast.Released(0.05f);
            }
        }
    }
}
