using UnityEngine;

namespace Umbral
{
    /// <summary>Atrezo de relleno: velas, libros, columnas rotas, altares, estanterías y objetos pequeños por región.</summary>
    public static partial class Props
    {
        static readonly Color Flame = new Color(1f, 0.72f, 0.35f);

        /// <summary>Grupo de velas de distintas alturas con llama parpadeante y una luz cálida.</summary>
        public static void Candles(Transform parent, Vector3 localPos, int n)
        {
            var root = Kit.Empty(parent, "Velas", localPos);
            var wax = Mats.Toon(new Color(0.9f, 0.86f, 0.75f), new Color(0.08f, 0.06f, 0.03f), 0.8f);
            var rng = new System.Random(Mathf.RoundToInt(localPos.x * 97 + localPos.z * 31));
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.4f, r = i == 0 ? 0 : 0.18f + i * 0.05f;
                float h = 0.25f + (float)rng.NextDouble() * 0.45f;
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                Kit.Part(root, MeshGen.Cylinder, wax, p + Vector3.up * h * 0.5f, new Vector3(0.09f, h * 0.5f, 0.09f));
                var f = Kit.Part(root, MeshGen.Sphere, Mats.Unlit(Flame, Mats.Blend.Additive, null, 3f, false), p + Vector3.up * (h + 0.07f), new Vector3(0.06f, 0.11f, 0.06f), default, "llama", false);
                f.gameObject.AddComponent<Flicker>();
            }
            Kit.PointLight(root, Flame, 3.5f, 1.2f, Vector3.up * 0.8f).gameObject.AddComponent<Flicker>();
        }

        /// <summary>Libro que orbita lentamente alrededor de un atril, con las páginas abiertas.</summary>
        public static void FloatingBook(Transform parent, int i)
        {
            var pivot = Kit.Empty(parent, "LibroOrbita", Vector3.zero, new Vector3(0, i * 90, 0));
            pivot.gameObject.AddComponent<Spin>().speed = 18f;
            var book = Kit.Empty(pivot, "Libro", new Vector3(1.3f, 1.6f + (i % 2) * 0.5f, 0), new Vector3(-20, 0, 15));
            Color c = Color.HSVToRGB(0.05f + i * 0.2f, 0.55f, 0.45f);
            Kit.Part(book, MeshGen.Cube, Mats.Toon(c, null, 0.8f), Vector3.zero, new Vector3(0.42f, 0.06f, 0.3f));
            Kit.Part(book, MeshGen.Cube, Mats.Toon(new Color(0.9f, 0.85f, 0.7f), null, 0.6f), new Vector3(0, 0.04f, 0), new Vector3(0.38f, 0.03f, 0.27f));
            book.gameObject.AddComponent<Floater>().amp = 0.12f;
        }

        /// <summary>Columna rota y ornamentada, baja para no tapar la vista.</summary>
        public static void BrokenColumn(Transform parent, Vector2 p, System.Random rng)
        {
            var stone = Mats.Toon(new Color(0.24f, 0.21f, 0.32f), null, 1f);
            var trim = Mats.Toon(new Color(0.75f, 0.62f, 0.32f), new Color(0.12f, 0.08f, 0f), 1f);
            var root = Kit.Empty(parent, "Columna", p.X0Z(), new Vector3(0, rng.Range(0, 360f), 0));
            float h = rng.Range(1.0f, 2.6f);
            Kit.Part(root, MeshGen.Cube, stone, new Vector3(0, 0.15f, 0), new Vector3(1.1f, 0.3f, 1.1f));
            Kit.Part(root, MeshGen.Cylinder, stone, new Vector3(0, 0.3f + h * 0.5f, 0), new Vector3(0.75f, h * 0.5f, 0.75f));
            Kit.Part(root, MeshGen.Cylinder, trim, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.05f, 0.8f));
            Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.35f, 0), stone, new Vector3(0, 0.3f + h, 0), new Vector3(0.75f, 0.35f, 0.75f));
            if (rng.NextDouble() < 0.4) Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 0), stone, new Vector3(0.8f, 0.15f, 0.3f), Vector3.one * 0.35f);
        }

        /// <summary>Altar de un dios: pedestal con su medallón pintado (si existe la imagen) y un resplandor de su color.</summary>
        public static void GodShrine(Transform parent, Vector2 at, string god)
        {
            var g = Gods.Get(god);
            var root = Kit.Empty(parent, "Altar_" + god, at.X0Z());
            var stone = Mats.Toon(new Color(0.2f, 0.18f, 0.28f), null, 1f);
            Kit.Part(root, MeshGen.Cylinder, stone, new Vector3(0, 0.45f, 0), new Vector3(0.6f, 0.45f, 0.6f));
            Kit.Part(root, MeshGen.Cylinder, Mats.Toon(g.color * 0.6f, g.color * 0.25f, 1f), new Vector3(0, 0.92f, 0), new Vector3(0.7f, 0.04f, 0.7f));
            var icon = ArtIA.Tex(ArtIA.Iconos, god);
            Transform medal;
            if (icon != null)
                medal = Kit.Part(root, MeshGen.Quad, Mats.Unlit(Color.white, Mats.Blend.Alpha, icon, 1.15f, false), new Vector3(0, 1.75f, 0), Vector3.one * 1.1f, default, "medallon", false);
            else
                medal = Kit.Glow(root, g.color, 0.5f, new Vector3(0, 1.75f, 0), 2.5f);
            medal.gameObject.AddComponent<Billboard>();
            medal.gameObject.AddComponent<Floater>().amp = 0.12f;
            Kit.PointLight(root, g.color, 3.5f, 1.4f, new Vector3(0, 1.6f, 0.4f));
            for (int i = 0; i < 3; i++) Kit.Glow(root, g.color, 0.06f, new Vector3(Mathf.Cos(i * 2.1f) * 0.5f, 1.2f + i * 0.2f, Mathf.Sin(i * 2.1f) * 0.5f), 2.5f).gameObject.AddComponent<Floater>().amp = 0.2f;
        }

        /// <summary>Estantería con libros de colores y algún frasco brillante.</summary>
        public static void Bookshelf(Transform parent, Vector2 at, Vector2 facing)
        {
            var root = Kit.Empty(parent, "Estanteria", at.X0Z());
            root.rotation = U.LookFlat(facing);
            var wood = Mats.Toon(new Color(0.24f, 0.15f, 0.1f), null, 1f);
            Kit.Part(root, MeshGen.Cube, wood, new Vector3(0, 1.1f, -0.2f), new Vector3(2f, 2.2f, 0.12f));
            Kit.Part(root, MeshGen.Cube, wood, new Vector3(-0.95f, 1.1f, 0), new Vector3(0.1f, 2.2f, 0.5f));
            Kit.Part(root, MeshGen.Cube, wood, new Vector3(0.95f, 1.1f, 0), new Vector3(0.1f, 2.2f, 0.5f));
            var rng = new System.Random(Mathf.RoundToInt(at.x * 13 + at.y * 7));
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = 0.1f + shelf * 0.55f;
                Kit.Part(root, MeshGen.Cube, wood, new Vector3(0, y, 0), new Vector3(1.9f, 0.06f, 0.5f));
                float x = -0.85f;
                while (x < 0.8f)
                {
                    float w = 0.08f + (float)rng.NextDouble() * 0.08f, hh = 0.3f + (float)rng.NextDouble() * 0.15f;
                    if (rng.NextDouble() < 0.12)
                        Kit.Part(root, MeshGen.Cylinder, Mats.Unlit(Color.HSVToRGB((float)rng.NextDouble(), 0.6f, 1f), Mats.Blend.Additive, null, 2f, false), new Vector3(x + 0.06f, y + 0.12f, 0.05f), new Vector3(0.1f, 0.1f, 0.1f), default, "frasco", false);
                    else
                        Kit.Part(root, MeshGen.Cube, Mats.Toon(Color.HSVToRGB((float)rng.NextDouble(), 0.5f, 0.4f + (float)rng.NextDouble() * 0.2f), null, 0.6f), new Vector3(x + w * 0.5f, y + hh * 0.5f + 0.03f, 0.03f), new Vector3(w, hh, 0.32f), new Vector3(0, 0, rng.NextDouble() < 0.1 ? 15 : 0));
                    x += w + 0.015f;
                }
            }
            Arena.Current?.obstacles.Add(new Arena.Obstacle(at, 0.8f, true));
        }

        /// <summary>Objetos pequeños que no bloquean el paso, para que el suelo no se vea vacío.</summary>
        public static void Clutter(Transform parent, string biome, Vector2 at, System.Random rng)
        {
            var root = Kit.Empty(parent, "Detalle", at.X0Z(), new Vector3(0, rng.Range(0, 360f), 0));
            int pick = rng.Next(0, 4);
            switch (biome)
            {
                case "bosque":
                    if (pick == 0) Mushrooms(root, new Color(0.4f, 1f, 0.6f), 0.25f, rng, 3 + rng.Next(0, 3));
                    else if (pick == 1) Kit.Part(root, MeshGen.Tentacle(8, 6, 0.25f, 1f), T(new Color(0.25f, 0.17f, 0.1f)), Vector3.zero, new Vector3(0.8f, 1.6f, 0.8f), new Vector3(85, 0, 0));
                    else if (pick == 2) for (int i = 0; i < 5; i++) Kit.Part(root, MeshGen.Disc(), Mats.Toon(Color.Lerp(new Color(0.45f, 0.35f, 0.12f), new Color(0.2f, 0.4f, 0.15f), (float)rng.NextDouble()), null, 0f), new Vector3(rng.Range(-0.5f, 0.5f), 0.03f, rng.Range(-0.5f, 0.5f)), new Vector3(0.18f, 1, 0.12f), new Vector3(0, rng.Range(0, 360f), 0));
                    else Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 0), T(new Color(0.32f, 0.36f, 0.28f)), new Vector3(0, 0.1f, 0), Vector3.one * rng.Range(0.25f, 0.45f));
                    break;
                case "rlyeh":
                    if (pick == 0) Kit.Part(root, MeshGen.Disc(), Mats.Unlit(new Color(0.2f, 0.7f, 0.75f, 0.35f), Mats.Blend.Additive, Tex.SoftCircle, 1.2f, false), new Vector3(0, 0.03f, 0), new Vector3(rng.Range(0.6f, 1.2f), 1, rng.Range(0.4f, 0.9f)));
                    else if (pick == 1) for (int i = 0; i < 3; i++) Kit.Part(root, MeshGen.Tentacle(8, 5, 0.2f, 0.8f), Mats.Toon(new Color(0.2f, 0.5f, 0.3f), new Color(0.02f, 0.1f, 0.05f), 0.8f), new Vector3(i * 0.15f, 0, 0), new Vector3(0.6f, rng.Range(0.6f, 1.1f), 0.6f)).gameObject.AddComponent<Sway>();
                    else if (pick == 2) Coral(root, 0.15f, rng);
                    else for (int i = 0; i < 3; i++) Kit.Part(root, MeshGen.Cone(6), T(new Color(0.85f, 0.75f, 0.65f)), new Vector3(rng.Range(-0.3f, 0.3f), 0, rng.Range(-0.3f, 0.3f)), new Vector3(0.15f, 0.12f, 0.15f));
                    break;
                case "leng":
                    if (pick == 0)
                    {
                        var bone = T(new Color(0.92f, 0.88f, 0.78f));
                        for (int i = 0; i < 3; i++) Kit.Part(root, MeshGen.Capsule, bone, new Vector3(rng.Range(-0.3f, 0.3f), 0.04f, rng.Range(-0.3f, 0.3f)), new Vector3(0.06f, 0.2f, 0.06f), new Vector3(90, rng.Range(0, 360f), 0));
                        Kit.Part(root, MeshGen.Sphere, bone, new Vector3(0, 0.1f, 0.3f), new Vector3(0.22f, 0.2f, 0.25f));
                    }
                    else if (pick == 1) Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.25f, 1), T(new Color(0.92f, 0.95f, 1f)), new Vector3(0, 0.05f, 0), new Vector3(0.7f, 0.25f, 0.6f));
                    else if (pick == 2) Candles(root, Vector3.zero, 2);
                    else Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 0), T(new Color(0.48f, 0.4f, 0.3f)), new Vector3(0, 0.1f, 0), Vector3.one * rng.Range(0.25f, 0.45f));
                    break;
                case "azathoth":
                    if (pick == 0) for (int i = 0; i < 3; i++) Kit.Part(root, MeshGen.Crystal(5, 0.3f), Mats.Toon(new Color(0.45f, 0.15f, 0.6f), new Color(0.3f, 0.05f, 0.4f), 1f), new Vector3(i * 0.15f - 0.15f, 0, 0), new Vector3(0.18f, rng.Range(0.4f, 0.9f), 0.18f), new Vector3(rng.Range(-20f, 20f), 0, rng.Range(-20f, 20f)));
                    else if (pick == 1)
                    {
                        Kit.Part(root, MeshGen.Sphere, T(new Color(0.35f, 0.08f, 0.2f)), new Vector3(0, 0.12f, 0), new Vector3(0.45f, 0.25f, 0.45f));
                        Kit.Part(root, MeshGen.Sphere, Mats.Unlit(new Color(1f, 0.45f, 0.75f), Mats.Blend.Additive, null, 3f, false), new Vector3(0, 0.25f, 0.12f), Vector3.one * 0.1f, default, "ojo", false);
                    }
                    else if (pick == 2) Kit.Part(root, MeshGen.Tentacle(8, 6, 0.3f, 1f), T(new Color(0.3f, 0.08f, 0.22f)), Vector3.zero, new Vector3(0.7f, 1.3f, 0.7f)).gameObject.AddComponent<Sway>();
                    else Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.4f, 0), T(new Color(0.2f, 0.1f, 0.22f)), new Vector3(0, 0.1f, 0), Vector3.one * rng.Range(0.25f, 0.4f));
                    break;
                default: // el Umbral
                    if (pick == 0)
                    {
                        // pila de libros
                        for (int i = 0; i < 3; i++) Kit.Part(root, MeshGen.Cube, Mats.Toon(Color.HSVToRGB(0.05f + i * 0.25f, 0.5f, 0.4f), null, 0.6f), new Vector3(0, 0.05f + i * 0.09f, 0), new Vector3(0.45f, 0.08f, 0.32f), new Vector3(0, i * 15, 0));
                    }
                    else if (pick == 1) Kit.Part(root, MeshGen.Crystal(6, 0.3f), Mats.Toon(new Color(0.5f, 0.45f, 0.85f), new Color(0.2f, 0.18f, 0.45f), 1f), Vector3.zero, new Vector3(0.25f, 0.7f, 0.25f), new Vector3(10, 0, 8));
                    else if (pick == 2) Candles(root, Vector3.zero, 2);
                    else
                    {
                        var g = Kit.Glow(root, Color.HSVToRGB((float)rng.NextDouble(), 0.4f, 1f), 0.18f, new Vector3(0, 1.1f, 0), 2f);
                        g.gameObject.AddComponent<Floater>().amp = 0.25f;
                    }
                    break;
            }
        }
    }
}
