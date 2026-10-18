using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Umbral
{
    /// <summary>
    /// Kit de interfaz construida por código (uGUI). Estilo: paneles oscuros con bordes dorados/violetas,
    /// como los menús ornamentados de Hades, en clave de grimorio antiguo.
    /// </summary>
    public static class UIKit
    {
        public static Font font;
        public static Sprite panelSprite, frameSprite, circleSprite, whiteSprite, glowSprite, diamondSprite;

        public static readonly Color Gold = new Color(0.95f, 0.78f, 0.38f);
        public static readonly Color Violet = new Color(0.62f, 0.42f, 0.95f);
        public static readonly Color Teal = new Color(0.3f, 0.9f, 0.85f);
        public static readonly Color Ink = new Color(0.04f, 0.02f, 0.07f, 0.94f);
        public static readonly Color Parchment = new Color(0.93f, 0.88f, 0.78f);
        public static readonly Color Dim = new Color(0.62f, 0.58f, 0.68f);
        public static readonly Color Blood = new Color(0.85f, 0.15f, 0.22f);

        public static void Init()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            whiteSprite = MakeSprite("white", 4, 4, (x, y) => Color.white, 0);
            panelSprite = MakeSprite("panel", 64, 64, (x, y) =>
            {
                float e = Mathf.Min(Mathf.Min(x, 63 - x), Mathf.Min(y, 63 - y));
                float a = 1f;
                // esquinas recortadas en diagonal
                float corner = Mathf.Min(x, 63 - x) + Mathf.Min(y, 63 - y);
                if (corner < 7) a = 0;
                Color c = Color.white;
                if (e < 2 || corner < 9) c = new Color(1, 1, 1, a); // borde
                else c = new Color(0.82f, 0.82f, 0.82f, a);
                return c;
            }, 14);
            frameSprite = MakeSprite("frame", 64, 64, (x, y) =>
            {
                float e = Mathf.Min(Mathf.Min(x, 63 - x), Mathf.Min(y, 63 - y));
                float corner = Mathf.Min(x, 63 - x) + Mathf.Min(y, 63 - y);
                if (corner < 7) return Color.clear;
                if (e < 2 || (corner >= 7 && corner < 9)) return Color.white;
                if (e >= 4 && e < 5) return new Color(1, 1, 1, 0.45f);
                return Color.clear;
            }, 14);
            circleSprite = MakeSprite("circle", 64, 64, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                return new Color(1, 1, 1, Mathf.Clamp01(31.5f - d));
            }, 0);
            glowSprite = MakeSprite("glow", 64, 64, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                return new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 2));
            }, 0);
            diamondSprite = MakeSprite("diamond", 64, 64, (x, y) =>
            {
                float d = Mathf.Abs(x - 31.5f) + Mathf.Abs(y - 31.5f);
                return new Color(1, 1, 1, Mathf.Clamp01(31.5f - d));
            }, 0);
        }

        static Sprite MakeSprite(string name, int w, int h, Func<int, int, Color> f, int border)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
            t.SetPixels(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        public static Canvas MakeCanvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin = default, Vector2 offMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        /// <summary>Rectángulo con anclaje en un punto, posición y tamaño.</summary>
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name, float pad = 0)
            => Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));

        public static Image Img(RectTransform rt, Color c, Sprite s = null, bool raycast = false)
        {
            var i = rt.gameObject.AddComponent<Image>();
            i.sprite = s ?? whiteSprite;
            i.color = c;
            i.raycastTarget = raycast;
            if (s == panelSprite || s == frameSprite) i.type = Image.Type.Sliced;
            return i;
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Stretch(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = c; t.alignment = align; t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        public static Text LabelAt(Transform parent, string text, int size, Color c, Vector2 anchor, Vector2 pos, Vector2 boxSize, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var box = Box(parent, "Label", anchor, pos, boxSize);
            return Label(box, text, size, c, align, style);
        }

        /// <summary>Panel ornamentado: fondo de tinta + marco dorado.</summary>
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color? border = null, Color? fill = null)
        {
            var rt = Box(parent, name, anchor, pos, size);
            Img(rt, fill ?? Ink, panelSprite, true);
            var fr = Stretch(rt, "Frame", -3);
            Img(fr, border ?? Gold.WithA(0.85f), frameSprite);
            return rt;
        }

        public static Button Button(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int fontSize = 30, Color? accent = null)
        {
            var rt = Box(parent, "Btn_" + text, anchor, pos, size);
            var bg = Img(rt, new Color(0.08f, 0.05f, 0.12f, 0.95f), panelSprite, true);
            var fr = Stretch(rt, "Frame", -2);
            var frImg = Img(fr, (accent ?? Gold).WithA(0.6f), frameSprite);
            var lbl = Label(rt, text, fontSize, Parchment);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.4f, 1.9f);
            colors.selectedColor = new Color(1.7f, 1.5f, 2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.colorMultiplier = 1.6f;
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            b.onClick.AddListener(() => { Sfx.Play(SfxId.UiConfirm); onClick?.Invoke(); });
            var hl = rt.gameObject.AddComponent<ButtonHighlight>();
            hl.frame = frImg; hl.label = lbl; hl.accent = accent ?? Gold;
            return b;
        }

        public static Image Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fill, Color back, out Image backImg)
        {
            var rt = Box(parent, "Bar", anchor, pos, size);
            backImg = Img(rt, back);
            var f = Stretch(rt, "Fill", 2);
            var img = Img(f, fill);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1;
            return img;
        }

        public static void Select(GameObject go)
        {
            if (EventSystem.current == null || go == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(go);
        }

        /// <summary>Configura la navegación explícita vertical/horizontal de una lista de botones.</summary>
        public static void ChainNav(IList<Selectable> items, bool horizontal = false)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                var prev = items[(i - 1 + items.Count) % items.Count];
                var next = items[(i + 1) % items.Count];
                if (horizontal) { nav.selectOnLeft = prev; nav.selectOnRight = next; }
                else { nav.selectOnUp = prev; nav.selectOnDown = next; }
                items[i].navigation = nav;
            }
        }

        public static void GridNav(IList<Selectable> items, int columns)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                int r = i / columns, c = i % columns;
                int rows = (items.Count + columns - 1) / columns;
                Selectable At(int rr, int cc)
                {
                    rr = (rr + rows) % rows; cc = (cc + columns) % columns;
                    int idx = rr * columns + cc;
                    if (idx >= items.Count) idx = items.Count - 1;
                    return items[idx];
                }
                nav.selectOnLeft = At(r, c - 1); nav.selectOnRight = At(r, c + 1);
                nav.selectOnUp = At(r - 1, c); nav.selectOnDown = At(r + 1, c);
                items[i].navigation = nav;
            }
        }
    }

    /// <summary>Resalta el marco del botón cuando está seleccionado (ratón o mando).</summary>
    public class ButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image frame; public Text label; public Color accent;
        public Action onSelect;
        bool on;
        public void OnSelect(BaseEventData e) { Set(true); Sfx.Play(SfxId.UiMove); onSelect?.Invoke(); }
        public void OnDeselect(BaseEventData e) => Set(false);
        public void OnPointerEnter(PointerEventData e) { if (GetComponent<Selectable>().interactable) UIKit.Select(gameObject); }
        public void OnPointerExit(PointerEventData e) { }
        void Set(bool v)
        {
            on = v;
            if (frame != null) frame.color = v ? accent : accent.WithA(0.6f);
            if (label != null) label.color = v ? Color.white : UIKit.Parchment;
            transform.localScale = v ? Vector3.one * 1.04f : Vector3.one;
        }
    }
}
