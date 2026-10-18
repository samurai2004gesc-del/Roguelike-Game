using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    public partial class UIRoot
    {
        /// <summary>Pantalla de elección de boon con el retrato del dios (como el encuentro con un olímpico en Hades).</summary>
        public void OpenBoonChoice(string god, List<Offer> offers, Action onDone)
        {
            var g = Gods.Get(god);
            string line = g.offerLines[UnityEngine.Random.Range(0, g.offerLines.Length)];
            OpenChoiceInternal(g.name, g.title, offers, g.color, onDone, god, line, () => BoonManager.RollGodOffer(god));
        }

        public void OpenChoice(string title, string subtitle, List<Offer> offers, Color c, Action onDone)
        {
            string speaker = title.Contains("Gatos") ? "gatos" : title.Contains("Azathoth") ? "azathoth" : null;
            Func<List<Offer>> reroll = null;
            if (title.Contains("Gatos")) reroll = BoonManager.RollCatOffer;
            OpenChoiceInternal(title, subtitle, offers, c, onDone, speaker, null, reroll);
        }

        void OpenChoiceInternal(string title, string subtitle, List<Offer> offers, Color c, Action onDone, string speaker, string line, Func<List<Offer>> reroll)
        {
            if (offers == null || offers.Count == 0)
            {
                Toast("No hay nada que ofrecer... por ahora.", UIKit.Dim);
                RunState.Current?.AddGold(50);
                onDone?.Invoke();
                return;
            }
            Sfx.Play(SfxId.Boon);
            var root = PushScreen("Eleccion", null, 0.82f);
            root.gameObject.AddComponent<NoCancel>();
            bool hasPortrait = speaker != null;
            if (hasPortrait)
            {
                var pr = UIKit.Box(root, "Retrato", new Vector2(0, 0), new Vector2(40, 40), new Vector2(560, 700), new Vector2(0, 0));
                var img = UIKit.Img(pr, Color.white); img.sprite = Portraits.Get(speaker); img.preserveAspect = true;
            }
            float x = hasPortrait ? 330 : 0;
            var head = UIKit.LabelAt(root, title, 58, c, new Vector2(0.5f, 1), new Vector2(x, -70), new Vector2(1200, 80), TextAnchor.MiddleCenter, FontStyle.Bold);
            head.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            UIKit.LabelAt(root, subtitle, 26, UIKit.Dim, new Vector2(0.5f, 1), new Vector2(x, -125), new Vector2(1200, 40), TextAnchor.MiddleCenter, FontStyle.Italic);
            if (!string.IsNullOrEmpty(line)) UIKit.LabelAt(root, "«" + Lines.G(line) + "»", 26, UIKit.Parchment, new Vector2(0.5f, 1), new Vector2(x, -175), new Vector2(1150, 60), TextAnchor.MiddleCenter, FontStyle.Italic);

            var cards = new List<Selectable>();
            float startY = 210, h = 170;
            for (int i = 0; i < offers.Count; i++)
            {
                var o = offers[i];
                var b = Card(root, o, new Vector2(x, startY - i * (h + 18) - (4 - offers.Count) * 30), new Vector2(1150, h), () =>
                {
                    Sfx.Play(SfxId.LevelUp);
                    BoonManager.ApplyOffer(o);
                    var p = PlayerController.Instance;
                    if (p != null) Fx.Burst(p.transform.position + Vector3.up, c, 40, 7f, 0.35f, 0.8f, FxKind.Star);
                    PopScreen();
                    onDone?.Invoke();
                });
                cards.Add(b);
            }
            var run = RunState.Current;
            if (reroll != null && run != null && run.rerolls > 0)
            {
                var rb = UIKit.Button(root, $"Hilos del Destino ({run.rerolls})", new Vector2(0.5f, 0), new Vector2(x, 60), new Vector2(420, 60), () =>
                {
                    run.rerolls--;
                    PopScreen();
                    OpenChoiceInternal(title, subtitle, reroll(), c, onDone, speaker, line, reroll);
                }, 24, UIKit.Violet);
                cards.Add(rb);
            }
            UIKit.ChainNav(cards);
            UIKit.Select(cards[0].gameObject);
        }

        Button Card(RectTransform root, Offer o, Vector2 pos, Vector2 size, Action pick)
        {
            Color rc; string kindLabel; string name; string desc; Color iconC; string iconTxt;
            if (o.hammer != null)
            {
                rc = new Color(0.6f, 0.75f, 1f); kindLabel = "Mejora de arma"; name = o.hammer.name; desc = o.hammer.desc; iconC = rc; iconTxt = "✦";
            }
            else if (o.upgrade != null)
            {
                var b = o.upgrade;
                rc = BoonInstance.RarityColor(b.rarity);
                kindLabel = $"Nivel {b.level} → {b.level + o.levels}";
                name = b.def.name;
                desc = b.def.Describe(b.rarity, b.level + o.levels);
                iconC = b.def.God?.color ?? Color.white; iconTxt = b.def.God != null ? b.def.God.glyph.ToString() : "✧";
            }
            else
            {
                var d = o.def;
                rc = BoonInstance.RarityColor(o.rarity);
                kindLabel = BoonInstance.RarityName(o.rarity) + (d.slot != MoveSlot.None ? "  ·  " + BoonInstance.SlotName(d.slot) : "") + (d.element != Element.None ? "  ·  " + GodDef.ElementName(d.element) : "");
                name = d.name;
                desc = d.Describe(o.rarity, 1);
                if (o.replaces != null) desc += $"\n<color=#FF9A7A>Reemplaza: {o.replaces.def.name} (conserva Nv{o.replaces.level})</color>";
                if (o.curse != null) desc += $"\n<color=#FF66CC>{o.curse.name} — {o.curse.desc} ({o.curseRooms} salas)</color>";
                if (d.kind == BoonKind.Duo) { var g2 = Gods.Get(d.requiresGods[1]); iconC = Color.Lerp(d.God.color, g2.color, 0.5f); iconTxt = d.God.glyph + "" + g2.glyph; }
                else if (d.God != null) { iconC = d.God.color; iconTxt = d.God.glyph.ToString(); }
                else if (d.kind == BoonKind.Cat) { iconC = new Color(0.6f, 1f, 0.4f); iconTxt = "≈"; }
                else { iconC = new Color(1f, 0.3f, 0.8f); iconTxt = "∞"; }
            }
            var rt = UIKit.Box(root, "Carta", new Vector2(0.5f, 0.5f), pos, size);
            var bg = UIKit.Img(rt, new Color(0.06f, 0.03f, 0.1f, 0.97f), UIKit.panelSprite, true);
            var fr = UIKit.Img(UIKit.Stretch(rt, "Fr", -3), rc.WithA(0.75f), UIKit.frameSprite);
            var ic = UIKit.Box(rt, "Icono", new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(110, 110), new Vector2(0, 0.5f));
            UIKit.Img(ic, iconC, UIKit.diamondSprite);
            UIKit.Label(ic, iconTxt, 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var nm = UIKit.LabelAt(rt, name, 34, rc, new Vector2(0, 1), new Vector2(160, -16), new Vector2(700, 44), TextAnchor.MiddleLeft, FontStyle.Bold);
            nm.rectTransform.pivot = new Vector2(0, 1);
            var kl = UIKit.LabelAt(rt, kindLabel, 20, UIKit.Dim, new Vector2(1, 1), new Vector2(-24, -20), new Vector2(500, 30), TextAnchor.MiddleRight, FontStyle.Italic);
            kl.rectTransform.pivot = new Vector2(1, 1);
            var ds = UIKit.LabelAt(rt, desc, 24, UIKit.Parchment, new Vector2(0, 0), new Vector2(160, 14), new Vector2(960, 100), TextAnchor.UpperLeft);
            ds.rectTransform.pivot = new Vector2(0, 0);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors; cols.highlightedColor = new Color(1.5f, 1.4f, 1.8f); cols.selectedColor = new Color(1.6f, 1.5f, 1.9f); cols.colorMultiplier = 1.5f; btn.colors = cols;
            btn.onClick.AddListener(() => pick());
            var hl = rt.gameObject.AddComponent<ButtonHighlight>(); hl.frame = fr; hl.label = null; hl.accent = rc;
            return btn;
        }

        /// <summary>Ecos del Padre: elegir uno nuevo o mejorar el actual.</summary>
        public void OpenHexChoice(Action onDone)
        {
            var run = RunState.Current;
            var root = PushScreen("Ecos", null, 0.85f);
            root.gameObject.AddComponent<NoCancel>();
            var pr = UIKit.Box(root, "Retrato", new Vector2(0, 0), new Vector2(40, 40), new Vector2(560, 700), new Vector2(0, 0));
            var img = UIKit.Img(pr, Color.white); img.sprite = Portraits.Get("yog"); img.preserveAspect = true;
            UIKit.LabelAt(root, "Ecos del Padre", 58, new Color(0.8f, 0.85f, 1f), new Vector2(0.5f, 1), new Vector2(330, -70), new Vector2(1200, 80), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.LabelAt(root, "«...SOTHIS... TOMA... UN FRAGMENTO DE LO QUE SOY...»", 26, UIKit.Parchment, new Vector2(0.5f, 1), new Vector2(330, -130), new Vector2(1150, 50), TextAnchor.MiddleCenter, FontStyle.Italic);
            var options = Hexes.All.Where(h => h.id != run.hexId).OrderBy(_ => run.rng.Next()).Take(2).ToList();
            if (!string.IsNullOrEmpty(run.hexId)) options.Insert(0, Hexes.Get(run.hexId));
            var sels = new List<Selectable>();
            for (int i = 0; i < options.Count; i++)
            {
                var h = options[i];
                bool upgrade = h.id == run.hexId;
                int lvl = upgrade ? run.hexLevel + 1 : 1;
                var rt = UIKit.Box(root, "Eco", new Vector2(0.5f, 0.5f), new Vector2(330, 160 - i * 190), new Vector2(1150, 170));
                var bg = UIKit.Img(rt, new Color(0.05f, 0.04f, 0.1f, 0.97f), UIKit.panelSprite, true);
                var fr = UIKit.Img(UIKit.Stretch(rt, "Fr", -3), h.color.WithA(0.75f), UIKit.frameSprite);
                var ic = UIKit.Box(rt, "Icono", new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(110, 110), new Vector2(0, 0.5f));
                UIKit.Img(ic, h.color, UIKit.circleSprite);
                var nm = UIKit.LabelAt(rt, h.name + (upgrade ? $"  (Nivel {lvl})" : ""), 34, h.color, new Vector2(0, 1), new Vector2(160, -16), new Vector2(900, 44), TextAnchor.MiddleLeft, FontStyle.Bold);
                nm.rectTransform.pivot = new Vector2(0, 1);
                var ds = UIKit.LabelAt(rt, h.desc + $"\n<color=#9AD8FF>Se carga gastando {Mathf.RoundToInt(h.CostAt(lvl))} de Lucidez.</color>" + (upgrade ? "  <color=#FFE08A>+25% de potencia, -15% de coste.</color>" : (string.IsNullOrEmpty(run.hexId) ? "" : $"\n<color=#FF9A7A>Reemplaza: {Hexes.Get(run.hexId).name}</color>")), 24, UIKit.Parchment, new Vector2(0, 0), new Vector2(160, 14), new Vector2(960, 100), TextAnchor.UpperLeft);
                ds.rectTransform.pivot = new Vector2(0, 0);
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                var cols = btn.colors; cols.highlightedColor = new Color(1.5f, 1.4f, 1.8f); cols.selectedColor = new Color(1.6f, 1.5f, 1.9f); cols.colorMultiplier = 1.5f; btn.colors = cols;
                btn.onClick.AddListener(() =>
                {
                    if (upgrade) run.hexLevel++;
                    else { run.hexId = h.id; run.hexLevel = 1; run.hexCharge = 0; }
                    Sfx.Play(SfxId.LevelUp);
                    PopScreen();
                    onDone?.Invoke();
                });
                var hl = rt.gameObject.AddComponent<ButtonHighlight>(); hl.frame = fr; hl.accent = h.color;
                sels.Add(btn);
            }
            UIKit.ChainNav(sels);
            UIKit.Select(sels[0].gameObject);
        }
    }
}
