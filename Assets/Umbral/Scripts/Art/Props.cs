using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>Atrezo procedural por región: árboles retorcidos, monolitos de R'lyeh, piedras de Leng, carne y cristal de Azathoth.</summary>
    public static partial class Props
    {
        static Material T(Color c, Color? e = null, float outline = 1f) => Mats.Toon(c, e, outline);
        static Material G(Color c, float i = 3f) => Mats.Unlit(c, Mats.Blend.Additive, null, i, false);

        public static void Obstacle(Transform parent, BiomeDef b, Vector2 at, float r, System.Random rng)
        {
            var root = Kit.Empty(parent, "Obstaculo", at.X0Z());
            switch (b.id)
            {
                case "bosque":
                    {
                        // roble retorcido del Bosque Encantado: tronco nudoso, copa densa y hongos fosforescentes
                        var bark = T(new Color(0.28f, 0.2f, 0.14f));
                        var leaves = Mats.Toon(new Color(0.12f, 0.32f, 0.2f), new Color(0.01f, 0.05f, 0.03f), 1f, new Color(0.4f, 1f, 0.6f));
                        Kit.Part(root, MeshGen.Tentacle(8, 7, 0.32f, 0.5f), bark, Vector3.zero, new Vector3(r * 2.4f, 2.6f, r * 2.4f));
                        for (int i = 0; i < 3; i++)
                        {
                            float a = i * 2.1f + rng.Range(0, 1f);
                            Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.35f, 1), leaves, new Vector3(Mathf.Cos(a) * r * 0.7f, 2.5f + i * 0.25f, Mathf.Sin(a) * r * 0.7f), Vector3.one * (r * 1.2f + rng.Range(0.1f, 0.4f)));
                        }
                        for (int i = 0; i < 4; i++) Kit.Part(root, MeshGen.Sphere, G(new Color(0.5f, 1f, 0.6f), 2.5f), new Vector3(rng.Range(-r, r), rng.Range(2.2f, 3.2f), rng.Range(-r, r)), Vector3.one * 0.12f, default, "luz", false);
                        Mushrooms(root, new Color(0.4f, 1f, 0.6f), r, rng, 4);
                        break;
                    }
                case "rlyeh":
                    {
                        // monolito inclinado de piedra verdosa con ángulos imposibles
                        var stone = T(new Color(0.18f, 0.32f, 0.3f), new Color(0.0f, 0.04f, 0.04f));
                        Kit.Part(root, MeshGen.Cube, stone, new Vector3(0, 1.1f, 0), new Vector3(r * 1.5f, 2.2f, r * 1.2f), new Vector3(rng.Range(-14f, 14f), rng.Range(0, 90f), rng.Range(-14f, 14f)));
                        Kit.Part(root, MeshGen.Cube, stone, new Vector3(0, 2.3f, 0), new Vector3(r * 1.1f, 0.5f, r * 1.6f), new Vector3(rng.Range(-25f, 25f), rng.Range(0, 90f), 0));
                        Kit.Part(root, MeshGen.Ring(0.8f), G(b.veins, 2f), new Vector3(0, 1.8f, r * 0.62f), Vector3.one * r * 0.5f, new Vector3(90, 0, 0), "runa", false);
                        Coral(root, r, rng);
                        break;
                    }
                case "leng":
                    {
                        var stone = T(new Color(0.45f, 0.4f, 0.35f));
                        Kit.Part(root, MeshGen.Crystal(5, 0.15f), stone, Vector3.zero, new Vector3(r * 1.7f, 2.4f, r * 1.7f), new Vector3(0, rng.Range(0, 90f), rng.Range(-6f, 6f)));
                        Kit.Part(root, MeshGen.Cube, T(new Color(0.85f, 0.7f, 0.25f)), new Vector3(0, 1.5f, 0), new Vector3(r * 1.75f, 0.25f, r * 1.75f), new Vector3(0, 45, 0));
                        Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 0), T(new Color(0.9f, 0.92f, 0.95f)), new Vector3(0, 2.3f, 0), new Vector3(r, 0.4f, r));
                        break;
                    }
                default:
                    {
                        // pilar de carne pulsante con ojos / cristal del caos
                        var flesh = T(new Color(0.35f, 0.1f, 0.25f), new Color(0.08f, 0.0f, 0.05f));
                        var pil = Kit.Part(root, MeshGen.Tentacle(10, 9, 0.45f, 0.4f), flesh, Vector3.zero, new Vector3(r * 2.2f, 2.6f, r * 2.2f));
                        pil.gameObject.AddComponent<Pulse>();
                        for (int i = 0; i < 4; i++)
                            Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.4f, 0.7f), 3f), new Vector3(Mathf.Cos(i * 1.7f) * r * 0.45f, 0.8f + i * 0.55f, Mathf.Sin(i * 1.7f) * r * 0.45f), Vector3.one * 0.18f, default, "ojo", false);
                        Kit.Part(root, MeshGen.Crystal(6, 0.3f), T(new Color(0.5f, 0.2f, 0.7f), new Color(0.3f, 0.05f, 0.4f)), new Vector3(r * 0.6f, 0, 0), new Vector3(0.5f, 1.8f, 0.5f), new Vector3(0, 0, -20));
                        break;
                    }
            }
        }

        public static void Pit(Transform parent, BiomeDef b, Vector2 at, float r)
        {
            // fosa al vacío: disco negro con borde brillante
            Fx.MeshObj("Fosa", MeshGen.Disc(), Mats.Unlit(new Color(0.01f, 0.0f, 0.02f, 1f), Mats.Blend.Alpha, null, 1f, false), at.X0Z(0.03f), Quaternion.identity, Vector3.one * r).transform.SetParent(parent, true);
            Fx.MeshObj("FosaBorde", MeshGen.Ring(0.9f), Mats.Unlit(b.veins.WithA(0.7f), Mats.Blend.Additive, null, 1.6f, false), at.X0Z(0.04f), Quaternion.identity, Vector3.one * r).transform.SetParent(parent, true);
            Fx.MeshObj("FosaFondo", MeshGen.Quad, Mats.Unlit(b.voidC.WithA(0.25f), Mats.Blend.Additive, Tex.Swirl, 1.2f, false), at.X0Z(0.035f), Quaternion.Euler(90, 0, 0), Vector3.one * r * 1.8f).transform.SetParent(parent, true);
        }

        public static void Mushrooms(Transform root, Color c, float r, System.Random rng, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = rng.Range(0, Mathf.PI * 2);
                var m = Kit.Empty(root, "hongo", new Vector3(Mathf.Cos(a) * r * 1.1f, 0, Mathf.Sin(a) * r * 1.1f));
                float s = rng.Range(0.2f, 0.45f);
                Kit.Part(m, MeshGen.Cylinder, T(new Color(0.85f, 0.85f, 0.75f)), new Vector3(0, s * 0.5f, 0), new Vector3(s * 0.25f, s * 0.5f, s * 0.25f));
                Kit.Part(m, MeshGen.Sphere, Mats.Toon(c * 0.6f, c * 0.8f, 0.8f), new Vector3(0, s, 0), new Vector3(s, s * 0.45f, s));
            }
        }

        public static void Coral(Transform root, float r, System.Random rng)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = rng.Range(0, Mathf.PI * 2);
                var c = Kit.Empty(root, "coral", new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), new Vector3(0, rng.Range(0, 360f), 0));
                Kit.Part(c, MeshGen.Tentacle(6, 5, 0.3f, 0.9f), Mats.Toon(new Color(0.9f, 0.4f, 0.45f), new Color(0.2f, 0.02f, 0.05f), 0.8f), Vector3.zero, Vector3.one * rng.Range(0.6f, 1.1f), new Vector3(rng.Range(-20f, 20f), 0, rng.Range(-20f, 20f)));
            }
        }

        /// <summary>Decoración de borde (fuera del área jugable).</summary>
        public static void EdgeDecor(Transform parent, BiomeDef b, Vector2 at, System.Random rng, bool low)
        {
            var root = Kit.Empty(parent, "Borde", at.X0Z(), new Vector3(0, rng.Range(0, 360f), 0));
            float s = low ? rng.Range(0.4f, 0.8f) : rng.Range(0.7f, 1.6f);
            int pick = rng.Next(0, 3);
            switch (b.id)
            {
                case "bosque":
                    if (pick == 0 && !low)
                    {
                        var bark = T(new Color(0.2f, 0.14f, 0.1f));
                        Kit.Part(root, MeshGen.Tentacle(10, 7, 0.35f, 1.2f), bark, Vector3.zero, new Vector3(2.4f, 5f * s, 2.4f), new Vector3(0, 0, rng.Range(-12f, 12f)));
                        Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.4f, 1), Mats.Toon(new Color(0.12f, 0.3f, 0.18f), new Color(0.02f, 0.08f, 0.04f), 1f), new Vector3(0.3f, 4.6f * s, 0), new Vector3(2.2f, 1.4f, 2.2f) * s);
                    }
                    else if (pick == 1) Mushrooms(root, new Color(0.4f, 1f, 0.6f), 0.3f, rng, 3);
                    else Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 1), T(new Color(0.3f, 0.32f, 0.26f)), new Vector3(0, 0.2f, 0), new Vector3(1.2f, 0.7f, 1f) * s);
                    break;
                case "rlyeh":
                    if (pick == 0 && !low) Kit.Part(root, MeshGen.Cube, T(new Color(0.16f, 0.3f, 0.28f)), new Vector3(0, 1.5f * s, 0), new Vector3(1f, 3f, 1.2f) * s, new Vector3(rng.Range(-25f, 25f), 0, rng.Range(-25f, 25f)));
                    else if (pick == 1) Coral(root, 0.3f, rng);
                    else Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 1), T(new Color(0.2f, 0.3f, 0.3f)), new Vector3(0, 0.2f, 0), new Vector3(1.2f, 0.6f, 1f) * s);
                    break;
                case "leng":
                    if (pick == 0 && !low) Kit.Part(root, MeshGen.Crystal(4, 0.2f), T(new Color(0.48f, 0.42f, 0.36f)), Vector3.zero, new Vector3(0.9f, 3.2f, 0.9f) * s, new Vector3(0, 0, rng.Range(-8f, 8f)));
                    else if (pick == 1) Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.25f, 1), T(new Color(0.92f, 0.94f, 0.98f)), new Vector3(0, 0.1f, 0), new Vector3(1.1f, 0.5f, 1.1f) * s);
                    else Kit.Part(root, MeshGen.Rock(rng.Next(99), 0.3f, 1), T(new Color(0.45f, 0.38f, 0.3f)), new Vector3(0, 0.2f, 0), new Vector3(1.3f, 0.8f, 1f) * s);
                    break;
                default:
                    if (pick == 0 && !low)
                    {
                        var t = Kit.Part(root, MeshGen.Tentacle(10, 7, 0.35f, 1f), T(new Color(0.3f, 0.08f, 0.22f), new Color(0.06f, 0, 0.04f)), Vector3.zero, new Vector3(1.6f, 3.5f * s, 1.6f), new Vector3(rng.Range(-15f, 15f), 0, rng.Range(-15f, 15f)));
                        t.gameObject.AddComponent<Sway>();
                    }
                    else if (pick == 1) Kit.Part(root, MeshGen.Crystal(5, 0.3f), T(new Color(0.45f, 0.15f, 0.6f), new Color(0.35f, 0.05f, 0.5f)), Vector3.zero, new Vector3(0.6f, 2f, 0.6f) * s, new Vector3(rng.Range(-20f, 20f), 0, rng.Range(-20f, 20f)));
                    else Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.5f, 0.8f), 2.5f), new Vector3(0, 0.6f + rng.Range(0, 1.5f), 0), Vector3.one * 0.3f * s, default, "ojo", false).gameObject.AddComponent<Floater>();
                    break;
            }
        }

        public static void LightSource(Transform parent, BiomeDef b, Vector2 at, System.Random rng)
        {
            var root = Kit.Empty(parent, "Luz", at.X0Z());
            Color c = b.veins;
            switch (b.id)
            {
                case "leng":
                    // brasero con fuego
                    Kit.Part(root, MeshGen.Cylinder, T(new Color(0.25f, 0.2f, 0.15f)), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 0.5f));
                    Kit.Part(root, MeshGen.Cone(8), G(new Color(1f, 0.55f, 0.15f), 3.5f), new Vector3(0, 1.05f, 0), new Vector3(0.45f, 0.8f, 0.45f), default, "fuego", false).gameObject.AddComponent<Flicker>();
                    c = new Color(1f, 0.6f, 0.25f);
                    break;
                case "rlyeh":
                    Kit.Part(root, MeshGen.Crystal(6, 0.4f), Mats.Toon(new Color(0.2f, 0.6f, 0.65f), new Color(0.1f, 0.6f, 0.7f), 1f), Vector3.zero, new Vector3(0.5f, 1.6f, 0.5f));
                    break;
                case "azathoth":
                    Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.4f, 0.8f), 3f), new Vector3(0, 1.6f, 0), Vector3.one * 0.5f, default, "orbe", false).gameObject.AddComponent<Floater>();
                    break;
                default:
                    Mushrooms(root, new Color(0.4f, 1f, 0.6f), 0.15f, rng, 3);
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.2f, 0.6f, 0.4f), new Color(0.4f, 1.2f, 0.7f), 0.8f), new Vector3(0, 0.6f, 0), new Vector3(0.9f, 0.4f, 0.9f));
                    break;
            }
            var l = Kit.PointLight(root, c, 7f, 2.2f, new Vector3(0, 1.5f, 0));
            if (b.id == "leng") l.gameObject.AddComponent<Flicker>();
        }

        /// <summary>Siluetas gigantes en el fondo del abismo (como los fondos pintados de Hades).</summary>
        public static void Backdrop(Transform parent, BiomeDef b, System.Random rng)
        {
            Vector2 up = Vector2.up.Rotate(-(CameraRig.Instance != null ? CameraRig.Instance.yaw : 45f));
            Vector2 c = up * 45f;
            switch (b.id)
            {
                case "bosque":
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 p = c + new Vector2(up.y, -up.x) * (i - 3) * 12f + up * rng.Range(-6f, 6f);
                        var t = Kit.Part(parent, MeshGen.Tentacle(12, 8, 0.3f, 1.5f), Mats.Toon(new Color(0.05f, 0.1f, 0.08f), new Color(0.02f, 0.06f, 0.04f), 0f), p.X0Z(-25f), new Vector3(10, 45, 10), new Vector3(0, rng.Range(0, 360f), 0), "arbol_lejano", false);
                    }
                    break;
                case "rlyeh":
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 p = c + new Vector2(up.y, -up.x) * (i - 2.5f) * 14f;
                        Kit.Part(parent, MeshGen.Cube, Mats.Toon(new Color(0.05f, 0.12f, 0.12f), new Color(0.0f, 0.05f, 0.06f), 0f), p.X0Z(-20f), new Vector3(8, 40, 8), new Vector3(rng.Range(-20f, 20f), rng.Range(0, 90f), rng.Range(-20f, 20f)), "monolito", false);
                    }
                    // la silueta durmiente
                    Kit.Part(parent, MeshGen.Sphere, Mats.Toon(new Color(0.03f, 0.08f, 0.07f), new Color(0.02f, 0.05f, 0.05f), 0f), (c * 1.4f).X0Z(-30f), new Vector3(30, 40, 30), default, "cthulhu", false);
                    Kit.Part(parent, MeshGen.Sphere, G(new Color(0.3f, 1f, 0.8f), 1.5f), (c * 1.4f - up * 5).X0Z(-10f), new Vector3(3, 1, 3), default, "ojo", false);
                    break;
                case "leng":
                    for (int i = 0; i < 9; i++)
                    {
                        Vector2 p = c + new Vector2(up.y, -up.x) * (i - 4) * 10f + up * rng.Range(0f, 10f);
                        Kit.Part(parent, MeshGen.Cone(5), Mats.Toon(new Color(0.15f, 0.1f, 0.08f), null, 0f), p.X0Z(-30f), new Vector3(16, rng.Range(25f, 45f), 16), new Vector3(0, rng.Range(0, 90f), 0), "montaña", false);
                    }
                    // el monasterio sin ventanas
                    Kit.Part(parent, MeshGen.Cube, Mats.Toon(new Color(0.1f, 0.07f, 0.05f), new Color(0.15f, 0.1f, 0f), 0f), (c * 1.2f).X0Z(-5f), new Vector3(10, 18, 10), new Vector3(0, 45, 0), "monasterio", false);
                    break;
                default:
                    // Azathoth: un sol de caos palpitante
                    var sunP = Kit.Part(parent, MeshGen.Sphere, Mats.Unlit(new Color(1f, 0.35f, 0.6f), Mats.Blend.Additive, Tex.SoftCircle, 1.5f, false), (c * 1.3f).X0Z(-35f), Vector3.one * 50f, default, "azathoth", false);
                    sunP.gameObject.AddComponent<Pulse>().amount = 0.08f;
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * 36f;
                        Vector2 p = c * 1.3f + U.Dir(a) * 30f;
                        var t = Kit.Part(parent, MeshGen.Tentacle(12, 7, 0.25f, 1.5f), Mats.Toon(new Color(0.08f, 0.02f, 0.08f), null, 0f), p.X0Z(-30f), new Vector3(8, 30, 8), new Vector3(0, -a, 70), "brazo", false);
                        t.gameObject.AddComponent<Sway>().amount = 6f;
                    }
                    break;
            }
        }

        /// <summary>Tentáculo/columna que brota del suelo (boones de erupción, ataques de jefes).</summary>
        public static void SpawnEruptionVisual(string kind, Vector2 at, float radius)
        {
            if (World.Root == null) return;
            Color c; Material mat;
            switch (kind)
            {
                case "cthulhu": c = new Color(0.2f, 0.85f, 0.7f); mat = Mats.Toon(new Color(0.15f, 0.45f, 0.4f), new Color(0.05f, 0.25f, 0.2f)); break;
                case "cthugha": c = new Color(1f, 0.5f, 0.1f); mat = Mats.Unlit(c, Mats.Blend.Additive, null, 3f, false); break;
                case "hastur": c = new Color(1f, 0.85f, 0.2f); mat = Mats.Toon(new Color(0.9f, 0.75f, 0.2f), new Color(0.3f, 0.25f, 0f)); break;
                case "nyar": c = new Color(1f, 0.2f, 0.3f); mat = Mats.Toon(new Color(0.08f, 0.02f, 0.06f), new Color(0.2f, 0f, 0.05f)); break;
                default: c = new Color(0.6f, 0.4f, 0.8f); mat = Mats.Toon(new Color(0.2f, 0.12f, 0.25f), new Color(0.05f, 0.02f, 0.08f)); break;
            }
            var go = new GameObject("Erupcion");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z();
            int n = kind == "cthugha" ? 1 : 3;
            for (int i = 0; i < n; i++)
            {
                var piv = Kit.Empty(go.transform, "t", new Vector3(Random.Range(-0.4f, 0.4f) * radius, 0, Random.Range(-0.4f, 0.4f) * radius), new Vector3(Random.Range(-15f, 15f), Random.Range(0, 360f), 0));
                if (kind == "cthugha") Kit.Part(piv, MeshGen.Cone(10), mat, Vector3.zero, new Vector3(radius * 1.2f, radius * 2.5f, radius * 1.2f));
                else Kit.Part(piv, MeshGen.Tentacle(10, 7, 0.3f, 0.6f), mat, Vector3.zero, new Vector3(radius * 1.2f, radius * 2.2f, radius * 1.2f));
            }
            Fx.Burst(at.X0Z(0.3f), c, 14, 6f, 0.3f, 0.5f);
            Runner.Run(Rise(go.transform));
        }

        static IEnumerator Rise(Transform t)
        {
            float k = 0;
            while (k < 1 && t != null) { k += Time.deltaTime / 0.15f; t.localScale = new Vector3(1, U.EaseOutBack(Mathf.Clamp01(k)), 1); yield return null; }
            yield return new WaitForSeconds(0.35f);
            k = 0;
            while (k < 1 && t != null) { k += Time.deltaTime / 0.3f; t.localScale = new Vector3(1, 1 - k, 1); yield return null; }
            if (t != null) Object.Destroy(t.gameObject);
        }

        /// <summary>Emblema 3D de recompensa (encima de puertas y en las recompensas).</summary>
        public static Transform Emblem(Transform parent, RewardType r, string god, Vector3 localPos, float scale = 1f)
        {
            var root = Kit.Empty(parent, "Emblema", localPos);
            root.localScale = Vector3.one * scale;
            Color c = Rewards.ColorOf(r, god);
            switch (r)
            {
                case RewardType.Boon:
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(c * 0.6f, c * 0.7f, 1f), Vector3.zero, Vector3.one * 0.7f);
                    Kit.Part(root, MeshGen.Ring(0.82f), G(c, 3f), Vector3.zero, Vector3.one * 0.6f, new Vector3(90, 0, 0), "anillo", false);
                    Kit.Part(root, MeshGen.Ring(0.82f), G(c, 3f), Vector3.zero, Vector3.one * 0.55f, new Vector3(0, 0, 0), "anillo2", false);
                    break;
                case RewardType.Pom:
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.25f, 0.05f, 0.25f), new Color(0.5f, 0.1f, 0.5f), 1f), Vector3.zero, Vector3.one * 0.7f);
                    Kit.Part(root, MeshGen.Cone(5), T(new Color(0.2f, 0.5f, 0.2f)), new Vector3(0, 0.3f, 0), new Vector3(0.2f, 0.25f, 0.2f));
                    break;
                case RewardType.Hammer:
                    Kit.Part(root, MeshGen.Crystal(5, 0.3f), Mats.Toon(new Color(0.55f, 0.6f, 0.75f), new Color(0.2f, 0.3f, 0.5f), 1f), new Vector3(0, -0.4f, 0), new Vector3(0.45f, 0.9f, 0.3f), new Vector3(0, 0, 15));
                    break;
                case RewardType.Heart:
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.75f, 0.1f, 0.15f), new Color(0.4f, 0.0f, 0.05f), 1f), new Vector3(0.17f, 0.1f, 0), Vector3.one * 0.42f);
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.75f, 0.1f, 0.15f), new Color(0.4f, 0.0f, 0.05f), 1f), new Vector3(-0.17f, 0.1f, 0), Vector3.one * 0.42f);
                    Kit.Part(root, MeshGen.Cone(4), Mats.Toon(new Color(0.75f, 0.1f, 0.15f), new Color(0.4f, 0.0f, 0.05f), 1f), new Vector3(0, -0.05f, 0), new Vector3(0.6f, 0.45f, 0.35f), new Vector3(180, 0, 0));
                    break;
                case RewardType.Gold:
                    for (int i = 0; i < 4; i++) Kit.Part(root, MeshGen.Cylinder, Mats.Toon(new Color(1f, 0.8f, 0.3f), new Color(0.4f, 0.3f, 0.05f), 1f), new Vector3(0, -0.3f + i * 0.13f, 0), new Vector3(0.45f, 0.05f, 0.45f));
                    break;
                case RewardType.Shop:
                    Kit.Part(root, MeshGen.Cube, T(new Color(0.15f, 0.1f, 0.08f)), Vector3.zero, new Vector3(0.4f, 0.55f, 0.4f));
                    Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.7f, 0.3f), 3f), Vector3.zero, Vector3.one * 0.3f, default, "llama", false);
                    break;
                case RewardType.Fountain:
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.3f, 0.7f, 1f), new Color(0.2f, 0.5f, 0.8f), 1f), Vector3.zero, new Vector3(0.45f, 0.6f, 0.45f));
                    break;
                case RewardType.Boss:
                case RewardType.Exit:
                    Kit.Part(root, MeshGen.Sphere, T(new Color(0.9f, 0.88f, 0.8f)), Vector3.zero, new Vector3(0.6f, 0.55f, 0.55f));
                    Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.2f, 0.2f), 4f), new Vector3(0.12f, 0.05f, 0.25f), Vector3.one * 0.12f, default, "ojo", false);
                    Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.2f, 0.2f), 4f), new Vector3(-0.12f, 0.05f, 0.25f), Vector3.one * 0.12f, default, "ojo", false);
                    break;
                case RewardType.Wilbur:
                    Kit.Part(root, MeshGen.Sphere, T(new Color(0.65f, 0.6f, 0.5f)), Vector3.zero, Vector3.one * 0.5f);
                    Kit.Part(root, MeshGen.Cone(6), T(new Color(0.15f, 0.12f, 0.1f)), new Vector3(0.15f, 0.25f, 0), new Vector3(0.1f, 0.3f, 0.1f), new Vector3(0, 0, -25));
                    Kit.Part(root, MeshGen.Cone(6), T(new Color(0.15f, 0.12f, 0.1f)), new Vector3(-0.15f, 0.25f, 0), new Vector3(0.1f, 0.3f, 0.1f), new Vector3(0, 0, 25));
                    break;
                case RewardType.Hex:
                    for (int i = 0; i < 4; i++) Kit.Part(root, MeshGen.Sphere, G(Color.HSVToRGB(i * 0.25f, 0.6f, 1f), 3f), new Vector3(Mathf.Cos(i * 1.57f) * 0.25f, Mathf.Sin(i * 1.57f) * 0.25f, 0), Vector3.one * 0.28f, default, "globo", false);
                    break;
                case RewardType.Cat:
                    Kit.Part(root, MeshGen.Sphere, T(new Color(0.08f, 0.07f, 0.1f)), Vector3.zero, Vector3.one * 0.55f);
                    Kit.Part(root, MeshGen.Cone(4), T(new Color(0.08f, 0.07f, 0.1f)), new Vector3(0.14f, 0.3f, 0), new Vector3(0.15f, 0.25f, 0.15f));
                    Kit.Part(root, MeshGen.Cone(4), T(new Color(0.08f, 0.07f, 0.1f)), new Vector3(-0.14f, 0.3f, 0), new Vector3(0.15f, 0.25f, 0.15f));
                    Kit.Part(root, MeshGen.Sphere, G(new Color(0.6f, 1f, 0.4f), 4f), new Vector3(0.1f, 0.05f, 0.25f), Vector3.one * 0.08f, default, "ojo", false);
                    Kit.Part(root, MeshGen.Sphere, G(new Color(0.6f, 1f, 0.4f), 4f), new Vector3(-0.1f, 0.05f, 0.25f), Vector3.one * 0.08f, default, "ojo", false);
                    break;
                case RewardType.Chaos:
                    Kit.Part(root, MeshGen.Sphere, Mats.Toon(new Color(0.1f, 0.0f, 0.1f), new Color(0.3f, 0.0f, 0.3f), 1f), Vector3.zero, Vector3.one * 0.7f);
                    Kit.Part(root, MeshGen.Sphere, G(new Color(1f, 0.3f, 0.8f), 4f), new Vector3(0, 0, 0.3f), new Vector3(0.25f, 0.4f, 0.1f), default, "ojo", false);
                    break;
                default: // recursos
                    Kit.Part(root, MeshGen.Crystal(6, 0.35f), Mats.Toon(c * 0.6f, c * 0.6f, 1f), new Vector3(0, -0.4f, 0), new Vector3(0.45f, 0.8f, 0.45f));
                    break;
            }
            root.gameObject.AddComponent<Spin>();
            Kit.Part(root, MeshGen.Quad, Mats.Unlit(c.WithA(0.6f), Mats.Blend.Additive, Tex.SoftCircle, 1.6f, false), Vector3.zero, Vector3.one * 1.8f, default, "halo", false).gameObject.AddComponent<Billboard>();
            return root;
        }
    }

    public class Spin : MonoBehaviour
    {
        public float speed = 70f;
        Vector3 basePos;
        void Start() { basePos = transform.localPosition; }
        void Update()
        {
            transform.Rotate(0, speed * Time.deltaTime, 0, Space.World);
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.12f;
        }
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    public class Pulse : MonoBehaviour
    {
        public float amount = 0.05f, speed = 2f;
        Vector3 s0; float ph;
        void Start() { s0 = transform.localScale; ph = Random.value * 6f; }
        void Update() { transform.localScale = s0 * (1f + Mathf.Sin(Time.time * speed + ph) * amount); }
    }

    public class Sway : MonoBehaviour
    {
        public float amount = 8f, speed = 1.2f;
        Quaternion r0; float ph;
        void Start() { r0 = transform.localRotation; ph = Random.value * 6f; }
        void Update() { transform.localRotation = r0 * Quaternion.Euler(Mathf.Sin(Time.time * speed + ph) * amount, 0, Mathf.Cos(Time.time * speed * 0.8f + ph) * amount); }
    }

    public class Flicker : MonoBehaviour
    {
        Light l; float i0; Vector3 s0; float seed;
        void Start() { l = GetComponent<Light>(); if (l != null) i0 = l.intensity; s0 = transform.localScale; seed = Random.value * 100f; }
        void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * 6f, seed);
            if (l != null) l.intensity = i0 * (0.7f + 0.6f * n);
            else transform.localScale = s0 * (0.85f + 0.3f * n);
        }
    }
}
