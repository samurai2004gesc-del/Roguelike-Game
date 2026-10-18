using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral
{
    /// <summary>
    /// Capa de interfaz anclada al mundo: números de daño, textos flotantes, barras de vida de enemigos
    /// (con armadura y pips de maldiciones) y avisos de interacción.
    /// </summary>
    public class UIFloat : MonoBehaviour
    {
        static UIFloat inst;
        RectTransform root;
        Canvas canvas;

        class Floater { public RectTransform rt; public Text text; public Vector3 world; public float t, life, scale; public Vector2 drift; public bool active; }
        readonly List<Floater> floaters = new List<Floater>();

        class Bar
        {
            public Combatant c; public RectTransform rt; public Image hp, hpLag, armor, back; public RectTransform pips; public Text name;
            public float lag; public readonly List<Image> pipImgs = new List<Image>();
        }
        readonly Dictionary<Combatant, Bar> bars = new Dictionary<Combatant, Bar>();
        readonly List<Combatant> remove = new List<Combatant>();

        class Prompt { public RectTransform rt; public Text text; public Vector3 world; public bool used; }
        readonly List<Prompt> prompts = new List<Prompt>();

        public static void Create(Transform parent)
        {
            var c = UIKit.MakeCanvas("WorldUI", 10, parent);
            inst = c.gameObject.AddComponent<UIFloat>();
            inst.canvas = c;
            inst.root = (RectTransform)c.transform;
        }

        Vector2 ToCanvas(Vector3 world, out bool visible)
        {
            visible = false;
            var cam = CameraRig.Instance != null ? CameraRig.Instance.cam : Camera.main;
            if (cam == null) return Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(world);
            visible = sp.z > 0;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var local);
            return local;
        }

        // ---------------- Números y textos ----------------

        public static void Number(Vector3 world, string txt, Color c, float scale) { if (GameSettings.DamageNumbers) Spawn(world, txt, c, scale, 0.75f, true); }
        public static void Text(Vector3 world, string txt, Color c, float scale) => Spawn(world, txt, c, scale, 1.2f, false);

        static void Spawn(Vector3 world, string txt, Color c, float scale, float life, bool isNumber)
        {
            if (inst == null) return;
            Floater f = null;
            foreach (var x in inst.floaters) if (!x.active) { f = x; break; }
            if (f == null)
            {
                f = new Floater();
                f.rt = UIKit.Box(inst.root, "Float", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 80));
                f.text = UIKit.Label(f.rt, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                var ol = f.text.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(0.05f, 0, 0.08f, 0.9f);
                ol.effectDistance = new Vector2(2, 2);
                inst.floaters.Add(f);
            }
            f.active = true;
            f.rt.gameObject.SetActive(true);
            f.text.text = txt;
            f.text.color = c;
            f.text.fontSize = isNumber ? 40 : 30;
            f.world = world + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
            f.t = 0; f.life = life; f.scale = scale;
            f.drift = isNumber ? new Vector2(Random.Range(-40f, 40f), 90f) : new Vector2(0, 50f);
        }

        // ---------------- Barras de vida ----------------

        public static void Track(Combatant c)
        {
            if (inst == null || c == null || inst.bars.ContainsKey(c) || c.isBoss) return;
            var b = new Bar { c = c };
            float w = c.isElite ? 150 : 100;
            b.rt = UIKit.Box(inst.root, "HpBar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 12));
            b.back = UIKit.Img(b.rt, new Color(0.05f, 0.02f, 0.06f, 0.85f));
            var lagRt = UIKit.Stretch(b.rt, "Lag", 2);
            b.hpLag = UIKit.Img(lagRt, new Color(1f, 0.9f, 0.6f, 0.9f));
            b.hpLag.type = Image.Type.Filled; b.hpLag.fillMethod = Image.FillMethod.Horizontal;
            var hpRt = UIKit.Stretch(b.rt, "Hp", 2);
            b.hp = UIKit.Img(hpRt, c.isElite ? new Color(0.95f, 0.4f, 0.15f) : new Color(0.85f, 0.15f, 0.2f));
            b.hp.type = Image.Type.Filled; b.hp.fillMethod = Image.FillMethod.Horizontal;
            var arRt = UIKit.Stretch(b.rt, "Armor", 2);
            b.armor = UIKit.Img(arRt, new Color(0.75f, 0.78f, 0.85f));
            b.armor.type = Image.Type.Filled; b.armor.fillMethod = Image.FillMethod.Horizontal;
            b.pips = UIKit.Box(b.rt, "Pips", new Vector2(0.5f, 1f), new Vector2(0, 14), new Vector2(w, 16), new Vector2(0.5f, 0));
            var hlg = b.pips.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 3; hlg.childControlWidth = false; hlg.childControlHeight = false;
            if (c.isElite)
            {
                var nb = UIKit.Box(b.rt, "Name", new Vector2(0.5f, 0f), new Vector2(0, -4), new Vector2(300, 24), new Vector2(0.5f, 1f));
                b.name = UIKit.Label(nb, c.displayName, 18, new Color(1f, 0.75f, 0.45f));
            }
            b.lag = 1;
            b.rt.gameObject.SetActive(false);
            inst.bars[c] = b;
        }

        // ---------------- Avisos de interacción ----------------

        public static void ShowPrompt(Vector3 world, string text)
        {
            if (inst == null) return;
            Prompt p = null;
            foreach (var x in inst.prompts) if (!x.used) { p = x; break; }
            if (p == null)
            {
                p = new Prompt();
                p.rt = UIKit.Box(inst.root, "Prompt", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 60));
                UIKit.Img(p.rt, new Color(0.03f, 0.02f, 0.06f, 0.85f), UIKit.panelSprite);
                var fr = UIKit.Stretch(p.rt, "Fr", -2); UIKit.Img(fr, UIKit.Gold.WithA(0.7f), UIKit.frameSprite);
                p.text = UIKit.Label(p.rt, "", 24, UIKit.Parchment);
                inst.prompts.Add(p);
            }
            p.used = true;
            p.world = world;
            p.text.text = text;
            float w = Mathf.Clamp(text.Length * 12f + 40f, 160f, 700f);
            p.rt.sizeDelta = new Vector2(w, text.Contains("\n") ? 84 : 56);
            p.rt.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var f in floaters)
            {
                if (!f.active) continue;
                f.t += dt;
                float k = f.t / f.life;
                if (k >= 1f) { f.active = false; f.rt.gameObject.SetActive(false); continue; }
                var p = ToCanvas(f.world, out bool vis);
                f.rt.anchoredPosition = p + f.drift * U.EaseOutCubic(k);
                float pop = k < 0.15f ? Mathf.Lerp(1.6f, 1f, k / 0.15f) : 1f;
                f.rt.localScale = Vector3.one * f.scale * pop;
                var c = f.text.color; c.a = k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f; f.text.color = c;
                f.rt.gameObject.SetActive(vis);
            }

            remove.Clear();
            foreach (var kv in bars)
            {
                var b = kv.Value; var c = kv.Key;
                if (c == null || !c.Alive) { remove.Add(c); continue; }
                bool show = c.hp < c.maxHp || c.isElite || c.armor > 0 || c.status.Active.GetEnumerator().MoveNext();
                if (c.isDummy) show = true;
                b.rt.gameObject.SetActive(show);
                if (!show) continue;
                var pos = ToCanvas(c.transform.position + Vector3.up * (c.height + 0.45f), out bool vis);
                b.rt.anchoredPosition = pos;
                if (!vis) { b.rt.gameObject.SetActive(false); continue; }
                float frac = c.HpFrac;
                b.hp.fillAmount = frac;
                b.lag = Mathf.MoveTowards(b.lag, frac, dt * (b.lag > frac ? 0.6f : 5f));
                b.hpLag.fillAmount = b.lag;
                b.armor.fillAmount = c.maxArmor > 0 ? c.armor / c.maxArmor : 0;
                UpdatePips(b);
            }
            foreach (var c in remove)
            {
                if (bars.TryGetValue(c, out var b) && b.rt != null) Destroy(b.rt.gameObject);
                bars.Remove(c);
            }

            foreach (var p in prompts)
            {
                if (!p.used) { if (p.rt.gameObject.activeSelf) p.rt.gameObject.SetActive(false); continue; }
                var pos = ToCanvas(p.world, out bool vis);
                p.rt.anchoredPosition = pos;
                p.rt.gameObject.SetActive(vis);
                p.used = false; // debe volver a pedirse cada fotograma
            }
        }

        void UpdatePips(Bar b)
        {
            int i = 0;
            foreach (var s in b.c.status.Active)
            {
                if (s == StatusType.Vulnerable) continue;
                if (i >= b.pipImgs.Count)
                {
                    var rt = UIKit.Box(b.pips, "pip", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
                    b.pipImgs.Add(UIKit.Img(rt, Color.white, UIKit.diamondSprite));
                }
                b.pipImgs[i].gameObject.SetActive(true);
                b.pipImgs[i].color = StatusController.ColorOf(s);
                i++;
            }
            for (; i < b.pipImgs.Count; i++) b.pipImgs[i].gameObject.SetActive(false);
        }

        public static void ClearAll()
        {
            if (inst == null) return;
            foreach (var kv in inst.bars) if (kv.Value.rt != null) Destroy(kv.Value.rt.gameObject);
            inst.bars.Clear();
            foreach (var f in inst.floaters) { f.active = false; f.rt.gameObject.SetActive(false); }
        }
    }
}
