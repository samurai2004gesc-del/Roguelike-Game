using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    public partial class UIRoot
    {
        Button RichButton(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string title, string body, Color accent, Action onClick, bool dim = false, int titleSize = 26, int bodySize = 19)
        {
            var rt = UIKit.Box(parent, "Rich", anchor, pos, size);
            var bg = UIKit.Img(rt, dim ? new Color(0.04f, 0.03f, 0.05f, 0.95f) : new Color(0.06f, 0.03f, 0.1f, 0.96f), UIKit.panelSprite, true);
            var fr = UIKit.Img(UIKit.Stretch(rt, "Fr", -2), accent.WithA(dim ? 0.3f : 0.7f), UIKit.frameSprite);
            var tt = UIKit.LabelAt(rt, title, titleSize, dim ? UIKit.Dim : accent, new Vector2(0, 1), new Vector2(18, -8), new Vector2(size.x - 30, titleSize + 12), TextAnchor.UpperLeft, FontStyle.Bold);
            tt.rectTransform.pivot = new Vector2(0, 1);
            var bt = UIKit.LabelAt(rt, body, bodySize, dim ? UIKit.Dim : UIKit.Parchment, new Vector2(0, 0), new Vector2(18, 8), new Vector2(size.x - 30, size.y - titleSize - 26), TextAnchor.UpperLeft);
            bt.rectTransform.pivot = new Vector2(0, 0);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var cols = b.colors; cols.highlightedColor = new Color(1.5f, 1.4f, 1.8f); cols.selectedColor = new Color(1.6f, 1.5f, 1.9f); cols.colorMultiplier = 1.5f; b.colors = cols;
            b.onClick.AddListener(() => onClick?.Invoke());
            var hl = rt.gameObject.AddComponent<ButtonHighlight>(); hl.frame = fr; hl.accent = accent;
            return b;
        }

        RectTransform HubScreen(string title, string subtitle, string speaker, Color c)
        {
            var root = PushScreen(title);
            if (speaker != null)
            {
                var pr = UIKit.Box(root, "Retrato", new Vector2(0, 0), new Vector2(10, 0), new Vector2(430, 540), new Vector2(0, 0));
                var img = UIKit.Img(pr, Color.white); img.sprite = Portraits.Get(speaker); img.preserveAspect = true;
            }
            var t = UIKit.LabelAt(root, title, 54, c, new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(1500, 76), TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.AddComponent<Outline>().effectColor = Color.black;
            UIKit.LabelAt(root, subtitle, 24, UIKit.Dim, new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(1500, 36), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.LabelAt(root, $"[{GameInput.Label(GameButton.Cancel)}] Cerrar", 20, UIKit.Dim, new Vector2(1, 0), new Vector2(-30, 20), new Vector2(300, 30), TextAnchor.MiddleRight).rectTransform.pivot = new Vector2(1, 0);
            return root;
        }

        void Refresh(Action reopen) { PopScreen(); reopen(); }

        string ResLine()
        {
            var s = Save.Data;
            return string.Join("    ", Res.AllIds.Select(id => $"<color=#{ColorUtility.ToHtmlStringRGB(Res.ColorOf(id))}>{Res.Name(id)}: {s.Res(id)}</color>"));
        }

        // ================================================================== ALTAR DE ARMAS
        public void OpenWeaponAltar()
        {
            var s = Save.Data;
            var root = HubScreen("Altar de las Armas del Umbral", "Elige el arma con la que cruzarás la Puerta", null, new Color(0.75f, 0.85f, 1f));
            var sels = new List<Selectable>();
            for (int i = 0; i < Weapons.All.Count; i++)
            {
                var w = Weapons.All[i];
                bool unlocked = w.Unlocked;
                bool selected = s.lastWeapon == w.id;
                string body = unlocked
                    ? $"<i>{w.title}</i>\n<color=#FFE08A>Ataque:</color> {w.attackDesc}\n<color=#FFE08A>Especial:</color> {w.specialDesc}\n<color=#9AD8FF>Ω Ataque:</color> {w.omegaAttackDesc}\n<color=#9AD8FF>Ω Especial:</color> {w.omegaSpecialDesc}"
                    : $"<i>Sellada.</i>\nRequiere el ritual: «{MetaDB.Ritual(w.ritual)?.name}» en el Caldero.";
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-470 + (i % 3) * 470, 240 - (i / 3) * 330), new Vector2(450, 310), (selected ? "► " : "") + w.name, body, unlocked ? w.color : UIKit.Dim, () =>
                {
                    if (!unlocked) { Sfx.Play(SfxId.UiBack); return; }
                    s.lastWeapon = w.id;
                    Save.Write();
                    PlayerController.Instance?.SetWeapon(w.id);
                    Refresh(OpenWeaponAltar);
                    if (s.HasRitual("r_aspectos")) OpenAspects(w);
                }, !unlocked, 26, 18);
                var wsp = ArtIA.Sprite(ArtIA.Iconos, w.id);
                if (wsp != null)
                {
                    var icr = UIKit.Box(b.transform, "IconoArma", new Vector2(1, 1), new Vector2(-12, -10), new Vector2(84, 84), new Vector2(1, 1));
                    var wi = UIKit.Img(icr, unlocked ? Color.white : new Color(0.3f, 0.3f, 0.35f)); wi.sprite = wsp; wi.preserveAspect = true;
                }
                sels.Add(b);
            }
            if (s.HasRitual("r_aspectos"))
            {
                var w = Weapons.Get(s.lastWeapon);
                sels.Add(UIKit.Button(root, "Aspectos de " + w.name, new Vector2(0.5f, 0), new Vector2(470, 120), new Vector2(450, 64), () => OpenAspects(w), 22, UIKit.Violet));
            }
            UIKit.GridNav(sels, 3);
            UIKit.Select(sels[Mathf.Max(0, Weapons.All.FindIndex(x => x.id == s.lastWeapon))].gameObject);
        }

        void OpenAspects(WeaponDef w)
        {
            var s = Save.Data;
            var root = HubScreen("Aspectos — " + w.name, ResLine(), null, w.color);
            var sels = new List<Selectable>();
            for (int i = 0; i < w.aspects.Count; i++)
            {
                var a = w.aspects[i];
                int idx = i;
                int lvl = s.AspectLevel(w.id + ":" + i);
                if (i == 0) lvl = Mathf.Max(1, lvl);
                bool chosen = s.ChosenAspect(w.id) == i;
                var next = lvl < 3 ? a.costs[lvl] : null;
                string body = (lvl > 0 ? a.Describe(lvl) : a.Describe(1)) + $"\nNivel {lvl}/3" + (next != null ? $"\nMejorar: {Res.Cost(next)}" : "\nNivel máximo") + (lvl == 0 ? "\n<color=#FF9A7A>Bloqueado — despiértalo primero</color>" : "");
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-400 + i * 800, 40), new Vector2(760, 360), (chosen ? "► " : "") + a.name, body, w.color, () =>
                {
                    if (lvl > 0 && !chosen) { s.ChooseAspect(w.id, idx); Save.Write(); Sfx.Play(SfxId.UiConfirm); Refresh(() => OpenAspects(w)); return; }
                    if (next != null && s.Spend(next))
                    {
                        s.SetAspectLevel(w.id + ":" + idx, lvl + 1);
                        if (lvl == 0) s.ChooseAspect(w.id, idx);
                        Save.Write(); Sfx.Play(SfxId.LevelUp);
                        Refresh(() => OpenAspects(w));
                    }
                    else Sfx.Play(SfxId.UiBack);
                }, false, 30, 22);
                sels.Add(b);
            }
            UIKit.ChainNav(sels, true);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== GRIMORIO DE KEZIAH (Arcanas)
        public void OpenGrimorio()
        {
            var s = Save.Data;
            int used = MetaDB.GraspUsed();
            int graspCost = 10 + (s.grasp - 10) * 8;
            var root = HubScreen("Grimorio de Keziah", $"Capacidad mental: {used}/{s.grasp}     ·     {ResLine()}", "keziah", new Color(0.6f, 1f, 0.45f));
            var sels = new List<Selectable>();
            for (int i = 0; i < MetaDB.Cards.Count; i++)
            {
                var c = MetaDB.Cards[i];
                bool owned = s.cards.Contains(c.id);
                bool active = s.activeCards.Contains(c.id);
                string state = !owned ? $"Despertar: {Res.Cost(c.unlock)}" : active ? "<color=#9CFF9C>ACTIVA</color>" : "Inactiva";
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-560 + (i % 5) * 280 + 140, 250 - (i / 5) * 230), new Vector2(266, 216),
                    $"{c.numeral}. {c.name}  <size=18>[{c.grasp}]</size>", c.desc + "\n" + state, active ? c.color : owned ? Color.Lerp(c.color, Color.gray, 0.4f) : UIKit.Dim, () =>
                    {
                        if (!owned)
                        {
                            if (s.Spend(c.unlock)) { s.cards.Add(c.id); Sfx.Play(SfxId.LevelUp); Save.Write(); }
                            else { Sfx.Play(SfxId.UiBack); Toast("Te falta material", new Color(1f, 0.5f, 0.4f)); return; }
                        }
                        else if (active) s.activeCards.Remove(c.id);
                        else if (MetaDB.GraspUsed() + c.grasp <= s.grasp) s.activeCards.Add(c.id);
                        else { Sfx.Play(SfxId.UiBack); Toast("Tu mente no puede sostener más cartas", new Color(1f, 0.5f, 0.4f)); return; }
                        Save.Write();
                        Refresh(OpenGrimorio);
                    }, !owned, 22, 17);
                sels.Add(b);
            }
            var cost = C.Cost(Res.Ensueno, graspCost);
            var gb = UIKit.Button(root, $"Ampliar capacidad (+1): {graspCost} Ensueño", new Vector2(0.5f, 0), new Vector2(140, 50), new Vector2(620, 62), () =>
            {
                if (s.Spend(cost)) { s.grasp++; Save.Write(); Sfx.Play(SfxId.LevelUp); Refresh(OpenGrimorio); }
                else { Sfx.Play(SfxId.UiBack); Toast("Necesitas más Ensueño", new Color(1f, 0.5f, 0.4f)); }
            }, 22, new Color(0.95f, 0.5f, 0.9f));
            sels.Add(gb);
            UIKit.GridNav(sels, 5);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== CALDERO (Conjuros)
        public void OpenCaldero()
        {
            var s = Save.Data;
            var root = HubScreen("El Caldero de 'Umr at-Tawil", ResLine(), null, new Color(0.8f, 0.7f, 1f));
            var sels = new List<Selectable>();
            var list = MetaDB.Rituals.OrderBy(r => s.HasRitual(r.id) ? 2 : r.available(s) ? 0 : 1).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                bool done = s.HasRitual(r.id);
                bool avail = r.available(s);
                string body = r.desc + "\n" + (done ? "<color=#9CFF9C>Completado</color>" : avail ? Res.Cost(r.cost) : $"<color=#FF9A7A>{r.requirementText}</color>");
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-610 + (i % 3) * 610, 320 - (i / 3) * 128), new Vector2(596, 120), r.name, body, done ? new Color(0.5f, 0.8f, 0.5f) : avail ? new Color(0.8f, 0.7f, 1f) : UIKit.Dim, () =>
                {
                    if (done || !avail) { Sfx.Play(SfxId.UiBack); return; }
                    if (!s.Spend(r.cost)) { Sfx.Play(SfxId.UiBack); Toast("Te faltan ingredientes", new Color(1f, 0.5f, 0.4f)); return; }
                    s.rituals.Add(r.id);
                    Save.Write();
                    Sfx.Play(SfxId.Boon);
                    Banner("Ritual completado", r.name, new Color(0.8f, 0.7f, 1f), 3f);
                    Game.Instance.OnRitualDone(r.id);
                    Refresh(OpenCaldero);
                }, done || !avail, 22, 16);
                sels.Add(b);
            }
            UIKit.GridNav(sels, 3);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== JURAMENTO (Pavor)
        public void OpenJuramento()
        {
            var s = Save.Data;
            var root = HubScreen("Juramento de lo Innombrable", $"Pavor total: {MetaDB.TotalFear()}   ·   Mejor victoria con Pavor: {s.bestFear}   ·   Más Pavor = más recursos (+3% por punto)", null, new Color(1f, 0.4f, 0.4f));
            var sels = new List<Selectable>();
            for (int i = 0; i < MetaDB.Vows.Count; i++)
            {
                var v = MetaDB.Vows[i];
                int r = s.Vow(v.id);
                string cur = r == 0 ? "Inactivo" : v.desc.Replace("{v}", "<color=#FFE08A>" + v.values[r - 1] + v.unit + "</color>");
                string next = r < v.values.Length ? "Siguiente: " + v.desc.Replace("{v}", v.values[r] + v.unit) : "Rango máximo";
                string pips = string.Join("", Enumerable.Range(0, v.values.Length).Select(k => k < r ? "◆" : "◇"));
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-400 + (i % 2) * 800, 300 - (i / 2) * 140), new Vector2(780, 128), $"{v.name}  {pips}", cur + "\n<size=16>" + next + "</size>", r > 0 ? new Color(1f, 0.45f, 0.4f) : new Color(0.75f, 0.6f, 0.6f), () =>
                {
                    s.SetVow(v.id, (r + 1) % (v.values.Length + 1));
                    Save.Write();
                    Sfx.Play(SfxId.UiConfirm);
                    Refresh(OpenJuramento);
                }, false, 24, 18);
                sels.Add(b);
            }
            UIKit.GridNav(sels, 2);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== RELIQUIAS Y FAMILIARES
        public void OpenReliquias()
        {
            var s = Save.Data;
            var root = HubScreen("Altar de Reliquias", "Lleva una reliquia y un familiar a tu próxima incursión. Las reliquias se consiguen regalando Licor de Luna.", null, UIKit.Gold);
            var sels = new List<Selectable>();
            int i = 0;
            foreach (var r in MetaDB.Relics)
            {
                bool owned = s.relics.Contains(r.id);
                bool eq = s.equippedRelic == r.id;
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-610 + (i % 3) * 610, 300 - (i / 3) * 150), new Vector2(596, 140), (eq ? "► " : "") + (owned ? r.name : "???"),
                    owned ? r.desc : $"Regala Licor de Luna a {Dialogue.Sp(r.npc).name}.", owned ? UIKit.Gold : UIKit.Dim, () =>
                    {
                        if (!owned) { Sfx.Play(SfxId.UiBack); return; }
                        s.equippedRelic = eq ? "" : r.id;
                        Save.Write(); Sfx.Play(SfxId.UiConfirm);
                        Refresh(OpenReliquias);
                    }, !owned, 24, 18);
                sels.Add(b); i++;
            }
            int row = (i + 2) / 3;
            int j = 0;
            foreach (var f in MetaDB.Familiars)
            {
                bool owned = s.familiars.Contains(f.id);
                bool eq = s.equippedFamiliar == f.id;
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-305 + j * 610, 300 - row * 150 - 30), new Vector2(596, 140), (eq ? "► " : "") + (owned ? f.name : "Familiar sin invocar"),
                    owned ? f.desc : "Invócalo con un ritual del Caldero.", owned ? new Color(0.6f, 1f, 0.4f) : UIKit.Dim, () =>
                    {
                        if (!owned) { Sfx.Play(SfxId.UiBack); return; }
                        s.equippedFamiliar = eq ? "" : f.id;
                        Save.Write(); Sfx.Play(SfxId.UiConfirm);
                        Game.Instance.RefreshFamiliar();
                        Refresh(OpenReliquias);
                    }, !owned, 24, 18);
                sels.Add(b); j++;
            }
            UIKit.GridNav(sels, 3);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== PRESAGIOS (Profecías)
        public void OpenPresagios()
        {
            var s = Save.Data;
            var root = HubScreen("Presagios de Randolph Carter", "Las estrellas indican lo que harás. Cúmplelo y reclama tu recompensa.", "carter", new Color(0.6f, 0.8f, 1f));
            var sels = new List<Selectable>();
            for (int i = 0; i < MetaDB.Quests.Count; i++)
            {
                var q = MetaDB.Quests[i];
                bool claimed = s.questsClaimed.Contains(q.id);
                bool done = q.done(s);
                string status = claimed ? "<color=#9CFF9C>Reclamado</color>" : done ? $"<color=#FFE08A>¡Cumplido! Reclamar: {Res.Cost(q.reward)}</color>" : $"{q.progress(s)}   Recompensa: {Res.Cost(q.reward)}";
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-400 + (i % 2) * 800, 330 - (i / 2) * 82), new Vector2(780, 76), q.name, q.desc + "   " + status, claimed ? new Color(0.45f, 0.6f, 0.45f) : done ? UIKit.Gold : new Color(0.6f, 0.75f, 0.95f), () =>
                {
                    if (claimed || !done) { Sfx.Play(SfxId.UiBack); return; }
                    s.questsClaimed.Add(q.id);
                    foreach (var kv in q.reward) s.AddRes(kv.Key, kv.Value);
                    Save.Write();
                    Sfx.Play(SfxId.Boon);
                    Refresh(OpenPresagios);
                }, false, 20, 15);
                sels.Add(b);
            }
            UIKit.GridNav(sels, 2);
            UIKit.Select(sels[0].gameObject);
        }

        // ================================================================== NECRONOMICÓN (Códice)
        public void OpenCodex(int tab = 0)
        {
            var s = Save.Data;
            string[] tabs = { "Dioses", "Bestiario", "Guardianes", "Regiones", "Armas" };
            var root = HubScreen("Necronomicón", "«Que no está muerto lo que puede yacer eternamente, y con los eones extraños incluso la muerte puede morir.»", null, new Color(0.55f, 1f, 0.5f));
            var sels = new List<Selectable>();
            for (int t = 0; t < tabs.Length; t++)
            {
                int tt = t;
                sels.Add(UIKit.Button(root, tabs[t], new Vector2(0.5f, 1), new Vector2(-560 + t * 280, -170), new Vector2(260, 54), () => Refresh(() => OpenCodex(tt)), 22, t == tab ? UIKit.Gold : UIKit.Dim));
            }
            var entries = new List<(string title, string body, bool known)>();
            switch (tab)
            {
                case 0: foreach (var g in Gods.All) entries.Add((g.name + " — " + g.title, g.lore + "\n<color=#FFE08A>Maldición: " + g.curseName + ".</color> " + g.curseDesc, s.codex.Contains("god_" + g.id))); break;
                case 1: foreach (var e in EnemyDB.All.Where(e => !e.id.StartsWith("boss_") && e.id != "dummy")) entries.Add((e.name, e.lore + $"\n<size=15>Derrotados: {s.Count("kill_" + e.id)}</size>", s.codex.Contains(e.id))); break;
                case 2: foreach (var e in EnemyDB.All.Where(e => e.id.StartsWith("boss_"))) entries.Add((e.name, e.lore + $"\n<size=15>Victorias: {s.Count(e.id)}</size>", s.codex.Contains(e.id))); break;
                case 3: foreach (var b in Biomes.All) entries.Add((b.name + " — " + b.subtitle, b.lore, s.Count("reached_" + b.id) > 0)); break;
                case 4: foreach (var w in Weapons.All) entries.Add((w.name, w.lore, w.Unlocked)); break;
            }
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var rt = UIKit.Box(root, "E", new Vector2(0.5f, 0.5f), new Vector2(-400 + (i % 2) * 800, 250 - (i / 2) * 150), new Vector2(780, 140));
                UIKit.Img(rt, new Color(0.05f, 0.04f, 0.06f, 0.95f), UIKit.panelSprite);
                UIKit.Img(UIKit.Stretch(rt, "f", -2), new Color(0.55f, 1f, 0.5f, e.known ? 0.5f : 0.15f), UIKit.frameSprite);
                var tt = UIKit.LabelAt(rt, e.known ? e.title : "???", 22, e.known ? new Color(0.6f, 1f, 0.55f) : UIKit.Dim, new Vector2(0, 1), new Vector2(16, -8), new Vector2(750, 30), TextAnchor.UpperLeft, FontStyle.Bold);
                tt.rectTransform.pivot = new Vector2(0, 1);
                var bt = UIKit.LabelAt(rt, e.known ? e.body : "Aún no conoces este secreto.", 16, e.known ? UIKit.Parchment : UIKit.Dim, new Vector2(0, 0), new Vector2(16, 6), new Vector2(750, 96), TextAnchor.UpperLeft);
                bt.rectTransform.pivot = new Vector2(0, 0);
            }
            UIKit.ChainNav(sels, true);
            UIKit.Select(sels[tab].gameObject);
        }

        // ================================================================== MERCADO DE WEST
        public void OpenWestMarket()
        {
            var s = Save.Data;
            var root = HubScreen("Laboratorio de Herbert West", ResLine(), "west", new Color(0.5f, 1f, 0.4f));
            var deals = new (int bones, string res, int amount)[]
            {
                (10, Res.Ceniza, 6), (15, Res.Ensueno, 12), (25, Res.Plata, 1), (40, Res.Licor, 1), (60, Res.Polvo, 1), (150, Res.Lagrima, 1)
            };
            var sels = new List<Selectable>();
            for (int i = 0; i < deals.Length; i++)
            {
                var d = deals[i];
                var b = RichButton(root, new Vector2(0.5f, 0.5f), new Vector2(-300 + (i % 2) * 600, 220 - (i / 2) * 150), new Vector2(580, 130), $"{d.amount} {Res.Name(d.res)}", $"Precio: {d.bones} {Res.Name(Res.Huesos)}", Res.ColorOf(d.res), () =>
                {
                    if (!s.Spend(C.Cost(Res.Huesos, d.bones))) { Sfx.Play(SfxId.UiBack); Toast("«Necesito más huesos.»", new Color(0.5f, 1f, 0.4f)); return; }
                    s.AddRes(d.res, d.amount);
                    Save.Write(); Sfx.Play(SfxId.Gold);
                    Refresh(OpenWestMarket);
                }, false, 28, 20);
                sels.Add(b);
            }
            UIKit.GridNav(sels, 2);
            UIKit.Select(sels[0].gameObject);
        }
    }
}
