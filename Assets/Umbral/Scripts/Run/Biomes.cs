using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Región de la incursión (equivalente a Érebo, Océano, Campos del Luto y Tártaro).</summary>
    public class BiomeDef
    {
        public string id, name, subtitle, music, lore;
        public string bossId, bossTitle;
        public int rooms = 8;
        public Color floorA, floorB, crack, veins;
        public Color voidA, voidB, voidC;
        public Color sun, ambient, rim, motes;
        public float sunIntensity = 1.1f;
        public Vector2 sunAngles = new Vector2(55, 30);
        public Color vignette;
        public float saturation = 18f, exposure = 0.15f;
        public bool organicFloor;
    }

    public static class Biomes
    {
        public static readonly List<BiomeDef> All = new List<BiomeDef>
        {
            new BiomeDef
            {
                id = "bosque", name = "El Bosque Encantado", subtitle = "Las Tierras del Sueño", music = "bosque",
                bossId = "boss_pickman", bossTitle = "El Pintor de Boston",
                lore = "Más allá de los setenta escalones del sueño ligero y los setecientos del sueño profundo se extiende el Bosque Encantado, de robles retorcidos y hongos fosforescentes, donde los zoogs susurran secretos.",
                floorA = new Color(0.22f, 0.3f, 0.2f), floorB = new Color(0.3f, 0.36f, 0.22f), crack = new Color(0.06f, 0.08f, 0.05f), veins = new Color(0.4f, 1f, 0.6f),
                voidA = new Color(0.01f, 0.03f, 0.03f), voidB = new Color(0.05f, 0.18f, 0.14f), voidC = new Color(0.3f, 0.9f, 0.6f),
                sun = new Color(0.75f, 0.95f, 0.85f), ambient = new Color(0.18f, 0.25f, 0.28f), rim = new Color(0.5f, 1f, 0.7f), motes = new Color(0.6f, 1f, 0.5f),
                vignette = new Color(0.0f, 0.08f, 0.05f), organicFloor = true, sunIntensity = 1.05f
            },
            new BiomeDef
            {
                id = "rlyeh", name = "R'lyeh Hundida", subtitle = "La ciudad de los ángulos imposibles", music = "rlyeh",
                bossId = "boss_dagon", bossTitle = "Padre Dagón y Madre Hidra",
                lore = "La ciudad-cadáver de Cthulhu, de piedra verde y viscosa, cuyas aristas son agudas donde deberían ser obtusas. Aquí el agua no moja: recuerda.",
                floorA = new Color(0.18f, 0.32f, 0.3f), floorB = new Color(0.12f, 0.24f, 0.26f), crack = new Color(0.02f, 0.06f, 0.07f), veins = new Color(0.2f, 0.9f, 1f),
                voidA = new Color(0.0f, 0.02f, 0.06f), voidB = new Color(0.02f, 0.12f, 0.22f), voidC = new Color(0.1f, 0.7f, 0.8f),
                sun = new Color(0.55f, 0.9f, 1f), ambient = new Color(0.1f, 0.22f, 0.3f), rim = new Color(0.3f, 0.9f, 1f), motes = new Color(0.5f, 0.95f, 1f),
                vignette = new Color(0.0f, 0.05f, 0.12f), sunIntensity = 0.95f
            },
            new BiomeDef
            {
                id = "leng", name = "La Meseta de Leng", subtitle = "Donde se cruzan los mundos", music = "leng",
                bossId = "boss_sacerdote", bossTitle = "El que No Debe Ser Descrito",
                lore = "Una meseta helada y espantosa entre el sueño y la vigilia, de monasterios sin ventanas y aldeas de piedra donde arden hogueras que nadie se atreve a mirar.",
                floorA = new Color(0.42f, 0.36f, 0.3f), floorB = new Color(0.5f, 0.42f, 0.32f), crack = new Color(0.12f, 0.08f, 0.05f), veins = new Color(1f, 0.75f, 0.3f),
                voidA = new Color(0.04f, 0.02f, 0.02f), voidB = new Color(0.25f, 0.12f, 0.05f), voidC = new Color(1f, 0.7f, 0.25f),
                sun = new Color(1f, 0.85f, 0.65f), ambient = new Color(0.28f, 0.2f, 0.18f), rim = new Color(1f, 0.8f, 0.4f), motes = new Color(1f, 0.95f, 0.85f),
                vignette = new Color(0.1f, 0.04f, 0.0f), sunIntensity = 1.15f
            },
            new BiomeDef
            {
                id = "azathoth", name = "La Corte de Azathoth", subtitle = "En el centro del caos último", music = "azathoth",
                bossId = "boss_nyarlathotep", bossTitle = "El Caos Reptante",
                lore = "En el centro del infinito, el Sultán Ciego e Idiota burbujea en su trono mientras flautas monótonas lo arrullan. Si despertara, todo dejaría de ser. Más allá de su corte espera la Última Puerta.",
                floorA = new Color(0.22f, 0.12f, 0.3f), floorB = new Color(0.32f, 0.12f, 0.28f), crack = new Color(0.04f, 0.0f, 0.06f), veins = new Color(1f, 0.3f, 0.8f),
                voidA = new Color(0.02f, 0.0f, 0.03f), voidB = new Color(0.25f, 0.02f, 0.2f), voidC = new Color(1f, 0.4f, 0.6f),
                sun = new Color(0.95f, 0.65f, 1f), ambient = new Color(0.22f, 0.1f, 0.25f), rim = new Color(1f, 0.4f, 0.9f), motes = new Color(1f, 0.5f, 0.9f),
                vignette = new Color(0.12f, 0.0f, 0.12f), organicFloor = true, sunIntensity = 1.0f, saturation = 25f
            },
        };

        public static BiomeDef Get(int i) => All[Mathf.Clamp(i, 0, All.Count - 1)];
    }
}
