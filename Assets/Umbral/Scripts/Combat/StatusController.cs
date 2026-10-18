using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Estados alterados. Cada dios primigenio tiene su "maldición", como los dioses olímpicos en Hades II:
    ///  · Marea (Cthulhu): el objetivo sufre más daño y es empujado más lejos.
    ///  · Locura (Hastur): el objetivo inflige menos daño y puede volverse contra sus aliados.
    ///  · Semilla (Shub-Niggurath): daño continuo; si muere, brota un Retoño Oscuro aliado.
    ///  · Sentencia (Nodens): el siguiente golpe que reciba invoca un rayo del Gran Abismo.
    ///  · Escarcha (Ithaqua): ralentiza; con 10 acumulaciones el objetivo queda Congelado.
    ///  · Llama Viva (Cthugha): daño por segundo por acumulación.
    /// Otros: Atado (Signo Arcano), Aturdido, Lento, Vulnerable, Traidor, Veneno.
    /// </summary>
    public enum StatusType { Marea, Locura, Semilla, Sentencia, Escarcha, Congelado, Llama, Atado, Aturdido, Lento, Vulnerable, Traidor, Veneno }

    public class StatusController : MonoBehaviour
    {
        public class State { public int stacks; public float until; public float power; public float tick; }

        public Combatant owner;
        readonly Dictionary<StatusType, State> states = new Dictionary<StatusType, State>();
        readonly List<StatusType> expired = new List<StatusType>();
        float fxTimer;

        public IEnumerable<StatusType> Active => states.Keys;
        public bool Has(StatusType t) => states.ContainsKey(t);
        public int Stacks(StatusType t) => states.TryGetValue(t, out var s) ? s.stacks : 0;
        public State Get(StatusType t) => states.TryGetValue(t, out var s) ? s : null;

        public static Color ColorOf(StatusType t)
        {
            switch (t)
            {
                case StatusType.Marea: return new Color(0.2f, 0.9f, 0.8f);
                case StatusType.Locura: return new Color(1f, 0.85f, 0.2f);
                case StatusType.Semilla: return new Color(0.45f, 0.9f, 0.3f);
                case StatusType.Sentencia: return new Color(0.75f, 0.65f, 1f);
                case StatusType.Escarcha: return new Color(0.7f, 0.9f, 1f);
                case StatusType.Congelado: return new Color(0.55f, 0.85f, 1f);
                case StatusType.Llama: return new Color(1f, 0.5f, 0.1f);
                case StatusType.Atado: return new Color(0.9f, 0.6f, 1f);
                case StatusType.Veneno: return new Color(0.5f, 0.85f, 0.2f);
                case StatusType.Traidor: return new Color(1f, 0.95f, 0.5f);
                default: return new Color(0.8f, 0.8f, 0.8f);
            }
        }

        public static string NameOf(StatusType t)
        {
            switch (t)
            {
                case StatusType.Marea: return "Marea";
                case StatusType.Locura: return "Locura";
                case StatusType.Semilla: return "Semilla";
                case StatusType.Sentencia: return "Sentencia";
                case StatusType.Escarcha: return "Escarcha";
                case StatusType.Congelado: return "Congelado";
                case StatusType.Llama: return "Llama Viva";
                case StatusType.Atado: return "Atado";
                case StatusType.Aturdido: return "Aturdido";
                case StatusType.Lento: return "Lento";
                case StatusType.Vulnerable: return "Vulnerable";
                case StatusType.Traidor: return "Traidor";
                case StatusType.Veneno: return "Veneno";
            }
            return t.ToString();
        }

        static PlayerModifiers M => RunState.Current?.mods ?? PlayerModifiers.Default;

        public void Apply(StatusType t, int stacks = 1, float duration = 4f, float power = 0f)
        {
            if (owner == null || !owner.Alive) return;
            if (owner.isBoss && (t == StatusType.Congelado || t == StatusType.Aturdido)) duration *= 0.35f;
            if (owner.isBoss && t == StatusType.Atado) duration *= 0.5f;
            bool isNew = !states.TryGetValue(t, out var s);
            if (isNew) { s = new State(); states[t] = s; }
            int maxStacks = t == StatusType.Llama ? M.llamaMaxStacks : t == StatusType.Escarcha ? 10 : 99;
            s.stacks = Mathf.Min(maxStacks, s.stacks + stacks);
            s.until = Mathf.Max(s.until, Time.time + duration);
            s.power = Mathf.Max(s.power, power);

            if (t == StatusType.Escarcha && s.stacks >= 10 && !Has(StatusType.Congelado))
            {
                states.Remove(StatusType.Escarcha);
                Apply(StatusType.Congelado, 1, M.freezeDuration);
                StatusEvents.RaiseFrozen(owner);
                Fx.Burst(owner.transform.position + Vector3.up, ColorOf(StatusType.Congelado), 20, 5f, 0.3f, 0.6f);
                Sfx.Play(SfxId.Freeze, owner.transform.position);
            }
            if (isNew) StatusEvents.RaiseApplied(owner, t);
            StatusEvents.RaiseStacks(owner, t, stacks);
            if (owner.rig != null && t == StatusType.Congelado) owner.rig.SetFrozen(true);
        }

        public void Remove(StatusType t)
        {
            if (states.Remove(t) && t == StatusType.Congelado && owner.rig != null) owner.rig.SetFrozen(false);
        }

        public void ClearAll()
        {
            states.Clear();
            if (owner != null && owner.rig != null) owner.rig.SetFrozen(false);
        }

        public float SpeedMult()
        {
            if (Has(StatusType.Congelado) || Has(StatusType.Aturdido) || Has(StatusType.Atado)) return 0f;
            float m = 1f;
            if (Has(StatusType.Escarcha)) m *= 1f - Mathf.Min(0.6f, Stacks(StatusType.Escarcha) * M.escarchaSlowPerStack);
            if (Has(StatusType.Lento)) m *= 0.55f;
            if (Has(StatusType.Marea) && M.mareaSlow > 0) m *= 1f - M.mareaSlow;
            return m;
        }

        /// <summary>Multiplicador de la velocidad de animación/ataques (la escarcha también entorpece).</summary>
        public float ActionSpeedMult()
        {
            float m = 1f;
            if (Has(StatusType.Escarcha)) m *= 1f - Mathf.Min(0.4f, Stacks(StatusType.Escarcha) * 0.03f);
            return m;
        }

        public bool CanAct => !Has(StatusType.Congelado) && !Has(StatusType.Aturdido);
        public bool CanMove => CanAct && !Has(StatusType.Atado);

        public float IncomingMult(Hit hit)
        {
            float m = 1f;
            if (Has(StatusType.Marea)) m += M.mareaVuln;
            if (Has(StatusType.Congelado)) m += M.frozenVuln;
            if (Has(StatusType.Vulnerable)) m += Get(StatusType.Vulnerable).power;
            if (Has(StatusType.Atado)) m += M.boundVuln;
            if (Has(StatusType.Locura) && M.locuraVuln > 0) m += M.locuraVuln;
            return m;
        }

        public float OutgoingMult()
        {
            float m = 1f;
            if (Has(StatusType.Locura)) m *= 1f - M.locuraWeak;
            return m;
        }

        /// <summary>Llamado por Combatant al recibir daño (antes de aplicar efectos del golpe).</summary>
        public void OnDamaged(Hit hit)
        {
            if (hit.tag == "sentencia") return;
            if (states.TryGetValue(StatusType.Sentencia, out var s))
            {
                states.Remove(StatusType.Sentencia);
                var src = PlayerController.Instance;
                StartCoroutine(Bolt(src, M.sentenciaDamage * (1f + s.power)));
            }
        }

        System.Collections.IEnumerator Bolt(Combatant src, float dmg)
        {
            yield return new WaitForSeconds(0.12f);
            if (owner == null || !owner.Alive) yield break;
            Fx.Lightning(owner.transform.position + Vector3.up * 9f, owner.transform.position, ColorOf(StatusType.Sentencia));
            Sfx.Play(SfxId.Thunder, owner.transform.position);
            var h = Hit.Proc(dmg, src, DamageType.Lightning, "sentencia");
            Combat.Deal(owner, h);
            StatusEvents.RaiseBolt(owner, src, dmg);
            // cadena de rayos (infusión de Éter de Nodens)
            int chains = M.sentenciaChains;
            var hitSet = new HashSet<Combatant> { owner };
            Vector2 from = owner.Pos;
            for (int i = 0; i < chains; i++)
            {
                var next = Combat.Nearest(from, Team.Player, 7f, null, hitSet);
                if (next == null) break;
                hitSet.Add(next);
                Fx.Lightning(from.X0Z(1f), next.transform.position + Vector3.up, ColorOf(StatusType.Sentencia));
                Combat.Deal(next, Hit.Proc(dmg * 0.6f, src, DamageType.Lightning, "sentencia"));
                from = next.Pos;
            }
        }

        void Update()
        {
            if (owner == null || states.Count == 0) return;
            float now = Time.time;
            expired.Clear();
            foreach (var kv in states)
            {
                var s = kv.Value;
                if (now >= s.until) { expired.Add(kv.Key); continue; }
                switch (kv.Key)
                {
                    case StatusType.Llama:
                        s.tick += Time.deltaTime;
                        if (s.tick >= 0.5f)
                        {
                            s.tick -= 0.5f;
                            float d = s.stacks * M.llamaDpsPerStack * 0.5f;
                            if (owner.status.Has(StatusType.Locura)) d *= M.llamaVsMadMult;
                            Combat.Deal(owner, Hit.Proc(d, PlayerController.Instance, DamageType.Fire, "llama"));
                        }
                        break;
                    case StatusType.Semilla:
                        s.tick += Time.deltaTime;
                        if (s.tick >= 1f)
                        {
                            s.tick -= 1f;
                            float d = M.semillaDps * (owner.status.Has(StatusType.Atado) ? M.semillaBoundMult : 1f);
                            Combat.Deal(owner, Hit.Proc(d, PlayerController.Instance, DamageType.Earth, "semilla"));
                        }
                        break;
                    case StatusType.Veneno:
                        s.tick += Time.deltaTime;
                        if (s.tick >= 1f)
                        {
                            s.tick -= 1f;
                            var h = Hit.Proc(Mathf.Max(1, s.power), null, DamageType.Earth, "veneno");
                            Combat.Deal(owner, h);
                        }
                        break;
                }
                if (!owner.Alive) return;
            }
            foreach (var t in expired) Remove(t);

            // partículas indicadoras
            fxTimer -= Time.deltaTime;
            if (fxTimer <= 0 && states.Count > 0)
            {
                fxTimer = 0.12f;
                foreach (var kv in states)
                {
                    if (kv.Key == StatusType.Atado || kv.Key == StatusType.Vulnerable) continue;
                    Vector3 p = owner.transform.position + new Vector3(Random.Range(-0.4f, 0.4f) * owner.radius * 2f, Random.Range(0.3f, owner.height), Random.Range(-0.4f, 0.4f) * owner.radius * 2f);
                    Vector3 vel = kv.Key == StatusType.Llama ? Vector3.up * 2.5f : kv.Key == StatusType.Marea ? Vector3.down * 1.2f : Vector3.up * 0.8f;
                    Fx.Emit(FxKind.Glow, p, vel, ColorOf(kv.Key), 0.22f, 0.5f);
                }
            }
        }
    }

    /// <summary>Eventos de estados para que los boones reaccionen (ganancias de Lucidez, legendarios, dúos).</summary>
    public static class StatusEvents
    {
        public static event System.Action<Combatant, StatusType> Applied;
        public static event System.Action<Combatant, StatusType, int> StacksAdded;
        public static event System.Action<Combatant> Frozen;
        public static event System.Action<Combatant, Combatant, float> BoltStruck;
        public static void RaiseApplied(Combatant c, StatusType t) => Applied?.Invoke(c, t);
        public static void RaiseStacks(Combatant c, StatusType t, int n) => StacksAdded?.Invoke(c, t, n);
        public static void RaiseFrozen(Combatant c) => Frozen?.Invoke(c);
        public static void RaiseBolt(Combatant target, Combatant src, float dmg) => BoltStruck?.Invoke(target, src, dmg);
        public static void Clear() { Applied = null; StacksAdded = null; Frozen = null; BoltStruck = null; }
    }
}
