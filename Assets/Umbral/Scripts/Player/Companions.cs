using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Cualquier cosa con la que se puede interactuar: puertas, recompensas, personajes, estaciones del santuario.</summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public string prompt = "Interactuar";
        public string secondaryPrompt;
        public float radius = 1.8f;
        public float promptHeight = 2.4f;
        public Action onUse, onSecondary;
        public Func<bool> usable;
        public bool Usable => usable == null || usable();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Interactable Add(GameObject go, string prompt, Action onUse, float radius = 1.8f, float height = 2.4f)
        {
            var i = go.AddComponent<Interactable>();
            i.prompt = prompt; i.onUse = onUse; i.radius = radius; i.promptHeight = height;
            return i;
        }
    }

    /// <summary>Retoño Oscuro de Shub-Niggurath: aliado temporal que brota de los enemigos con Semilla.</summary>
    public class Ally : Combatant
    {
        float until, nextAttack;
        static PlayerModifiers M => RunState.Current?.mods ?? PlayerModifiers.Default;
        Combatant target;

        public static Ally SpawnRetono(Vector2 at)
        {
            if (World.Root == null) return null;
            var go = new GameObject("RetonoOscuro");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var a = go.AddComponent<Ally>();
            a.team = Team.Player; a.isAlly = true; a.targetable = false; a.radius = 0.4f; a.height = 1.4f;
            a.displayName = "Retoño Oscuro";
            a.SetMaxHp(1);
            a.until = Time.time + M.retonoDuration;
            a.rig = ArtIA.SpriteRig(go.transform, "retono", 1.4f, 0.4f) ?? CharacterArt.DarkYoung(go.transform, 0.55f);
            a.rig.Init();
            Fx.Burst(go.transform.position + Vector3.up * 0.5f, new Color(0.4f, 0.85f, 0.25f), 16, 5f, 0.3f, 0.6f);
            Sfx.Play(SfxId.Growl, go.transform.position, 0.6f, 1.4f);
            foreach (var f in M.onAllySpawn.ToArray()) f(a);
            return a;
        }

        protected override void Update()
        {
            base.Update();
            if (dead) return;
            if (Time.time >= until) { Expire(); return; }
            if (target == null || !target.Alive) target = Combat.Nearest(Pos, Team.Player, 14f);
            Vector2 vel = Vector2.zero;
            if (target != null)
            {
                Vector2 d = target.Pos - Pos;
                if (d.magnitude > target.radius + 0.9f) vel = d.normalized * 6f;
                else if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 0.8f;
                    var h = Hit.Proc(M.retonoDamage, this, DamageType.Earth, "retono");
                    h.dir = d.normalized; h.knockback = 2f;
                    rig.Kick(0.3f);
                    Fx.Slash(transform.position, d.normalized, 1.4f, 120, new Color(0.45f, 0.9f, 0.3f), 0.15f, 0.6f);
                    Combat.Deal(target, h);
                    foreach (var f in M.onAllyHit.ToArray()) f(target, h);
                }
                facing = d.normalized;
                transform.rotation = U.LookFlat(facing);
            }
            MoveBy(vel * Time.deltaTime);
            rig.velocity = vel;
        }

        void Expire()
        {
            dead = true;
            if (M.Flag("retono_explota"))
            {
                Fx.Ring(transform.position, 2.5f, new Color(0.45f, 0.9f, 0.3f), 0.35f);
                Fx.Burst(transform.position + Vector3.up * 0.5f, new Color(0.45f, 0.9f, 0.3f), 20, 7f, 0.3f, 0.5f);
                Combat.Explode(Pos, 2.5f, Hit.Proc(80, PlayerController.Instance, DamageType.Earth, "retono_boom"), Team.Player, 6f);
            }
            else Fx.Smoke(transform.position + Vector3.up * 0.5f, new Color(0.1f, 0.15f, 0.05f, 0.8f), 8, 0.8f);
            Destroy(gameObject);
        }
    }

    /// <summary>Familiares (como los de Hades II): Ébano el gato de Ulthar y Pipo el zoog.</summary>
    public class Familiar : MonoBehaviour
    {
        public string id;
        PlayerController P;
        ModelRig rig;
        Vector2 pos;
        float nextAction;
        Vector2 vel;

        public static Familiar Spawn(PlayerController p, string id)
        {
            var go = new GameObject("Familiar_" + id);
            go.transform.SetParent(p.transform.parent, false);
            var f = go.AddComponent<Familiar>();
            f.id = id; f.P = p;
            f.pos = p.Pos + new Vector2(-1, -1);
            go.transform.position = f.pos.X0Z();
            f.rig = ArtIA.SpriteRig(go.transform, id, id == "fam_gato" ? 0.7f : 0.9f, 0.3f) ?? (id == "fam_gato" ? CharacterArt.Cat(go.transform) : CharacterArt.Zoog(go.transform, 0.6f, true));
            f.rig.Init();
            return f;
        }

        void Update()
        {
            if (P == null) { Destroy(gameObject); return; }
            Vector2 behind = P.Pos - P.facing * 1.4f + new Vector2(P.facing.y, -P.facing.x) * 0.9f;
            Vector2 goal = behind;
            if (id == "fam_gato" && RoomDirector.InCombat && Time.time >= nextAction)
            {
                var t = Combat.Nearest(P.Pos, Team.Player, 7f);
                if (t != null)
                {
                    nextAction = Time.time + 2.2f;
                    StartCoroutine(Pounce(t));
                }
            }
            Vector2 d = goal - pos;
            vel = Vector2.Lerp(vel, d.magnitude > 0.5f ? d.normalized * Mathf.Min(9f, d.magnitude * 3f) : Vector2.zero, Time.deltaTime * 8f);
            if (!pouncing) pos += vel * Time.deltaTime;
            if (Arena.Current != null) pos = Arena.Current.Resolve(pos, 0.25f);
            transform.position = pos.X0Z();
            if (vel.sqrMagnitude > 0.1f) transform.rotation = U.LookFlat(vel);
            rig.velocity = vel;
        }

        bool pouncing;
        System.Collections.IEnumerator Pounce(Combatant t)
        {
            pouncing = true;
            Vector2 from = pos;
            float k = 0;
            while (k < 1 && t != null)
            {
                k += Time.deltaTime / 0.25f;
                pos = Vector2.Lerp(from, t.Pos, k);
                transform.position = pos.X0Z(Mathf.Sin(k * Mathf.PI) * 1f);
                yield return null;
            }
            if (t != null && t.Alive)
            {
                Combat.Deal(t, Hit.Proc(18, P, DamageType.Physical, "gato"));
                Fx.Slash(t.transform.position, (t.Pos - from).normalized, 1.2f, 120, new Color(0.9f, 0.9f, 1f), 0.12f, 0.6f);
                Sfx.Play(SfxId.Swing, t.transform.position, 0.5f, 1.8f);
            }
            pouncing = false;
        }

        /// <summary>Pipo encuentra oro al limpiar una sala.</summary>
        public void OnRoomCleared()
        {
            if (id != "fam_zoog" || RunState.Current == null) return;
            int g = UnityEngine.Random.Range(5, 16);
            RunState.Current.AddGold(g);
            UIFloat.Text(transform.position + Vector3.up * 1.4f, $"¡Pipo encontró {g} de oro!", Res.ColorOf("oro"), 0.9f);
        }
    }
}
