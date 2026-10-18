using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    public partial class UIRoot
    {
        public bool dialogueActive;

        /// <summary>Reproduce un diálogo con retratos grandes y texto que se escribe solo (como en Hades).</summary>
        public void PlayDialogue(IList<DLine> lines, Action onDone)
        {
            StartCoroutine(DialogueCo(lines, onDone));
        }

        IEnumerator DialogueCo(IList<DLine> lines, Action onDone)
        {
            dialogueActive = true;
            var root = PushScreen("Dialogo", null, 0.35f);
            root.gameObject.AddComponent<NoCancel>();
            var portraitRt = UIKit.Box(root, "Retrato", new Vector2(0, 0), new Vector2(90, 210), new Vector2(400, 500), new Vector2(0, 0));
            var portrait = UIKit.Img(portraitRt, Color.white);
            portrait.preserveAspect = true;
            var panel = UIKit.Panel(root, "Caja", new Vector2(0.5f, 0), new Vector2(120, 40), new Vector2(1500, 250), null, new Color(0.03f, 0.01f, 0.06f, 0.95f));
            panel.pivot = new Vector2(0.5f, 0);
            panel.anchoredPosition = new Vector2(120, 40);
            var nameBox = UIKit.Box(panel, "Nombre", new Vector2(0, 1), new Vector2(40, -6), new Vector2(700, 50), new Vector2(0, 0));
            var nameText = UIKit.Label(nameBox, "", 34, Color.white, TextAnchor.LowerLeft, FontStyle.Bold);
            var titleText = UIKit.LabelAt(panel, "", 20, UIKit.Dim, new Vector2(0, 1), new Vector2(40, -30), new Vector2(900, 30), TextAnchor.MiddleLeft, FontStyle.Italic);
            titleText.rectTransform.pivot = new Vector2(0, 0.5f);
            var body = UIKit.LabelAt(panel, "", 30, UIKit.Parchment, new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(1400, 160), TextAnchor.UpperLeft);
            var hint = UIKit.LabelAt(panel, "▶", 26, UIKit.Gold, new Vector2(1, 0), new Vector2(-30, 26), new Vector2(40, 40), TextAnchor.MiddleCenter);
            yield return null;

            foreach (var l in lines)
            {
                var sp = Dialogue.Sp(l.who);
                portrait.sprite = Portraits.Get(l.who);
                bool isMe = l.who == "sothis";
                portraitRt.anchorMin = portraitRt.anchorMax = portraitRt.pivot = isMe ? new Vector2(1, 0) : new Vector2(0, 0);
                portraitRt.anchoredPosition = isMe ? new Vector2(-90, 210) : new Vector2(90, 210);
                nameText.text = sp.name;
                nameText.color = sp.color;
                titleText.text = sp.title;
                string full = Lines.G(l.text);
                hint.gameObject.SetActive(false);
                Sfx.Play(SfxId.Whisper, null, 0.15f, 1.6f);
                float t = 0; int shown = 0;
                yield return null;
                while (shown < full.Length)
                {
                    t += Time.unscaledDeltaTime * 55f;
                    shown = Mathf.Min(full.Length, Mathf.FloorToInt(t));
                    body.text = StripPartialTags(full.Substring(0, shown));
                    if (Advance()) { shown = full.Length; body.text = full; yield return null; break; }
                    yield return null;
                }
                body.text = full;
                hint.gameObject.SetActive(true);
                while (!Advance()) { hint.color = UIKit.Gold.WithA(0.5f + 0.5f * Mathf.PingPong(Time.unscaledTime * 2, 1)); yield return null; }
                Sfx.Play(SfxId.UiMove, null, 0.5f);
                yield return null;
            }
            dialogueActive = false;
            PopScreen();
            onDone?.Invoke();
        }

        static string StripPartialTags(string s)
        {
            int lt = s.LastIndexOf('<'), gt = s.LastIndexOf('>');
            return lt > gt ? s.Substring(0, lt) : s;
        }

        public static bool ForceAdvance;
        static bool Advance() => ForceAdvance || GameInput.Pressed(GameButton.Confirm) || GameInput.Pressed(GameButton.Attack) || GameInput.Pressed(GameButton.Interact);

        // ------------------------------------------------------------------ personajes
        public void TalkTo(string npc)
        {
            var e = Dialogue.Next(npc);
            if (e != null)
            {
                Dialogue.MarkSeen(e);
                PlayDialogue(e.lines, () => { e.onEnd?.Invoke(); Save.Write(); });
            }
            else PlayDialogue(new[] { new DLine(npc, Dialogue.Idle(npc)) }, null);
        }

        public void GiftTo(string npc)
        {
            var s = Save.Data;
            if (s.Res(Res.Licor) <= 0) { Toast("No tienes Licor de Luna para regalar", new Color(0.6f, 0.95f, 0.85f)); return; }
            s.AddRes(Res.Licor, -1);
            s.AddAffinity(npc, 1);
            var sp = Dialogue.Sp(npc);
            string line = sp.giftThanks != null && sp.giftThanks.Length > 0 ? sp.giftThanks[Mathf.Min(s.Affinity(npc) - 1, sp.giftThanks.Length - 1)] : "Gracias.";
            Sfx.Play(SfxId.Boon);
            PlayDialogue(new[] { new DLine("sothis", "Toma. Licor de luna. Para ti."), new DLine(npc, line) }, () =>
            {
                if (!string.IsNullOrEmpty(sp.relic) && !s.relics.Contains(sp.relic))
                {
                    s.relics.Add(sp.relic);
                    var rel = MetaDB.Relic(sp.relic);
                    Banner("Reliquia obtenida", rel.name + " — " + rel.desc, UIKit.Gold, 4f);
                    if (!s.HasRitual("r_reliquias")) Toast("Completa el ritual «Altar de Reliquias» para equiparla", UIKit.Dim, 4f);
                }
                Save.Write();
            });
        }

        // ------------------------------------------------------------------ jefes
        public void ShowBossIntro(Boss b, Action done)
        {
            var e = Dialogue.Next(b.def.id);
            if (e == null) e = Dialogue.Entries.Find(x => x.npc == b.def.id && x.priority == 90);
            Action after = () =>
            {
                Banner(b.def.name.ToUpper(), b.title, b.def.accent, 3f);
                Sfx.Play(SfxId.BossRoar);
                done?.Invoke();
            };
            if (e != null) { Dialogue.MarkSeen(e); PlayDialogue(e.lines, after); }
            else after();
        }

        public void ShowBossLine(Boss b, string text)
        {
            var rt = UIKit.Box(top, "BossLine", new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1100, 120), new Vector2(0.5f, 1f));
            UIKit.Img(rt, new Color(0.03f, 0.01f, 0.05f, 0.9f), UIKit.panelSprite);
            UIKit.Img(UIKit.Stretch(rt, "fr", -2), b.def.accent.WithA(0.8f), UIKit.frameSprite);
            var pic = UIKit.Box(rt, "pic", new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(96, 120), new Vector2(0, 0.5f));
            var img = UIKit.Img(pic, Color.white); img.sprite = Portraits.Get(b.def.id); img.preserveAspect = true;
            var name = UIKit.LabelAt(rt, Dialogue.Sp(b.def.id).name, 24, b.def.accent, new Vector2(0, 1), new Vector2(130, -10), new Vector2(900, 30), TextAnchor.UpperLeft, FontStyle.Bold);
            name.rectTransform.pivot = new Vector2(0, 1);
            var t = UIKit.LabelAt(rt, Lines.G(text), 26, UIKit.Parchment, new Vector2(0, 0), new Vector2(130, 14), new Vector2(940, 70), TextAnchor.MiddleLeft, FontStyle.Italic);
            t.rectTransform.pivot = new Vector2(0, 0);
            StartCoroutine(ToastLife(rt, 3.5f));
        }

        public void ShowBossOutro(string bossId, Action done)
        {
            string line = bossId switch
            {
                "boss_pickman" => "Bah... volveré a pintarte. Mejor.",
                "boss_dagon" => "LAS... MAREAS... VUELVEN...",
                "boss_hydra" => "Mis hijos... te recordarán...",
                "boss_sacerdote" => "...",
                _ => null
            };
            if (line != null) Toast(Dialogue.Sp(bossId).name + ": «" + line + "»", Dialogue.Sp(bossId).color, 3.5f);
            done?.Invoke();
        }
    }
}
