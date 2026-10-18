using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    public partial class UIRoot
    {
        RectTransform hudRun;
        Image hpFill, hpLag, mgFill, castFill, hexFill;
        Text hpText, mgText, goldText, roomText, resText, ammoText, castText, hexText, controlsText;
        readonly List<Image> ddPips = new List<Image>();
        readonly Image[] slotImgs = new Image[5];
        readonly Text[] slotTxt = new Text[5];
        Text extraBoons;
        RectTransform bossBars;
        readonly List<(Boss b, Image fill, Text name)> bossUi = new List<(Boss, Image, Text)>();
        float hpLagV = 1f;

        void BuildHud()
        {
            hudRun = UIKit.Stretch(hud, "Run");
            // ---------- vida y Lucidez (abajo izquierda)
            var hpBox = UIKit.Box(hudRun, "HP", new Vector2(0, 0), new Vector2(40, 96), new Vector2(460, 34), new Vector2(0, 0));
            UIKit.Img(hpBox, new Color(0.04f, 0.0f, 0.04f, 0.9f), UIKit.panelSprite);
            var lagRt = UIKit.Stretch(hpBox, "Lag", 4);
            hpLag = UIKit.Img(lagRt, new Color(1f, 0.85f, 0.6f)); hpLag.type = Image.Type.Filled; hpLag.fillMethod = Image.FillMethod.Horizontal;
            var hpRt = UIKit.Stretch(hpBox, "Fill", 4);
            hpFill = UIKit.Img(hpRt, new Color(0.85f, 0.12f, 0.2f)); hpFill.type = Image.Type.Filled; hpFill.fillMethod = Image.FillMethod.Horizontal;
            var hpFr = UIKit.Stretch(hpBox, "Fr", -3); UIKit.Img(hpFr, UIKit.Gold.WithA(0.85f), UIKit.frameSprite);
            hpText = UIKit.Label(hpBox, "", 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            hpText.rectTransform.offsetMin = new Vector2(14, 0);

            var mgBox = UIKit.Box(hudRun, "MG", new Vector2(0, 0), new Vector2(40, 66), new Vector2(380, 20), new Vector2(0, 0));
            UIKit.Img(mgBox, new Color(0.02f, 0.0f, 0.06f, 0.9f), UIKit.panelSprite);
            var mgRt = UIKit.Stretch(mgBox, "Fill", 3);
            mgFill = UIKit.Img(mgRt, new Color(0.45f, 0.75f, 1f)); mgFill.type = Image.Type.Filled; mgFill.fillMethod = Image.FillMethod.Horizontal;
            var mgFr = UIKit.Stretch(mgBox, "Fr", -2); UIKit.Img(mgFr, UIKit.Violet.WithA(0.8f), UIKit.frameSprite);
            mgText = UIKit.Label(mgBox, "", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            mgText.rectTransform.offsetMin = new Vector2(12, 0);

            var ddRow = UIKit.Box(hudRun, "DD", new Vector2(0, 0), new Vector2(46, 138), new Vector2(300, 30), new Vector2(0, 0));
            var h = ddRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6; h.childControlWidth = false; h.childControlHeight = false; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childAlignment = TextAnchor.MiddleLeft;

            // Signo y Eco
            var castBox = UIKit.Box(hudRun, "Cast", new Vector2(0, 0), new Vector2(520, 66), new Vector2(64, 64), new Vector2(0, 0));
            UIKit.Img(castBox, new Color(0.05f, 0.02f, 0.1f, 0.9f), UIKit.circleSprite);
            castFill = UIKit.Img(UIKit.Stretch(castBox, "f", 5), new Color(0.75f, 0.5f, 1f, 0.9f), UIKit.circleSprite);
            castFill.type = Image.Type.Filled; castFill.fillMethod = Image.FillMethod.Radial360;
            castText = UIKit.Label(castBox, "Q", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var hexBox = UIKit.Box(hudRun, "Hex", new Vector2(0, 0), new Vector2(596, 66), new Vector2(64, 64), new Vector2(0, 0));
            UIKit.Img(hexBox, new Color(0.05f, 0.02f, 0.1f, 0.9f), UIKit.circleSprite);
            hexFill = UIKit.Img(UIKit.Stretch(hexBox, "f", 5), new Color(0.6f, 0.95f, 1f, 0.9f), UIKit.circleSprite);
            hexFill.type = Image.Type.Filled; hexFill.fillMethod = Image.FillMethod.Radial360;
            hexText = UIKit.Label(hexBox, "R", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            // ---------- ranuras de boones (abajo derecha)
            string[] slotNames = { "ATQ", "ESP", "SIG", "ESPR", "LUC" };
            for (int i = 0; i < 5; i++)
            {
                var s = UIKit.Box(hudRun, "Slot" + i, new Vector2(1, 0), new Vector2(-40 - (4 - i) * 74, 40), new Vector2(62, 62), new Vector2(1, 0));
                slotImgs[i] = UIKit.Img(s, new Color(0.1f, 0.08f, 0.14f, 0.85f), UIKit.diamondSprite);
                slotTxt[i] = UIKit.Label(s, slotNames[i], 15, UIKit.Dim, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            extraBoons = UIKit.LabelAt(hudRun, "", 20, UIKit.Parchment, new Vector2(1, 0), new Vector2(-40, 112), new Vector2(380, 30), TextAnchor.MiddleRight);
            extraBoons.rectTransform.pivot = new Vector2(1, 0);
            ammoText = UIKit.LabelAt(hudRun, "", 26, new Color(1f, 0.4f, 0.45f), new Vector2(1, 0), new Vector2(-40, 145), new Vector2(380, 34), TextAnchor.MiddleRight, FontStyle.Bold);
            ammoText.rectTransform.pivot = new Vector2(1, 0);

            // ---------- arriba
            goldText = UIKit.LabelAt(hudRun, "", 26, Res.ColorOf("oro"), new Vector2(0, 1), new Vector2(40, -40), new Vector2(600, 36), TextAnchor.MiddleLeft, FontStyle.Bold);
            goldText.rectTransform.pivot = new Vector2(0, 1);
            roomText = UIKit.LabelAt(hudRun, "", 22, UIKit.Parchment, new Vector2(0, 1), new Vector2(40, -78), new Vector2(700, 30), TextAnchor.MiddleLeft, FontStyle.Italic);
            roomText.rectTransform.pivot = new Vector2(0, 1);
            resText = UIKit.LabelAt(hud, "", 20, UIKit.Parchment, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(500, 300), TextAnchor.UpperRight);
            resText.rectTransform.pivot = new Vector2(1, 1);
            controlsText = UIKit.LabelAt(hud, "", 18, UIKit.Dim, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(1600, 28), TextAnchor.MiddleCenter);
            controlsText.rectTransform.pivot = new Vector2(0.5f, 0);

            bossBars = UIKit.Box(hud, "BossBars", new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1100, 200), new Vector2(0.5f, 1));
            var v = bossBars.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6; v.childControlHeight = false; v.childControlWidth = false; v.childForceExpandHeight = false; v.childForceExpandWidth = false; v.childAlignment = TextAnchor.UpperCenter;
        }

        void UpdateHud()
        {
            if (hud == null) return;
            var g = Game.Instance;
            bool inRun = g != null && g.state == GameState.Run;
            bool inHub = g != null && g.state == GameState.Hub;
            bool hideForMenu = MenuOpen && !(stack.Count > 0 && stack[stack.Count - 1] != null && stack[stack.Count - 1].name == "Boones");
            hudRun.gameObject.SetActive(inRun && !hideForMenu);
            resText.gameObject.SetActive(!MenuOpen || (g != null && g.state == GameState.Hub && !dialogueActive));
            bossBars.gameObject.SetActive(inRun);
            controlsText.gameObject.SetActive(inHub || inRun);
            var p = PlayerController.Instance;
            var run = RunState.Current;

            if (inHub || (g != null && g.state == GameState.Hub))
            {
                var s = Save.Data;
                resText.text = string.Join("\n", Res.AllIds.Select(id => $"<color=#{ColorUtility.ToHtmlStringRGB(Res.ColorOf(id))}>{Res.Name(id)}: {s.Res(id)}</color>"));
                controlsText.text = $"[{GameInput.Label(GameButton.Interact)}] Interactuar   [{GameInput.Label(GameButton.Gift)}] Regalar   [{GameInput.Label(GameButton.Attack)}/{GameInput.Label(GameButton.Special)}] Practicar   [{GameInput.Label(GameButton.Pause)}] Pausa";
            }
            else if (inRun && run != null)
            {
                resText.text = run.gained.Count == 0 ? "" : "<i>Esta incursión:</i>\n" + string.Join("\n", run.gained.Select(kv => $"<color=#{ColorUtility.ToHtmlStringRGB(Res.ColorOf(kv.Key))}>+{kv.Value} {Res.Name(kv.Key)}</color>"));
                controlsText.text = run.depth <= 2 ? $"[{GameInput.Label(GameButton.Attack)}] Ataque  [{GameInput.Label(GameButton.Special)}] Especial  (mantén: Omega)   [{GameInput.Label(GameButton.Cast)}] Signo   [{GameInput.Label(GameButton.Sprint)}] Esprint   [{GameInput.Label(GameButton.Hex)}] Eco   [{GameInput.Label(GameButton.Info)}] Boones" : "";
            }
            else { resText.text = ""; controlsText.text = ""; }

            if (!inRun || p == null || run == null) return;
            float frac = p.HpFrac;
            hpFill.fillAmount = frac;
            hpLagV = Mathf.MoveTowards(hpLagV, frac, Time.unscaledDeltaTime * (hpLagV > frac ? 0.5f : 5f));
            hpLag.fillAmount = hpLagV;
            hpText.text = $"{Mathf.CeilToInt(p.hp)} / {Mathf.RoundToInt(p.maxHp)}";
            hpFill.color = frac < 0.3f ? Color.Lerp(new Color(0.85f, 0.12f, 0.2f), new Color(1f, 0.4f, 0.4f), Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : new Color(0.85f, 0.12f, 0.2f);
            mgFill.fillAmount = p.MaxMagick > 0 ? p.magick / p.MaxMagick : 0;
            mgFill.color = Color.Lerp(new Color(0.45f, 0.75f, 1f), PlayerController.IridColor(), 0.35f);
            mgText.text = $"Lucidez {Mathf.FloorToInt(p.magick)}/{Mathf.RoundToInt(p.MaxMagick)}";

            while (ddPips.Count < run.maxDeathDefiance)
            {
                var rt = UIKit.Box(hudRun.Find("DD"), "pip", new Vector2(0, 0.5f), Vector2.zero, new Vector2(26, 26));
                ddPips.Add(UIKit.Img(rt, Color.white, UIKit.diamondSprite));
            }
            for (int i = 0; i < ddPips.Count; i++)
            {
                ddPips[i].gameObject.SetActive(i < run.maxDeathDefiance);
                ddPips[i].color = i < run.deathDefiance ? new Color(0.85f, 0.6f, 1f) : new Color(0.25f, 0.2f, 0.3f);
            }

            castFill.fillAmount = p.cast.Available > 0 ? 1f : p.cast.RechargeFrac;
            castText.text = GameInput.Label(GameButton.Cast) + (p.cast.MaxCharges > 1 ? $"\n<size=14>{p.cast.Available}</size>" : "");
            var hexDef = p.hex.Def;
            hexFill.transform.parent.gameObject.SetActive(hexDef != null);
            if (hexDef != null)
            {
                hexFill.fillAmount = Mathf.Clamp01(p.hex.Charge / p.hex.Cost);
                hexFill.color = p.hex.Ready ? Color.Lerp(hexDef.color, Color.white, Mathf.PingPong(Time.unscaledTime * 2, 1)) : hexDef.color.WithA(0.7f);
                hexText.text = GameInput.Label(GameButton.Hex);
            }

            MoveSlot[] slots = { MoveSlot.Attack, MoveSlot.Special, MoveSlot.Cast, MoveSlot.Sprint, MoveSlot.Magick };
            for (int i = 0; i < 5; i++)
            {
                var b = run.InSlot(slots[i]);
                var gsp = b != null ? ArtIA.Sprite(ArtIA.Iconos, b.def.god) : null;
                slotImgs[i].sprite = gsp != null ? gsp : UIKit.diamondSprite;
                slotImgs[i].preserveAspect = gsp != null;
                slotImgs[i].color = gsp != null ? Color.white : b != null ? b.def.God.color : new Color(0.1f, 0.08f, 0.14f, 0.85f);
                slotTxt[i].color = b != null ? Color.white : UIKit.Dim;
                slotTxt[i].text = b != null ? $"{b.def.God.glyph}\n<size=12>Nv{b.level}</size>" : new[] { "ATQ", "ESP", "SIG", "ESPR", "LUC" }[i];
            }
            int others = run.boons.Count(b => b.def.kind != BoonKind.Core);
            extraBoons.text = (others > 0 ? $"+{others} boones" : "") + (run.chaosCurses.Count > 0 ? $"   <color=#FF66CC>{run.chaosCurses.Count} maldición(es)</color>" : "") + $"   [{GameInput.Label(GameButton.Info)}]";
            ammoText.text = p.weapon is TrapezohedronWeapon tz ? "Fragmentos: " + new string('◆', tz.ammo) + new string('◇', Mathf.Max(0, tz.maxAmmo - tz.ammo)) : "";

            var biome = Biomes.Get(run.biome);
            goldText.text = $"Oro Onírico: {run.gold}";
            string fear = run.fear > 0 ? $"   ·   Pavor {run.fear}" : "";
            roomText.text = $"{biome.name}   ·   Sala {Mathf.Min(run.room, biome.rooms)}/{biome.rooms}{fear}";

            // barras de jefe
            for (int i = bossUi.Count - 1; i >= 0; i--)
                if (bossUi[i].b == null || bossUi[i].b.dead) { if (bossUi[i].fill != null) Destroy(bossUi[i].fill.transform.parent.parent.gameObject); bossUi.RemoveAt(i); }
            foreach (var b in Boss.Active)
            {
                if (b == null || b.dead || bossUi.Any(x => x.b == b)) continue;
                var rt = UIKit.Box(bossBars, "BossBar", new Vector2(0.5f, 1), Vector2.zero, new Vector2(Boss.Active.Count > 1 ? 700 : 1000, 64));
                var name = UIKit.LabelAt(rt, b.displayName, 26, b.def.accent, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(1000, 30), TextAnchor.MiddleCenter, FontStyle.Bold);
                var bar = UIKit.Box(rt, "Bar", new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(Boss.Active.Count > 1 ? 680 : 980, 22), new Vector2(0.5f, 0));
                UIKit.Img(bar, new Color(0.05f, 0, 0.03f, 0.9f), UIKit.panelSprite);
                var fill = UIKit.Img(UIKit.Stretch(bar, "f", 3), b.def.accent);
                fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
                UIKit.Img(UIKit.Stretch(bar, "fr", -2), UIKit.Gold.WithA(0.8f), UIKit.frameSprite);
                bossUi.Add((b, fill, name));
            }
            foreach (var x in bossUi)
            {
                x.fill.fillAmount = x.b.HpFrac;
                x.name.text = x.b.displayName + (string.IsNullOrEmpty(x.b.title) ? "" : $"  <size=18><i>— {x.b.title}</i></size>");
            }
        }
    }
}
