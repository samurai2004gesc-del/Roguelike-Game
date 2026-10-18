using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Ecos del Padre: poderes de Yog-Sothoth (equivalente a los Hechizos de Selene / Senda de las Estrellas).
    /// Solo puedes llevar uno; se carga gastando Lucidez y se activa con [R]. Elegir el mismo Eco otra vez lo mejora.
    /// </summary>
    public class HexDef
    {
        public string id, name, desc;
        public float cost;
        public Color color;
        public Action<PlayerController, int> activate;
        public float CostAt(int level) => cost * Mathf.Pow(0.85f, Mathf.Max(0, level - 1));
        public static float Power(int level) => 1f + 0.25f * Mathf.Max(0, level - 1);
    }

    public static class Hexes
    {
        public static readonly List<HexDef> All = new List<HexDef>();
        public static HexDef Get(string id) => All.FirstOrDefault(h => h.id == id);

        static Hexes()
        {
            All.Add(new HexDef
            {
                id = "hex_mirada", name = "Mirada Coterminal", cost = 120, color = new Color(0.85f, 0.7f, 1f),
                desc = "Tu Padre abre todos sus ojos: el tiempo se detiene para tus enemigos durante 3,5 s.",
                activate = (p, l) =>
                {
                    float dur = 3.5f * HexDef.Power(l);
                    foreach (var c in Combatant.Enemies(Team.Player).ToList()) { c.status.Apply(StatusType.Aturdido, 1, dur); c.status.Apply(StatusType.Vulnerable, 1, dur, 0.1f * l); }
                    foreach (var pr in Projectile.Active.ToArray()) if (pr.team == Team.Enemy) pr.End();
                    p.StartCoroutine(Hexes.ScreenEyes(dur));
                }
            });
            All.Add(new HexDef
            {
                id = "hex_esferas", name = "Las Esferas Iridiscentes", cost = 100, color = new Color(0.5f, 0.95f, 1f),
                desc = "Invocas 5 esferas que orbitan a tu alrededor 9 s e infligen 25 de daño al tocar.",
                activate = (p, l) => p.StartCoroutine(Hexes.Globes(p, 9f, 25f * HexDef.Power(l)))
            });
            All.Add(new HexDef
            {
                id = "hex_puerta", name = "La Puerta Abierta", cost = 140, color = new Color(0.7f, 0.45f, 1f),
                desc = "Abres un vórtice hacia el Más Allá que atrae y despedaza a los enemigos durante 3 s.",
                activate = (p, l) => p.StartCoroutine(Hexes.Vortex(p.ClampedAimPoint(8f), 3f, 20f * HexDef.Power(l)))
            });
            All.Add(new HexDef
            {
                id = "hex_forma", name = "Forma Verdadera", cost = 160, color = new Color(1f, 0.6f, 0.9f),
                desc = "Revelas tu herencia: 5 s invulnerable y +40% de daño.",
                activate = (p, l) => p.StartCoroutine(Hexes.TrueForm(p, 5f * HexDef.Power(l)))
            });
            All.Add(new HexDef
            {
                id = "hex_llave", name = "La Llave y la Puerta", cost = 110, color = new Color(0.9f, 0.9f, 1f),
                desc = "Te desplazas a través del espacio hasta el punto señalado y estallas en 150 de daño.",
                activate = (p, l) =>
                {
                    Vector2 to = p.ClampedAimPoint(9f);
                    Fx.Beam(p.Pos.X0Z(1f), to.X0Z(1f), Color.white, 0.6f, 0.3f);
                    p.Teleport(to);
                    p.SetInvulnerable(0.6f);
                    BoonFxHex.Blast(to, 4f, 150f * HexDef.Power(l), new Color(0.9f, 0.9f, 1f));
                }
            });
        }

        public static IEnumerator ScreenEyes(float dur)
        {
            HitStop.Slow(0.35f, 0.4f);
            Sfx.Play(SfxId.Bell);
            CameraRig.Shake(0.3f);
            var p = PlayerController.Instance;
            if (p != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 at = p.Pos + UnityEngine.Random.insideUnitCircle * 9f;
                    Kit.Glow(World.Root, PlayerController.IridColor(), 1.2f, at.X0Z(0.5f), 2f).gameObject.AddComponent<AutoFade>().life = dur;
                }
            }
            yield return null;
        }

        public static IEnumerator Globes(PlayerController p, float dur, float dmg)
        {
            var globes = new List<Transform>();
            for (int i = 0; i < 5; i++) globes.Add(Kit.Glow(World.Root, PlayerController.IridColor(), 0.6f, p.transform.position, 3f));
            var lastHit = new Dictionary<Combatant, float>();
            float t = 0;
            while (t < dur && p != null && World.Root != null)
            {
                t += Time.deltaTime;
                for (int i = 0; i < globes.Count; i++)
                {
                    if (globes[i] == null) continue;
                    float a = (t * 160f + i * 72f) * Mathf.Deg2Rad;
                    float r = 2.2f + Mathf.Sin(t * 2f) * 0.4f;
                    Vector2 pos = p.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    globes[i].position = pos.X0Z(1.1f);
                    foreach (var c in Combat.InCircle(pos, 0.6f, Team.Player))
                    {
                        if (lastHit.TryGetValue(c, out var lt) && Time.time - lt < 0.4f) continue;
                        lastHit[c] = Time.time;
                        Combat.Deal(c, Hit.Proc(dmg, p, DamageType.Void, "esfera"));
                    }
                }
                yield return null;
            }
            foreach (var g in globes) if (g != null) UnityEngine.Object.Destroy(g.gameObject);
        }

        public static IEnumerator Vortex(Vector2 at, float dur, float dmg)
        {
            var mat = Mats.NewUnlit(new Color(0.7f, 0.45f, 1f, 0.9f), Mats.Blend.Additive, Tex.Swirl, 3f, false);
            var go = Fx.MeshObj("Vortice", MeshGen.Quad, mat, at.X0Z(0.1f), Quaternion.Euler(90, 0, 0), Vector3.one * 8f);
            var l = Fx.Light(at.X0Z(1.5f), new Color(0.7f, 0.45f, 1f), 12f, 6f, dur);
            Sfx.Play(SfxId.Portal, at.X0Z());
            float t = 0, tick = 0;
            while (t < dur && go != null)
            {
                t += Time.deltaTime; tick += Time.deltaTime;
                go.transform.Rotate(0, 0, 400f * Time.deltaTime, Space.Self);
                foreach (var c in Combat.InCircle(at, 6f, Team.Player))
                    if (!c.isBoss) c.MoveBy((at - c.Pos).SafeNormal(Vector2.zero) * 5f * Time.deltaTime);
                if (tick >= 0.25f)
                {
                    tick = 0;
                    foreach (var c in Combat.InCircle(at, 2.5f, Team.Player)) Combat.Deal(c, Hit.Proc(dmg, PlayerController.Instance, DamageType.Void, "vortice"));
                }
                yield return null;
            }
            if (go != null) UnityEngine.Object.Destroy(go);
            UnityEngine.Object.Destroy(mat);
        }

        public static IEnumerator TrueForm(PlayerController p, float dur)
        {
            p.SetInvulnerable(dur);
            var m = RunState.Current?.mods;
            if (m != null) m.globalDamageMult *= 1.4f;
            var orig = p.rig.transform.localScale;
            p.rig.transform.localScale = orig * 1.25f;
            float t = 0;
            while (t < dur && p != null)
            {
                t += Time.deltaTime;
                if (UnityEngine.Random.value < 0.6f) Fx.Emit(FxKind.Star, p.transform.position + UnityEngine.Random.insideUnitSphere * 1.2f + Vector3.up, Vector3.up, PlayerController.IridColor(), 0.4f, 0.5f);
                yield return null;
            }
            if (p != null) p.rig.transform.localScale = orig;
            if (m != null && RunState.Current != null && RunState.Current.mods == m) m.globalDamageMult /= 1.4f;
        }
    }

    public static class BoonFxHex
    {
        public static void Blast(Vector2 at, float r, float dmg, Color c)
        {
            Fx.Ring(at.X0Z(), r, c, 0.5f);
            Fx.GroundFlash(at.X0Z(), r, c, 0.4f);
            Fx.Burst(at.X0Z(1f), c, 40, 10f, 0.4f, 0.7f, FxKind.Star);
            Fx.Light(at.X0Z(1.5f), c, 12, 8, 0.5f);
            Sfx.Play(SfxId.Explosion, at.X0Z());
            CameraRig.Shake(0.5f);
            Combat.Explode(at, r, Hit.Proc(dmg, PlayerController.Instance, DamageType.Void, "hex"), Team.Player, 10f);
        }
    }

    public class AutoFade : MonoBehaviour
    {
        public float life = 1f;
        float t; Vector3 s0;
        void Start() { s0 = transform.localScale; }
        void Update()
        {
            t += Time.deltaTime;
            transform.localScale = s0 * Mathf.Clamp01(1f - t / life);
            if (t >= life) Destroy(gameObject);
        }
    }

    public class HexController : MonoBehaviour
    {
        public PlayerController P;
        public HexDef Def => RunState.Current != null ? Hexes.Get(RunState.Current.hexId) : null;
        public int Level => RunState.Current?.hexLevel ?? 0;
        public float Cost => Def != null ? Def.CostAt(Level) : 1f;
        public float Charge => RunState.Current?.hexCharge ?? 0;
        public bool Ready => Def != null && Charge >= Cost;

        public void AddCharge(float magickSpent)
        {
            var r = RunState.Current;
            if (r == null || Def == null) return;
            bool wasReady = Ready;
            r.hexCharge = Mathf.Min(Cost, r.hexCharge + magickSpent * r.mods.hexChargeMult);
            if (!wasReady && Ready)
            {
                Sfx.Play(SfxId.ChargeReady);
                UIFloat.Text(P.transform.position + Vector3.up * 2.6f, "¡Eco listo! [" + GameInput.Label(GameButton.Hex) + "]", Def.color, 1f);
            }
        }

        public void TryActivate()
        {
            if (!Ready) { if (Def != null) Sfx.Play(SfxId.UiBack, null, 0.5f); return; }
            RunState.Current.hexCharge = 0;
            Fx.Burst(P.transform.position + Vector3.up, Def.color, 30, 8f, 0.35f, 0.6f, FxKind.Star);
            UIFloat.Text(P.transform.position + Vector3.up * 2.6f, Def.name, Def.color, 1.2f);
            Def.activate(P, Level);
        }
    }
}
