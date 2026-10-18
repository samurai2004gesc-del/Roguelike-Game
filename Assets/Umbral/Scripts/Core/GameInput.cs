using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Umbral
{
    public enum GameButton { Attack, Special, Cast, Sprint, Interact, Hex, Pause, Info, Gift, Confirm, Cancel }

    /// <summary>
    /// Entrada unificada teclado+ratón / mando. Funciona con el Input System nuevo y, si no está activo,
    /// con el Input Manager clásico.
    ///
    /// Teclado: WASD mover · Clic izq / J ataque · Clic der / K especial · Q / L lanzamiento (Signo Arcano)
    ///          Espacio / Shift esprint · E interactuar · R eco (hex) · Tab boones · G regalo · Esc pausa
    /// Mando:   Stick izq mover · Stick der apuntar · X ataque · Y especial · RB lanzamiento · A esprint
    ///          B interactuar · LB eco · Select boones · Cruceta arriba regalo · Start pausa
    /// </summary>
    public static class GameInput
    {
        public static bool UsingGamepad { get; private set; }
        static Vector3 lastMouse;

        public static void Tick()
        {
#if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            if (gp != null && (gp.leftStick.ReadValue().sqrMagnitude > 0.1f || gp.rightStick.ReadValue().sqrMagnitude > 0.1f ||
                               gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame))
                UsingGamepad = true;
            var m = Mouse.current;
            if (m != null)
            {
                Vector3 mp = m.position.ReadValue();
                if ((mp - lastMouse).sqrMagnitude > 4f || m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame) UsingGamepad = false;
                lastMouse = mp;
            }
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) UsingGamepad = false;
#else
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0) { }
            if ((Input.mousePosition - lastMouse).sqrMagnitude > 4f) UsingGamepad = false;
            lastMouse = Input.mousePosition;
#endif
        }

        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
                }
                var gp = Gamepad.current;
                if (gp != null)
                {
                    var s = gp.leftStick.ReadValue();
                    if (s.sqrMagnitude > 0.04f) v += s;
                    v += gp.dpad.ReadValue() * 0f; // la cruceta se reserva para menús/regalos
                }
#else
                v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>Dirección del stick derecho (si se está usando).</summary>
        public static Vector2 AimStick
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var gp = Gamepad.current;
                if (gp != null)
                {
                    var s = gp.rightStick.ReadValue();
                    if (s.sqrMagnitude > 0.15f) return s;
                }
#endif
                return Vector2.zero;
            }
        }

        public static Vector2 MouseScreen
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        /// <summary>Navegación de menús (stick, cruceta o flechas).</summary>
        public static Vector2 NavigatePressed
        {
            get
            {
                Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var gp = Gamepad.current;
                if (gp != null)
                {
                    if (gp.dpad.up.wasPressedThisFrame) v.y += 1;
                    if (gp.dpad.down.wasPressedThisFrame) v.y -= 1;
                    if (gp.dpad.left.wasPressedThisFrame) v.x -= 1;
                    if (gp.dpad.right.wasPressedThisFrame) v.x += 1;
                }
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.upArrowKey.wasPressedThisFrame) v.y += 1;
                    if (k.downArrowKey.wasPressedThisFrame) v.y -= 1;
                    if (k.leftArrowKey.wasPressedThisFrame) v.x -= 1;
                    if (k.rightArrowKey.wasPressedThisFrame) v.x += 1;
                }
#else
                if (Input.GetKeyDown(KeyCode.UpArrow)) v.y += 1;
                if (Input.GetKeyDown(KeyCode.DownArrow)) v.y -= 1;
                if (Input.GetKeyDown(KeyCode.LeftArrow)) v.x -= 1;
                if (Input.GetKeyDown(KeyCode.RightArrow)) v.x += 1;
#endif
                return v;
            }
        }

        public static bool Pressed(GameButton b) => Query(b, 0);
        public static bool Held(GameButton b) => Query(b, 1);
        public static bool Released(GameButton b) => Query(b, 2);

#if ENABLE_INPUT_SYSTEM
        static bool Q(UnityEngine.InputSystem.Controls.ButtonControl c, int mode)
        {
            if (c == null) return false;
            return mode == 0 ? c.wasPressedThisFrame : mode == 1 ? c.isPressed : c.wasReleasedThisFrame;
        }
#else
        static bool Q(KeyCode c, int mode) => mode == 0 ? Input.GetKeyDown(c) : mode == 1 ? Input.GetKey(c) : Input.GetKeyUp(c);
#endif

        static bool Query(GameButton b, int mode)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
            bool r = false;
            switch (b)
            {
                case GameButton.Attack:
                    r = (m != null && Q(m.leftButton, mode)) || (k != null && Q(k.jKey, mode)) || (g != null && Q(g.buttonWest, mode)); break;
                case GameButton.Special:
                    r = (m != null && Q(m.rightButton, mode)) || (k != null && Q(k.kKey, mode)) || (g != null && Q(g.buttonNorth, mode)); break;
                case GameButton.Cast:
                    r = (k != null && (Q(k.qKey, mode) || Q(k.lKey, mode))) || (g != null && Q(g.rightShoulder, mode)); break;
                case GameButton.Sprint:
                    r = (k != null && (Q(k.spaceKey, mode) || Q(k.leftShiftKey, mode))) || (g != null && Q(g.buttonSouth, mode)); break;
                case GameButton.Interact:
                    r = (k != null && Q(k.eKey, mode)) || (g != null && Q(g.buttonEast, mode)); break;
                case GameButton.Hex:
                    r = (k != null && (Q(k.rKey, mode) || Q(k.fKey, mode))) || (g != null && Q(g.leftShoulder, mode)); break;
                case GameButton.Pause:
                    r = (k != null && Q(k.escapeKey, mode)) || (g != null && Q(g.startButton, mode)); break;
                case GameButton.Info:
                    r = (k != null && Q(k.tabKey, mode)) || (g != null && Q(g.selectButton, mode)); break;
                case GameButton.Gift:
                    r = (k != null && Q(k.gKey, mode)) || (g != null && Q(g.dpad.up, mode)); break;
                case GameButton.Confirm:
                    r = (k != null && (Q(k.enterKey, mode) || Q(k.eKey, mode) || Q(k.spaceKey, mode))) || (g != null && Q(g.buttonSouth, mode)); break;
                case GameButton.Cancel:
                    r = (k != null && (Q(k.escapeKey, mode) || Q(k.backspaceKey, mode))) || (g != null && Q(g.buttonEast, mode)); break;
            }
            return r;
#else
            switch (b)
            {
                case GameButton.Attack: return Q(KeyCode.Mouse0, mode) || Q(KeyCode.J, mode) || Q(KeyCode.JoystickButton2, mode);
                case GameButton.Special: return Q(KeyCode.Mouse1, mode) || Q(KeyCode.K, mode) || Q(KeyCode.JoystickButton3, mode);
                case GameButton.Cast: return Q(KeyCode.Q, mode) || Q(KeyCode.L, mode) || Q(KeyCode.JoystickButton5, mode);
                case GameButton.Sprint: return Q(KeyCode.Space, mode) || Q(KeyCode.LeftShift, mode) || Q(KeyCode.JoystickButton0, mode);
                case GameButton.Interact: return Q(KeyCode.E, mode) || Q(KeyCode.JoystickButton1, mode);
                case GameButton.Hex: return Q(KeyCode.R, mode) || Q(KeyCode.F, mode) || Q(KeyCode.JoystickButton4, mode);
                case GameButton.Pause: return Q(KeyCode.Escape, mode) || Q(KeyCode.JoystickButton7, mode);
                case GameButton.Info: return Q(KeyCode.Tab, mode) || Q(KeyCode.JoystickButton6, mode);
                case GameButton.Gift: return Q(KeyCode.G, mode);
                case GameButton.Confirm: return Q(KeyCode.Return, mode) || Q(KeyCode.E, mode) || Q(KeyCode.Space, mode) || Q(KeyCode.JoystickButton0, mode);
                case GameButton.Cancel: return Q(KeyCode.Escape, mode) || Q(KeyCode.Backspace, mode) || Q(KeyCode.JoystickButton1, mode);
            }
            return false;
#endif
        }

        public static string Label(GameButton b)
        {
            if (UsingGamepad)
            {
                switch (b)
                {
                    case GameButton.Attack: return "X";
                    case GameButton.Special: return "Y";
                    case GameButton.Cast: return "RB";
                    case GameButton.Sprint: return "A";
                    case GameButton.Interact: return "B";
                    case GameButton.Hex: return "LB";
                    case GameButton.Pause: return "Start";
                    case GameButton.Info: return "Select";
                    case GameButton.Gift: return "↑";
                    case GameButton.Confirm: return "A";
                    case GameButton.Cancel: return "B";
                }
            }
            switch (b)
            {
                case GameButton.Attack: return "Clic Izq";
                case GameButton.Special: return "Clic Der";
                case GameButton.Cast: return "Q";
                case GameButton.Sprint: return "Espacio";
                case GameButton.Interact: return "E";
                case GameButton.Hex: return "R";
                case GameButton.Pause: return "Esc";
                case GameButton.Info: return "Tab";
                case GameButton.Gift: return "G";
                case GameButton.Confirm: return "Enter";
                case GameButton.Cancel: return "Esc";
            }
            return "?";
        }
    }
}
