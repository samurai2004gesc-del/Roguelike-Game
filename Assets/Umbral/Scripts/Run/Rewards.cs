using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    public enum RewardType
    {
        None, Boon, Pom, Hammer, Heart, Gold, Ceniza, Ensueno, Huesos, Plata, Polvo, Licor, Lagrima,
        Cat, Hex, Chaos, Shop, Fountain, Boss, Exit, Wilbur
    }

    public enum RoomKind { Combat, Elite, Shop, Fountain, Antesala, Boss, Wilbur, Chaos, Exit }

    /// <summary>Plan de la siguiente sala (lo que muestra la puerta).</summary>
    public class RoomPlan
    {
        public RoomKind kind = RoomKind.Combat;
        public RewardType reward = RewardType.Boon;
        public string god;
    }

    public static class Rewards
    {
        public static string Name(RewardType r, string god)
        {
            switch (r)
            {
                case RewardType.Boon: return "Favor de " + (Gods.Get(god)?.name ?? "un dios");
                case RewardType.Pom: return "Fruto Oscuro";
                case RewardType.Hammer: return "Fragmento de Eltdown";
                case RewardType.Heart: return "Corazón de Gug";
                case RewardType.Gold: return "Oro Onírico";
                case RewardType.Ceniza: return Res.Name(Res.Ceniza);
                case RewardType.Ensueno: return Res.Name(Res.Ensueno);
                case RewardType.Huesos: return Res.Name(Res.Huesos);
                case RewardType.Plata: return Res.Name(Res.Plata);
                case RewardType.Polvo: return Res.Name(Res.Polvo);
                case RewardType.Licor: return Res.Name(Res.Licor);
                case RewardType.Lagrima: return Res.Name(Res.Lagrima);
                case RewardType.Cat: return "Gatos de Ulthar";
                case RewardType.Hex: return "Eco del Padre";
                case RewardType.Chaos: return "Grieta de Azathoth";
                case RewardType.Shop: return "Galera Negra de Leng";
                case RewardType.Fountain: return "Manantial de Kadath";
                case RewardType.Boss: return "Guardián";
                case RewardType.Exit: return "Siguiente región";
                case RewardType.Wilbur: return "Eco de Wilbur";
            }
            return "";
        }

        public static string Desc(RewardType r)
        {
            switch (r)
            {
                case RewardType.Boon: return "Un favor divino para esta incursión.";
                case RewardType.Pom: return "Sube de nivel uno de tus boones.";
                case RewardType.Hammer: return "Mejora permanente de tu arma para esta incursión.";
                case RewardType.Heart: return "+25 de vida máxima.";
                case RewardType.Gold: return "Moneda para las galeras de Leng.";
                case RewardType.Cat: return "Bendiciones de agilidad.";
                case RewardType.Hex: return "Un poder del Padre que se carga con Lucidez.";
                case RewardType.Shop: return "Mercaderes de Leng.";
                case RewardType.Fountain: return "Agua curativa.";
                case RewardType.Wilbur: return "Tu hermano te espera.";
            }
            return "Recurso permanente.";
        }

        public static Color ColorOf(RewardType r, string god)
        {
            switch (r)
            {
                case RewardType.Boon: return Gods.Get(god)?.color ?? Color.white;
                case RewardType.Pom: return new Color(0.75f, 0.3f, 0.85f);
                case RewardType.Hammer: return new Color(0.6f, 0.75f, 1f);
                case RewardType.Heart: return new Color(1f, 0.25f, 0.3f);
                case RewardType.Gold: return Res.ColorOf("oro");
                case RewardType.Ceniza: return Res.ColorOf(Res.Ceniza);
                case RewardType.Ensueno: return Res.ColorOf(Res.Ensueno);
                case RewardType.Huesos: return Res.ColorOf(Res.Huesos);
                case RewardType.Plata: return Res.ColorOf(Res.Plata);
                case RewardType.Polvo: return Res.ColorOf(Res.Polvo);
                case RewardType.Licor: return Res.ColorOf(Res.Licor);
                case RewardType.Lagrima: return Res.ColorOf(Res.Lagrima);
                case RewardType.Cat: return new Color(0.6f, 1f, 0.4f);
                case RewardType.Hex: return new Color(0.7f, 0.9f, 1f);
                case RewardType.Chaos: return new Color(1f, 0.3f, 0.8f);
                case RewardType.Shop: return new Color(1f, 0.7f, 0.3f);
                case RewardType.Fountain: return new Color(0.3f, 0.75f, 1f);
                case RewardType.Boss: return new Color(1f, 0.25f, 0.2f);
                case RewardType.Wilbur: return new Color(1f, 0.6f, 0.3f);
            }
            return Color.white;
        }

        public static string ResourceId(RewardType r)
        {
            switch (r)
            {
                case RewardType.Ceniza: return Res.Ceniza;
                case RewardType.Ensueno: return Res.Ensueno;
                case RewardType.Huesos: return Res.Huesos;
                case RewardType.Plata: return Res.Plata;
                case RewardType.Polvo: return Res.Polvo;
                case RewardType.Licor: return Res.Licor;
                case RewardType.Lagrima: return Res.Lagrima;
            }
            return null;
        }

        /// <summary>Genera las opciones de puerta tras limpiar la sala actual.</summary>
        public static List<RoomPlan> NextDoors(RunState run)
        {
            var biome = Biomes.Get(run.biome);
            var rng = run.rng;
            int next = run.room + 1;
            var s = Save.Data;
            var list = new List<RoomPlan>();
            if (next > biome.rooms) { list.Add(new RoomPlan { kind = RoomKind.Exit, reward = RewardType.Exit }); return list; }
            if (next == biome.rooms) { list.Add(new RoomPlan { kind = RoomKind.Boss, reward = RewardType.Boss }); return list; }
            if (next == biome.rooms - 1)
            {
                list.Add(new RoomPlan { kind = RoomKind.Antesala, reward = s.HasRitual("r_mercaderes") ? RewardType.Shop : RewardType.Fountain });
                return list;
            }

            int count = rng.Chance(0.55f) ? 2 : 3;
            if (next <= 1) count = 2;
            var used = new HashSet<RewardType>();
            string lastGod = null;
            for (int i = 0; i < count; i++)
            {
                var plan = new RoomPlan();
                // sala de élite a mitad de región
                if (next == biome.rooms / 2 && i == 0) { plan.kind = RoomKind.Elite; plan.reward = rng.Chance(0.5f) ? RewardType.Boon : RewardType.Hammer; }
                else if (next == biome.rooms / 2 + 1 && i == 1 && s.HasRitual("r_mercaderes")) { plan.kind = RoomKind.Shop; plan.reward = RewardType.Shop; }
                else if (s.HasRitual("r_wilbur") && !run.wilburMet && run.biome >= 1 && rng.Chance(0.12f) && i == count - 1) { plan.kind = RoomKind.Wilbur; plan.reward = RewardType.Wilbur; }
                else plan.reward = RollReward(run, used);
                if (plan.reward == RewardType.Boon)
                {
                    plan.god = BoonManager.PickGod(rng, lastGod ?? run.lastGodOffered);
                    if (list.Any(p => p.god == plan.god)) plan.god = BoonManager.PickGod(rng, plan.god);
                    lastGod = plan.god;
                }
                if (plan.reward == RewardType.Fountain) plan.kind = RoomKind.Fountain;
                used.Add(plan.reward);
                list.Add(plan);
            }
            // al menos un boon al principio de la incursión
            if (run.boons.Count < 2 && !list.Any(p => p.reward == RewardType.Boon))
            {
                list[0].reward = RewardType.Boon; list[0].kind = list[0].kind == RoomKind.Elite ? RoomKind.Elite : RoomKind.Combat;
                list[0].god = BoonManager.PickGod(rng, run.lastGodOffered);
            }
            return list;
        }

        static RewardType RollReward(RunState run, HashSet<RewardType> used)
        {
            var s = Save.Data;
            var table = new List<(RewardType, float)>
            {
                (RewardType.Boon, 3.2f), (RewardType.Pom, run.boons.Any(b => b.def.levelable) ? 1.3f : 0f),
                (RewardType.Hammer, run.hammers.Count < 2 ? 0.6f : 0.15f), (RewardType.Heart, 0.6f), (RewardType.Gold, 0.9f),
                (RewardType.Ceniza, 0.7f), (RewardType.Ensueno, 0.7f), (RewardType.Huesos, 0.5f), (RewardType.Plata, 0.45f),
                (RewardType.Polvo, 0.12f), (RewardType.Licor, 0.15f),
                (RewardType.Cat, s.HasRitual("r_gatos") ? 0.45f : 0f), (RewardType.Hex, s.HasRitual("r_ecos") ? (string.IsNullOrEmpty(run.hexId) ? 0.6f : 0.25f) : 0f),
                (RewardType.Fountain, s.HasRitual("r_manantial") && run.room >= 3 ? 0.3f : 0f),
            };
            for (int i = 0; i < table.Count; i++) if (used.Contains(table[i].Item1) && table[i].Item1 != RewardType.Boon) table[i] = (table[i].Item1, 0);
            float total = table.Sum(t => t.Item2);
            float r = (float)run.rng.NextDouble() * total;
            foreach (var t in table) { r -= t.Item2; if (r <= 0) return t.Item1; }
            return RewardType.Boon;
        }

        /// <summary>Entrega una recompensa (al recogerla en la sala).</summary>
        public static void Grant(RewardType r, string god, Vector3 at, System.Action onDone)
        {
            var run = RunState.Current;
            var p = PlayerController.Instance;
            switch (r)
            {
                case RewardType.Boon:
                    run.lastGodOffered = god;
                    Save.Data.Codex("god_" + god);
                    UIRoot.Instance.OpenBoonChoice(god, BoonManager.RollGodOffer(god), onDone);
                    return;
                case RewardType.Pom:
                    UIRoot.Instance.OpenChoice("Fruto Oscuro", "Elige un boon para subir de nivel", BoonManager.RollPomOffer(), new Color(0.75f, 0.3f, 0.85f), onDone);
                    return;
                case RewardType.Hammer:
                    UIRoot.Instance.OpenChoice("Fragmento de Eltdown", "Grabados prehumanos revelan cómo mejorar tu arma", BoonManager.RollHammerOffer(), new Color(0.6f, 0.75f, 1f), onDone);
                    return;
                case RewardType.Cat:
                    UIRoot.Instance.OpenChoice("Los Gatos de Ulthar", "Los gatos de Ulthar te observan... y te ofrecen su gracia", BoonManager.RollCatOffer(), new Color(0.6f, 1f, 0.4f), onDone);
                    return;
                case RewardType.Chaos:
                    Save.Data.Inc("chaos");
                    UIRoot.Instance.OpenChoice("Azathoth", "El Sultán Ciego burbujea. Cada bendición trae una maldición temporal.", BoonManager.RollChaosOffer(), new Color(1f, 0.3f, 0.8f), onDone);
                    return;
                case RewardType.Hex:
                    UIRoot.Instance.OpenHexChoice(onDone);
                    return;
                case RewardType.Heart:
                    run.heartsHp += 25;
                    BoonManager.Recompute();
                    p.Heal(25);
                    Sfx.Play(SfxId.LevelUp);
                    UIFloat.Text(at + Vector3.up * 2f, "+25 vida máxima", new Color(1f, 0.4f, 0.45f), 1.2f);
                    break;
                case RewardType.Gold:
                    {
                        int g = run.rng.Next(40, 80) + run.biome * 15;
                        run.AddGold(g);
                        UIFloat.Text(at + Vector3.up * 2f, $"+{Mathf.RoundToInt(g * run.mods.goldMult)} Oro Onírico", Res.ColorOf("oro"), 1.2f);
                        break;
                    }
                default:
                    {
                        string id = ResourceId(r);
                        if (id != null)
                        {
                            int n = r switch
                            {
                                RewardType.Ceniza => run.rng.Next(5, 11) + run.biome * 2,
                                RewardType.Ensueno => run.rng.Next(10, 21) + run.biome * 4,
                                RewardType.Huesos => run.rng.Next(6, 13) + run.biome * 2,
                                RewardType.Plata => run.rng.Next(1, 4),
                                _ => 1
                            };
                            int before = Save.Data.Res(id);
                            run.AddResource(id, n);
                            Sfx.Play(SfxId.Pickup);
                            UIFloat.Text(at + Vector3.up * 2f, $"+{Save.Data.Res(id) - before} {Res.Name(id)}", Res.ColorOf(id), 1.2f);
                        }
                        break;
                    }
            }
            onDone?.Invoke();
        }
    }

    /// <summary>Recompensa física en la sala: flota, brilla y se recoge con [E].</summary>
    public class RewardPickup : MonoBehaviour
    {
        public RewardType type;
        public string god;
        public System.Action onCollected;
        bool taken;

        public static RewardPickup Spawn(Vector2 at, RewardType type, string god, System.Action onCollected)
        {
            var go = new GameObject("Recompensa_" + type);
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var r = go.AddComponent<RewardPickup>();
            r.type = type; r.god = god; r.onCollected = onCollected;
            Props.Emblem(go.transform, type, god, new Vector3(0, 1.3f, 0), 1.1f);
            var c = Rewards.ColorOf(type, god);
            Kit.PointLight(go.transform, c, 5f, 2.5f, new Vector3(0, 1.6f, 0));
            Fx.MeshObj("aura", MeshGen.Quad, Mats.Unlit(c.WithA(0.5f), Mats.Blend.Additive, Tex.Ring, 2f, false), at.X0Z(0.06f), Quaternion.Euler(90, 0, 0), Vector3.one * 2.4f).transform.SetParent(go.transform, true);
            var it = Interactable.Add(go, "Tomar: " + Rewards.Name(type, god), r.Collect, 2f, 3f);
            Fx.Burst(go.transform.position + Vector3.up, c, 24, 6f, 0.3f, 0.6f, FxKind.Star);
            Sfx.Play(SfxId.Bell, go.transform.position, 0.5f, 1.5f);
            return r;
        }

        void Collect()
        {
            if (taken) return;
            taken = true;
            GetComponent<Interactable>().enabled = false;
            Fx.Burst(transform.position + Vector3.up * 1.3f, Rewards.ColorOf(type, god), 30, 7f, 0.3f, 0.6f, FxKind.Star);
            Sfx.Play(type == RewardType.Boon ? SfxId.Boon : SfxId.Pickup, transform.position);
            Rewards.Grant(type, god, transform.position, () =>
            {
                onCollected?.Invoke();
                if (this != null) Destroy(gameObject);
            });
            foreach (Transform c in transform) c.gameObject.SetActive(false);
        }
    }
}
