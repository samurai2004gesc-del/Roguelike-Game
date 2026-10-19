using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// El Umbral: santuario flotante entre las esferas (equivalente a la Encrucijada de Hades II).
    /// Aquí están las estaciones de progresión permanente y los allegados con los que se conversa y a quienes se regala.
    /// </summary>
    public static class HubBuilder
    {
        static readonly BiomeDef HubStyle = new BiomeDef
        {
            id = "umbral", name = "El Umbral", subtitle = "El espacio entre los espacios",
            floorA = new Color(0.22f, 0.18f, 0.32f), floorB = new Color(0.3f, 0.24f, 0.4f), crack = new Color(0.06f, 0.03f, 0.1f), veins = new Color(0.7f, 0.6f, 1f),
            voidA = new Color(0.01f, 0.0f, 0.04f), voidB = new Color(0.15f, 0.05f, 0.3f), voidC = new Color(0.5f, 0.8f, 1f),
            sun = new Color(0.85f, 0.8f, 1f), ambient = new Color(0.22f, 0.18f, 0.32f), rim = new Color(0.8f, 0.7f, 1f), motes = new Color(0.8f, 0.85f, 1f),
            vignette = new Color(0.05f, 0.0f, 0.12f), sunIntensity = 1.0f, saturation = 20f
        };

        static Vector2 up, right;
        static Vector2 At(float x, float y) => (right * x + up * y) * 0.7f;

        public static Vector2 Build()
        {
            World.Reset();
            var rng = new System.Random(77);
            float yaw = CameraRig.Instance.yaw;
            up = Vector2.up.Rotate(-yaw); right = Vector2.right.Rotate(-yaw);

            // planta: una gran plaza octogonal con alas
            var pts = new List<Vector2>();
            float[] sx = { -20, -9, -6, 6, 9, 20, 20, 14, 6, 0, -6, -14, -20 };
            float[] sy = { -10, -14, -16, -16, -14, -10, 8, 14, 17, 18, 17, 14, 8 };
            for (int i = 0; i < sx.Length; i++) pts.Add(At(sx[i], sy[i]));
            var arena = new Arena();
            arena.SetPolygon(pts);
            Arena.Current = arena;

            RoomBuilder.ApplyLighting(HubStyle);
            RoomBuilder.BuildVoid(HubStyle, Vector2.zero, true);
            RoomBuilder.BuildFloor(HubStyle, arena, 5);
            RoomBuilder.BuildMotes(HubStyle, Vector2.zero, 30f);
            var parent = World.Child("Santuario");
            Decorate(parent, arena, rng);

            var s = Save.Data;
            // ---------------- estaciones ----------------
            Portal(parent, At(0, 14.5f));
            Station(parent, "Altar de las Armas", At(-11f, 9f), StationKind.Weapons, () => UIRoot.Instance.OpenWeaponAltar());
            Station(parent, "El Caldero", At(-7f, 1f), StationKind.Cauldron, () => UIRoot.Instance.OpenCaldero());
            Station(parent, "Grimorio de Keziah", At(10f, 3f), StationKind.Grimoire, () => UIRoot.Instance.OpenGrimorio());
            Station(parent, "Tablero de Presagios", At(12f, 10f), StationKind.Quests, () => UIRoot.Instance.OpenPresagios());
            Station(parent, "Necronomicón", At(5f, 8f), StationKind.Codex, () => UIRoot.Instance.OpenCodex());
            if (s.HasRitual("r_juramento")) Station(parent, "Juramento de lo Innombrable", At(-5f, 11f), StationKind.Oath, () => UIRoot.Instance.OpenJuramento());
            if (s.HasRitual("r_reliquias")) Station(parent, "Altar de Reliquias", At(15f, -4f), StationKind.Relics, () => UIRoot.Instance.OpenReliquias());
            if (s.HasRitual("r_west")) Station(parent, "Laboratorio de West", At(-15f, -3f), StationKind.Lab, () => UIRoot.Instance.OpenWestMarket());
            if (s.HasRitual("r_monigote"))
            {
                var d = Enemy.Spawn(EnemyDB.Get("dummy"), At(-12f, -8f), false, 0.01f);
                d.isDummy = true; d.countsForClear = false; d.SetMaxHp(99999);
                d.def.attacks.Clear();
                var dps = d.gameObject.AddComponent<DpsMeter>();
                dps.target = d;
            }

            // ---------------- allegados ----------------
            Npc(parent, "umr", At(-9.5f, 3.5f), At(0, 0));
            Npc(parent, "carter", At(13.5f, 8f), At(0, 0));
            Npc(parent, "keziah", At(12f, 1f), At(0, 0));
            Npc(parent, "jenkin", At(9f, 0.5f), At(0, 0));
            Npc(parent, "zadok", At(-14f, -6f), At(-12f, -8f));
            if (s.HasRitual("r_west")) Npc(parent, "west", At(-16.5f, -1f), At(0, 0));
            if (s.Count("wilbur_defeated") > 0) Npc(parent, "wilbur", At(16f, 4f), At(0, 0));

            Music.Play("hub");
            return At(0, -9f);
        }

        // ------------------------------------------------------------------ decoración
        static void Decorate(Transform parent, Arena arena, System.Random rng)
        {
            // columnas rotas y esferas flotantes alrededor
            for (int i = 0; i < arena.polygon.Count; i++)
            {
                Vector2 a = arena.polygon[i], b = arena.polygon[(i + 1) % arena.polygon.Count];
                Vector2 dir = (b - a).normalized; Vector2 outN = new Vector2(dir.y, -dir.x);
                float len = Vector2.Distance(a, b);
                for (float t = 1.5f; t < len - 1f; t += 3.5f)
                {
                    Vector2 p = a + dir * t + outN * 0.6f;
                    if (Vector2.Dot(outN, up) < -0.4f) continue;
                    Props.BrokenColumn(parent, p, rng);
                }
            }
            // las esferas iridiscentes del Padre, enormes, en lo alto
            for (int i = 0; i < 9; i++)
            {
                Vector2 p = up * 30f + right * (i - 4) * 9f + Random.insideUnitCircle * 4f;
                float sz = rng.Range(4f, 10f);
                var g = Kit.Part(parent, MeshGen.Sphere, Mats.Unlit(Color.HSVToRGB(i / 9f, 0.45f, 1f).WithA(0.85f), Mats.Blend.Additive, Tex.SoftCircle, 1.4f, false), p.X0Z(rng.Range(-6f, 6f)), Vector3.one * sz, default, "globo", false);
                g.gameObject.AddComponent<Floater>().amp = 1f;
                g.gameObject.AddComponent<Pulse>().amount = 0.05f;
            }
            // runas en el suelo (círculo central)
            var rune = Fx.MeshObj("RunaCentral", MeshGen.Quad, Mats.Unlit(new Color(0.7f, 0.6f, 1f, 0.35f), Mats.Blend.Additive, Tex.ElderSign, 1.5f, false), Vector3.up * 0.05f, Quaternion.Euler(90, 0, 0), Vector3.one * 7f);
            rune.transform.SetParent(parent, true);
            rune.AddComponent<Spin>().speed = 0;
            Kit.PointLight(parent, new Color(0.7f, 0.6f, 1f), 12f, 1.6f, new Vector3(0, 3f, 0));
            // santuario de los Primigenios: medallones de los dioses junto a la Puerta
            string[] gods = { "cthulhu", "hastur", "shub", "nodens", "ithaqua", "cthugha" };
            for (int i = 0; i < gods.Length; i++)
            {
                float x = (i < 3 ? -1 : 1) * (5.5f + (i % 3) * 2.6f);
                var at = At(x, 12.5f - (i % 3) * 1.6f);
                Props.GodShrine(parent, at, gods[i]);
                arena.obstacles.Add(new Arena.Obstacle(at, 0.5f, true));
            }
            // estanterías, velas y objetos repartidos
            Props.Bookshelf(parent, At(15.5f, 1.5f), -up);
            Props.Bookshelf(parent, At(8f, 10.5f), -up);
            Props.Bookshelf(parent, At(-16f, 5f), right);
            foreach (var c in new[] { At(-3f, 4f), At(3f, 4f), At(-6f, -5f), At(7f, -6f), At(14f, 5f), At(-13f, 1f), At(1f, 11f), At(-9f, 7.5f) })
                Props.Candles(parent, c.X0Z(), 3 + rng.Next(0, 3));
            for (int i = 0; i < 14; i++)
            {
                var q = arena.RandomPoint(rng, 0.3f, 3.5f, Vector2.zero);
                Props.Clutter(parent, "umbral", q, rng);
            }
            RoomBuilder.Dress(new BiomeDef
            {
                id = "azathoth", floorA = HubStyle.floorA, floorB = HubStyle.floorB, crack = HubStyle.crack, veins = HubStyle.veins,
                voidA = HubStyle.voidA, voidB = HubStyle.voidB, voidC = HubStyle.voidC
            }, new Arena(), rng, false);
        }

        // ------------------------------------------------------------------ estaciones
        enum StationKind { Weapons, Cauldron, Grimoire, Quests, Codex, Oath, Relics, Lab }

        static void Station(Transform parent, string name, Vector2 at, StationKind kind, System.Action open)
        {
            var root = Kit.Empty(parent, name, at.X0Z());
            root.rotation = U.LookFlat(-up);
            var stone = Mats.Toon(new Color(0.22f, 0.19f, 0.3f), null, 1f);
            Color glow = new Color(0.7f, 0.6f, 1f);
            switch (kind)
            {
                case StationKind.Weapons:
                    Kit.Part(root, MeshGen.Cylinder, stone, new Vector3(0, 0.5f, 0), new Vector3(2.4f, 0.5f, 2.4f));
                    for (int i = 0; i < 5; i++)
                    {
                        var w = Weapons.All[i];
                        var wpos = new Vector3(Mathf.Cos(i * 1.256f) * 0.85f, 1.6f + (i % 2) * 0.3f, Mathf.Sin(i * 1.256f) * 0.85f);
                        var wicon = ArtIA.Tex(ArtIA.Iconos, w.id);
                        Transform g = wicon != null
                            ? Kit.Part(root, MeshGen.Quad, Mats.Unlit(w.Unlocked ? Color.white : new Color(0.25f, 0.25f, 0.3f), Mats.Blend.Alpha, wicon, 1.1f, false), wpos, Vector3.one * 0.7f, default, "arma", false)
                            : Kit.Glow(root, w.Unlocked ? w.color : new Color(0.2f, 0.2f, 0.25f), 0.35f, wpos, w.Unlocked ? 2.5f : 0.6f);
                        if (wicon != null) g.gameObject.AddComponent<Billboard>();
                        g.gameObject.AddComponent<Floater>().amp = 0.15f;
                    }
                    glow = new Color(0.75f, 0.85f, 1f);
                    break;
                case StationKind.Cauldron:
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.12f, 0.1f, 0.14f), null, 1f), new Vector3(0, 0.8f, 0), new Vector3(2.4f, 1.6f, 2.4f));
                    Kit.Part(root, MeshGen.Cylinder, Mats.Unlit(new Color(0.6f, 1f, 0.5f), Mats.Blend.Additive, null, 2.2f, false), new Vector3(0, 1.45f, 0), new Vector3(1.9f, 0.04f, 1.9f), default, "liquido", false).gameObject.AddComponent<Pulse>();
                    var bub = new GameObject("Burbujas"); bub.transform.SetParent(root, false); bub.transform.localPosition = Vector3.up * 1.5f;
                    bub.AddComponent<Bubbles>().color = new Color(0.6f, 1f, 0.5f);
                    glow = new Color(0.6f, 1f, 0.5f);
                    break;
                case StationKind.Grimoire:
                case StationKind.Codex:
                    {
                        // atril de madera oscura con un libro abierto, velas y runas flotantes
                        bool codex = kind == StationKind.Codex;
                        glow = codex ? new Color(0.5f, 1f, 0.45f) : new Color(0.6f, 1f, 0.4f);
                        var wood = Mats.Toon(new Color(0.25f, 0.15f, 0.1f), null, 1f);
                        Kit.Part(root, MeshGen.Cylinder, wood, new Vector3(0, 0.55f, 0), new Vector3(0.22f, 0.55f, 0.22f));
                        Kit.Part(root, MeshGen.Cylinder, wood, new Vector3(0, 0.04f, 0), new Vector3(0.9f, 0.04f, 0.9f));
                        var top = Kit.Empty(root, "atril", new Vector3(0, 1.15f, 0), new Vector3(-28, 0, 0));
                        Kit.Part(top, MeshGen.Cube, wood, Vector3.zero, new Vector3(1.2f, 0.08f, 0.85f));
                        var cover = Mats.Toon(codex ? new Color(0.35f, 0.18f, 0.12f) : new Color(0.12f, 0.22f, 0.1f), null, 1f);
                        var page = Mats.Toon(new Color(0.9f, 0.84f, 0.68f), new Color(0.1f, 0.12f, 0.05f), 0.7f);
                        Kit.Part(top, MeshGen.Cube, cover, new Vector3(0, 0.07f, 0), new Vector3(1.0f, 0.05f, 0.7f));
                        Kit.Part(top, MeshGen.Cube, page, new Vector3(-0.24f, 0.11f, 0), new Vector3(0.46f, 0.04f, 0.62f), new Vector3(0, 0, 6));
                        Kit.Part(top, MeshGen.Cube, page, new Vector3(0.24f, 0.11f, 0), new Vector3(0.46f, 0.04f, 0.62f), new Vector3(0, 0, -6));
                        Kit.Part(top, MeshGen.Quad, Mats.Unlit(glow.WithA(0.9f), Mats.Blend.Additive, Tex.ElderSign, 2f, false), new Vector3(0.24f, 0.14f, 0), Vector3.one * 0.4f, new Vector3(90, 0, 0), "runa", false);
                        Props.Candles(root, new Vector3(0.75f, 0, 0.3f), 3);
                        Props.Candles(root, new Vector3(-0.75f, 0, -0.2f), 2);
                        if (codex) for (int i = 0; i < 4; i++) Props.FloatingBook(root, i);
                        break;
                    }
                case StationKind.Quests:
                    {
                        // astrolabio de anillos girando sobre un pedestal: los presagios en las estrellas
                        Kit.Part(root, MeshGen.Cylinder, stone, new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.45f, 0.9f));
                        Kit.Part(root, MeshGen.Cylinder, Mats.Toon(new Color(0.85f, 0.7f, 0.35f), new Color(0.2f, 0.12f, 0f), 1f), new Vector3(0, 0.93f, 0), new Vector3(1f, 0.04f, 1f));
                        var gold = Mats.Toon(new Color(0.9f, 0.75f, 0.35f), new Color(0.25f, 0.15f, 0f), 0.8f);
                        for (int i = 0; i < 3; i++)
                        {
                            var ring = Kit.Empty(root, "anillo", new Vector3(0, 1.9f, 0), new Vector3(i * 60, i * 45, 0));
                            Kit.Part(ring, MeshGen.Ring(0.9f), gold, Vector3.zero, Vector3.one * (0.75f - i * 0.12f), new Vector3(90, 0, 0), "aro", false);
                            ring.gameObject.AddComponent<Spin>().speed = 25f + i * 20f;
                        }
                        Kit.Glow(root, new Color(0.7f, 0.85f, 1f), 0.28f, new Vector3(0, 1.9f, 0), 2.2f);
                        for (int i = 0; i < 7; i++) Kit.Glow(root, new Color(0.8f, 0.9f, 1f), 0.07f, new Vector3(Mathf.Cos(i * 0.9f) * 0.9f, 1.5f + Mathf.Sin(i * 1.7f) * 0.6f, Mathf.Sin(i * 0.9f) * 0.9f), 2.5f).gameObject.AddComponent<Floater>().amp = 0.1f;
                        glow = new Color(0.6f, 0.8f, 1f);
                        break;
                    }
                case StationKind.Oath:
                    Kit.Part(root, MeshGen.Crystal(4, 0.25f), Mats.Toon(new Color(0.1f, 0.04f, 0.06f), new Color(0.3f, 0.0f, 0.05f), 1f), Vector3.zero, new Vector3(1.2f, 3.4f, 1.2f));
                    Kit.Part(root, MeshGen.Ring(0.85f), Mats.Unlit(new Color(1f, 0.25f, 0.25f), Mats.Blend.Additive, null, 2.5f, false), new Vector3(0, 1.8f, 0.45f), Vector3.one * 0.45f, new Vector3(90, 0, 0), "runa", false);
                    glow = new Color(1f, 0.3f, 0.3f);
                    break;
                case StationKind.Relics:
                    Kit.Part(root, MeshGen.Cube, stone, new Vector3(0, 0.6f, 0), new Vector3(2f, 1.2f, 1f));
                    for (int i = 0; i < 3; i++) Kit.Glow(root, UIKit.Gold, 0.2f, new Vector3(-0.6f + i * 0.6f, 1.45f, 0), 2.5f);
                    glow = UIKit.Gold;
                    break;
                case StationKind.Lab:
                    Kit.Part(root, MeshGen.Cube, Mats.Toon(new Color(0.35f, 0.25f, 0.18f), null, 1f), new Vector3(0, 0.6f, 0), new Vector3(2.4f, 0.12f, 1.1f));
                    for (int i = 0; i < 4; i++) Kit.Part(root, MeshGen.Cylinder, Mats.Unlit(new Color(0.4f, 1f, 0.3f), Mats.Blend.Additive, null, 2.2f, false), new Vector3(-0.8f + i * 0.5f, 0.9f, 0), new Vector3(0.15f, 0.25f, 0.15f), default, "vial", false);
                    Kit.Part(root, MeshGen.Cylinder, Mats.Toon(new Color(0.35f, 0.25f, 0.18f), null, 1f), new Vector3(1f, 0.3f, 0.4f), new Vector3(0.12f, 0.3f, 0.12f));
                    Kit.Part(root, MeshGen.Cylinder, Mats.Toon(new Color(0.35f, 0.25f, 0.18f), null, 1f), new Vector3(-1f, 0.3f, -0.4f), new Vector3(0.12f, 0.3f, 0.12f));
                    glow = new Color(0.4f, 1f, 0.3f);
                    break;
            }
            Kit.PointLight(root, glow, 5f, 1.8f, new Vector3(0, 2f, 0.5f));
            // alfombra rúnica bajo cada estación
            var rug = Fx.MeshObj("Alfombra", MeshGen.Quad, Mats.Unlit(glow.WithA(0.35f), Mats.Blend.Additive, Tex.Ring, 1.4f, false), at.X0Z(0.04f), Quaternion.Euler(90, 0, 0), Vector3.one * 3.6f);
            rug.transform.SetParent(root, true);
            Arena.Current.obstacles.Add(new Arena.Obstacle(at, kind == StationKind.Quests ? 1.0f : 0.9f, true));
            Interactable.Add(root.gameObject, name, open, 2.6f, 3.2f);
        }

        static void Portal(Transform parent, Vector2 at)
        {
            var root = Kit.Empty(parent, "LaPuerta", at.X0Z());
            root.rotation = U.LookFlat(-up);
            var stone = Mats.Toon(new Color(0.2f, 0.17f, 0.28f), null, 1f);
            Kit.Part(root, MeshGen.Cube, stone, new Vector3(2.4f, 2.6f, 0), new Vector3(1f, 5.2f, 1f));
            Kit.Part(root, MeshGen.Cube, stone, new Vector3(-2.4f, 2.6f, 0), new Vector3(1f, 5.2f, 1f));
            Kit.Part(root, MeshGen.Cube, stone, new Vector3(0, 5.5f, 0), new Vector3(6f, 0.8f, 1.1f));
            Kit.Part(root, MeshGen.Ring(0.85f), Mats.Unlit(new Color(0.8f, 0.75f, 1f), Mats.Blend.Additive, null, 2.5f, false), new Vector3(0, 5.5f, 0.6f), Vector3.one * 0.6f, new Vector3(90, 0, 0), "llave", false);
            var mat = Mats.NewUnlit(new Color(0.7f, 0.5f, 1f, 0.95f), Mats.Blend.Additive, Tex.Swirl, 2.2f, false);
            var q = Kit.Part(root, MeshGen.Quad, mat, new Vector3(0, 2.6f, 0), new Vector3(3.8f, 5f, 1f), new Vector3(0, 180, 0), "portal", false);
            q.gameObject.AddComponent<PortalSwirl>().mat = mat;
            Kit.PointLight(root, new Color(0.7f, 0.5f, 1f), 9f, 3f, new Vector3(0, 2.6f, 1.2f));
            Arena.Current.obstacles.Add(new Arena.Obstacle(at + right * 2.4f, 0.8f, true));
            Arena.Current.obstacles.Add(new Arena.Obstacle(at - right * 2.4f, 0.8f, true));
            var it = Interactable.Add(root.gameObject, "Cruzar la Puerta (comenzar incursión)", () => Game.Instance.StartRun(), 2.8f, 5.6f);
        }

        // ------------------------------------------------------------------ allegados
        static void Npc(Transform parent, string id, Vector2 at, Vector2 lookAt)
        {
            var go = new GameObject("NPC_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = at.X0Z();
            Vector2 look = (lookAt - at).sqrMagnitude > 0.1f ? (lookAt - at) : -up;
            go.transform.rotation = U.LookFlat(look);
            var rig = CharacterArt.NpcOrArt(go.transform, id);
            var npc = go.AddComponent<HubNpc>();
            npc.id = id;
            var sp = Dialogue.Sp(id);
            var it = Interactable.Add(go, "Hablar con " + sp.name, () => UIRoot.Instance.TalkTo(id), 2.4f, id == "umr" ? 3.8f : 2.8f);
            it.secondaryPrompt = "Regalar Licor";
            it.onSecondary = () => UIRoot.Instance.GiftTo(id);
            Arena.Current.obstacles.Add(new Arena.Obstacle(at, id == "jenkin" ? 0.35f : 0.55f, false));
        }
    }

    /// <summary>Indicador de "tiene algo que decirte" sobre los allegados.</summary>
    public class HubNpc : MonoBehaviour
    {
        public string id;
        Transform bubble;
        float check;
        void Start()
        {
            // rombo dorado sobre la cabeza: tiene algo nuevo que decirte
            float h = id == "umr" ? 3.2f : id == "jenkin" ? 0.8f : id == "wilbur" ? 2.4f : 2f;
            float top = ArtIA.Has(ArtIA.Personajes, id) ? h * 1.2f + (id == "umr" ? 0.35f : 0f) : h + 0.2f;
            bubble = Kit.Part(transform, MeshGen.Crystal(4, 0.5f), Mats.Unlit(new Color(1f, 0.82f, 0.35f), Mats.Blend.Additive, null, 1.6f, false), new Vector3(0, top + 0.45f, 0), new Vector3(0.18f, 0.32f, 0.18f), new Vector3(180, 0, 0), "burbuja", false);
            bubble.gameObject.AddComponent<Spin>();
        }
        void Update()
        {
            check -= Time.unscaledDeltaTime;
            if (check > 0) return;
            check = 0.5f;
            bubble.gameObject.SetActive(Dialogue.Next(id) != null);
        }
    }

    public class PortalSwirl : MonoBehaviour
    {
        public Material mat;
        void Update() { if (mat != null) { mat.SetTextureOffset("_BaseMap", new Vector2(Time.time * 0.05f, -Time.time * 0.2f)); mat.SetColor(Mats.BaseColorId, Color.Lerp(new Color(0.7f, 0.5f, 1f), PlayerController.IridColor(), 0.4f).WithA(0.95f)); } }
    }

    public class Bubbles : MonoBehaviour
    {
        public Color color;
        float t;
        void Update()
        {
            t -= Time.deltaTime;
            if (t > 0) return;
            t = 0.12f;
            Fx.Emit(FxKind.Glow, transform.position + new Vector3(Random.Range(-0.7f, 0.7f), 0, Random.Range(-0.7f, 0.7f)), Vector3.up * Random.Range(0.6f, 1.6f), color, Random.Range(0.15f, 0.35f), 1f);
        }
    }

    /// <summary>Medidor de daño por segundo para el monigote de práctica.</summary>
    public class DpsMeter : MonoBehaviour
    {
        public Combatant target;
        readonly Queue<(float t, float d)> hits = new Queue<(float, float)>();
        float total;
        void OnEnable() { Combat.OnDamageDealt += OnDmg; }
        void OnDisable() { Combat.OnDamageDealt -= OnDmg; }
        void OnDmg(Combatant c, Hit h, float d)
        {
            if (c != target) return;
            hits.Enqueue((Time.time, d)); total += d;
        }
        void Update()
        {
            while (hits.Count > 0 && Time.time - hits.Peek().t > 5f) total -= hits.Dequeue().d;
            if (hits.Count > 0 && target != null)
                UIFloat.ShowPrompt(target.transform.position + Vector3.up * 3.2f, $"Daño/s (5 s): {Mathf.RoundToInt(total / 5f)}");
        }
    }
}
