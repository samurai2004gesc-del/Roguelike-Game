using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    public class AspectDef
    {
        public string name, desc;     // {v} = valor según nivel
        public float[] perLevel;      // valores en niveles 1..3
        public Action<PlayerModifiers, int> apply;
        public Dictionary<string, int>[] costs; // coste para nivel 1 (desbloqueo), 2, 3
        public string Describe(int lvl) => desc.Replace("{v}", "<color=#FFE08A>" + perLevel[Mathf.Clamp(lvl - 1, 0, perLevel.Length - 1)] + "</color>");
    }

    /// <summary>Mejora de arma (equivalente al Martillo de Dédalo / Tablillas): aquí, Fragmentos de Eltdown.</summary>
    public class HammerDef
    {
        public string id, name, desc;
        public string[] excludes;
        public Action<PlayerModifiers> apply;
    }

    public class WeaponDef
    {
        public string id, name, title, lore, ritual;
        public string attackDesc, specialDesc, omegaAttackDesc, omegaSpecialDesc;
        public Color color;
        public List<AspectDef> aspects = new List<AspectDef>();
        public List<HammerDef> hammers = new List<HammerDef>();
        public Type behaviour;
        public bool Unlocked => ritual == null || Save.Data.HasRitual(ritual);
    }

    /// <summary>
    /// Las cinco Armas del Umbral (equivalentes a las Armas Nocturnas de Hades II).
    /// Cada una tiene Ataque, Especial y sus versiones Omega (mantener pulsado, gasta Lucidez).
    /// </summary>
    public static class Weapons
    {
        public static readonly List<WeaponDef> All = new List<WeaponDef>();
        public static WeaponDef Get(string id) => All.FirstOrDefault(w => w.id == id);

        static Dictionary<string, int>[] AspectCosts(bool baseAspect) => baseAspect
            ? new[] { C.Cost(), C.Cost(Res.Polvo, 1), C.Cost(Res.Polvo, 2) }
            : new[] { C.Cost(Res.Polvo, 2), C.Cost(Res.Polvo, 2, Res.Plata, 3), C.Cost(Res.Polvo, 4, Res.Lagrima, 1) };

        static Weapons()
        {
            All.Add(new WeaponDef
            {
                id = "baculo", name = "Báculo de la Llave de Plata", title = "El báculo de Randolph Carter", behaviour = typeof(StaffWeapon),
                color = new Color(0.75f, 0.85f, 1f),
                lore = "Un báculo de ébano coronado por la Llave de Plata que abre las Puertas del Sueño. Carter lo dejó atrás cuando cruzó la Última Puerta.",
                attackDesc = "Combo de 3 golpes; el tercero es una estocada potente.",
                specialDesc = "Dispara un proyectil arcano.",
                omegaAttackDesc = "Rayo de la Llave: un haz que atraviesa a todos los enemigos en línea.",
                omegaSpecialDesc = "Esfera Iridiscente: una esfera lenta que estalla en un área amplia.",
                aspects =
                {
                    new AspectDef { name = "Aspecto del Soñador", desc = "Tus movimientos Omega cuestan un {v}% menos de Lucidez.", perLevel = new[] { 10f, 20, 30 }, costs = AspectCosts(true), apply = (m, l) => m.omegaCostMult *= 1f - new[] { 0.1f, 0.2f, 0.3f }[l - 1] },
                    new AspectDef { name = "Aspecto de 'Umr at-Tawil", desc = "Tu Especial atraviesa enemigos e inflige +{v}% de daño.", perLevel = new[] { 15f, 30, 45 }, costs = AspectCosts(false), apply = (m, l) => { m.slotMult[(int)MoveSlot.Special] *= 1f + new[] { 0.15f, 0.3f, 0.45f }[l - 1]; m.flags.Add("staff_pierce"); } },
                },
                hammers =
                {
                    new HammerDef { id = "st_largo", name = "Llave Alargada", desc = "Tu Ataque tiene un 35% más de alcance.", apply = m => m.flags.Add("st_largo") },
                    new HammerDef { id = "st_onda", name = "Tercer Giro", desc = "La estocada final de tu Ataque lanza una onda que inflige 30 de daño.", apply = m => m.flags.Add("st_onda") },
                    new HammerDef { id = "st_doble", name = "Proyectil Doble", desc = "Tu Especial dispara 2 proyectiles.", apply = m => m.flags.Add("st_doble") },
                    new HammerDef { id = "st_rayo", name = "Rayo Prolongado", desc = "Tu Rayo de la Llave es un 50% más largo e inflige +30% de daño.", apply = m => m.flags.Add("st_rayo") },
                    new HammerDef { id = "st_esfera", name = "Esfera Inestable", desc = "Tu Esfera Iridiscente se divide en 4 esferas menores al estallar.", apply = m => m.flags.Add("st_esfera") },
                    new HammerDef { id = "st_ganzua", name = "Ganzúa Onírica", desc = "+25% de probabilidad de crítico contra enemigos Atados por tu Signo.", apply = m => m.flags.Add("st_ganzua") },
                    new HammerDef { id = "st_rapido", name = "Llave Ligera", desc = "Tu Ataque es un 20% más rápido.", apply = m => m.attackSpeedMult *= 1.2f },
                }
            });

            All.Add(new WeaponDef
            {
                id = "dagas", name = "Colmillos de Tindalos", title = "Dagas gemelas de los ángulos", behaviour = typeof(DaggerWeapon), ritual = "r_dagas",
                color = new Color(0.4f, 0.9f, 0.85f),
                lore = "Forjadas con el metal azulado de los ángulos del tiempo, donde moran los Sabuesos. Hieren más cuando atacan por la espalda... o desde una esquina.",
                attackDesc = "Ráfaga de 4 tajos rápidos. Los golpes por la espalda tienen +40% de probabilidad de crítico.",
                specialDesc = "Lanza 3 dagas en abanico.",
                omegaAttackDesc = "Ángulo Agudo: te teletransportas a la espalda del enemigo y apuñalas.",
                omegaSpecialDesc = "Lluvia de Colmillos: lanza 8 dagas perforantes.",
                aspects =
                {
                    new AspectDef { name = "Aspecto del Sabueso", desc = "+{v}% de probabilidad de crítico por la espalda.", perLevel = new[] { 10f, 20, 30 }, costs = AspectCosts(true), apply = (m, l) => m.backstabCrit += new[] { 0.1f, 0.2f, 0.3f }[l - 1] },
                    new AspectDef { name = "Aspecto de Keziah", desc = "Tu Especial lanza +{v} dagas que dejan Vulnerable al enemigo.", perLevel = new[] { 1f, 2, 3 }, costs = AspectCosts(false), apply = (m, l) => { m.flags.Add("dg_keziah"); m.flags.Add("dg_extra" + l); } },
                },
                hammers =
                {
                    new HammerDef { id = "dg_colmillo", name = "Colmillo Extra", desc = "Tu Especial lanza 2 dagas más.", apply = m => m.flags.Add("dg_colmillo") },
                    new HammerDef { id = "dg_herida", name = "Herida Angular", desc = "+25% de probabilidad de crítico por la espalda.", apply = m => m.backstabCrit += 0.25f },
                    new HammerDef { id = "dg_doble", name = "Sombra Doble", desc = "Tu Ángulo Agudo golpea dos veces.", apply = m => m.flags.Add("dg_doble") },
                    new HammerDef { id = "dg_rebote", name = "Daga Rebotadora", desc = "Tus dagas rebotan hacia otro enemigo cercano.", apply = m => m.flags.Add("dg_rebote") },
                    new HammerDef { id = "dg_ritmo", name = "Ritmo de Caza", desc = "Tu Ataque es un 25% más rápido.", apply = m => m.attackSpeedMult *= 1.25f },
                    new HammerDef { id = "dg_esquina", name = "Desde la Esquina", desc = "Tu primer Ataque tras esprintar es crítico garantizado.", apply = m => m.flags.Add("dg_esquina") },
                }
            });

            All.Add(new WeaponDef
            {
                id = "tomo", name = "Necronomicón", title = "Al-Azif, el libro de los muertos", behaviour = typeof(TomeWeapon), ritual = "r_tomo",
                color = new Color(0.55f, 1f, 0.5f),
                lore = "El libro del árabe loco Abdul Alhazred, encuadernado en piel de origen dudoso. Sus páginas arden con llamas fatuas cuando se pronuncian sus versos.",
                attackDesc = "Dispara llamas fatuas teledirigidas (mantén pulsado para disparar en ráfaga).",
                specialDesc = "Lanza un Glifo de Alhazred que estalla tras un instante.",
                omegaAttackDesc = "Letanía: 6 llamas fatuas potentes en abanico.",
                omegaSpecialDesc = "Círculo de Invocación: atrae a los enemigos y estalla.",
                aspects =
                {
                    new AspectDef { name = "Aspecto de Alhazred", desc = "Tus llamas fatuas infligen +{v}% de daño.", perLevel = new[] { 10f, 20, 30 }, costs = AspectCosts(true), apply = (m, l) => m.slotMult[(int)MoveSlot.Attack] *= 1f + new[] { 0.1f, 0.2f, 0.3f }[l - 1] },
                    new AspectDef { name = "Aspecto de John Dee", desc = "Tus glifos estallan una segunda vez con el {v}% del daño.", perLevel = new[] { 40f, 60, 80 }, costs = AspectCosts(false), apply = (m, l) => { m.flags.Add("tm_dee"); m.flags.Add("tm_dee" + l); } },
                },
                hammers =
                {
                    new HammerDef { id = "tm_doble", name = "Llama Doble", desc = "Tu Ataque dispara 2 llamas fatuas.", apply = m => m.flags.Add("tm_doble") },
                    new HammerDef { id = "tm_persistente", name = "Glifo Persistente", desc = "Tus glifos dejan una zona de fuego verde 3 s.", apply = m => m.flags.Add("tm_persistente") },
                    new HammerDef { id = "tm_paginas", name = "Páginas Sueltas", desc = "Tu Letanía dispara 4 llamas más.", apply = m => m.flags.Add("tm_paginas") },
                    new HammerDef { id = "tm_tinta", name = "Tinta Negra", desc = "Tus llamas fatuas dejan Vulnerable (+10% de daño) al enemigo.", apply = m => m.flags.Add("tm_tinta") },
                    new HammerDef { id = "tm_azif", name = "Al-Azif", desc = "Tus glifos tienen un 40% más de radio.", apply = m => m.flags.Add("tm_azif") },
                    new HammerDef { id = "tm_rapido", name = "Versos Rápidos", desc = "Tu Especial se recarga un 40% más rápido.", apply = m => m.specialSpeedMult *= 1.4f },
                }
            });

            All.Add(new WeaponDef
            {
                id = "hoz", name = "Hoz de Leng", title = "La gran hoz de la meseta", behaviour = typeof(ScytheWeapon), ritual = "r_hoz",
                color = new Color(1f, 0.85f, 0.4f),
                lore = "Los sacerdotes de Leng siegan con ella las cabezas de los herejes en el monasterio sin ventanas. Pesada, lenta y devastadora.",
                attackDesc = "Tres siegas amplias y pesadas; la tercera golpea el suelo.",
                specialDesc = "Mantén pulsado para guardarte (reduce el daño frontal un 80%). Al soltar, contraatacas.",
                omegaAttackDesc = "Torbellino: giras sin parar mientras lo mantienes (gasta Lucidez).",
                omegaSpecialDesc = "Salto de la Meseta: saltas hacia el objetivo y golpeas el suelo.",
                aspects =
                {
                    new AspectDef { name = "Aspecto del Sumo Sacerdote", desc = "+{v} de vida máxima.", perLevel = new[] { 15f, 30, 45 }, costs = AspectCosts(true), apply = (m, l) => m.maxHpBonus += new[] { 15f, 30, 45 }[l - 1] },
                    new AspectDef { name = "Aspecto de los Hombres de Leng", desc = "Tu Ataque es un {v}% más rápido.", perLevel = new[] { 10f, 20, 30 }, costs = AspectCosts(false), apply = (m, l) => m.attackSpeedMult *= 1f + new[] { 0.1f, 0.2f, 0.3f }[l - 1] },
                },
                hammers =
                {
                    new HammerDef { id = "hz_filo", name = "Filo Lunar", desc = "Tu Ataque tiene un 25% más de alcance.", apply = m => m.flags.Add("hz_filo") },
                    new HammerDef { id = "hz_contra", name = "Contraataque", desc = "Tu contraataque inflige el doble de daño.", apply = m => m.flags.Add("hz_contra") },
                    new HammerDef { id = "hz_eterno", name = "Torbellino Eterno", desc = "Tu Torbellino cuesta la mitad de Lucidez.", apply = m => m.flags.Add("hz_eterno") },
                    new HammerDef { id = "hz_grieta", name = "Tierra Quebrada", desc = "El golpe final de tu Ataque abre una grieta que avanza.", apply = m => m.flags.Add("hz_grieta") },
                    new HammerDef { id = "hz_hierro", name = "Guardia de Hierro", desc = "Tu guardia devuelve los proyectiles enemigos.", apply = m => m.flags.Add("hz_hierro") },
                    new HammerDef { id = "hz_caida", name = "Caída Pesada", desc = "Tu Salto de la Meseta cubre un 50% más de área.", apply = m => m.flags.Add("hz_caida") },
                }
            });

            All.Add(new WeaponDef
            {
                id = "trapezoedro", name = "Trapezoedro Resplandeciente", title = "La piedra del Morador de las Tinieblas", behaviour = typeof(TrapezohedronWeapon), ritual = "r_trapezoedro",
                color = new Color(1f, 0.25f, 0.3f),
                lore = "Un cristal negro de caras irregulares con vetas rojas. Quien lo contempla en la oscuridad convoca al Morador, avatar de Nyarlathotep.",
                attackDesc = "Dispara fragmentos (3 de munición). Recógelos o espera a que vuelvan.",
                specialDesc = "Embestida: cargas hacia delante y recoges los fragmentos.",
                omegaAttackDesc = "Fragmentación: disparas todos tus fragmentos a la vez.",
                omegaSpecialDesc = "Morador de las Tinieblas: embestida que acaba en una explosión de oscuridad.",
                aspects =
                {
                    new AspectDef { name = "Aspecto de Robert Blake", desc = "Tus fragmentos infligen +{v}% de daño.", perLevel = new[] { 10f, 20, 30 }, costs = AspectCosts(true), apply = (m, l) => m.slotMult[(int)MoveSlot.Attack] *= 1f + new[] { 0.1f, 0.2f, 0.3f }[l - 1] },
                    new AspectDef { name = "Aspecto del Morador", desc = "Tu Embestida es más larga y deja Vulnerable (+{v}% de daño).", perLevel = new[] { 15f, 25, 35 }, costs = AspectCosts(false), apply = (m, l) => { m.flags.Add("tz_morador"); m.flags.Add("tz_morador" + l); } },
                },
                hammers =
                {
                    new HammerDef { id = "tz_cuarto", name = "Cuarto Fragmento", desc = "+1 de munición.", apply = m => m.flags.Add("tz_cuarto") },
                    new HammerDef { id = "tz_ardiente", name = "Fragmento Ardiente", desc = "Tus fragmentos dejan una zona de fuego rojo al caer.", apply = m => m.flags.Add("tz_ardiente") },
                    new HammerDef { id = "tz_iman", name = "Imán Oscuro", desc = "Tus fragmentos vuelven a ti tras 1 s.", apply = m => m.flags.Add("tz_iman") },
                    new HammerDef { id = "tz_doble", name = "Embestida Doble", desc = "Puedes usar tu Embestida dos veces seguidas.", apply = m => m.flags.Add("tz_doble") },
                    new HammerDef { id = "tz_esquirla", name = "Esquirla", desc = "Tus fragmentos atraviesan enemigos.", apply = m => m.flags.Add("tz_esquirla") },
                    new HammerDef { id = "tz_hambre", name = "Sombra Hambrienta", desc = "Tu Embestida inflige el doble de daño.", apply = m => m.slotMult[(int)MoveSlot.Special] *= 2f },
                }
            });
        }
    }
}
