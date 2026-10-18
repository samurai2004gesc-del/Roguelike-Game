using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    public partial class UIRoot
    {
        // ================================================================== MENÚ PRINCIPAL
        public void OpenMainMenu()
        {
            CloseAll();
            var root = PushScreen("MenuPrincipal", null, 0f);
            root.gameObject.AddComponent<NoCancel>();
            var title = UIKit.LabelAt(root, "UMBRAL", 150, new Color(0.85f, 0.8f, 1f), new Vector2(0.27f, 0.72f), Vector2.zero, new Vector2(1000, 200), TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.1f, 0.5f, 0.9f);
            title.gameObject.AddComponent<TitleGlow>();
            UIKit.LabelAt(root, "Vástago de Yog-Sothoth", 40, UIKit.Gold, new Vector2(0.27f, 0.6f), Vector2.zero, new Vector2(1000, 60), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.LabelAt(root, "«No está muerto lo que yace eternamente...»", 24, UIKit.Dim, new Vector2(0.27f, 0.54f), Vector2.zero, new Vector2(1000, 40), TextAnchor.MiddleCenter, FontStyle.Italic);

            var btns = new List<Selectable>();
            float y = 0.42f;
            if (Save.Data.started)
            {
                btns.Add(UIKit.Button(root, "Continuar", new Vector2(0.27f, y), Vector2.zero, new Vector2(460, 70), () => { PopScreen(); Game.Instance.EnterHub(true); }));
                y -= 0.085f;
            }
            btns.Add(UIKit.Button(root, "Nueva partida", new Vector2(0.27f, y), Vector2.zero, new Vector2(460, 70), () =>
            {
                if (Save.Data.started) Confirm("¿Empezar de nuevo? Se perderá todo el progreso.", OpenGenderSelect);
                else OpenGenderSelect();
            })); y -= 0.085f;
            btns.Add(UIKit.Button(root, "Opciones", new Vector2(0.27f, y), Vector2.zero, new Vector2(460, 70), OpenOptions)); y -= 0.085f;
            btns.Add(UIKit.Button(root, "Salir", new Vector2(0.27f, y), Vector2.zero, new Vector2(460, 70), () =>
            {
                Save.Write();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }));
            UIKit.LabelAt(root, "Un roguelike lovecraftiano inspirado en Hades II  ·  Unity 6", 20, UIKit.Dim, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1600, 30));
            UIKit.ChainNav(btns);
            UIKit.Select(btns[0].gameObject);
        }

        void OpenGenderSelect()
        {
            var root = PushScreen("Genero", null, 0.9f);
            UIKit.LabelAt(root, "¿Quién cruzará la Puerta?", 56, UIKit.Gold, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(1400, 80), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.LabelAt(root, "Sothis Whateley, tercer vástago de Lavinia Whateley y Yog-Sothoth, nacido en el espacio entre los espacios.", 26, UIKit.Parchment, new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(1500, 40), TextAnchor.MiddleCenter, FontStyle.Italic);
            var sels = new List<Selectable>();
            for (int i = 0; i < 2; i++)
            {
                bool female = i == 1;
                var pr = UIKit.Box(root, "Retrato", new Vector2(i == 0 ? 0.33f : 0.67f, 0.45f), Vector2.zero, new Vector2(400, 500));
                var img = UIKit.Img(pr, Color.white);
                bool prev = Save.Data.female;
                Save.Data.female = female;
                img.sprite = Portraits.Get("sothis");
                Save.Data.female = prev;
                img.preserveAspect = true;
                sels.Add(UIKit.Button(root, female ? "Hija de Yog-Sothoth" : "Hijo de Yog-Sothoth", new Vector2(i == 0 ? 0.33f : 0.67f, 0.12f), Vector2.zero, new Vector2(460, 76), () =>
                {
                    Save.Delete();
                    Save.Data.female = female;
                    Save.Data.started = true;
                    foreach (var c in MetaDB.Cards) if (c.unlock.Values.All(v => v == 0)) { Save.Data.cards.Add(c.id); Save.Data.activeCards.Add(c.id); }
                    Save.Write();
                    CloseAll();
                    Game.Instance.EnterHub(true);
                }, 28));
            }
            UIKit.ChainNav(sels, true);
            UIKit.Select(sels[0].gameObject);
        }

        public void Confirm(string question, Action yes)
        {
            var root = PushScreen("Confirmar", null, 0.85f);
            var panel = UIKit.Panel(root, "p", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 320));
            UIKit.LabelAt(panel, question, 32, UIKit.Parchment, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(820, 140));
            var a = UIKit.Button(panel, "Sí", new Vector2(0.3f, 0.2f), Vector2.zero, new Vector2(260, 64), () => { PopScreen(); yes?.Invoke(); });
            var b = UIKit.Button(panel, "No", new Vector2(0.7f, 0.2f), Vector2.zero, new Vector2(260, 64), PopScreen);
            UIKit.ChainNav(new List<Selectable> { a, b }, true);
            UIKit.Select(b.gameObject);
        }

        // ================================================================== PAUSA
        void OpenPause()
        {
            var root = PushScreen("Pausa");
            var panel = UIKit.Panel(root, "p", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 640));
            UIKit.LabelAt(panel, "PAUSA", 54, UIKit.Gold, new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(600, 80), TextAnchor.MiddleCenter, FontStyle.Bold);
            var sels = new List<Selectable>();
            float y = 0.74f;
            void Add(string t, Action a) { sels.Add(UIKit.Button(panel, t, new Vector2(0.5f, y), Vector2.zero, new Vector2(460, 64), a, 26)); y -= 0.13f; }
            Add("Continuar", PopScreen);
            bool inRun = Game.Instance.state == GameState.Run;
            if (inRun) Add("Boones y mejoras", OpenBoonList);
            Add("Opciones", OpenOptions);
            if (inRun) Add("Abandonar incursión", () => Confirm("¿Abandonar? Volverás al Umbral como si hubieras muerto (conservas los recursos).", () => { CloseAll(); Game.Instance.AbandonRun(); }));
            Add("Menú principal", () => Confirm(inRun ? "¿Salir al menú? Se perderá la incursión actual." : "¿Salir al menú principal?", () => { CloseAll(); Game.Instance.ToMainMenu(); }));
            UIKit.ChainNav(sels);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== OPCIONES
        void OpenOptions()
        {
            var root = PushScreen("Opciones");
            var panel = UIKit.Panel(root, "p", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 760));
            UIKit.LabelAt(panel, "OPCIONES", 50, UIKit.Gold, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(800, 70), TextAnchor.MiddleCenter, FontStyle.Bold);
            var sels = new List<Selectable>();
            float y = 0.79f;
            Button Toggle(Func<string> label, Action click)
            {
                Button b = null;
                b = UIKit.Button(panel, label(), new Vector2(0.5f, y), Vector2.zero, new Vector2(760, 62), () => { click(); b.GetComponentInChildren<Text>().text = label(); }, 24);
                y -= 0.105f;
                sels.Add(b);
                return b;
            }
            string Pct(float v) => Mathf.RoundToInt(v * 100) + "%";
            Toggle(() => "Volumen general: " + Pct(GameSettings.MasterVolume), () => GameSettings.MasterVolume = Cycle(GameSettings.MasterVolume));
            Toggle(() => "Música: " + Pct(GameSettings.MusicVolume), () => GameSettings.MusicVolume = Cycle(GameSettings.MusicVolume));
            Toggle(() => "Efectos: " + Pct(GameSettings.SfxVolume), () => GameSettings.SfxVolume = Cycle(GameSettings.SfxVolume));
            Toggle(() => "Temblor de cámara: " + (GameSettings.ScreenShake ? "Sí" : "No"), () => GameSettings.ScreenShake = !GameSettings.ScreenShake);
            Toggle(() => "Congelación de golpe: " + (GameSettings.HitStop ? "Sí" : "No"), () => GameSettings.HitStop = !GameSettings.HitStop);
            Toggle(() => "Números de daño: " + (GameSettings.DamageNumbers ? "Sí" : "No"), () => GameSettings.DamageNumbers = !GameSettings.DamageNumbers);
            Toggle(() => "Modo Dios: " + (GameSettings.GodMode ? "Activado" : "Desactivado"), () => { GameSettings.GodMode = !GameSettings.GodMode; if (RunState.Current != null) BoonManager.Recompute(); });
            UIKit.LabelAt(panel, "Modo Dios: recibes un 20% menos de daño, +2% por cada muerte (máx. 80%).", 18, UIKit.Dim, new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(820, 40));
            UIKit.ChainNav(sels);
            UIKit.Select(sels[0].gameObject);
        }

        static float Cycle(float v)
        {
            v = Mathf.Round(v * 10f) / 10f + 0.1f;
            return v > 1.001f ? 0f : v;
        }

        // ================================================================== BOONES
        void OpenBoonList()
        {
            var run = RunState.Current;
            if (run == null) return;
            var root = PushScreen("Boones");
            UIKit.LabelAt(root, "Favores de esta incursión", 48, UIKit.Gold, new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(1400, 70), TextAnchor.MiddleCenter, FontStyle.Bold);
            var w = Weapons.Get(run.weaponId);
            var asp = w.aspects[Mathf.Clamp(run.aspect, 0, w.aspects.Count - 1)];
            string header = $"<b>{w.name}</b> — {asp.name} (Nv{run.aspectLevel})";
            if (run.hammers.Count > 0) header += "\nMejoras: " + string.Join(", ", run.hammers.Select(h => w.hammers.First(x => x.id == h).name));
            if (!string.IsNullOrEmpty(run.hexId)) header += $"\nEco: {Hexes.Get(run.hexId).name} (Nv{run.hexLevel})";
            UIKit.LabelAt(root, header, 24, UIKit.Parchment, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(1600, 110), TextAnchor.MiddleCenter);
            var all = run.boons.Concat(run.chaosCurses).ToList();
            if (all.Count == 0) UIKit.LabelAt(root, "Aún no tienes boones. Busca puertas con el emblema de un dios.", 28, UIKit.Dim, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 60));
            int cols = 2;
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                int c = i % cols, r = i / cols;
                var rt = UIKit.Box(root, "b", new Vector2(c == 0 ? 0.27f : 0.73f, 0.72f), new Vector2(0, -r * 92), new Vector2(880, 86));
                bool curse = b.def.god == "azathoth_curse";
                Color col = curse ? new Color(1f, 0.4f, 0.8f) : BoonInstance.RarityColor(b.rarity);
                UIKit.Img(rt, new Color(0.05f, 0.03f, 0.09f, 0.95f), UIKit.panelSprite);
                UIKit.Img(UIKit.Stretch(rt, "f", -2), col.WithA(0.6f), UIKit.frameSprite);
                var ic = UIKit.Box(rt, "i", new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(62, 62), new Vector2(0, 0.5f));
                UIKit.Img(ic, b.def.God?.color ?? (curse ? new Color(0.6f, 0.1f, 0.4f) : new Color(0.6f, 1f, 0.4f)), UIKit.diamondSprite);
                string nm = $"<b>{b.def.name}</b>  <size=18><color=#{ColorUtility.ToHtmlStringRGB(col)}>{(curse ? $"Maldición · {b.chaosRoomsLeft} salas" : BoonInstance.RarityName(b.rarity) + (b.def.levelable ? $" · Nv{b.level}" : ""))}</color></size>";
                var t = UIKit.LabelAt(rt, nm + "\n<size=19>" + b.def.Describe(b.rarity, b.level) + "</size>", 22, UIKit.Parchment, new Vector2(0, 0.5f), new Vector2(88, 0), new Vector2(780, 84), TextAnchor.MiddleLeft);
                t.rectTransform.pivot = new Vector2(0, 0.5f);
            }
            var m = run.mods;
            UIKit.LabelAt(root, $"Crítico {Mathf.RoundToInt(m.critChance * 100)}%   ·   Esquiva {Mathf.RoundToInt(m.dodgeChance * 100)}%   ·   Resistencia {Mathf.RoundToInt(m.damageResist * 100)}%   ·   Último Umbral {run.deathDefiance}/{run.maxDeathDefiance}   ·   [{GameInput.Label(GameButton.Cancel)}] Cerrar", 22, UIKit.Dim, new Vector2(0.5f, 0.04f), Vector2.zero, new Vector2(1800, 40));
        }

        // ================================================================== MUERTE / VICTORIA
        public void OpenDeathScreen(RunState run, string killer, Action onContinue)
        {
            var root = PushScreen("Muerte", null, 0.9f);
            root.gameObject.AddComponent<NoCancel>();
            var t = UIKit.LabelAt(root, "LA PUERTA TE RECLAMA", 84, new Color(0.85f, 0.3f, 0.45f), new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(1600, 120), TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.AddComponent<Outline>().effectColor = Color.black;
            UIKit.LabelAt(root, string.IsNullOrEmpty(killer) ? "Has muerto." : $"Te ha derrotado: {killer}", 32, UIKit.Parchment, new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(1400, 50), TextAnchor.MiddleCenter, FontStyle.Italic);
            Summary(root, run);
            var b = UIKit.Button(root, "Volver al Umbral", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(520, 76), () => { PopScreen(); onContinue?.Invoke(); }, 30);
            UIKit.Select(b.gameObject);
        }

        public void OpenVictoryScreen(RunState run, bool first, Action onContinue)
        {
            var root = PushScreen("Victoria", null, 0.92f);
            root.gameObject.AddComponent<NoCancel>();
            var t = UIKit.LabelAt(root, "HAS CRUZADO LA ÚLTIMA PUERTA", 72, new Color(0.85f, 0.85f, 1f), new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(1700, 110), TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.AddComponent<TitleGlow>();
            string epi = first
                ? "Las colinas redondeadas de Dunwich se alzan bajo un cielo de atardecer. Huele a lluvia, a hierba, a la piedra de los círculos antiguos. Los chotacabras callan al verte.\n\nPor primera vez, Sothis respira aire que no es el de las esferas.\n\nPero el amanecer llega, y con él la llamada de la sangre. La vigilia aún no te reconoce. Volverás a intentarlo."
                : "De nuevo las colinas, de nuevo el cielo inmenso. Esta vez el mundo te recuerda un poco más: una granjera te mira sin gritar; un perro no te ladra.\n\nCada cruce desgasta la frontera. Algún día, la Puerta no se cerrará tras de ti.";
            UIKit.LabelAt(root, Lines.G(epi), 28, UIKit.Parchment, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1400, 300), TextAnchor.MiddleCenter, FontStyle.Italic);
            Summary(root, run, 0.36f);
            var b = UIKit.Button(root, "Despertar en el Umbral", new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(560, 76), () => { PopScreen(); onContinue?.Invoke(); }, 30);
            UIKit.Select(b.gameObject);
        }

        void Summary(RectTransform root, RunState run, float y = 0.45f)
        {
            if (run == null) return;
            var ts = TimeSpan.FromSeconds(run.time);
            string res = run.gained.Count == 0 ? "ninguno" : string.Join("   ", run.gained.Select(kv => $"<color=#{ColorUtility.ToHtmlStringRGB(Res.ColorOf(kv.Key))}>+{kv.Value} {Res.Name(kv.Key)}</color>"));
            string txt = $"Arma: {Weapons.Get(run.weaponId).name}     Tiempo: {ts.Minutes:00}:{ts.Seconds:00}     Salas: {run.depth}     Enemigos derrotados: {run.kills}     Boones: {run.boons.Count}\n\nRecursos obtenidos: {res}";
            UIKit.LabelAt(root, txt, 26, UIKit.Parchment, new Vector2(0.5f, y), Vector2.zero, new Vector2(1600, 200), TextAnchor.MiddleCenter);
        }
    }

    /// <summary>Brillo pulsante para títulos.</summary>
    public class TitleGlow : MonoBehaviour
    {
        Text t; Color c0;
        void Start() { t = GetComponent<Text>(); c0 = t.color; }
        void Update() { if (t != null) t.color = Color.Lerp(c0, PlayerController.IridColor(), 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 1.5f)); }
    }
}
