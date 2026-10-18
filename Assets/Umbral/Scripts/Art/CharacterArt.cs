using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Modelos de personajes construidos por código: siluetas legibles desde la cámara isométrica,
    /// colores saturados, contorno de tinta y ojos brillantes (dirección de arte inspirada en Hades, en clave lovecraftiana).
    /// </summary>
    public static partial class CharacterArt
    {
        public static ModelRig Root(Transform parent, float shadowRadius, out Transform body, float scale = 1f)
        {
            var go = new GameObject("Model");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var rig = go.AddComponent<ModelRig>();
            body = Kit.Empty(go.transform, "Body", Vector3.zero);
            rig.body = body;
            Kit.Shadow(go.transform, shadowRadius);
            return rig;
        }

        static Material T(float r, float g, float b, float er = 0, float eg = 0, float eb = 0, float outline = 1f)
            => Mats.Toon(new Color(r, g, b), new Color(er, eg, eb), outline);

        static Material Glow(Color c, float i = 3f) => Mats.Unlit(c, Mats.Blend.Additive, null, i, false);

        // =================================================================
        // SOTHIS WHATELEY — protagonista
        // =================================================================
        public static ModelRig Protagonist(Transform parent, bool female)
        {
            var rig = Root(parent, 0.55f, out var b);
            var robe = T(0.24f, 0.12f, 0.36f);
            var robeDark = T(0.13f, 0.07f, 0.2f);
            var skin = Mats.Toon(new Color(0.86f, 0.8f, 0.9f), null, 1f, new Color(0.7f, 0.6f, 1f));
            var hair = Mats.Toon(new Color(0.86f, 0.87f, 0.98f), new Color(0.08f, 0.08f, 0.15f), 1f, new Color(0.6f, 0.9f, 1f));
            var horn = T(0.12f, 0.08f, 0.16f);
            var irid = new Color(0.45f, 0.95f, 1f);

            float w = female ? 1.12f : 0.96f;
            Kit.Part(b, MeshGen.Robe(16, 0.2f, 0.07f), robe, Vector3.zero, new Vector3(w, 1.18f, w));
            Kit.Part(b, MeshGen.Robe(16, 0.3f, 0.05f), robeDark, new Vector3(0, 0.02f, 0), new Vector3(w * 1.06f, 0.42f, w * 1.06f));
            Kit.Part(b, MeshGen.Ring(0.88f), Glow(irid, 2.2f), new Vector3(0, 0.06f, 0), Vector3.one * w * 0.56f, default, "hem", false);
            // torso, hombros y cuello alto
            Kit.Part(b, MeshGen.Capsule, robeDark, new Vector3(0, 1.28f, 0), new Vector3(female ? 0.4f : 0.46f, 0.36f, 0.3f));
            Kit.Part(b, MeshGen.Sphere, robe, new Vector3(0.26f, 1.46f, 0), Vector3.one * 0.22f);
            Kit.Part(b, MeshGen.Sphere, robe, new Vector3(-0.26f, 1.46f, 0), Vector3.one * 0.22f);
            Kit.Part(b, MeshGen.Cylinder, robe, new Vector3(0, 1.52f, -0.02f), new Vector3(0.3f, 0.06f, 0.3f));
            // brazos
            Kit.Part(b, MeshGen.Capsule, robeDark, new Vector3(0.33f, 1.18f, 0.05f), new Vector3(0.13f, 0.3f, 0.13f), new Vector3(10, 0, 18));
            Kit.Part(b, MeshGen.Capsule, robeDark, new Vector3(-0.33f, 1.18f, 0.05f), new Vector3(0.13f, 0.3f, 0.13f), new Vector3(10, 0, -18));
            Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0.38f, 0.92f, 0.12f), Vector3.one * 0.11f);
            Kit.Part(b, MeshGen.Sphere, skin, new Vector3(-0.38f, 0.92f, 0.12f), Vector3.one * 0.11f);
            // cabeza
            rig.head = Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.76f, 0.02f), new Vector3(0.4f, 0.43f, 0.4f));
            Kit.Part(b, MeshGen.Sphere, Glow(irid, 4f), new Vector3(0.085f, 1.78f, 0.2f), Vector3.one * 0.065f, default, "eye", false);
            Kit.Part(b, MeshGen.Sphere, Glow(irid, 4f), new Vector3(-0.085f, 1.78f, 0.2f), Vector3.one * 0.065f, default, "eye", false);
            // el tercer ojo del Padre
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(1f, 0.45f, 0.95f), 4f), new Vector3(0, 1.88f, 0.19f), new Vector3(0.05f, 0.08f, 0.04f), default, "eye3", false);
            // cuernos de cabra (herencia Whateley)
            Kit.Part(b, MeshGen.Cone(8), horn, new Vector3(0.14f, 1.95f, -0.03f), new Vector3(0.08f, 0.24f, 0.08f), new Vector3(-35, 0, -25));
            Kit.Part(b, MeshGen.Cone(8), horn, new Vector3(-0.14f, 1.95f, -0.03f), new Vector3(0.08f, 0.24f, 0.08f), new Vector3(-35, 0, 25));
            if (female)
            {
                Kit.Part(b, MeshGen.Sphere, hair, new Vector3(0, 1.86f, -0.04f), new Vector3(0.47f, 0.32f, 0.47f));
                Kit.Part(b, MeshGen.Capsule, hair, new Vector3(0, 1.45f, -0.18f), new Vector3(0.4f, 0.5f, 0.2f), new Vector3(12, 0, 0));
                Kit.Part(b, MeshGen.Capsule, hair, new Vector3(0.2f, 1.55f, -0.08f), new Vector3(0.14f, 0.32f, 0.14f), new Vector3(5, 0, 8));
                Kit.Part(b, MeshGen.Capsule, hair, new Vector3(-0.2f, 1.55f, -0.08f), new Vector3(0.14f, 0.32f, 0.14f), new Vector3(5, 0, -8));
                Kit.Part(b, MeshGen.Sphere, hair, new Vector3(0, 1.9f, 0.12f), new Vector3(0.38f, 0.12f, 0.2f), new Vector3(-20, 0, 0));
            }
            else
            {
                Kit.Part(b, MeshGen.Sphere, hair, new Vector3(0, 1.88f, -0.05f), new Vector3(0.45f, 0.26f, 0.45f));
                for (int i = 0; i < 4; i++)
                    Kit.Part(b, MeshGen.Cone(6), hair, new Vector3((i - 1.5f) * 0.1f, 1.9f, -0.15f), new Vector3(0.12f, 0.28f, 0.12f), new Vector3(-110 + i * 5, 0, (i - 1.5f) * 12));
                // capucha caída sobre los hombros
                Kit.Part(b, MeshGen.Robe(12, 0.35f, 0f), robe, new Vector3(0, 1.45f, -0.12f), new Vector3(0.55f, 0.22f, 0.45f), new Vector3(180, 0, 0));
            }
            // zarcillos del manto
            Kit.TentacleRing(rig, b, T(0.2f, 0.1f, 0.3f, 0.1f, 0.15f, 0.25f), 5, 0.32f, 0.6f, 115, new Vector3(0, 0.32f, -0.05f), 0.8f, 0.6f);
            // esferas iridiscentes que la siguen (Yog-Sothoth es una conglomeración de globos)
            for (int i = 0; i < 3; i++) rig.orbiters.Add(Kit.Part(rig.transform, MeshGen.Sphere, Glow(i == 0 ? irid : i == 1 ? new Color(1f, 0.5f, 0.95f) : new Color(1f, 0.9f, 0.5f), 2.8f), Vector3.zero, Vector3.one * 0.16f, default, "globe", false));
            rig.orbitRadius = 0.85f; rig.orbitHeight = 1.4f; rig.orbitSpeed = 60f;
            rig.weaponPivot = Kit.Empty(b, "WeaponPivot", Vector3.zero);
            rig.bobAmount = 0.05f; rig.speedRef = 7f;
            Kit.PointLight(rig.transform, new Color(0.6f, 0.75f, 1f), 4.5f, 0.9f, new Vector3(0, 2f, 0.4f));
            rig.Init();
            return rig;
        }

        /// <summary>Las tres esferas iridiscentes que orbitan a Sothis (también con arte pintado).</summary>
        public static void AddGlobes(ModelRig rig)
        {
            var irid = new Color(0.45f, 0.95f, 1f);
            for (int i = 0; i < 3; i++) rig.orbiters.Add(Kit.Part(rig.transform, MeshGen.Sphere, Glow(i == 0 ? irid : i == 1 ? new Color(1f, 0.5f, 0.95f) : new Color(1f, 0.9f, 0.5f), 2.8f), Vector3.zero, Vector3.one * 0.16f, default, "globe", false));
            rig.orbitRadius = 0.85f; rig.orbitHeight = 1.4f; rig.orbitSpeed = 60f;
            rig.Init();
        }

        /// <summary>Allegado: arte pintado si existe, si no el modelo procedural.</summary>
        public static ModelRig NpcOrArt(Transform parent, string id)
        {
            float h = id == "umr" ? 3.2f : id == "jenkin" ? 0.8f : id == "wilbur" ? 2.4f : 2f;
            return ArtIA.SpriteRig(parent, id, h, id == "jenkin" ? 0.3f : 0.5f) ?? Npc(parent, id);
        }

        // =================================================================
        // ALIADOS Y FAMILIARES
        // =================================================================
        public static ModelRig DarkYoung(Transform parent, float scale)
        {
            var rig = Root(parent, 0.5f, out var b, scale);
            var bark = T(0.08f, 0.1f, 0.06f, 0.02f, 0.05f, 0.01f);
            var green = new Color(0.45f, 0.95f, 0.3f);
            Kit.Part(b, MeshGen.Rock(21, 0.3f, 1), bark, new Vector3(0, 1.4f, 0), new Vector3(0.9f, 1.1f, 0.9f));
            // patas de cabra
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Kit.Part(b, MeshGen.Cylinder, bark, new Vector3(Mathf.Cos(a) * 0.35f, 0.4f, Mathf.Sin(a) * 0.35f), new Vector3(0.14f, 0.45f, 0.14f), new Vector3(Mathf.Sin(a) * 15, 0, -Mathf.Cos(a) * 15));
                Kit.Part(b, MeshGen.Sphere, T(0.03f, 0.03f, 0.03f), new Vector3(Mathf.Cos(a) * 0.42f, 0.05f, Mathf.Sin(a) * 0.42f), new Vector3(0.18f, 0.1f, 0.18f));
            }
            Kit.TentacleRing(rig, b, bark, 6, 0.3f, 1.1f, 40, new Vector3(0, 1.8f, 0), 1f, 0.8f);
            for (int i = 0; i < 4; i++) Kit.Part(b, MeshGen.Sphere, Glow(green, 3f), new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(1.1f, 1.8f), 0.4f), Vector3.one * 0.09f, default, "mouth", false);
            rig.Init();
            return rig;
        }

        public static ModelRig Cat(Transform parent)
        {
            var rig = Root(parent, 0.3f, out var b, 0.8f);
            var fur = T(0.06f, 0.05f, 0.08f);
            Kit.Part(b, MeshGen.Capsule, fur, new Vector3(0, 0.35f, 0), new Vector3(0.3f, 0.3f, 0.6f), new Vector3(90, 0, 0));
            var head = Kit.Part(b, MeshGen.Sphere, fur, new Vector3(0, 0.55f, 0.32f), Vector3.one * 0.3f);
            Kit.Part(b, MeshGen.Cone(4), fur, new Vector3(0.08f, 0.72f, 0.32f), new Vector3(0.08f, 0.14f, 0.08f));
            Kit.Part(b, MeshGen.Cone(4), fur, new Vector3(-0.08f, 0.72f, 0.32f), new Vector3(0.08f, 0.14f, 0.08f));
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(0.6f, 1f, 0.4f), 4f), new Vector3(0.06f, 0.57f, 0.46f), Vector3.one * 0.05f, default, "eye", false);
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(0.6f, 1f, 0.4f), 4f), new Vector3(-0.06f, 0.57f, 0.46f), Vector3.one * 0.05f, default, "eye", false);
            var tail = Kit.Empty(b, "tail", new Vector3(0, 0.45f, -0.3f));
            Kit.Part(tail, MeshGen.Tentacle(8, 6, 0.25f, 0.6f), fur, Vector3.zero, Vector3.one * 0.55f, new Vector3(-40, 0, 0));
            rig.tentacles.Add(tail);
            for (int i = 0; i < 4; i++) Kit.Part(b, MeshGen.Capsule, fur, new Vector3(i < 2 ? 0.1f : -0.1f, 0.12f, i % 2 == 0 ? 0.2f : -0.2f), new Vector3(0.07f, 0.13f, 0.07f));
            rig.speedRef = 8f; rig.bobSpeed = 12f;
            rig.Init();
            return rig;
        }

        /// <summary>Zoog: pequeña criatura peluda del Bosque Encantado con hocico de tentáculos.</summary>
        public static ModelRig Zoog(Transform parent, float scale, bool friendly = false)
        {
            var rig = Root(parent, 0.35f, out var b, scale);
            var fur = friendly ? T(0.55f, 0.4f, 0.28f) : T(0.42f, 0.28f, 0.18f);
            Kit.Part(b, MeshGen.Sphere, fur, new Vector3(0, 0.5f, 0), new Vector3(0.7f, 0.65f, 0.75f));
            Kit.Part(b, MeshGen.Sphere, fur, new Vector3(0, 0.95f, 0.15f), Vector3.one * 0.45f);
            Kit.Part(b, MeshGen.Cone(5), fur, new Vector3(0.15f, 1.15f, 0.1f), new Vector3(0.12f, 0.25f, 0.12f), new Vector3(0, 0, -20));
            Kit.Part(b, MeshGen.Cone(5), fur, new Vector3(-0.15f, 1.15f, 0.1f), new Vector3(0.12f, 0.25f, 0.12f), new Vector3(0, 0, 20));
            Color eye = friendly ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.2f, 0.15f);
            Kit.Part(b, MeshGen.Sphere, Glow(eye, 4f), new Vector3(0.1f, 1.02f, 0.34f), Vector3.one * 0.08f, default, "eye", false);
            Kit.Part(b, MeshGen.Sphere, Glow(eye, 4f), new Vector3(-0.1f, 1.02f, 0.34f), Vector3.one * 0.08f, default, "eye", false);
            // hocico de tentáculos
            var snout = Kit.Empty(b, "snout", new Vector3(0, 0.88f, 0.35f), new Vector3(70, 0, 0));
            var tm = MeshGen.Tentacle(6, 5, 0.25f, 0.3f);
            for (int i = 0; i < 4; i++)
            {
                var piv = Kit.Empty(snout, "t", new Vector3((i - 1.5f) * 0.05f, 0, 0), new Vector3(0, 0, (i - 1.5f) * 15));
                Kit.Part(piv, tm, T(0.65f, 0.35f, 0.35f), Vector3.zero, Vector3.one * 0.3f);
                rig.tentacles.Add(piv);
            }
            Kit.Part(b, MeshGen.Capsule, fur, new Vector3(0, 0.45f, -0.4f), new Vector3(0.12f, 0.3f, 0.12f), new Vector3(-60, 0, 0));
            rig.speedRef = 7f; rig.bobSpeed = 13f; rig.bobAmount = 0.1f;
            rig.Init();
            return rig;
        }

        /// <summary>Monigote de práctica de Zadok: espantapájaros con cabeza de pez.</summary>
        public static ModelRig Dummy(Transform parent)
        {
            var rig = Root(parent, 0.5f, out var b);
            var wood = T(0.4f, 0.28f, 0.16f);
            var cloth = T(0.3f, 0.35f, 0.4f);
            Kit.Part(b, MeshGen.Cylinder, wood, new Vector3(0, 0.9f, 0), new Vector3(0.12f, 0.9f, 0.12f));
            Kit.Part(b, MeshGen.Cylinder, wood, new Vector3(0, 1.4f, 0), new Vector3(0.08f, 0.6f, 0.08f), new Vector3(0, 0, 90));
            Kit.Part(b, MeshGen.Robe(10, 0.25f, 0.08f), cloth, new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.8f, 0.6f));
            var fish = Kit.Part(b, MeshGen.Sphere, T(0.5f, 0.6f, 0.55f), new Vector3(0, 1.85f, 0), new Vector3(0.4f, 0.5f, 0.7f));
            Kit.Part(b, MeshGen.Sphere, T(0.95f, 0.95f, 0.8f), new Vector3(0.14f, 1.92f, 0.22f), Vector3.one * 0.12f);
            Kit.Part(b, MeshGen.Sphere, T(0.95f, 0.95f, 0.8f), new Vector3(-0.14f, 1.92f, 0.22f), Vector3.one * 0.12f);
            Kit.Part(b, MeshGen.Cone(4), T(0.45f, 0.55f, 0.5f), new Vector3(0, 1.85f, -0.4f), new Vector3(0.3f, 0.3f, 0.05f), new Vector3(-90, 0, 0));
            rig.bobAmount = 0;
            rig.Init();
            return rig;
        }

        // =================================================================
        // ALLEGADOS DEL SANTUARIO (NPC)
        // =================================================================
        public static ModelRig Npc(Transform parent, string id)
        {
            switch (id)
            {
                case "umr": return Umr(parent);
                case "carter": return Carter(parent);
                case "keziah": return Keziah(parent);
                case "jenkin": return Jenkin(parent);
                case "zadok": return Zadok(parent);
                case "west": return West(parent);
                case "wilbur": return Wilbur(parent, false);
            }
            return Carter(parent);
        }

        /// <summary>'Umr at-Tawil, el Guía, el Prolongado de Vida: figura velada, avatar de Yog-Sothoth.</summary>
        static ModelRig Umr(Transform parent)
        {
            var rig = Root(parent, 0.7f, out var b, 1.25f);
            var veil = Mats.Toon(new Color(0.3f, 0.22f, 0.45f), new Color(0.05f, 0.03f, 0.1f), 1f, new Color(0.9f, 0.8f, 1f));
            Kit.Part(b, MeshGen.Robe(18, 0.12f, 0.1f), veil, Vector3.zero, new Vector3(1.2f, 2.2f, 1.2f));
            Kit.Part(b, MeshGen.Sphere, veil, new Vector3(0, 2.25f, 0), new Vector3(0.5f, 0.6f, 0.5f));
            // el rostro es un óvalo de luz cambiante
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(0.85f, 0.75f, 1f), 3f), new Vector3(0, 2.22f, 0.2f), new Vector3(0.3f, 0.4f, 0.14f), default, "face", false);
            for (int i = 0; i < 5; i++) rig.orbiters.Add(Kit.Part(rig.transform, MeshGen.Sphere, Glow(Color.HSVToRGB(i / 5f, 0.6f, 1f), 2.5f), Vector3.zero, Vector3.one * 0.22f, default, "globe", false));
            rig.orbitRadius = 1.3f; rig.orbitHeight = 2.4f; rig.orbitSpeed = 25f;
            rig.floats = true; rig.floatHeight = 0.3f;
            Kit.PointLight(rig.transform, new Color(0.8f, 0.7f, 1f), 6f, 2f, new Vector3(0, 2.5f, 0.5f));
            rig.Init();
            return rig;
        }

        /// <summary>Randolph Carter, el soñador de Boston, con su traje de los años veinte.</summary>
        static ModelRig Carter(Transform parent)
        {
            var rig = Root(parent, 0.5f, out var b);
            var suit = T(0.25f, 0.24f, 0.28f);
            var skin = T(0.9f, 0.78f, 0.68f);
            Kit.Part(b, MeshGen.Capsule, suit, new Vector3(0.12f, 0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f));
            Kit.Part(b, MeshGen.Capsule, suit, new Vector3(-0.12f, 0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f));
            Kit.Part(b, MeshGen.Capsule, suit, new Vector3(0, 1.2f, 0), new Vector3(0.5f, 0.5f, 0.32f));
            Kit.Part(b, MeshGen.Cube, T(0.85f, 0.85f, 0.82f), new Vector3(0, 1.4f, 0.12f), new Vector3(0.14f, 0.25f, 0.08f));
            Kit.Part(b, MeshGen.Cube, T(0.5f, 0.1f, 0.12f), new Vector3(0, 1.38f, 0.16f), new Vector3(0.05f, 0.2f, 0.03f));
            Kit.Part(b, MeshGen.Capsule, suit, new Vector3(0.32f, 1.15f, 0), new Vector3(0.14f, 0.35f, 0.14f), new Vector3(0, 0, 10));
            Kit.Part(b, MeshGen.Capsule, suit, new Vector3(-0.32f, 1.15f, 0), new Vector3(0.14f, 0.35f, 0.14f), new Vector3(0, 0, -10));
            rig.head = Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.75f, 0), Vector3.one * 0.36f);
            Kit.Part(b, MeshGen.Sphere, T(0.85f, 0.85f, 0.85f), new Vector3(0, 1.88f, -0.04f), new Vector3(0.36f, 0.18f, 0.34f));
            Kit.Part(b, MeshGen.Cube, T(0.85f, 0.85f, 0.85f), new Vector3(0, 1.66f, 0.17f), new Vector3(0.18f, 0.04f, 0.04f));
            Kit.Eyes(rig.head, new Color(0.5f, 0.8f, 1f), 0.22f, 0.12f, 0.45f, 0.08f);
            // la llave de plata colgando del cinturón
            Kit.Part(b, MeshGen.Ring(0.6f), Glow(new Color(0.85f, 0.9f, 1f), 2.5f), new Vector3(0.28f, 0.9f, 0.15f), Vector3.one * 0.12f, new Vector3(90, 0, 0), "key", false);
            rig.Init();
            return rig;
        }

        /// <summary>Keziah Mason, la bruja de Arkham que viajó por los ángulos.</summary>
        static ModelRig Keziah(Transform parent)
        {
            var rig = Root(parent, 0.5f, out var b);
            var dress = T(0.18f, 0.2f, 0.12f);
            var skin = T(0.75f, 0.72f, 0.58f);
            Kit.Part(b, MeshGen.Robe(14, 0.2f, 0.08f), dress, Vector3.zero, new Vector3(1f, 1.3f, 1f));
            Kit.Part(b, MeshGen.Capsule, dress, new Vector3(0, 1.3f, -0.08f), new Vector3(0.42f, 0.3f, 0.34f), new Vector3(20, 0, 0));
            rig.head = Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.58f, 0.18f), new Vector3(0.36f, 0.38f, 0.36f));
            Kit.Part(b, MeshGen.Cone(6), skin, new Vector3(0, 1.5f, 0.38f), new Vector3(0.08f, 0.2f, 0.08f), new Vector3(110, 0, 0));
            Kit.Eyes(rig.head, new Color(0.7f, 1f, 0.3f), 0.25f, 0.12f, 0.42f, 0.1f);
            Kit.Part(b, MeshGen.Sphere, T(0.8f, 0.8f, 0.78f), new Vector3(0, 1.72f, 0.1f), new Vector3(0.44f, 0.22f, 0.4f));
            Kit.Part(b, MeshGen.Cone(10), T(0.1f, 0.08f, 0.1f), new Vector3(0, 1.78f, 0.12f), new Vector3(0.5f, 0.75f, 0.5f), new Vector3(-12, 0, 0));
            Kit.Part(b, MeshGen.Cylinder, T(0.1f, 0.08f, 0.1f), new Vector3(0, 1.78f, 0.12f), new Vector3(0.75f, 0.02f, 0.75f));
            // bastón torcido
            Kit.Part(b, MeshGen.Cylinder, T(0.3f, 0.2f, 0.1f), new Vector3(0.4f, 0.75f, 0.2f), new Vector3(0.06f, 0.8f, 0.06f), new Vector3(10, 0, -5));
            Kit.Part(b, MeshGen.Sphere, Glow(new Color(0.6f, 1f, 0.4f), 3f), new Vector3(0.48f, 1.55f, 0.32f), Vector3.one * 0.12f, default, "orb", false);
            rig.Init();
            return rig;
        }

        /// <summary>Brown Jenkin, el familiar de Keziah: una rata con rostro humano.</summary>
        static ModelRig Jenkin(Transform parent)
        {
            var rig = Root(parent, 0.3f, out var b, 0.9f);
            var fur = T(0.35f, 0.25f, 0.18f);
            Kit.Part(b, MeshGen.Capsule, fur, new Vector3(0, 0.3f, 0), new Vector3(0.32f, 0.3f, 0.55f), new Vector3(90, 0, 0));
            rig.head = Kit.Part(b, MeshGen.Sphere, T(0.85f, 0.72f, 0.65f), new Vector3(0, 0.45f, 0.3f), new Vector3(0.26f, 0.26f, 0.28f));
            Kit.Eyes(rig.head, new Color(1f, 0.85f, 0.3f), 0.25f, 0.14f, 0.42f, 0.1f);
            Kit.Part(b, MeshGen.Sphere, fur, new Vector3(0.12f, 0.58f, 0.25f), new Vector3(0.12f, 0.14f, 0.05f));
            Kit.Part(b, MeshGen.Sphere, fur, new Vector3(-0.12f, 0.58f, 0.25f), new Vector3(0.12f, 0.14f, 0.05f));
            var tail = Kit.Empty(b, "tail", new Vector3(0, 0.25f, -0.3f));
            Kit.Part(tail, MeshGen.Tentacle(8, 5, 0.12f, 0.8f), T(0.8f, 0.6f, 0.55f), Vector3.zero, Vector3.one * 0.7f, new Vector3(-70, 0, 0));
            rig.tentacles.Add(tail);
            rig.bobSpeed = 14f; rig.bobAmount = 0.05f;
            rig.Init();
            return rig;
        }

        /// <summary>Zadok Allen, el viejo borracho de Innsmouth.</summary>
        static ModelRig Zadok(Transform parent)
        {
            var rig = Root(parent, 0.5f, out var b);
            var coat = T(0.2f, 0.25f, 0.3f);
            var skin = T(0.8f, 0.68f, 0.6f);
            Kit.Part(b, MeshGen.Capsule, T(0.25f, 0.2f, 0.15f), new Vector3(0.12f, 0.4f, 0), new Vector3(0.18f, 0.4f, 0.18f));
            Kit.Part(b, MeshGen.Capsule, T(0.25f, 0.2f, 0.15f), new Vector3(-0.12f, 0.4f, 0), new Vector3(0.18f, 0.4f, 0.18f));
            Kit.Part(b, MeshGen.Capsule, coat, new Vector3(0, 1.1f, 0), new Vector3(0.55f, 0.48f, 0.4f), new Vector3(10, 0, 0));
            rig.head = Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.62f, 0.08f), Vector3.one * 0.36f);
            Kit.Part(b, MeshGen.Sphere, T(0.9f, 0.9f, 0.9f), new Vector3(0, 1.5f, 0.16f), new Vector3(0.32f, 0.3f, 0.25f));
            Kit.Part(b, MeshGen.Cylinder, T(0.15f, 0.15f, 0.2f), new Vector3(0, 1.82f, 0.05f), new Vector3(0.38f, 0.08f, 0.38f));
            Kit.Eyes(rig.head, new Color(0.9f, 0.9f, 0.6f), 0.22f, 0.1f, 0.45f, 0.08f);
            // la botella de whisky
            Kit.Part(b, MeshGen.Cylinder, Mats.Toon(new Color(0.4f, 0.25f, 0.1f), new Color(0.15f, 0.08f, 0f), 0.8f), new Vector3(0.35f, 1.05f, 0.25f), new Vector3(0.1f, 0.15f, 0.1f));
            rig.Init();
            return rig;
        }

        /// <summary>Herbert West, el reanimador, con su bata y su jeringa de reactivo verde.</summary>
        static ModelRig West(Transform parent)
        {
            var rig = Root(parent, 0.5f, out var b);
            var coat = T(0.85f, 0.85f, 0.82f);
            var skin = T(0.88f, 0.8f, 0.72f);
            Kit.Part(b, MeshGen.Robe(12, 0.25f, 0.03f), coat, Vector3.zero, new Vector3(0.8f, 1.35f, 0.7f));
            Kit.Part(b, MeshGen.Capsule, coat, new Vector3(0, 1.3f, 0), new Vector3(0.42f, 0.35f, 0.3f));
            rig.head = Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.75f, 0), Vector3.one * 0.34f);
            Kit.Part(b, MeshGen.Sphere, T(0.9f, 0.82f, 0.45f), new Vector3(0, 1.86f, -0.03f), new Vector3(0.35f, 0.18f, 0.34f));
            // gafas
            Kit.Part(b, MeshGen.Ring(0.7f), T(0.1f, 0.1f, 0.1f), new Vector3(0.07f, 1.77f, 0.17f), Vector3.one * 0.07f, new Vector3(90, 0, 0));
            Kit.Part(b, MeshGen.Ring(0.7f), T(0.1f, 0.1f, 0.1f), new Vector3(-0.07f, 1.77f, 0.17f), Vector3.one * 0.07f, new Vector3(90, 0, 0));
            Kit.Part(b, MeshGen.Cylinder, Glow(new Color(0.4f, 1f, 0.3f), 3f), new Vector3(0.35f, 1.15f, 0.2f), new Vector3(0.06f, 0.15f, 0.06f), new Vector3(0, 0, 40), "vial", false);
            Kit.PointLight(rig.transform, new Color(0.4f, 1f, 0.3f), 3f, 1f, new Vector3(0.35f, 1.2f, 0.4f));
            rig.Init();
            return rig;
        }

        /// <summary>Wilbur Whateley: el hermano mayor, alto, de rasgos caprinos y con tentáculos bajo el abrigo.</summary>
        public static ModelRig Wilbur(Transform parent, bool hostile)
        {
            var rig = Root(parent, 0.6f, out var b, 1.15f);
            var coat = T(0.28f, 0.2f, 0.14f);
            var skin = T(0.65f, 0.6f, 0.5f);
            Kit.Part(b, MeshGen.Robe(14, 0.25f, 0.06f), coat, Vector3.zero, new Vector3(0.9f, 1.4f, 0.8f));
            Kit.Part(b, MeshGen.Capsule, coat, new Vector3(0, 1.4f, 0), new Vector3(0.55f, 0.42f, 0.38f));
            rig.head = Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.95f, 0.06f), new Vector3(0.34f, 0.42f, 0.38f));
            Kit.Part(b, MeshGen.Sphere, skin, new Vector3(0, 1.82f, 0.2f), new Vector3(0.18f, 0.2f, 0.22f));
            Kit.Eyes(rig.head, hostile ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.85f, 0.4f), 0.25f, 0.1f, 0.42f, 0.1f);
            Kit.Part(b, MeshGen.Cone(6), T(0.15f, 0.12f, 0.1f), new Vector3(0.12f, 2.15f, 0f), new Vector3(0.08f, 0.22f, 0.08f), new Vector3(-30, 0, -20));
            Kit.Part(b, MeshGen.Cone(6), T(0.15f, 0.12f, 0.1f), new Vector3(-0.12f, 2.15f, 0f), new Vector3(0.08f, 0.22f, 0.08f), new Vector3(-30, 0, 20));
            Kit.TentacleRing(rig, b, T(0.3f, 0.35f, 0.3f, 0.05f, 0.1f, 0.05f), 6, 0.35f, 0.9f, 100, new Vector3(0, 0.5f, 0), 1.1f, 0.6f);
            rig.Init();
            return rig;
        }
    }
}
