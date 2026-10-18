using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Sothis Whateley, vástago de Yog-Sothoth.
    /// Movimiento en 8 direcciones, esprint (embestida corta invulnerable + carrera sostenida), apuntado con ratón o stick,
    /// Lucidez (maná para movimientos Omega, se rellena al limpiar la sala), Último Umbral (desafío a la muerte),
    /// Signo Arcano (lanzamiento que ata enemigos) y Ecos del Padre (hex).
    /// </summary>
    public class PlayerController : Combatant
    {
        public static PlayerController Instance;

        public Weapon weapon;
        public CastController cast;
        public HexController hex;
        public bool female;

        public Vector2 aimDir = Vector2.up;
        public Vector2 aimPoint;
        public float magick;
        public float MaxMagick { get; private set; } = 50f;
        public float baseMoveSpeed = 7.4f;
        public Vector2 velocity;

        // esprint
        bool dashing; float dashEnd; Vector2 dashDir, dashFrom; float dashSpeed;
        public bool sprinting; float sprintTick; float dashReadyAt;
        public bool IsDashing => dashing;

        // entradas
        float bufAttack = -9, bufSpecial = -9, bufDash = -9;
        float atkPressTime = -9, spPressTime = -9, castPressTime = -9;
        bool atkHeld, spHeld;
        public bool inputLocked;
        public bool inHub;

        // varios
        public bool blocking; public Vector2 blockDir;
        public float empoweredNext, empoweredActive;
        bool shieldReady;
        Vector2 lungeVel;
        Interactable nearest;
        float hurtCooldown = 0.55f;
        public Familiar familiar;

        public static PlayerController Spawn(Transform parent, string weaponId, bool female)
        {
            var go = new GameObject("Sothis");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<PlayerController>();
            p.team = Team.Player;
            p.displayName = "Sothis";
            p.radius = 0.45f;
            p.height = 1.9f;
            p.female = female;
            Instance = p;
            p.rig = CharacterArt.Protagonist(go.transform, female);
            p.cast = go.AddComponent<CastController>(); p.cast.P = p;
            p.hex = go.AddComponent<HexController>(); p.hex.P = p;
            p.SetWeapon(weaponId);
            p.RecomputeStats();
            p.hp = p.maxHp;
            p.magick = p.MaxMagick;
            return p;
        }

        public void SetWeapon(string weaponId)
        {
            if (weapon != null) { weapon.Cancel(); Destroy(weapon); }
            if (rig.weaponPivot != null) rig.weaponPivot.DestroyChildren();
            rig.orbiters.RemoveAll(o => o == null || o.name == "glow");
            foreach (Transform c in transform) if (c.name == "glow") Destroy(c.gameObject);
            var def = Weapons.Get(weaponId) ?? Weapons.All[0];
            weapon = (Weapon)gameObject.AddComponent(def.behaviour);
            weapon.Init(this, def);
            rig.Init();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this) Instance = null;
        }

        public void RecomputeStats()
        {
            var m = RunState.Current?.mods ?? PlayerModifiers.Default;
            float hearts = RunState.Current?.heartsHp ?? 0;
            float newMax = Mathf.Round((50f + m.maxHpBonus + hearts) * m.maxHpMult);
            if (newMax > maxHp && maxHp > 0) hp += newMax - maxHp;
            maxHp = newMax;
            hp = Mathf.Min(hp, maxHp);
            MaxMagick = Mathf.Round((50f + m.maxMagickBonus) * m.maxMagickMult);
            magick = Mathf.Min(magick, MaxMagick);
        }

        // =====================================================================
        protected override void Update()
        {
            base.Update();
            if (dead) return;
            GameInput.Tick();
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            var m = RunState.Current?.mods ?? PlayerModifiers.Default;

            UpdateAim();
            if (!inputLocked) HandleInput();
            Move(dt, m);

            // regeneración de Lucidez y carga pasiva del Eco
            if (m.magickRegen > 0 && !inHub) GainMagick(m.magickRegen * dt);
            if (inHub) magick = MaxMagick;
            if (m.hexPassiveCharge > 0 && RoomDirector.InCombat) hex.AddCharge(m.hexPassiveCharge * dt);
            if (RoomDirector.InCombat) m.Tick(dt);

            UpdateInteract();
            rig.velocity = velocity;
        }

        void UpdateAim()
        {
            Vector2 a = Vector2.zero;
            var cr = CameraRig.Instance;
            if (GameInput.UsingGamepad)
            {
                var stick = GameInput.AimStick;
                if (stick.sqrMagnitude > 0.01f && cr != null) a = cr.InputToWorld(stick);
                else if (GameInput.Move.sqrMagnitude > 0.05f && cr != null) a = cr.InputToWorld(GameInput.Move);
                // asistencia de apuntado suave hacia el enemigo más cercano en la dirección
                var assist = AimAssist(a.sqrMagnitude > 0.01f ? a.normalized : aimDir);
                if (assist.HasValue) a = Vector2.Lerp(a.normalized, assist.Value, 0.5f);
                if (a.sqrMagnitude > 0.01f) { aimDir = a.normalized; aimPoint = Pos + aimDir * 5f; }
            }
            else if (cr != null)
            {
                aimPoint = cr.ScreenToGround(GameInput.MouseScreen);
                a = aimPoint - Pos;
                if (a.sqrMagnitude > 0.04f) aimDir = a.normalized;
            }
        }

        Vector2? AimAssist(Vector2 dir)
        {
            Combatant best = null; float bestScore = 0;
            foreach (var c in Combat.InArc(Pos, dir, 9f, 50f, Team.Player))
            {
                float score = 1f / (1f + Vector2.Distance(c.Pos, Pos));
                if (score > bestScore) { bestScore = score; best = c; }
            }
            return best != null ? (best.Pos - Pos).normalized : (Vector2?)null;
        }

        public Vector2 ClampedAimPoint(float maxDist)
        {
            Vector2 d = aimPoint - Pos;
            if (GameInput.UsingGamepad) d = aimDir * maxDist * 0.75f;
            if (d.magnitude > maxDist) d = d.normalized * maxDist;
            Vector2 p = Pos + d;
            if (Arena.Current != null && !Arena.Current.InsidePolygon(p)) p = Arena.Current.Resolve(p, 0.5f);
            return p;
        }

        void HandleInput()
        {
            float now = Time.time;
            if (GameInput.Pressed(GameButton.Attack)) { bufAttack = now; atkPressTime = now; atkHeld = true; }
            if (GameInput.Pressed(GameButton.Special)) { bufSpecial = now; spPressTime = now; spHeld = true; }
            if (GameInput.Pressed(GameButton.Sprint)) bufDash = now;
            bool atkDown = GameInput.Held(GameButton.Attack), spDown = GameInput.Held(GameButton.Special);

            // esprint (cancela ataques)
            if (now - bufDash < 0.2f && !dashing && now >= dashReadyAt && !(cast != null && cast.Busy))
            {
                bufDash = -9;
                StartDash();
            }

            if (dashing) { atkHeld = atkDown; spHeld = spDown; return; }

            if (weapon.charging)
            {
                bool held = weapon.chargeSlot == MoveSlot.Attack ? atkDown : spDown;
                weapon.TickCharge(held);
            }
            else
            {
                if (now - bufAttack < 0.25f && weapon.CanStart)
                {
                    bufAttack = -9; StopSprint();
                    ConsumeEmpoweredFor();
                    weapon.Attack();
                }
                else if (now - bufSpecial < 0.25f && weapon.CanStart)
                {
                    bufSpecial = -9; StopSprint();
                    ConsumeEmpoweredFor();
                    weapon.Special();
                }
                else if (atkDown && atkHeld) { if (!weapon.Busy) ConsumeEmpoweredFor(); weapon.AttackHeld(now - atkPressTime); }
                else if (spDown && spHeld) weapon.SpecialHeld(now - spPressTime);
            }
            if (GameInput.Released(GameButton.Attack)) { atkHeld = false; weapon.AttackReleased(); }
            if (GameInput.Released(GameButton.Special)) { spHeld = false; weapon.SpecialReleased(); }
            if (!atkDown) atkHeld = false;
            if (!spDown) spHeld = false;

            // Signo Arcano
            if (!inHub || Save.Data.Flag("hub_cast"))
            {
                if (GameInput.Pressed(GameButton.Cast)) { castPressTime = now; cast.Pressed(); }
                if (GameInput.Held(GameButton.Cast) && castPressTime > 0) cast.Held(now - castPressTime);
                if (GameInput.Released(GameButton.Cast) && castPressTime > 0) { cast.Released(now - castPressTime); castPressTime = -9; }
            }
            if (GameInput.Pressed(GameButton.Hex) && !inHub) hex.TryActivate();
        }

        void ConsumeEmpoweredFor()
        {
            empoweredActive = empoweredNext;
            empoweredNext = 0;
        }

        /// <summary>Compatibilidad con las armas: el bonus ya se consumió al iniciar el movimiento.</summary>
        public float ConsumeEmpowered() => empoweredActive;

        void Move(float dt, PlayerModifiers m)
        {
            Vector2 input = inputLocked ? Vector2.zero : GameInput.Move;
            Vector2 world = CameraRig.Instance != null ? CameraRig.Instance.InputToWorld(input) : input;

            if (dashing)
            {
                MoveBy(dashDir * dashSpeed * dt);
                velocity = dashDir * dashSpeed;
                if (Random.value < 0.8f) Fx.Emit(FxKind.Glow, transform.position + Vector3.up * Random.Range(0.4f, 1.6f), -velocity.X0Z() * 0.1f, IridColor(), 0.35f, 0.3f);
                if (Time.time >= dashEnd) EndDash();
                return;
            }

            float speed = baseMoveSpeed * m.moveSpeedMult * weapon.moveMult * status.SpeedMult() * (cast != null ? cast.MoveMult : 1f);
            if (sprinting)
            {
                if (!GameInput.Held(GameButton.Sprint) || input.sqrMagnitude < 0.05f || weapon.Busy) StopSprint();
                else
                {
                    speed *= 1.6f * m.sprintSpeedMult;
                    sprintTick += dt;
                    if (sprintTick >= 0.16f)
                    {
                        sprintTick = 0;
                        foreach (var f in m.onSprintTick.ToArray()) f(Pos);
                    }
                    if (Random.value < 0.5f) Fx.Emit(FxKind.Glow, transform.position + Vector3.up * 0.3f, Vector3.up * 0.5f, IridColor().WithA(0.6f), 0.3f, 0.35f);
                }
            }

            Vector2 v = world * speed;
            velocity = Vector2.Lerp(velocity, v, 1f - Mathf.Exp(-20f * dt));
            Vector2 delta = velocity * dt + lungeVel * dt;
            lungeVel = Vector2.Lerp(lungeVel, Vector2.zero, 1f - Mathf.Exp(-14f * dt));
            if (delta.sqrMagnitude > 1e-7f) MoveBy(delta);

            // empujón suave de los enemigos (no se atraviesa a los enemigos salvo esprintando)
            foreach (var c in Combatant.All)
            {
                if (c == null || c == this || !c.Alive || c.team == Team.Player || c.isDummy && false) continue;
                Vector2 d = Pos - c.Pos;
                float min = radius + c.radius * 0.85f;
                float mag = d.magnitude;
                if (mag < min && mag > 0.001f) MoveBy(d / mag * (min - mag) * 0.6f);
            }

            // orientación: al moverse se mira hacia donde se camina salvo si se está atacando/apuntando
            if (!weapon.Busy && !weapon.charging && world.sqrMagnitude > 0.05f && !GameInput.UsingGamepad && !atkHeld && !spHeld) Face(world);
            else if (!weapon.Busy && !weapon.charging && world.sqrMagnitude > 0.05f && GameInput.UsingGamepad && GameInput.AimStick.sqrMagnitude < 0.01f) Face(world);
            if (weapon.charging || blocking) Face(aimDir);
        }

        public static Color IridColor()
        {
            float t = Time.time * 0.4f;
            return new Color(0.5f + 0.5f * Mathf.Cos(6.28f * t), 0.5f + 0.5f * Mathf.Cos(6.28f * (t + 0.33f)), 0.5f + 0.5f * Mathf.Cos(6.28f * (t + 0.67f)));
        }

        public void Face(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            facing = dir.normalized;
            transform.rotation = U.LookFlat(facing);
        }

        public void Lunge(Vector2 dir, float dist) { lungeVel += dir.normalized * dist * 10f; }

        public void SetInvulnerable(float seconds) { invulnUntil = Mathf.Max(invulnUntil, Time.time + seconds); }

        // ---------------- esprint ----------------
        void StartDash()
        {
            weapon.Cancel();
            if (cast != null) cast.CancelCharge();
            Vector2 input = GameInput.Move;
            Vector2 world = CameraRig.Instance != null ? CameraRig.Instance.InputToWorld(input) : input;
            dashDir = world.sqrMagnitude > 0.05f ? world.normalized : facing;
            var m = RunState.Current?.mods ?? PlayerModifiers.Default;
            float dist = 3.4f * m.dashDistanceMult;
            float dur = 0.17f;
            dashSpeed = dist / dur;
            dashing = true;
            dashFrom = Pos;
            dashEnd = Time.time + dur;
            SetInvulnerable(dur + 0.05f);
            Face(dashDir);
            Sfx.Play(SfxId.Dash, transform.position);
            rig.Kick(-0.2f);
            if (weapon is DaggerWeapon dg) dg.OnDash();
        }

        void EndDash()
        {
            dashing = false;
            dashReadyAt = Time.time + 0.08f;
            var m = RunState.Current?.mods ?? PlayerModifiers.Default;
            if (!inHub) foreach (var f in m.onDash.ToArray()) f(dashFrom, Pos);
            if (GameInput.Held(GameButton.Sprint) && GameInput.Move.sqrMagnitude > 0.05f) { sprinting = true; sprintTick = 0; }
            velocity = dashDir * baseMoveSpeed * 1.6f;
        }

        void StopSprint() { sprinting = false; }

        // ---------------- Lucidez ----------------
        public bool SpendMagick(float cost)
        {
            if (magick < cost - 0.01f) return false;
            magick = Mathf.Max(0, magick - cost);
            hex.AddCharge(cost);
            return true;
        }

        public void GainMagick(float x) { magick = Mathf.Min(MaxMagick, magick + x); }

        // ---------------- daño ----------------
        public float ModifyIncoming(float dmg, Hit hit)
        {
            var m = RunState.Current?.mods;
            if (m != null && m.firstHitShield > 0 && shieldReady && !inHub)
            {
                shieldReady = false;
                UIFloat.Text(transform.position + Vector3.up * 2.2f, "¡Velo del Guía!", new Color(0.8f, 0.7f, 1f), 1f);
                Fx.Ring(transform.position, 1.6f, new Color(0.8f, 0.7f, 1f), 0.4f);
                invulnUntil = Time.time + 0.5f;
                return 0;
            }
            if (blocking)
            {
                Vector2 from = hit.source != null ? hit.source.Pos : hit.origin;
                Vector2 toSrc = (from - Pos);
                if (toSrc.sqrMagnitude < 0.01f || Vector2.Angle(blockDir, toSrc) < 80f)
                {
                    dmg *= 0.2f;
                    if (weapon is ScytheWeapon sc) sc.OnBlocked();
                    Fx.HitSpark(transform.position + Vector3.up + blockDir.X0Z() * 0.6f, -toSrc.normalized, new Color(1f, 0.85f, 0.4f));
                    Sfx.Play(SfxId.HitArmor, transform.position);
                }
            }
            return dmg;
        }

        public override float ReceiveDamage(Hit hit, float amount)
        {
            if (inHub) { amount = 0; }
            float r = base.ReceiveDamage(hit, amount);
            if (dead) return r;
            invulnUntil = Mathf.Max(invulnUntil, Time.time + hurtCooldown);
            CameraRig.HurtPulse();
            var m = RunState.Current?.mods;
            if (m != null) foreach (var f in m.onPlayerHurt.ToArray()) f(amount);
            // Reactivo verde de West
            var run = RunState.Current;
            if (run != null && m != null && m.lowHpRescue > 0 && !run.rescueUsed && hp < maxHp * 0.3f && hp > 0)
            {
                run.rescueUsed = true;
                HealScaled(maxHp * m.lowHpRescue);
                UIFloat.Text(transform.position + Vector3.up * 2.4f, "¡Reactivo de West!", new Color(0.5f, 1f, 0.4f), 1.1f);
                Fx.Burst(transform.position + Vector3.up, new Color(0.4f, 1f, 0.3f), 20, 5f, 0.3f, 0.6f);
            }
            if (RunState.Current != null) Save.Data.Inc("damage_taken", Mathf.RoundToInt(amount));
            return r;
        }

        public override void Die(Hit killingBlow)
        {
            var run = RunState.Current;
            if (run != null && run.deathDefiance > 0 && !inHub)
            {
                run.deathDefiance--;
                hp = 1;
                StartCoroutine(DeathDefianceCo());
                return;
            }
            if (inHub) { hp = maxHp; return; }
            base.Die(killingBlow);
            Game.Instance.OnPlayerDeath(killingBlow);
        }

        IEnumerator DeathDefianceCo()
        {
            SetInvulnerable(2.2f);
            HitStop.Slow(0.2f, 1.0f);
            Sfx.Play(SfxId.Bell, transform.position);
            CameraRig.Shake(0.5f);
            UIFloat.Text(transform.position + Vector3.up * 2.6f, "ÚLTIMO UMBRAL", new Color(0.9f, 0.6f, 1f), 1.6f);
            Fx.Ring(transform.position, 5f, IridColor(), 0.8f);
            Fx.Burst(transform.position + Vector3.up, Color.white, 40, 9f, 0.4f, 0.9f, FxKind.Star);
            // los enemigos cercanos son repelidos
            foreach (var c in Combat.InCircle(Pos, 5f, Team.Player)) c.Knock(c.Pos - Pos, 14f);
            float target = maxHp * 0.5f;
            float t = 0;
            while (t < 0.8f)
            {
                t += Time.unscaledDeltaTime;
                hp = Mathf.Lerp(1, target, t / 0.8f);
                yield return null;
            }
            hp = target;
        }

        public void HealScaled(float amount)
        {
            var m = RunState.Current?.mods;
            float mult = m != null ? m.healMult : 1f;
            Heal(amount * mult);
        }

        // ---------------- salas ----------------
        public void OnRoomStart()
        {
            shieldReady = true;
            dashing = false; sprinting = false;
            weapon.Cancel();
            if (weapon is TrapezohedronWeapon tz) tz.ClearShards();
            cast.ResetAll();
            status.ClearAll();
            knockVel = Vector2.zero;
            lungeVel = Vector2.zero;
            velocity = Vector2.zero;
        }

        public void OnRoomCleared()
        {
            var m = RunState.Current?.mods;
            magick = MaxMagick;
            if (m != null && m.healOnRoomClear > 0) HealScaled(m.healOnRoomClear);
            if (m != null) foreach (var f in m.onRoomClear.ToArray()) f();
        }

        // ---------------- interacción ----------------
        void UpdateInteract()
        {
            nearest = null;
            float best = 999;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !it.Usable) continue;
                float d = Vector2.Distance(it.transform.position.XZ(), Pos);
                if (d < it.radius && d < best) { best = d; nearest = it; }
            }
            if (nearest != null && !inputLocked)
            {
                string txt = $"[{GameInput.Label(GameButton.Interact)}] {nearest.prompt}";
                if (nearest.onSecondary != null && !string.IsNullOrEmpty(nearest.secondaryPrompt))
                    txt += $"    [{GameInput.Label(GameButton.Gift)}] {nearest.secondaryPrompt}";
                UIFloat.ShowPrompt(nearest.transform.position + Vector3.up * nearest.promptHeight, txt);
                if (GameInput.Pressed(GameButton.Interact) && !UIRoot.MenuOpen) nearest.onUse?.Invoke();
                else if (GameInput.Pressed(GameButton.Gift) && nearest.onSecondary != null && !UIRoot.MenuOpen) nearest.onSecondary.Invoke();
            }
        }
    }
}
