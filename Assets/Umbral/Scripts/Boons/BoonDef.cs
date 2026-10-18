using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    public enum BoonKind { Core, Passive, Infusion, Duo, Legendary, Cat, Chaos }
    public enum Rarity { Common, Rare, Epic, Heroic, Duo, Legendary }

    /// <summary>
    /// Definición de un boon (favor divino). Los valores van por rareza [Común, Raro, Épico, Heroico];
    /// los Frutos Oscuros (equivalente a las Granadas) suben de nivel el boon.
    /// </summary>
    public class BoonDef
    {
        public string id, god, name, desc;
        public MoveSlot slot = MoveSlot.None;
        public BoonKind kind = BoonKind.Passive;
        public float[] values = { 1, 1, 1, 1 };
        public float levelGain = 0.3f;              // +30% del valor base por nivel extra
        public string unit = "";                     // "%" o "" (se añade tras el valor)
        public Element element = Element.None;
        public int infusionCost;                     // para infusiones: cuántos boones del elemento hacen falta
        public Element infusionElement;
        public string[] requiresAny;                 // ids de boones (al menos uno)
        public string[] requiresGods;                // dúos: un boon de cada dios
        public StatusType? requiresCurse;            // requiere que alguna ranura aplique esta maldición
        public Action<PlayerModifiers, float, BoonInstance> apply;
        public bool levelable = true;
        public float weight = 1f;

        public float ValueFor(Rarity r, int level)
        {
            int idx = Mathf.Clamp((int)r, 0, values.Length - 1);
            if (r == Rarity.Duo || r == Rarity.Legendary) idx = 0;
            float baseV = values[idx];
            return baseV * (1f + (level - 1) * levelGain);
        }

        public string Describe(Rarity r, int level)
        {
            float v = ValueFor(r, level);
            string vs = (Mathf.Abs(v - Mathf.Round(v)) < 0.05f ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#")) + unit;
            return desc.Replace("{v}", "<color=#FFE08A>" + vs + "</color>");
        }

        public bool IsCore => kind == BoonKind.Core;
        public GodDef God => Gods.Get(god);
    }

    public class BoonInstance
    {
        public BoonDef def;
        public Rarity rarity;
        public int level = 1;
        public int chaosRoomsLeft;    // maldiciones de Azathoth
        public float Value => def.ValueFor(rarity, level);
        public BoonInstance(BoonDef d, Rarity r) { def = d; rarity = r; }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return new Color(0.35f, 0.65f, 1f);
                case Rarity.Epic: return new Color(0.75f, 0.4f, 1f);
                case Rarity.Heroic: return new Color(1f, 0.35f, 0.3f);
                case Rarity.Duo: return new Color(0.75f, 1f, 0.4f);
                case Rarity.Legendary: return new Color(1f, 0.6f, 0.15f);
            }
            return new Color(0.88f, 0.86f, 0.82f);
        }

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return "Raro";
                case Rarity.Epic: return "Épico";
                case Rarity.Heroic: return "Heroico";
                case Rarity.Duo: return "Dúo";
                case Rarity.Legendary: return "Legendario";
            }
            return "Común";
        }

        public static string SlotName(MoveSlot s)
        {
            switch (s)
            {
                case MoveSlot.Attack: return "Ataque";
                case MoveSlot.Special: return "Especial";
                case MoveSlot.Cast: return "Signo";
                case MoveSlot.Sprint: return "Esprint";
                case MoveSlot.Magick: return "Lucidez";
            }
            return "";
        }
    }

    /// <summary>Efectos reutilizables que invocan los boones.</summary>
    public static class BoonFx
    {
        static PlayerController P => PlayerController.Instance;
        static PlayerModifiers M => RunState.Current?.mods ?? PlayerModifiers.Default;

        /// <summary>Aplica la maldición de un dios a un objetivo.</summary>
        public static void Curse(string god, Combatant t, Hit h, int stacks = 1)
        {
            if (t == null || !t.Alive) return;
            switch (god)
            {
                case "cthulhu":
                    t.status.Apply(StatusType.Marea, 1, 4f);
                    t.Knock(h.dir.sqrMagnitude > 0.01f ? h.dir : (t.Pos - (P != null ? P.Pos : t.Pos)), 6f * M.mareaKnockMult);
                    if (M.Flag("duo_mar_helado")) t.status.Apply(StatusType.Escarcha, 2, 5f);
                    break;
                case "hastur":
                    t.status.Apply(StatusType.Locura, 1, 4f);
                    if (M.locuraTurncoat > 0 && UnityEngine.Random.value < M.locuraTurncoat) t.status.Apply(StatusType.Traidor, 1, 3f);
                    if (M.Flag("duo_sueno_carcosa")) t.status.Apply(StatusType.Marea, 1, 4f);
                    break;
                case "shub":
                    t.status.Apply(StatusType.Semilla, 1, 6f);
                    break;
                case "nodens":
                    t.status.Apply(StatusType.Sentencia, 1, 8f);
                    break;
                case "ithaqua":
                    t.status.Apply(StatusType.Escarcha, 2 * stacks, 5f);
                    break;
                case "cthugha":
                    t.status.Apply(StatusType.Llama, 2 * stacks, 6f);
                    break;
            }
        }

        public static DamageType TypeOf(string god)
        {
            switch (god)
            {
                case "cthulhu": return DamageType.Water;
                case "hastur": return DamageType.Madness;
                case "shub": return DamageType.Earth;
                case "nodens": return DamageType.Lightning;
                case "ithaqua": return DamageType.Frost;
                case "cthugha": return DamageType.Fire;
            }
            return DamageType.Physical;
        }

        /// <summary>Onda expansiva de daño de un dios (con su maldición).</summary>
        public static void Wave(string god, Vector2 center, float radius, float damage, float knock = 0f, bool curse = true)
        {
            var g = Gods.Get(god);
            Color c = g != null ? g.color : Color.white;
            Fx.Ring(center.X0Z(), radius, c, 0.35f);
            Fx.GroundFlash(center.X0Z(), radius * 0.8f, c, 0.25f);
            Fx.Burst(center.X0Z(0.5f), c, 14, 6f, 0.3f, 0.5f);
            Sfx.Play(god == "cthugha" ? SfxId.Fire : god == "cthulhu" ? SfxId.Water : SfxId.Magic, center.X0Z());
            foreach (var t in Combat.InCircle(center, radius, Team.Player))
            {
                var h = Hit.Proc(damage, P, TypeOf(god), "wave_" + god);
                h.dir = (t.Pos - center).SafeNormal(Vector2.up);
                h.knockback = knock;
                Combat.Deal(t, h);
                if (curse) Curse(god, t, h);
            }
        }

        public static void Restore(float magick, Vector3? at = null)
        {
            if (P == null || magick <= 0) return;
            P.GainMagick(magick);
        }

        /// <summary>Tentáculo/púa/llama que brota del suelo bajo un objetivo tras un breve aviso.</summary>
        public static void Eruption(string god, Vector2 at, float radius, float damage, float delay = 0.45f)
        {
            var g = Gods.Get(god);
            Color c = g != null ? g.color : Color.white;
            var tele = Telegraph.Circle(at, radius, delay, c);
            Runner.Delay(delay, () =>
            {
                if (World.Root == null) return;
                Fx.GroundFlash(at.X0Z(), radius, c, 0.35f);
                Fx.Burst(at.X0Z(0.3f), c, 20, 9f, 0.35f, 0.6f, FxKind.Spark);
                Props.SpawnEruptionVisual(god, at, radius);
                CameraRig.Shake(0.2f);
                foreach (var t in Combat.InCircle(at, radius, Team.Player))
                {
                    var h = Hit.Proc(damage, P, TypeOf(god), "erupt_" + god);
                    h.dir = (t.Pos - at).SafeNormal(Vector2.up);
                    Combat.Deal(t, h);
                    Curse(god, t, h);
                }
            });
        }

        public static Combatant RandomEnemy()
        {
            var list = new List<Combatant>(Combatant.Enemies(Team.Player));
            list.RemoveAll(c => c.isDummy);
            return list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : null;
        }
    }
}
