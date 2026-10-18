using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>Una opción en una pantalla de elección (boon, mejora de nivel, martillo, pacto de Azathoth).</summary>
    public class Offer
    {
        public BoonDef def;
        public Rarity rarity;
        public BoonInstance replaces;   // boon de ranura que se sustituye
        public BoonInstance upgrade;    // Fruto Oscuro: boon que sube de nivel
        public int levels = 1;
        public HammerDef hammer;
        public BoonDef curse;           // Azathoth: maldición asociada
        public int curseRooms;
        public int price;               // tienda
    }

    /// <summary>
    /// Gestor de boones: reconstruye modificadores, genera ofertas (con rarezas, dúos, legendarios, infusiones),
    /// y conecta los eventos del combate con los ganchos de los boones.
    /// </summary>
    public static class BoonManager
    {
        static bool hooked;
        static RunState R => RunState.Current;

        public static void HookEvents()
        {
            if (hooked) return;
            hooked = true;
            Combat.OnDeath += OnDeath;
            StatusEvents.Applied += (t, s) => { if (R != null) foreach (var f in R.mods.onStatusApplied.ToArray()) f(t, s); };
            StatusEvents.StacksAdded += (t, s, n) => { if (R != null) foreach (var f in R.mods.onStacks.ToArray()) f(t, s, n); };
            StatusEvents.Frozen += t => { if (R != null) foreach (var f in R.mods.onFrozen.ToArray()) f(t); };
            StatusEvents.BoltStruck += (t, src, d) => { if (R != null) foreach (var f in R.mods.onBolt.ToArray()) f(t, d); };
        }

        static void OnDeath(Combatant c, Hit h)
        {
            if (R == null || c == null) return;
            if (c.team == Team.Enemy && !c.isDummy)
            {
                R.kills++;
                Save.Data.Inc("kills");
                if (!string.IsNullOrEmpty(c.codexId)) Save.Data.Inc("kill_" + c.codexId);
                var p = PlayerController.Instance;
                if (p != null && R.mods.healOnKill > 0) p.HealScaled(R.mods.healOnKill);
                foreach (var f in R.mods.onKill.ToArray()) f(c, h);

                // Semilla de Shub-Niggurath: brota un Retoño Oscuro aliado
                bool seed = c.status.Has(StatusType.Semilla) || (R.mods.Flag("retono_locura") && c.status.Has(StatusType.Locura));
                if (seed && !c.isBoss) Ally.SpawnRetono(c.Pos);
            }
        }

        // ---------------------------------------------------------------- recomputar

        public static void Recompute()
        {
            if (R == null) return;
            var m = new PlayerModifiers();
            var s = Save.Data;
            foreach (var id in s.activeCards) MetaDB.Card(id)?.apply?.Invoke(m);
            if (s.HasRitual("r_reliquias") && !string.IsNullOrEmpty(s.equippedRelic)) MetaDB.Relic(s.equippedRelic)?.apply?.Invoke(m);
            if (!string.IsNullOrEmpty(s.equippedFamiliar)) MetaDB.Familiar(s.equippedFamiliar)?.apply?.Invoke(m);
            var w = Weapons.Get(R.weaponId);
            if (w != null)
            {
                var asp = w.aspects[Mathf.Clamp(R.aspect, 0, w.aspects.Count - 1)];
                asp.apply?.Invoke(m, R.aspectLevel);
                foreach (var hid in R.hammers) w.hammers.FirstOrDefault(x => x.id == hid)?.apply?.Invoke(m);
            }
            foreach (var b in R.boons) b.def.apply?.Invoke(m, b.Value, b);
            float chaosDmg = 1f;
            foreach (var c in R.chaosCurses)
            {
                c.def.apply?.Invoke(m, 0, c);
                if (BoonDB.chaosEnemyDamage.TryGetValue(c.def.id, out var x)) chaosDmg += x;
            }
            R.chaosEnemyDamageMult = chaosDmg;
            m.maxMagickMult *= R.vowMagickMult;
            m.healMult *= R.vowHealMult;
            // Modo Dios (accesibilidad)
            if (GameSettings.GodMode) m.damageResist += Mathf.Min(0.8f, 0.2f + 0.02f * s.Count("godmode_deaths"));

            R.mods = m;
            R.maxDeathDefiance = 1 + m.extraDeathDefiance;
            R.deathDefiance = Mathf.Min(R.deathDefiance, R.maxDeathDefiance);
            PlayerController.Instance?.RecomputeStats();
        }

        // ---------------------------------------------------------------- añadir

        public static BoonInstance Grant(BoonDef def, Rarity rarity, BoonInstance replaces = null)
        {
            if (replaces != null) R.boons.Remove(replaces);
            var existing = R.boons.FirstOrDefault(b => b.def == def);
            if (existing != null) { existing.level++; Recompute(); return existing; }
            var inst = new BoonInstance(def, rarity);
            // conservar niveles al sustituir una ranura (como en Hades)
            if (replaces != null) inst.level = replaces.level;
            R.boons.Add(inst);
            if (def.god != null && Gods.Get(def.god) != null) Save.Data.Inc("boon_" + def.god);
            if (def.kind == BoonKind.Duo) Save.Data.Inc("duo");
            if (def.kind == BoonKind.Legendary) Save.Data.Inc("legendary");
            Save.Data.Codex("god_" + def.god);
            Recompute();
            return inst;
        }

        public static void ApplyOffer(Offer o)
        {
            if (o.hammer != null) { R.hammers.Add(o.hammer.id); Recompute(); PlayerController.Instance?.weapon?.OnUpgradesChanged(); return; }
            if (o.upgrade != null) { o.upgrade.level += o.levels; Recompute(); return; }
            if (o.def != null) Grant(o.def, o.rarity, o.replaces);
            if (o.curse != null)
            {
                var c = new BoonInstance(o.curse, Rarity.Common) { chaosRoomsLeft = o.curseRooms };
                R.chaosCurses.Add(c);
                Recompute();
            }
        }

        public static void OnRoomCleared()
        {
            if (R == null) return;
            bool changed = false;
            foreach (var c in R.chaosCurses.ToArray())
            {
                c.chaosRoomsLeft--;
                if (c.chaosRoomsLeft <= 0) { R.chaosCurses.Remove(c); changed = true; UIFloat.Text(PlayerController.Instance.transform.position + Vector3.up * 2.5f, "Se disipa: " + c.def.name, new Color(0.8f, 0.5f, 1f), 1f); }
            }
            if (changed) Recompute();
        }

        // ---------------------------------------------------------------- elegibilidad

        public static bool Eligible(BoonDef d)
        {
            if (R == null) return false;
            if (R.boons.Any(b => b.def == d)) return false;
            if (d.requiresGods != null && !d.requiresGods.All(g => R.boons.Any(b => b.def.god == g && b.def.kind != BoonKind.Duo))) return false;
            if (d.requiresAny != null && !d.requiresAny.Any(id => R.Has(id))) return false;
            if (d.requiresCurse.HasValue && !R.mods.slotStatus.Any(x => x == d.requiresCurse.Value)) return false;
            if (d.kind == BoonKind.Infusion && R.ElementCount(d.infusionElement) < d.infusionCost) return false;
            if (d.kind == BoonKind.Legendary && R.boons.Any(b => b.def.kind == BoonKind.Legendary && b.def.god == d.god)) return false;
            return true;
        }

        public static Rarity RollRarity(float bonus = 0f)
        {
            float depthBonus = R.depth * 0.006f;
            float r = (float)R.rng.NextDouble();
            float heroic = 0.015f + depthBonus * 0.3f + bonus * 0.2f;
            float epic = 0.06f + depthBonus * 0.6f + bonus * 0.5f;
            float rare = 0.18f + depthBonus + bonus;
            if (r < heroic) return Rarity.Heroic;
            if (r < heroic + epic) return Rarity.Epic;
            if (r < heroic + epic + rare) return Rarity.Rare;
            return Rarity.Common;
        }

        public static int ChoiceCount => Mathf.Clamp(3 + R.mods.extraBoonChoices - R.vowChoicePenalty, 1, 4);

        /// <summary>Oferta de un dios: prioriza ranuras vacías, luego pasivos, con opción de dúo/legendario/infusión.</summary>
        public static List<Offer> RollGodOffer(string god)
        {
            var offers = new List<Offer>();
            int count = ChoiceCount;
            var pool = BoonDB.OfGod(god).Where(Eligible).ToList();
            var specials = new List<BoonDef>();
            specials.AddRange(BoonDB.DuosOf(god).Where(Eligible));
            specials.AddRange(pool.Where(b => b.kind == BoonKind.Legendary || b.kind == BoonKind.Infusion));
            pool.RemoveAll(b => b.kind == BoonKind.Legendary || b.kind == BoonKind.Infusion);

            // un dúo/legendario/infusión con cierta probabilidad
            if (specials.Count > 0 && R.rng.Chance(0.35f + R.depth * 0.01f))
            {
                var sp = specials.Pick(R.rng);
                offers.Add(new Offer { def = sp, rarity = sp.kind == BoonKind.Duo ? Rarity.Duo : sp.kind == BoonKind.Legendary ? Rarity.Legendary : Rarity.Epic });
            }

            var cores = pool.Where(b => b.kind == BoonKind.Core).ToList();
            var emptySlotCores = cores.Where(b => R.InSlot(b.slot) == null).ToList();
            var passives = pool.Where(b => b.kind == BoonKind.Passive).ToList();
            emptySlotCores.Shuffle(R.rng);
            passives.Shuffle(R.rng);
            var replaceCores = cores.Where(b => R.InSlot(b.slot) != null).ToList();
            replaceCores.Shuffle(R.rng);

            bool hasGod = R.boons.Any(b => b.def.god == god && b.def.kind == BoonKind.Core);
            var ordered = new List<BoonDef>();
            if (!hasGod) { ordered.AddRange(emptySlotCores.Take(2)); ordered.AddRange(passives.Take(1)); ordered.AddRange(emptySlotCores.Skip(2)); }
            else { ordered.AddRange(passives.Take(2)); ordered.AddRange(emptySlotCores.Take(1)); ordered.AddRange(passives.Skip(2)); ordered.AddRange(emptySlotCores.Skip(1)); }
            if (R.rng.Chance(0.3f)) ordered.InsertRange(Mathf.Min(1, ordered.Count), replaceCores.Take(1));
            ordered.AddRange(replaceCores.Skip(1));

            foreach (var d in ordered)
            {
                if (offers.Count >= count) break;
                if (offers.Any(o => o.def == d)) continue;
                var o = new Offer { def = d, rarity = RollRarity(R.mods.rarityBonus + (R.mods.favoredGod == god ? 0.1f : 0f)) };
                if (d.kind == BoonKind.Core) o.replaces = R.InSlot(d.slot);
                offers.Add(o);
            }
            return offers;
        }

        public static List<Offer> RollCatOffer()
        {
            var pool = BoonDB.CatBoons.Where(Eligible).ToList();
            pool.Shuffle(R.rng);
            // ponderación simple: los de peso bajo se descartan a veces
            pool = pool.Where(b => R.rng.NextDouble() < b.weight || pool.Count <= 3).ToList();
            return pool.Take(ChoiceCount).Select(d => new Offer { def = d, rarity = RollRarity(R.mods.rarityBonus) }).ToList();
        }

        public static List<Offer> RollPomOffer()
        {
            var pool = R.boons.Where(b => b.def.levelable && b.def.kind != BoonKind.Chaos || (b.def.kind == BoonKind.Chaos && b.def.levelable)).ToList();
            pool.Shuffle(R.rng);
            return pool.Take(ChoiceCount).Select(b => new Offer { upgrade = b, levels = R.rng.Chance(0.25f) ? 2 : 1 }).ToList();
        }

        public static List<Offer> RollHammerOffer()
        {
            var w = Weapons.Get(R.weaponId);
            var pool = w.hammers.Where(h => !R.hammers.Contains(h.id) && (h.excludes == null || !h.excludes.Any(R.hammers.Contains))).ToList();
            pool.Shuffle(R.rng);
            return pool.Take(3).Select(h => new Offer { hammer = h }).ToList();
        }

        public static List<Offer> RollChaosOffer()
        {
            var bless = BoonDB.ChaosBlessings.Where(Eligible).ToList();
            var curses = BoonDB.ChaosCurses.ToList();
            bless.Shuffle(R.rng);
            var list = new List<Offer>();
            foreach (var b in bless.Take(3))
                list.Add(new Offer { def = b, rarity = RollRarity(0.15f), curse = curses.Pick(R.rng), curseRooms = R.rng.Next(3, 5) });
            return list;
        }

        /// <summary>Elige el dios para una puerta de boon (evitando repetir siempre el mismo).</summary>
        public static string PickGod(System.Random rng, string avoid = null)
        {
            var gods = Gods.All.Select(g => g.id).ToList();
            var weights = new List<float>();
            foreach (var g in gods)
            {
                float w = 1f;
                if (g == avoid) w = 0.15f;
                if (R != null && R.mods.favoredGod == g) w *= 2f;
                // tras tener 4 dioses, preferir los que ya tienes (como Hades limita los dioses por incursión)
                if (R != null)
                {
                    var owned = R.boons.Where(b => Gods.Get(b.def.god) != null).Select(b => b.def.god).Distinct().ToList();
                    if (owned.Count >= 4 && !owned.Contains(g)) w *= 0.05f;
                }
                weights.Add(w);
            }
            float total = weights.Sum();
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < gods.Count; i++) { r -= weights[i]; if (r <= 0) return gods[i]; }
            return gods[0];
        }
    }
}
