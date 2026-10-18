using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Umbral
{
    /// <summary>
    /// Raíz de la interfaz: lienzos, HUD, transiciones de pantalla, avisos y pila de menús.
    /// Los menús pausan el juego y bloquean la entrada del jugador.
    /// </summary>
    public partial class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance;
        public static bool MenuOpen => Instance != null && Instance.stack.Count > 0;

        Canvas hudCanvas, menuCanvas, topCanvas;
        public RectTransform hud, menus, top;
        Image fade;
        readonly List<GameObject> stack = new List<GameObject>();
        readonly List<Action> onCloseStack = new List<Action>();

        public static void Create(Transform parent)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<UIRoot>();
            Instance.Build();
        }

        void Build()
        {
            // sistema de eventos (ratón + mando)
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            hudCanvas = UIKit.MakeCanvas("HUD", 20, transform);
            hud = (RectTransform)hudCanvas.transform;
            menuCanvas = UIKit.MakeCanvas("Menus", 40, transform);
            menus = (RectTransform)menuCanvas.transform;
            topCanvas = UIKit.MakeCanvas("Top", 60, transform);
            top = (RectTransform)topCanvas.transform;
            var f = UIKit.Stretch(top, "Fade");
            fade = UIKit.Img(f, new Color(0.01f, 0, 0.02f, 0));
            BuildHud();
            BuildToasts();
        }

        // ------------------------------------------------------------------ pila de menús
        public RectTransform PushScreen(string name, Action onClose = null, float dim = 0.78f)
        {
            var root = UIKit.Stretch(menus, name);
            var bg = UIKit.Img(root, new Color(0.01f, 0.0f, 0.03f, dim), null, true);
            stack.Add(root.gameObject);
            onCloseStack.Add(onClose);
            HitStop.pauseLocks = stack.Count;
            if (PlayerController.Instance != null) PlayerController.Instance.inputLocked = true;
            StartCoroutine(PopIn(root));
            return root;
        }

        IEnumerator PopIn(RectTransform rt)
        {
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            float t = 0;
            while (t < 0.15f && rt != null) { t += Time.unscaledDeltaTime; cg.alpha = t / 0.15f; yield return null; }
            if (cg != null) cg.alpha = 1;
        }

        public void PopScreen()
        {
            if (stack.Count == 0) return;
            int i = stack.Count - 1;
            var go = stack[i];
            var cb = onCloseStack[i];
            stack.RemoveAt(i);
            onCloseStack.RemoveAt(i);
            if (go != null) Destroy(go);
            HitStop.pauseLocks = stack.Count;
            if (stack.Count == 0 && PlayerController.Instance != null) StartCoroutine(UnlockNextFrame());
            cb?.Invoke();
        }

        IEnumerator UnlockNextFrame()
        {
            yield return null;
            if (stack.Count == 0 && PlayerController.Instance != null && !dialogueActive) PlayerController.Instance.inputLocked = false;
        }

        public void CloseAll() { while (stack.Count > 0) PopScreen(); }

        bool cancelReady;
        void Update()
        {
            Music.Tick();
            HitStop.Tick();
            UpdateHud();
            if (stack.Count > 0)
            {
                // Cancelar cierra el menú superior (si lo permite)
                if (cancelReady && GameInput.Pressed(GameButton.Cancel) && !dialogueActive)
                {
                    var top = stack[stack.Count - 1];
                    if (top != null && top.GetComponent<NoCancel>() == null) { Sfx.Play(SfxId.UiBack); PopScreen(); }
                }
                cancelReady = true;
                // mantener una selección para el mando
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && GameInput.UsingGamepad)
                {
                    var sel = stack[stack.Count - 1]?.GetComponentInChildren<Selectable>();
                    if (sel != null) UIKit.Select(sel.gameObject);
                }
            }
            else cancelReady = false;

            // menú de pausa / boones
            if (Game.Instance != null && Game.Instance.state == GameState.Run && stack.Count == 0 && !dialogueActive && PlayerController.Instance != null && PlayerController.Instance.Alive)
            {
                if (GameInput.Pressed(GameButton.Pause)) OpenPause();
                else if (GameInput.Pressed(GameButton.Info)) OpenBoonList();
            }
            else if (Game.Instance != null && Game.Instance.state == GameState.Hub && stack.Count == 0 && !dialogueActive && GameInput.Pressed(GameButton.Pause)) OpenPause();
        }

        // ------------------------------------------------------------------ transiciones
        public IEnumerator FadeOut(float t = 0.35f)
        {
            fade.raycastTarget = true;
            for (float x = 0; x < t; x += Time.unscaledDeltaTime) { fade.color = new Color(0.01f, 0, 0.02f, x / t); yield return null; }
            fade.color = new Color(0.01f, 0, 0.02f, 1);
        }

        public IEnumerator FadeIn(float t = 0.45f)
        {
            for (float x = 0; x < t; x += Time.unscaledDeltaTime) { fade.color = new Color(0.01f, 0, 0.02f, 1 - x / t); yield return null; }
            fade.color = new Color(0.01f, 0, 0.02f, 0);
            fade.raycastTarget = false;
        }

        // ------------------------------------------------------------------ avisos
        RectTransform toastRoot;
        void BuildToasts()
        {
            toastRoot = UIKit.Box(top, "Toasts", new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(900, 400), new Vector2(0.5f, 1f));
            var v = toastRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter; v.spacing = 8; v.childControlHeight = false; v.childControlWidth = false;
        }

        public void Toast(string text, Color c, float time = 2.6f)
        {
            var rt = UIKit.Box(toastRoot, "Toast", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Mathf.Clamp(text.Length * 15 + 80, 300, 880), 56));
            UIKit.Img(rt, new Color(0.03f, 0.01f, 0.06f, 0.88f), UIKit.panelSprite);
            var fr = UIKit.Stretch(rt, "f", -2); UIKit.Img(fr, c.WithA(0.7f), UIKit.frameSprite);
            UIKit.Label(rt, Lines.G(text), 26, c);
            StartCoroutine(ToastLife(rt, time));
        }

        IEnumerator ToastLife(RectTransform rt, float time)
        {
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            float t = 0;
            while (t < time && rt != null)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = t < 0.2f ? t / 0.2f : t > time - 0.4f ? (time - t) / 0.4f : 1;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        /// <summary>Gran rótulo central (nombre de región, "HAS MUERTO", etc.).</summary>
        public void Banner(string title, string subtitle, Color c, float time = 3f)
        {
            var rt = UIKit.Box(top, "Banner", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1400, 220));
            var bg = UIKit.Img(rt, new Color(0, 0, 0, 0.55f), UIKit.glowSprite);
            var t = UIKit.LabelAt(rt, title, 74, c, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1400, 100), TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            if (!string.IsNullOrEmpty(subtitle)) UIKit.LabelAt(rt, subtitle, 32, UIKit.Parchment, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(1400, 50), TextAnchor.MiddleCenter, FontStyle.Italic);
            StartCoroutine(ToastLife(rt, time));
        }
    }

    /// <summary>Marca una pantalla que no se puede cerrar con Cancelar.</summary>
    public class NoCancel : MonoBehaviour { }
}
