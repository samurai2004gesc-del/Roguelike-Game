using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>Estado de una incursión (se pierde al morir, salvo los recursos permanentes que ya se guardaron).</summary>
    public class RunState
    {
        public static RunState Current;

        public int seed;
        public System.Random rng;
        public string weaponId;
        public int aspect, aspectLevel;

        public int biome;          // 0..3
        public int room;           // sala dentro de la región
        public int depth;          // salas totales
        public int roomsCleared;

        public readonly List<BoonInstance> boons = new List<BoonInstance>();
        public readonly List<BoonInstance> chaosCurses = new List<BoonInstance>();
        public readonly HashSet<string> hammers = new HashSet<string>();
        public string hexId;
        public int hexLevel;
        public float hexCharge;

        public int gold;
        public int deathDefiance, maxDeathDefiance;
        public float heartsHp;     // vida extra de Corazones de Gug
        public int rerolls;
        public bool pendingStartBoon;
        public bool rescueUsed;
        public bool wilburMet;
        public int fear;

        public PlayerModifiers mods = new PlayerModifiers();

        // modificadores de los pactos del Juramento
        public float vowEnemyHpMult = 1f, vowEnemyDamageMult = 1f, vowEnemyCountMult = 1f, vowArmorChance, vowShopMult = 1f, vowHealMult = 1f, vowMagickMult = 1f, vowBossHpMult = 1f, vowBossSpeed = 1f;
        public int vowChoicePenalty;
        public bool vowTimer;
        public float chaosEnemyDamageMult = 1f;

        // estadísticas
        public float time, biomeTime;
        public int kills;
        public readonly Dictionary<string, int> gained = new Dictionary<string, int>();
        public readonly List<string> godsSeen = new List<string>();
        public string lastGodOffered;

        public float Value(string boonId) { var b = boons.FirstOrDefault(x => x.def.id == boonId); return b != null ? b.Value : 0; }
        public bool Has(string boonId) => boons.Any(b => b.def.id == boonId);
        public bool HasGod(string god) => boons.Any(b => b.def.god == god || (b.def.requiresGods != null && b.def.requiresGods.Contains(god)));
        public BoonInstance InSlot(MoveSlot s) => boons.FirstOrDefault(b => b.def.kind == BoonKind.Core && b.def.slot == s);

        public int ElementCount(Element e)
        {
            int n = 0;
            foreach (var b in boons) if (b.def.element == e && b.def.kind != BoonKind.Infusion) n++;
            return n;
        }

        public static RunState Begin(string weaponId)
        {
            var s = Save.Data;
            var r = new RunState();
            r.seed = Random.Range(1, int.MaxValue);
            r.rng = new System.Random(r.seed);
            r.weaponId = weaponId;
            r.aspect = s.ChosenAspect(weaponId);
            r.aspectLevel = Mathf.Max(1, s.AspectLevel(weaponId + ":" + r.aspect));
            r.rerolls = s.HasRitual("r_destino") ? 2 : 0;

            // Juramento
            r.vowEnemyHpMult = 1f + MetaDB.VowValue("v_carne") / 100f;
            r.vowEnemyDamageMult = 1f + MetaDB.VowValue("v_fauces") / 100f;
            r.vowEnemyCountMult = 1f + MetaDB.VowValue("v_horda") / 100f;
            r.vowArmorChance = MetaDB.VowValue("v_armadura") / 100f;
            r.vowShopMult = 1f + MetaDB.VowValue("v_escasez") / 100f;
            r.vowHealMult = 1f - MetaDB.VowValue("v_sinrespiro") / 100f;
            r.vowMagickMult = 1f - MetaDB.VowValue("v_mente") / 100f;
            r.vowChoicePenalty = Mathf.RoundToInt(MetaDB.VowValue("v_eleccion"));
            r.vowBossHpMult = 1f + MetaDB.VowValue("v_jefes") / 100f;
            r.vowBossSpeed = 1f + MetaDB.VowValue("v_jefes") / 200f;
            r.vowTimer = MetaDB.VowValue("v_tiempo") > 0;
            r.fear = MetaDB.TotalFear();

            Current = r;
            foreach (var id in s.activeCards) MetaDB.Card(id)?.onRunStart?.Invoke(r);
            BoonManager.Recompute();
            r.deathDefiance = r.maxDeathDefiance;
            s.Inc("runs");
            s.Inc("run_" + weaponId);
            return r;
        }

        public void AddGold(int n)
        {
            int amt = Mathf.RoundToInt(n * mods.goldMult);
            gold += amt;
            Sfx.Play(SfxId.Gold);
        }

        /// <summary>Recurso permanente: se guarda al instante (como en Hades, no se pierde al morir).</summary>
        public void AddResource(string id, int n)
        {
            float mult = mods.resourceMult * (1f + fear * 0.03f) * (Save.Data.HasRitual("r_abundancia") ? 1.25f : 1f);
            int amt = Mathf.Max(1, Mathf.RoundToInt(n * mult));
            Save.Data.AddRes(id, amt);
            gained[id] = (gained.TryGetValue(id, out var g) ? g : 0) + amt;
            Save.Write();
        }
    }
}
