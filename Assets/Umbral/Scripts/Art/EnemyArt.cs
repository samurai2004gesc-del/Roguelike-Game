using UnityEngine;

namespace Umbral
{
    /// <summary>Modelos de enemigos y jefes por región.</summary>
    public static class EnemyArt
    {
        static Material T(float r, float g, float b, float er = 0, float eg = 0, float eb = 0) => Mats.Toon(new Color(r, g, b), new Color(er, eg, eb), 1f);
        static Material Glow(Color c, float i = 3.5f) => Mats.Unlit(c, Mats.Blend.Additive, null, i, false);
        static Transform P(Transform p, Mesh m, Material mat, float x, float y, float z, float sx, float sy, float sz, float rx = 0, float ry = 0, float rz = 0)
            => Kit.Part(p, m, mat, new Vector3(x, y, z), new Vector3(sx, sy, sz), new Vector3(rx, ry, rz));
        static void Eye(Transform p, Color c, float x, float y, float z, float s) => Kit.Part(p, MeshGen.Sphere, Glow(c, 4f), new Vector3(x, y, z), Vector3.one * s, default, "eye", false);

        static void Wing(ModelRig rig, Transform b, Material mat, float side, Vector3 at, float size)
        {
            var piv = Kit.Empty(b, "wing", at, new Vector3(0, 0, 0));
            var w = Kit.Part(piv, MeshGen.Sector(100, 6), mat, new Vector3(side * size * 0.5f, 0, -size * 0.1f), new Vector3(size, 1, size), new Vector3(0, side * 90 + 180, 0));
            rig.wings.Add(piv);
        }

        // ====================== BOSQUE ENCANTADO ======================
        public static ModelRig Ghoul(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.5f, out var b);
            var skin = T(0.45f, 0.52f, 0.42f);
            var dark = T(0.25f, 0.3f, 0.22f);
            P(b, MeshGen.Capsule, dark, 0.13f, 0.4f, 0, 0.17f, 0.4f, 0.17f, 0, 0, 0);
            P(b, MeshGen.Capsule, dark, -0.13f, 0.4f, 0, 0.17f, 0.4f, 0.17f);
            P(b, MeshGen.Capsule, skin, 0, 1.05f, 0.1f, 0.5f, 0.45f, 0.38f, 35, 0, 0);
            var head = P(b, MeshGen.Sphere, skin, 0, 1.38f, 0.42f, 0.36f, 0.32f, 0.42f);
            P(b, MeshGen.Sphere, skin, 0, 1.3f, 0.65f, 0.2f, 0.16f, 0.3f);
            P(b, MeshGen.Cone(5), skin, 0.14f, 1.55f, 0.36f, 0.1f, 0.2f, 0.1f, -20, 0, -20);
            P(b, MeshGen.Cone(5), skin, -0.14f, 1.55f, 0.36f, 0.1f, 0.2f, 0.1f, -20, 0, 20);
            Eye(b, new Color(1f, 0.85f, 0.2f), 0.09f, 1.42f, 0.6f, 0.07f);
            Eye(b, new Color(1f, 0.85f, 0.2f), -0.09f, 1.42f, 0.6f, 0.07f);
            rig.weaponPivot = Kit.Empty(b, "arms", new Vector3(0, 1.1f, 0.2f));
            P(rig.weaponPivot, MeshGen.Capsule, skin, 0.35f, -0.15f, 0.25f, 0.13f, 0.45f, 0.13f, 60, 0, 10);
            P(rig.weaponPivot, MeshGen.Capsule, skin, -0.35f, -0.15f, 0.25f, 0.13f, 0.45f, 0.13f, 60, 0, -10);
            for (int i = 0; i < 3; i++)
            {
                P(rig.weaponPivot, MeshGen.Cone(4), T(0.9f, 0.88f, 0.8f), 0.38f + i * 0.05f - 0.05f, -0.4f, 0.65f, 0.04f, 0.18f, 0.04f, 80, 0, 0);
                P(rig.weaponPivot, MeshGen.Cone(4), T(0.9f, 0.88f, 0.8f), -0.38f - i * 0.05f + 0.05f, -0.4f, 0.65f, 0.04f, 0.18f, 0.04f, 80, 0, 0);
            }
            rig.Init();
            return rig;
        }

        public static ModelRig Nightgaunt(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.5f, out var b);
            var black = Mats.Toon(new Color(0.06f, 0.05f, 0.08f), null, 1f, new Color(0.4f, 0.4f, 0.6f));
            P(b, MeshGen.Capsule, black, 0, 1.4f, 0, 0.35f, 0.5f, 0.28f, 15, 0, 0);
            P(b, MeshGen.Sphere, black, 0, 1.95f, 0.1f, 0.3f, 0.36f, 0.3f);
            P(b, MeshGen.Cone(6), black, 0.12f, 2.1f, 0.05f, 0.07f, 0.35f, 0.07f, -30, 0, 30);
            P(b, MeshGen.Cone(6), black, -0.12f, 2.1f, 0.05f, 0.07f, 0.35f, 0.07f, -30, 0, -30);
            P(b, MeshGen.Capsule, black, 0.12f, 0.8f, 0, 0.1f, 0.4f, 0.1f, 20, 0, 0);
            P(b, MeshGen.Capsule, black, -0.12f, 0.8f, 0, 0.1f, 0.4f, 0.1f, 20, 0, 0);
            Wing(rig, b, black, 1, new Vector3(0.2f, 1.6f, -0.1f), 1.5f);
            Wing(rig, b, black, -1, new Vector3(-0.2f, 1.6f, -0.1f), 1.5f);
            var tail = Kit.Empty(b, "tail", new Vector3(0, 1.0f, -0.2f));
            P(tail, MeshGen.Tentacle(10, 5, 0.12f, 0.8f), black, 0, 0, 0, 1.2f, 1.2f, 1.2f, -120, 0, 0);
            rig.tentacles.Add(tail);
            rig.floats = true; rig.floatHeight = 0.5f;
            rig.Init();
            return rig;
        }

        public static ModelRig Gug(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 1.1f, out var b, 1.9f);
            var fur = T(0.32f, 0.26f, 0.24f);
            P(b, MeshGen.Capsule, fur, 0.25f, 0.45f, 0, 0.3f, 0.45f, 0.3f);
            P(b, MeshGen.Capsule, fur, -0.25f, 0.45f, 0, 0.3f, 0.45f, 0.3f);
            P(b, MeshGen.Rock(4, 0.15f, 1), fur, 0, 1.35f, 0, 0.8f, 0.9f, 0.65f);
            // la boca vertical, de oreja a... cabeza
            P(b, MeshGen.Cube, T(0.15f, 0.02f, 0.04f, 0.3f, 0, 0.05f), 0, 1.55f, 0.55f, 0.12f, 0.8f, 0.1f);
            for (int i = 0; i < 6; i++)
            {
                P(b, MeshGen.Cone(4), T(0.95f, 0.92f, 0.85f), 0.06f, 1.25f + i * 0.12f, 0.6f, 0.05f, 0.1f, 0.05f, 0, 0, 90);
                P(b, MeshGen.Cone(4), T(0.95f, 0.92f, 0.85f), -0.06f, 1.25f + i * 0.12f, 0.6f, 0.05f, 0.1f, 0.05f, 0, 0, -90);
            }
            Eye(b, new Color(1f, 0.45f, 0.6f), 0.55f, 1.9f, 0.3f, 0.12f);
            Eye(b, new Color(1f, 0.45f, 0.6f), -0.55f, 1.9f, 0.3f, 0.12f);
            rig.weaponPivot = Kit.Empty(b, "arms", new Vector3(0, 1.5f, 0));
            for (int s = -1; s <= 1; s += 2)
            {
                P(rig.weaponPivot, MeshGen.Capsule, fur, s * 0.75f, -0.1f, 0.1f, 0.22f, 0.45f, 0.22f, 20, 0, s * 30);
                P(rig.weaponPivot, MeshGen.Capsule, fur, s * 0.95f, -0.55f, 0.35f, 0.16f, 0.35f, 0.16f, 50, 0, s * 10);
                P(rig.weaponPivot, MeshGen.Capsule, fur, s * 0.7f, -0.6f, 0.45f, 0.16f, 0.35f, 0.16f, 50, 0, -s * 10);
            }
            rig.bobAmount = 0.08f; rig.bobSpeed = 4f;
            rig.Init();
            return rig;
        }

        public static ModelRig Pickman(Transform parent)
        {
            var rig = Ghoul(parent);
            rig.transform.localScale = Vector3.one * 1.6f;
            var b = rig.body;
            P(b, MeshGen.Robe(12, 0.25f, 0.05f), T(0.82f, 0.78f, 0.68f), 0, 0.55f, 0.05f, 0.75f, 0.75f, 0.6f, 25, 0, 0);
            // manchas de pintura
            for (int i = 0; i < 6; i++) Kit.Part(b, MeshGen.Sphere, Glow(Color.HSVToRGB(Random.value, 0.8f, 1f), 1.6f), new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.7f, 1.2f), 0.35f), Vector3.one * 0.08f, default, "paint", false);
            P(b, MeshGen.Cylinder, T(0.5f, 0.1f, 0.15f), 0, 1.6f, 0.38f, 0.42f, 0.05f, 0.42f, -15, 0, 10);
            // paleta y pincel
            var pal = Kit.Part(rig.weaponPivot, MeshGen.Disc(), T(0.55f, 0.38f, 0.22f), new Vector3(-0.55f, -0.3f, 0.5f), new Vector3(0.35f, 1f, 0.28f), new Vector3(-60, 0, 0));
            for (int i = 0; i < 4; i++) Kit.Part(pal, MeshGen.Sphere, Glow(Color.HSVToRGB(i * 0.25f, 0.9f, 1f), 2f), new Vector3(Mathf.Cos(i * 1.5f) * 0.5f, 0.05f, Mathf.Sin(i * 1.5f) * 0.5f), Vector3.one * 0.25f, default, "dab", false);
            Kit.Part(rig.weaponPivot, MeshGen.Cylinder, T(0.2f, 0.12f, 0.08f), new Vector3(0.5f, -0.3f, 0.6f), new Vector3(0.04f, 0.3f, 0.04f), new Vector3(70, 0, 0));
            rig.Init();
            return rig;
        }

        // ====================== R'LYEH ======================
        public static ModelRig DeepOne(Transform parent, bool priest = false)
        {
            var rig = CharacterArt.Root(parent, 0.5f, out var b);
            var scale = T(0.25f, 0.45f, 0.4f);
            var belly = T(0.6f, 0.7f, 0.55f);
            P(b, MeshGen.Capsule, scale, 0.15f, 0.42f, 0, 0.2f, 0.42f, 0.2f);
            P(b, MeshGen.Capsule, scale, -0.15f, 0.42f, 0, 0.2f, 0.42f, 0.2f);
            P(b, MeshGen.Sphere, T(0.2f, 0.35f, 0.3f), 0.18f, 0.05f, 0.15f, 0.22f, 0.08f, 0.35f);
            P(b, MeshGen.Sphere, T(0.2f, 0.35f, 0.3f), -0.18f, 0.05f, 0.15f, 0.22f, 0.08f, 0.35f);
            if (priest) P(b, MeshGen.Robe(14, 0.25f, 0.06f), T(0.2f, 0.12f, 0.3f), 0, 0.1f, 0, 0.85f, 1.2f, 0.85f);
            P(b, MeshGen.Capsule, scale, 0, 1.15f, 0, 0.55f, 0.5f, 0.42f, 10, 0, 0);
            P(b, MeshGen.Sphere, belly, 0, 1.1f, 0.13f, 0.4f, 0.45f, 0.25f);
            var head = P(b, MeshGen.Sphere, scale, 0, 1.6f, 0.1f, 0.48f, 0.38f, 0.5f);
            Eye(b, new Color(0.9f, 1f, 0.6f), 0.2f, 1.68f, 0.25f, 0.13f);
            Eye(b, new Color(0.9f, 1f, 0.6f), -0.2f, 1.68f, 0.25f, 0.13f);
            P(b, MeshGen.Cube, T(0.08f, 0.1f, 0.1f), 0, 1.5f, 0.32f, 0.3f, 0.04f, 0.08f);
            // aleta dorsal
            P(b, MeshGen.Sector(80, 6), T(0.3f, 0.65f, 0.6f, 0.02f, 0.08f, 0.08f), 0, 1.0f, -0.25f, 0.9f, 1, 0.9f, 0, 0, 90);
            if (priest)
            {
                P(b, MeshGen.Cone(8), Mats.Toon(new Color(0.95f, 0.75f, 0.3f), new Color(0.3f, 0.2f, 0f), 1f), 0, 1.85f, 0.05f, 0.35f, 0.35f, 0.35f);
                rig.weaponPivot = Kit.Empty(b, "staff", new Vector3(0.45f, 1.0f, 0.15f));
                P(rig.weaponPivot, MeshGen.Cylinder, T(0.3f, 0.25f, 0.2f), 0, 0, 0, 0.06f, 0.9f, 0.06f);
                Kit.Part(rig.weaponPivot, MeshGen.Sphere, Glow(new Color(0.3f, 0.95f, 0.85f), 3f), new Vector3(0, 0.95f, 0), Vector3.one * 0.22f, default, "orb", false);
            }
            else
            {
                rig.weaponPivot = Kit.Empty(b, "trident", new Vector3(0.45f, 1.1f, 0.2f));
                P(rig.weaponPivot, MeshGen.Cylinder, T(0.35f, 0.3f, 0.2f), 0, 0, 0.3f, 0.05f, 0.9f, 0.05f, 75, 0, 0);
                for (int i = -1; i <= 1; i++) P(rig.weaponPivot, MeshGen.Cone(4), T(0.6f, 0.65f, 0.6f), i * 0.1f, 0.25f, 1.15f, 0.06f, 0.3f, 0.06f, 80, 0, 0);
            }
            rig.Init();
            return rig;
        }

        public static ModelRig StarSpawn(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.9f, out var b, 1.6f);
            var skin = T(0.2f, 0.4f, 0.3f, 0.01f, 0.04f, 0.03f);
            P(b, MeshGen.Rock(9, 0.18f, 1), skin, 0, 1.0f, 0, 0.8f, 0.9f, 0.7f);
            P(b, MeshGen.Sphere, skin, 0, 1.85f, 0.15f, 0.6f, 0.75f, 0.6f, -15, 0, 0);
            Eye(b, new Color(1f, 0.3f, 0.2f), 0.17f, 2.0f, 0.42f, 0.11f);
            Eye(b, new Color(1f, 0.3f, 0.2f), -0.17f, 2.0f, 0.42f, 0.11f);
            var face = Kit.Empty(b, "face", new Vector3(0, 1.6f, 0.4f), new Vector3(150, 0, 0));
            Kit.TentacleRing(rig, face, skin, 6, 0.12f, 0.7f, 15, Vector3.zero, 0.9f, 0.5f);
            Wing(rig, b, T(0.12f, 0.25f, 0.18f), 1, new Vector3(0.4f, 1.6f, -0.3f), 1.1f);
            Wing(rig, b, T(0.12f, 0.25f, 0.18f), -1, new Vector3(-0.4f, 1.6f, -0.3f), 1.1f);
            P(b, MeshGen.Capsule, skin, 0.35f, 0.3f, 0, 0.25f, 0.35f, 0.25f);
            P(b, MeshGen.Capsule, skin, -0.35f, 0.3f, 0, 0.25f, 0.35f, 0.25f);
            rig.bobSpeed = 4f;
            rig.Init();
            return rig;
        }

        public static ModelRig Polyp(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.6f, out var b);
            var mat = Mats.Toon(new Color(0.12f, 0.15f, 0.25f), new Color(0.05f, 0.1f, 0.2f), 0.6f, new Color(0.6f, 0.8f, 1f));
            P(b, MeshGen.Rock(17, 0.3f, 1), mat, 0, 1.4f, 0, 0.75f, 0.9f, 0.75f);
            for (int i = 0; i < 5; i++) Eye(b, new Color(0.6f, 0.9f, 1f), Mathf.Cos(i * 1.3f) * 0.3f, 1.3f + Mathf.Sin(i * 2.1f) * 0.3f, 0.32f, 0.08f);
            Kit.TentacleRing(rig, b, mat, 5, 0.25f, 0.9f, 160, new Vector3(0, 1.0f, 0), 1f, 0.7f);
            rig.floats = true; rig.floatHeight = 0.3f;
            rig.Init();
            return rig;
        }

        public static ModelRig Dagon(Transform parent)
        {
            var rig = DeepOne(parent, false);
            rig.transform.localScale = Vector3.one * 2.4f;
            for (int i = 0; i < 7; i++) Kit.Part(rig.body, MeshGen.Crystal(4, 0.5f), Mats.Toon(new Color(0.9f, 0.45f, 0.4f), new Color(0.2f, 0.05f, 0.05f), 1f), new Vector3(Mathf.Cos(i * 0.9f - 2.7f) * 0.25f, 1.85f, Mathf.Sin(i * 0.9f - 2.7f) * 0.25f), new Vector3(0.08f, 0.3f, 0.08f));
            rig.Init();
            return rig;
        }

        public static ModelRig Hydra(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.9f, out var b, 2.1f);
            var skin = T(0.45f, 0.6f, 0.55f);
            // cola de serpiente
            var tail = Kit.Empty(b, "tail", new Vector3(0, 0.35f, 0));
            Kit.Part(tail, MeshGen.Tentacle(12, 8, 0.35f, 1.2f), skin, Vector3.zero, Vector3.one * 1.4f, new Vector3(-100, 180, 0));
            rig.tentacles.Add(tail);
            P(b, MeshGen.Capsule, skin, 0, 1.1f, 0, 0.5f, 0.6f, 0.4f);
            P(b, MeshGen.Sphere, skin, 0, 1.75f, 0.05f, 0.42f, 0.45f, 0.42f);
            Eye(b, new Color(0.6f, 1f, 1f), 0.15f, 1.8f, 0.2f, 0.09f);
            Eye(b, new Color(0.6f, 1f, 1f), -0.15f, 1.8f, 0.2f, 0.09f);
            // melena de aletas
            for (int i = 0; i < 5; i++) P(b, MeshGen.Sector(60, 4), T(0.3f, 0.7f, 0.7f, 0.05f, 0.15f, 0.15f), 0, 1.9f, -0.1f, 0.7f, 1, 0.7f, -40, 180 + (i - 2) * 25, 90);
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(0.4f, 1f, 0.9f), 2.5f), new Vector3(0, 1.2f, 0.35f), Vector3.one * 0.18f, default, "pearl", false);
            rig.Init();
            return rig;
        }

        // ====================== LENG ======================
        public static ModelRig LengMan(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.5f, out var b);
            var fur = T(0.4f, 0.3f, 0.22f);
            var cloth = T(0.55f, 0.15f, 0.12f);
            P(b, MeshGen.Capsule, fur, 0.14f, 0.45f, -0.05f, 0.18f, 0.45f, 0.18f, -25, 0, 0);
            P(b, MeshGen.Capsule, fur, -0.14f, 0.45f, -0.05f, 0.18f, 0.45f, 0.18f, -25, 0, 0);
            P(b, MeshGen.Robe(12, 0.3f, 0.06f), cloth, 0, 0.6f, 0, 0.7f, 0.75f, 0.6f);
            P(b, MeshGen.Capsule, cloth, 0, 1.2f, 0, 0.45f, 0.4f, 0.35f);
            P(b, MeshGen.Sphere, T(0.6f, 0.5f, 0.4f), 0, 1.68f, 0.06f, 0.36f, 0.38f, 0.38f);
            P(b, MeshGen.Cube, T(0.1f, 0.02f, 0.02f), 0, 1.58f, 0.2f, 0.28f, 0.06f, 0.05f);
            // turbante que oculta los cuernos
            P(b, MeshGen.Sphere, T(0.85f, 0.75f, 0.45f), 0, 1.85f, 0.02f, 0.42f, 0.25f, 0.42f);
            P(b, MeshGen.Cone(6), T(0.2f, 0.15f, 0.1f), 0.18f, 1.95f, 0, 0.07f, 0.2f, 0.07f, 0, 0, -40);
            P(b, MeshGen.Cone(6), T(0.2f, 0.15f, 0.1f), -0.18f, 1.95f, 0, 0.07f, 0.2f, 0.07f, 0, 0, 40);
            Eye(b, new Color(1f, 0.6f, 0.2f), 0.1f, 1.72f, 0.21f, 0.06f);
            Eye(b, new Color(1f, 0.6f, 0.2f), -0.1f, 1.72f, 0.21f, 0.06f);
            rig.weaponPivot = Kit.Empty(b, "spear", new Vector3(0.4f, 1.1f, 0.1f));
            P(rig.weaponPivot, MeshGen.Cylinder, T(0.3f, 0.2f, 0.12f), 0, 0, 0.4f, 0.05f, 1.1f, 0.05f, 80, 0, 0);
            P(rig.weaponPivot, MeshGen.Crystal(4, 0.6f), T(0.75f, 0.7f, 0.6f), 0, 0.2f, 1.5f, 0.12f, 0.4f, 0.12f, 80, 0, 0);
            rig.Init();
            return rig;
        }

        public static ModelRig MoonBeast(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.7f, out var b, 1.2f);
            var skin = T(0.82f, 0.82f, 0.78f);
            P(b, MeshGen.Sphere, skin, 0, 0.8f, 0, 1.1f, 0.9f, 1.2f);
            P(b, MeshGen.Sphere, skin, 0, 1.15f, 0.45f, 0.65f, 0.55f, 0.6f);
            var snout = Kit.Empty(b, "snout", new Vector3(0, 1.1f, 0.75f), new Vector3(80, 0, 0));
            Kit.TentacleRing(rig, snout, T(0.95f, 0.55f, 0.6f), 5, 0.08f, 0.5f, 20, Vector3.zero, 0.8f, 0.4f);
            for (int i = 0; i < 4; i++) P(b, MeshGen.Capsule, skin, i < 2 ? 0.4f : -0.4f, 0.25f, i % 2 == 0 ? 0.35f : -0.35f, 0.18f, 0.3f, 0.18f);
            rig.bobSpeed = 6f; rig.bobAmount = 0.1f;
            rig.Init();
            return rig;
        }

        public static ModelRig Shantak(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.9f, out var b, 1.5f);
            var scale = T(0.35f, 0.33f, 0.3f);
            P(b, MeshGen.Capsule, scale, 0, 1.2f, 0, 0.55f, 0.55f, 0.85f, 90, 0, 0);
            P(b, MeshGen.Capsule, scale, 0, 1.6f, 0.65f, 0.22f, 0.4f, 0.22f, 40, 0, 0);
            P(b, MeshGen.Sphere, scale, 0, 1.95f, 0.95f, 0.3f, 0.3f, 0.55f);
            Eye(b, new Color(1f, 0.9f, 0.3f), 0.12f, 2.0f, 1.1f, 0.06f);
            Eye(b, new Color(1f, 0.9f, 0.3f), -0.12f, 2.0f, 1.1f, 0.06f);
            Wing(rig, b, T(0.25f, 0.22f, 0.2f), 1, new Vector3(0.35f, 1.4f, 0), 2f);
            Wing(rig, b, T(0.25f, 0.22f, 0.2f), -1, new Vector3(-0.35f, 1.4f, 0), 2f);
            rig.floats = true; rig.floatHeight = 0.6f;
            rig.Init();
            return rig;
        }

        public static ModelRig Spider(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.7f, out var b, 1.1f);
            var chitin = T(0.3f, 0.2f, 0.35f);
            P(b, MeshGen.Sphere, chitin, 0, 0.6f, -0.3f, 0.8f, 0.6f, 0.9f);
            P(b, MeshGen.Sphere, chitin, 0, 0.6f, 0.35f, 0.45f, 0.4f, 0.45f);
            for (int i = 0; i < 6; i++) Eye(b, new Color(0.9f, 0.3f, 1f), (i % 3 - 1) * 0.1f, 0.72f + (i / 3) * 0.08f, 0.56f, 0.05f);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                {
                    var leg = Kit.Empty(b, "leg", new Vector3(s * 0.25f, 0.6f, 0.3f - i * 0.2f), new Vector3(0, s * (60 + i * 20), 0));
                    P(leg, MeshGen.Capsule, chitin, 0, 0.1f, 0.45f, 0.07f, 0.4f, 0.07f, 60, 0, 0);
                    P(leg, MeshGen.Capsule, chitin, 0, -0.25f, 0.85f, 0.06f, 0.35f, 0.06f, 160, 0, 0);
                    rig.tentacles.Add(leg);
                }
            rig.bobSpeed = 16f; rig.bobAmount = 0.04f;
            rig.Init();
            return rig;
        }

        public static ModelRig HighPriest(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.9f, out var b, 1.7f);
            var silk = Mats.Toon(new Color(0.95f, 0.8f, 0.2f), new Color(0.15f, 0.1f, 0f), 1f, new Color(1f, 0.95f, 0.6f));
            var dark = T(0.15f, 0.1f, 0.05f);
            P(b, MeshGen.Robe(18, 0.15f, 0.12f), silk, 0, 0, 0, 1.3f, 2.0f, 1.3f);
            P(b, MeshGen.Robe(18, 0.15f, 0.06f), dark, 0, 0.02f, 0, 1.36f, 0.4f, 1.36f);
            P(b, MeshGen.Sphere, silk, 0, 2.15f, 0, 0.55f, 0.65f, 0.55f);
            // la máscara de seda amarilla: no tiene rasgos... salvo lo que se intuye debajo
            P(b, MeshGen.Sphere, Mats.Toon(new Color(1f, 0.95f, 0.7f), new Color(0.3f, 0.25f, 0.1f), 0.8f), 0, 2.15f, 0.2f, 0.42f, 0.5f, 0.2f);
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(1f, 0.85f, 0.2f), 2f), new Vector3(0, 2.15f, 0.32f), new Vector3(0.1f, 0.25f, 0.02f), default, "sign", false);
            for (int s = -1; s <= 1; s += 2)
            {
                P(b, MeshGen.Capsule, silk, s * 0.55f, 1.45f, 0.1f, 0.18f, 0.45f, 0.18f, 30, 0, s * 25);
                Kit.Part(b, MeshGen.Sphere, Glow(new Color(1f, 0.8f, 0.3f), 3f), new Vector3(s * 0.75f, 1.05f, 0.45f), Vector3.one * 0.2f, default, "hand", false);
            }
            rig.floats = true; rig.floatHeight = 0.4f;
            Kit.PointLight(rig.transform, new Color(1f, 0.85f, 0.3f), 7f, 2f, new Vector3(0, 2.5f, 0.6f));
            rig.Init();
            return rig;
        }

        // ====================== CORTE DE AZATHOTH ======================
        public static ModelRig MiGo(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.6f, out var b);
            var shell = T(0.85f, 0.45f, 0.5f);
            for (int i = 0; i < 3; i++) P(b, MeshGen.Sphere, shell, 0, 1.2f, 0.3f - i * 0.35f, 0.5f - i * 0.05f, 0.4f, 0.45f);
            var head = P(b, MeshGen.Rock(31, 0.25f, 1), Mats.Toon(new Color(0.4f, 0.2f, 0.5f), new Color(0.3f, 0.05f, 0.4f), 1f), 0, 1.5f, 0.5f, 0.4f, 0.35f, 0.4f);
            for (int i = 0; i < 5; i++) Kit.Part(b, MeshGen.Cylinder, T(0.6f, 0.3f, 0.5f), new Vector3((i - 2) * 0.1f, 1.8f, 0.5f), new Vector3(0.03f, 0.2f, 0.03f), new Vector3(-20, 0, (i - 2) * 15));
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(0.9f, 0.4f, 1f), 3f), new Vector3(0, 1.55f, 0.75f), Vector3.one * 0.12f, default, "glow", false);
            Wing(rig, b, Mats.Toon(new Color(0.55f, 0.4f, 0.55f), new Color(0.1f, 0.05f, 0.1f), 0.7f), 1, new Vector3(0.3f, 1.4f, -0.1f), 1.3f);
            Wing(rig, b, Mats.Toon(new Color(0.55f, 0.4f, 0.55f), new Color(0.1f, 0.05f, 0.1f), 0.7f), -1, new Vector3(-0.3f, 1.4f, -0.1f), 1.3f);
            for (int s = -1; s <= 1; s += 2) for (int i = 0; i < 3; i++) P(b, MeshGen.Capsule, shell, s * 0.35f, 0.75f, 0.3f - i * 0.35f, 0.06f, 0.35f, 0.06f, 0, 0, s * 30);
            rig.floats = true; rig.floatHeight = 0.5f;
            rig.Init();
            return rig;
        }

        public static ModelRig Tindalos(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.6f, out var b, 1.1f);
            var angle = Mats.Toon(new Color(0.1f, 0.15f, 0.3f), new Color(0.05f, 0.15f, 0.35f), 1f, new Color(0.4f, 0.8f, 1f));
            P(b, MeshGen.Crystal(4, 0.3f), angle, 0, 0.9f, -0.5f, 0.6f, 1.3f, 0.5f, 80, 0, 0);
            P(b, MeshGen.Crystal(3, 0.5f), angle, 0, 1.15f, 0.7f, 0.4f, 0.8f, 0.35f, 70, 0, 0);
            for (int i = 0; i < 4; i++) P(b, MeshGen.Crystal(3, 0.6f), angle, i < 2 ? 0.25f : -0.25f, 0.6f, i % 2 == 0 ? 0.4f : -0.4f, 0.12f, 0.65f, 0.12f, 180, 0, i < 2 ? -15 : 15);
            Eye(b, new Color(0.3f, 0.8f, 1f), 0.1f, 1.25f, 1.1f, 0.07f);
            Eye(b, new Color(0.3f, 0.8f, 1f), -0.1f, 1.25f, 1.1f, 0.07f);
            // lengua hueca
            var tongue = Kit.Empty(b, "tongue", new Vector3(0, 1.0f, 1.15f), new Vector3(80, 0, 0));
            Kit.Part(tongue, MeshGen.Tentacle(8, 5, 0.1f, 0.6f), Glow(new Color(0.3f, 0.7f, 1f), 2f), Vector3.zero, Vector3.one * 0.8f);
            rig.tentacles.Add(tongue);
            rig.bobSpeed = 11f;
            rig.Init();
            return rig;
        }

        public static ModelRig Shoggoth(Transform parent, float scale = 1.8f)
        {
            var rig = CharacterArt.Root(parent, 0.9f * scale / 1.8f, out var b, scale);
            var tar = Mats.Toon(new Color(0.04f, 0.04f, 0.05f), new Color(0.02f, 0.05f, 0.04f), 1f, new Color(0.3f, 1f, 0.6f));
            P(b, MeshGen.Rock(41, 0.35f, 2), tar, 0, 0.75f, 0, 1.2f, 0.9f, 1.2f);
            P(b, MeshGen.Rock(42, 0.4f, 1), tar, 0.3f, 1.35f, 0.1f, 0.7f, 0.6f, 0.7f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 2.4f;
                Eye(b, new Color(0.4f, 1f, 0.5f), Mathf.Cos(a) * 0.55f, 0.6f + (i % 4) * 0.25f, Mathf.Sin(a) * 0.55f + 0.1f, 0.06f + (i % 3) * 0.03f);
            }
            Kit.TentacleRing(rig, b, tar, 5, 0.6f, 0.8f, 70, new Vector3(0, 0.5f, 0), 1.3f, 0.7f);
            rig.bobSpeed = 3f; rig.bobAmount = 0.12f;
            rig.Init();
            return rig;
        }

        public static ModelRig Piper(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.6f, out var b);
            var flesh = Mats.Toon(new Color(0.2f, 0.1f, 0.25f), new Color(0.1f, 0.02f, 0.12f), 1f, new Color(1f, 0.6f, 0.3f));
            P(b, MeshGen.Rock(51, 0.4f, 1), flesh, 0, 1.2f, 0, 0.6f, 1.0f, 0.6f);
            P(b, MeshGen.Rock(52, 0.4f, 1), flesh, 0, 1.9f, 0.05f, 0.4f, 0.45f, 0.4f);
            Kit.Part(b, MeshGen.Cylinder, Glow(new Color(1f, 0.75f, 0.3f), 2.5f), new Vector3(0.1f, 1.7f, 0.45f), new Vector3(0.05f, 0.35f, 0.05f), new Vector3(70, 0, 0), "flute", false);
            Kit.TentacleRing(rig, b, flesh, 4, 0.25f, 0.7f, 140, new Vector3(0, 0.8f, 0), 1f, 0.8f);
            rig.floats = true; rig.floatHeight = 0.3f;
            Kit.PointLight(rig.transform, new Color(1f, 0.7f, 0.3f), 3.5f, 1.2f, new Vector3(0, 1.7f, 0.5f));
            rig.Init();
            return rig;
        }

        public static ModelRig Pharaoh(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 0.9f, out var b, 1.8f);
            var skin = T(0.08f, 0.06f, 0.08f);
            var gold = Mats.Toon(new Color(0.95f, 0.75f, 0.3f), new Color(0.2f, 0.12f, 0f), 1f, new Color(1f, 0.9f, 0.5f));
            var blue = T(0.15f, 0.25f, 0.6f);
            P(b, MeshGen.Robe(16, 0.22f, 0.04f), T(0.9f, 0.88f, 0.8f), 0, 0, 0, 0.9f, 1.2f, 0.8f);
            P(b, MeshGen.Capsule, skin, 0, 1.4f, 0, 0.5f, 0.5f, 0.35f);
            P(b, MeshGen.Cylinder, gold, 0, 1.75f, 0, 0.6f, 0.05f, 0.45f);
            P(b, MeshGen.Sphere, skin, 0, 2.05f, 0.03f, 0.36f, 0.42f, 0.38f);
            // tocado nemes a rayas
            for (int i = 0; i < 4; i++) P(b, MeshGen.Robe(10, 0.3f, 0f), i % 2 == 0 ? gold : blue, 0, 1.75f + i * 0.06f, -0.04f, 0.62f - i * 0.08f, 0.5f - i * 0.1f, 0.52f - i * 0.08f, 180, 0, 0);
            P(b, MeshGen.Cylinder, gold, 0, 2.3f, 0, 0.3f, 0.1f, 0.3f);
            Eye(b, new Color(1f, 0.5f, 0.1f), 0.1f, 2.08f, 0.2f, 0.07f);
            Eye(b, new Color(1f, 0.5f, 0.1f), -0.1f, 2.08f, 0.2f, 0.07f);
            rig.weaponPivot = Kit.Empty(b, "ankh", new Vector3(0.55f, 1.2f, 0.2f));
            P(rig.weaponPivot, MeshGen.Cylinder, gold, 0, 0, 0, 0.05f, 0.8f, 0.05f);
            Kit.Part(rig.weaponPivot, MeshGen.Ring(0.7f), gold, new Vector3(0, 0.95f, 0), Vector3.one * 0.3f, new Vector3(90, 0, 0));
            Kit.Part(rig.weaponPivot, MeshGen.Sphere, Glow(new Color(1f, 0.6f, 0.2f), 3f), new Vector3(0, 0.95f, 0), Vector3.one * 0.15f, default, "orb", false);
            rig.Init();
            return rig;
        }

        public static ModelRig Haunter(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 1.2f, out var b, 2.2f);
            var black = Mats.Toon(new Color(0.03f, 0.02f, 0.04f), null, 1f, new Color(0.6f, 0.2f, 0.3f));
            P(b, MeshGen.Rock(61, 0.3f, 1), black, 0, 1.5f, 0, 0.8f, 1.0f, 0.7f);
            // el ojo trilobulado ardiente
            for (int i = 0; i < 3; i++) Kit.Part(b, MeshGen.Sphere, Glow(new Color(1f, 0.25f, 0.1f), 4f), new Vector3(Mathf.Cos(i * 2.09f + 1.57f) * 0.15f, 1.9f + Mathf.Sin(i * 2.09f + 1.57f) * 0.15f, 0.62f), Vector3.one * 0.17f, default, "eye", false);
            Wing(rig, b, black, 1, new Vector3(0.45f, 1.8f, -0.1f), 2.4f);
            Wing(rig, b, black, -1, new Vector3(-0.45f, 1.8f, -0.1f), 2.4f);
            rig.floats = true; rig.floatHeight = 0.8f;
            Kit.PointLight(rig.transform, new Color(1f, 0.3f, 0.1f), 7f, 3f, new Vector3(0, 2f, 1.2f));
            rig.Init();
            return rig;
        }

        public static ModelRig CrawlingChaos(Transform parent)
        {
            var rig = CharacterArt.Root(parent, 1.6f, out var b, 2.6f);
            var flesh = Mats.Toon(new Color(0.06f, 0.03f, 0.06f), new Color(0.15f, 0.0f, 0.05f), 1f, new Color(1f, 0.2f, 0.3f));
            P(b, MeshGen.Cone(14), flesh, 0, 0, 0, 1.6f, 3.2f, 1.6f);
            P(b, MeshGen.Rock(71, 0.4f, 1), flesh, 0, 1.2f, 0, 0.9f, 0.9f, 0.9f);
            // un rostro sin rostro: un cono rojo que sangra luz
            Kit.Part(b, MeshGen.Cone(10), Glow(new Color(1f, 0.1f, 0.15f), 3f), new Vector3(0, 2.6f, 0.15f), new Vector3(0.35f, 0.7f, 0.35f), new Vector3(20, 0, 0), "face", false);
            Kit.TentacleRing(rig, b, flesh, 10, 0.9f, 1.6f, 75, new Vector3(0, 0.3f, 0), 1.6f, 0.9f);
            Kit.TentacleRing(rig, b, flesh, 6, 0.4f, 1.1f, 35, new Vector3(0, 2.0f, 0), 1f, 0.7f);
            Kit.PointLight(rig.transform, new Color(1f, 0.15f, 0.2f), 9f, 3f, new Vector3(0, 3f, 1f));
            rig.bobSpeed = 2f; rig.bobAmount = 0.1f;
            rig.Init();
            return rig;
        }
    }
}
