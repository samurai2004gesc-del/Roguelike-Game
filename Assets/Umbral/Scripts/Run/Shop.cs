using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// La Galera Negra: los mercaderes de Leng venden favores a cambio de Oro Onírico
    /// (equivalente al Pozo/Tienda de Caronte).
    /// </summary>
    public static class Shop
    {
        public static void Build(RoomDirector d, RoomBuilder.Layout layout, RunState run, bool small = false)
        {
            Vector2 up = layout.up;
            Vector2 right = new Vector2(up.y, -up.x);
            Vector2 merchantAt = layout.center + up * 3.5f + (small ? right * 4f : Vector2.zero);
            // mercader: un Hombre de Leng con turbante tras su mostrador
            var m = new GameObject("Mercader");
            m.transform.SetParent(World.Root, false);
            m.transform.position = merchantAt.X0Z();
            m.transform.rotation = U.LookFlat(-up);
            var rig = EnemyArt.LengMan(m.transform);
            if (rig.weaponPivot != null) rig.weaponPivot.gameObject.SetActive(false);
            var counter = Kit.Part(m.transform, MeshGen.Cube, Mats.Toon(new Color(0.25f, 0.15f, 0.1f), null, 1f), new Vector3(0, 0.5f, 1.1f), new Vector3(3.2f, 1f, 0.8f));
            Kit.Part(m.transform, MeshGen.Cube, Mats.Toon(new Color(0.55f, 0.12f, 0.12f), null, 1f), new Vector3(0, 1.02f, 1.1f), new Vector3(3.3f, 0.06f, 0.9f));
            Kit.PointLight(m.transform, new Color(1f, 0.7f, 0.35f), 6f, 2f, new Vector3(0, 2.4f, 1.2f));
            Interactable.Add(m, "Hablar con el Mercader de Leng", () => UIRoot.Instance.Toast(Lines.Merchant(), new Color(1f, 0.75f, 0.4f)), 2.6f, 3f);

            var stock = RollStock(run, small ? 3 : 5);
            float span = (stock.Count - 1) * 2.6f;
            for (int i = 0; i < stock.Count; i++)
            {
                Vector2 at = layout.center + (small ? right * 4f : Vector2.zero) - up * 0.5f + right * (-span / 2 + i * 2.6f);
                ShopItem.Create(at, stock[i]);
            }
        }

        public class Item
        {
            public string name, desc;
            public int price;
            public RewardType icon;
            public string god;
            public System.Action buy;
        }

        static int P(int baseP) => Mathf.RoundToInt(baseP * (RunState.Current?.vowShopMult ?? 1f));

        static List<Item> RollStock(RunState run, int n)
        {
            var all = new List<Item>();
            string god = BoonManager.PickGod(run.rng);
            var g = Gods.Get(god);
            all.Add(new Item { name = "Favor de " + g.name, desc = "Un boon de " + g.name + ".", price = P(150), icon = RewardType.Boon, god = god, buy = () => UIRoot.Instance.OpenBoonChoice(god, BoonManager.RollGodOffer(god), null) });
            if (run.boons.Any(b => b.def.levelable))
                all.Add(new Item { name = "Fruto Oscuro", desc = "Sube de nivel un boon.", price = P(110), icon = RewardType.Pom, buy = () => UIRoot.Instance.OpenChoice("Fruto Oscuro", "Elige un boon para subir de nivel", BoonManager.RollPomOffer(), new Color(0.75f, 0.3f, 0.85f), null) });
            all.Add(new Item { name = "Corazón de Gug", desc = "+25 de vida máxima.", price = P(120), icon = RewardType.Heart, buy = () => { run.heartsHp += 25; BoonManager.Recompute(); PlayerController.Instance.Heal(25); } });
            all.Add(new Item { name = "Vino Lunar de los Zoogs", desc = "Recupera el 50% de tu vida.", price = P(70), icon = RewardType.Fountain, buy = () => PlayerController.Instance.HealScaled(PlayerController.Instance.maxHp * 0.5f) });
            if (run.hammers.Count < Weapons.Get(run.weaponId).hammers.Count)
                all.Add(new Item { name = "Fragmento de Eltdown", desc = "Mejora tu arma.", price = P(180), icon = RewardType.Hammer, buy = () => UIRoot.Instance.OpenChoice("Fragmento de Eltdown", "Elige una mejora", BoonManager.RollHammerOffer(), new Color(0.6f, 0.75f, 1f), null) });
            if (run.deathDefiance < run.maxDeathDefiance)
                all.Add(new Item { name = "Ojo de la Puerta", desc = "Restaura un Último Umbral.", price = P(200), icon = RewardType.Hex, buy = () => run.deathDefiance++ });
            all.Add(new Item { name = "Licor de Luna", desc = "Un regalo para tus allegados.", price = P(100), icon = RewardType.Licor, buy = () => run.AddResource(Res.Licor, 1) });
            all.Add(new Item { name = "Saco de Ceniza Estelar", desc = "+15 Ceniza Estelar.", price = P(60), icon = RewardType.Ceniza, buy = () => run.AddResource(Res.Ceniza, 15) });
            if (Save.Data.HasRitual("r_gatos"))
                all.Add(new Item { name = "Bendición Felina", desc = "Un favor de los Gatos de Ulthar.", price = P(130), icon = RewardType.Cat, buy = () => UIRoot.Instance.OpenChoice("Los Gatos de Ulthar", "", BoonManager.RollCatOffer(), new Color(0.6f, 1f, 0.4f), null) });
            var first = all[0];
            var rest = all.Skip(1).ToList();
            rest.Shuffle(run.rng);
            var res = new List<Item> { first };
            res.AddRange(rest.Take(n - 1));
            return res;
        }
    }

    public class ShopItem : MonoBehaviour
    {
        Shop.Item item;
        bool sold;

        public static void Create(Vector2 at, Shop.Item item)
        {
            var go = new GameObject("Articulo");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            var s = go.AddComponent<ShopItem>();
            s.item = item;
            Kit.Part(go.transform, MeshGen.Cylinder, Mats.Toon(new Color(0.3f, 0.22f, 0.18f), null, 1f), new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.45f, 0.9f));
            Props.Emblem(go.transform, item.icon, item.god, new Vector3(0, 1.6f, 0), 0.8f);
            var it = Interactable.Add(go, $"Comprar {item.name} — {item.price} oro\n<size=18>{item.desc}</size>", s.Buy, 1.5f, 2.8f);
            it.usable = () => !s.sold;
        }

        void Buy()
        {
            var run = RunState.Current;
            if (sold) return;
            if (run.gold < item.price)
            {
                Sfx.Play(SfxId.UiBack);
                UIRoot.Instance.Toast("No tienes suficiente Oro Onírico", new Color(1f, 0.5f, 0.4f));
                return;
            }
            run.gold -= item.price;
            sold = true;
            Sfx.Play(SfxId.Gold);
            Fx.Burst(transform.position + Vector3.up * 1.6f, Res.ColorOf("oro"), 20, 5f, 0.3f, 0.5f, FxKind.Star);
            foreach (Transform c in transform) if (c.name == "Emblema") c.gameObject.SetActive(false);
            item.buy?.Invoke();
        }
    }
}
