using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    static class C
    {
        public static Dictionary<string, int> Cost(params object[] kv)
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[(string)kv[i]] = Convert.ToInt32(kv[i + 1]);
            return d;
        }
    }

    // =========================================================================
    // GRIMORIO DE KEZIAH (equivalente a las Cartas Arcanas)
    // =========================================================================
    public class CardDef
    {
        public string id, numeral, name, desc;
        public int grasp;
        public Dictionary<string, int> unlock;
        public Action<PlayerModifiers> apply;
        public Action<RunState> onRunStart;
        public Color color;
    }

    // =========================================================================
    // RITUALES DEL CALDERO (equivalente a los Conjuros del Caldero)
    // =========================================================================
    public class RitualDef
    {
        public string id, name, desc;
        public Dictionary<string, int> cost;
        public Func<SaveData, bool> available = s => true;
        public string requirementText = "";
    }

    // =========================================================================
    // JURAMENTO DE LO INNOMBRABLE (equivalente al Juramento de lo Invisible / Miedo)
    // =========================================================================
    public class VowDef
    {
        public string id, name, desc;   // {v} = valor del rango
        public int[] fearPerRank;
        public float[] values;
        public string unit = "%";
    }

    // =========================================================================
    // RELIQUIAS (equivalente a los Recuerdos) y FAMILIARES
    // =========================================================================
    public class RelicDef
    {
        public string id, name, desc, npc;
        public Action<PlayerModifiers> apply;
    }

    public class FamiliarDef
    {
        public string id, name, desc;
        public Action<PlayerModifiers> apply;
    }

    // =========================================================================
    // PRESAGIOS (equivalente a la Lista de Profecías)
    // =========================================================================
    public class QuestDef
    {
        public string id, name, desc;
        public Func<SaveData, bool> done;
        public Func<SaveData, string> progress = s => "";
        public Dictionary<string, int> reward;
    }

    public static class MetaDB
    {
        public static readonly List<CardDef> Cards = new List<CardDef>();
        public static readonly List<RitualDef> Rituals = new List<RitualDef>();
        public static readonly List<VowDef> Vows = new List<VowDef>();
        public static readonly List<RelicDef> Relics = new List<RelicDef>();
        public static readonly List<FamiliarDef> Familiars = new List<FamiliarDef>();
        public static readonly List<QuestDef> Quests = new List<QuestDef>();

        public static CardDef Card(string id) => Cards.FirstOrDefault(c => c.id == id);
        public static RitualDef Ritual(string id) => Rituals.FirstOrDefault(c => c.id == id);
        public static VowDef Vow(string id) => Vows.FirstOrDefault(c => c.id == id);
        public static RelicDef Relic(string id) => Relics.FirstOrDefault(c => c.id == id);
        public static FamiliarDef Familiar(string id) => Familiars.FirstOrDefault(c => c.id == id);

        static MetaDB()
        {
            // -------------------- Grimorio (5 x 3) --------------------
            Card("c_sonador", "I", "El Soñador", 1, "+30 de Lucidez máxima.", C.Cost(Res.Ceniza, 0), m => m.maxMagickBonus += 30, new Color(0.7f, 0.6f, 1f));
            Card("c_llave", "II", "La Llave de Plata", 3, "+1 Último Umbral (vuelves a la vida con 50% de vida).", C.Cost(Res.Ceniza, 10), m => m.extraDeathDefiance += 1, new Color(0.85f, 0.88f, 0.95f));
            Card("c_bruja", "III", "La Bruja", 2, "Tus movimientos Omega cuestan un 25% menos de Lucidez.", C.Cost(Res.Ceniza, 10), m => m.omegaCostMult *= 0.75f, new Color(0.6f, 0.9f, 0.5f));
            Card("c_vagabundo", "IV", "El Vagabundo", 2, "+25 de vida máxima.", C.Cost(Res.Ceniza, 15), m => m.maxHpBonus += 25, new Color(0.9f, 0.5f, 0.4f));
            Card("c_luna", "V", "La Luna", 1, "Tu Eco del Padre se carga solo, como si gastaras 2 de Lucidez por segundo.", C.Cost(Res.Ceniza, 15), m => m.hexPassiveCharge += 2, new Color(0.8f, 0.85f, 1f));
            Card("c_necrofago", "VI", "El Necrófago", 2, "+30% de daño contra enemigos con armadura.", C.Cost(Res.Ceniza, 20), m => m.armorDamageMult *= 1.3f, new Color(0.6f, 0.55f, 0.45f));
            Card("c_sabueso", "VII", "El Sabueso", 2, "+50% de probabilidad de crítico al golpear por la espalda.", C.Cost(Res.Ceniza, 20), m => m.backstabCrit += 0.5f, new Color(0.4f, 0.75f, 0.8f));
            Card("c_faraon", "VIII", "El Faraón Negro", 1, "Empiezas cada incursión con 80 de Oro Onírico.", C.Cost(Res.Ceniza, 20), null, new Color(0.95f, 0.8f, 0.3f), r => r.gold += 80);
            Card("c_sombra", "IX", "La Sombra", 2, "+10% de probabilidad de esquivar.", C.Cost(Res.Ceniza, 25), m => m.dodgeChance += 0.1f, new Color(0.45f, 0.4f, 0.6f));
            Card("c_abismo", "X", "El Abismo", 3, "Recuperas 2 de Lucidez por segundo.", C.Cost(Res.Ceniza, 30), m => m.magickRegen += 2f, new Color(0.3f, 0.5f, 0.9f));
            Card("c_primigenio", "XI", "El Primigenio", 2, "+30% de daño durante los primeros 10 s de cada encuentro.", C.Cost(Res.Ceniza, 30), m => m.firstSecondsBonus += 0.3f, new Color(0.3f, 0.8f, 0.6f));
            Card("c_estrella", "XII", "La Estrella", 3, "Los boones tienen más probabilidad de ser Raros o mejores.", C.Cost(Res.Ceniza, 35), m => m.rarityBonus += 0.2f, new Color(1f, 0.9f, 0.6f));
            Card("c_puerta", "XIII", "La Puerta", 4, "Empiezas cada incursión con un boon al azar.", C.Cost(Res.Ceniza, 40, Res.Ensueno, 20), null, new Color(0.85f, 0.5f, 1f), r => r.pendingStartBoon = true);
            Card("c_olvido", "XIV", "El Olvido", 2, "Recuperas 8 de vida al limpiar cada sala.", C.Cost(Res.Ceniza, 30), m => m.healOnRoomClear += 8, new Color(0.6f, 0.8f, 0.7f));
            Card("c_caos", "XV", "El Caos Reptante", 4, "+25% de daño mientras tu Lucidez esté llena.", C.Cost(Res.Ceniza, 50, Res.Ensueno, 30), m => m.fullMagickDamage += 0.25f, new Color(0.95f, 0.3f, 0.5f));

            // -------------------- Rituales --------------------
            Ritual("r_mercaderes", "Llamada a los Mercaderes de Leng", "Las galeras negras de Leng aparecerán durante tus incursiones para venderte favores.", C.Cost(Res.Plata, 3, Res.Ceniza, 5));
            Ritual("r_manantial", "Manantiales de Kadath", "Aparecerán salas con fuentes curativas antes de cada guardián.", C.Cost(Res.Huesos, 10, Res.Plata, 2));
            Ritual("r_gatos", "Pacto con los Gatos de Ulthar", "Los gatos de Ulthar te ofrecerán sus bendiciones de agilidad.", C.Cost(Res.Ensueno, 15, Res.Plata, 3));
            Ritual("r_ecos", "Ecos del Padre", "Las esferas iridiscentes de Yog-Sothoth te ofrecerán Ecos (poderes que se cargan gastando Lucidez).", C.Cost(Res.Ensueno, 20, Res.Ceniza, 10));
            Ritual("r_azathoth", "Grietas hacia Azathoth", "Aparecerán Puertas de Azathoth: pagas sangre a cambio de una bendición caótica... y una maldición.", C.Cost(Res.Huesos, 20, Res.Ensueno, 15), s => s.Count("boss_pickman") > 0, "Requiere derrotar a Pickman.");
            Ritual("r_wilbur", "Invocar el Eco de Wilbur", "El eco de tu hermano Wilbur Whateley aparecerá en tus incursiones para competir contigo.", C.Cost(Res.Plata, 5, Res.Huesos, 10), s => s.Count("boss_pickman") > 0, "Requiere derrotar a Pickman.");
            Ritual("r_dagas", "Despertar los Colmillos de Tindalos", "Desbloquea las dagas gemelas forjadas en los ángulos del tiempo.", C.Cost(Res.Plata, 4, Res.Polvo, 1));
            Ritual("r_tomo", "Encuadernar el Necronomicón", "Desbloquea el tomo prohibido de Abdul Alhazred.", C.Cost(Res.Plata, 6, Res.Polvo, 1, Res.Ceniza, 10));
            Ritual("r_hoz", "Afilar la Hoz de Leng", "Desbloquea la gran hoz de los sacerdotes de la meseta.", C.Cost(Res.Plata, 8, Res.Polvo, 2), s => s.Count("runs") >= 3, "Requiere 3 incursiones.");
            Ritual("r_trapezoedro", "Despertar el Trapezoedro Resplandeciente", "Desbloquea la piedra que convoca al Morador de las Tinieblas.", C.Cost(Res.Plata, 10, Res.Polvo, 3), s => s.Count("boss_dagon") > 0, "Requiere derrotar a Padre Dagón.");
            Ritual("r_monigote", "Monigote de Zadok", "El viejo Zadok Allen tallará un monigote de práctica en el santuario.", C.Cost(Res.Huesos, 5));
            Ritual("r_west", "Laboratorio de Herbert West", "El reanimador abrirá su mercado de restos en el santuario (cambia Huesos por recursos).", C.Cost(Res.Huesos, 15, Res.Plata, 2), s => s.Count("runs") >= 2, "Requiere 2 incursiones.");
            Ritual("r_juramento", "El Juramento de lo Innombrable", "Permite jurar pactos que endurecen las incursiones a cambio de mayores recompensas.", C.Cost(Res.Ensueno, 30, Res.Lagrima, 1), s => s.Count("wins") > 0 || s.Count("boss_sacerdote") > 0, "Requiere derrotar al Sumo Sacerdote.");
            Ritual("r_reliquias", "Altar de Reliquias", "Permite equipar las reliquias que te regalan tus allegados.", C.Cost(Res.Ceniza, 10, Res.Plata, 2));
            Ritual("r_fam_gato", "Llamar a Ébano, gato de Ulthar", "Un gato negro te acompañará: ataca a tus enemigos y te concede un Último Umbral adicional.", C.Cost(Res.Ensueno, 25, Res.Licor, 1));
            Ritual("r_fam_zoog", "Domesticar a Pipo, el zoog", "Un zoog curioso te acompañará: roba Oro Onírico y recursos para ti.", C.Cost(Res.Ensueno, 20, Res.Huesos, 10));
            Ritual("r_abundancia", "Cosecha del Bosque Encantado", "+25% de recursos permanentes en las salas.", C.Cost(Res.Plata, 6, Res.Ceniza, 25));
            Ritual("r_destino", "Hilos del Destino", "Puedes volver a tirar las ofertas de boones 2 veces por incursión.", C.Cost(Res.Ensueno, 40, Res.Plata, 6));
            Ritual("r_aspectos", "Altar de los Aspectos", "Despierta aspectos alternativos de tus armas usando Polvo de Kadath.", C.Cost(Res.Polvo, 2, Res.Plata, 4), s => s.Count("runs") >= 4, "Requiere 4 incursiones.");
            Ritual("r_epilogo", "Abrir la Última Puerta", "Usa las lágrimas de tu padre para abrir la Puerta de verdad.", C.Cost(Res.Lagrima, 3, Res.Plata, 10, Res.Ensueno, 50), s => s.Count("wins") >= 3, "Requiere escapar 3 veces.");

            // -------------------- Juramento --------------------
            Vow("v_carne", "Carne Dura", "Los enemigos tienen +{v} de vida.", new[] { 1, 1, 1 }, new[] { 20f, 40, 60 });
            Vow("v_fauces", "Fauces Hambrientas", "Los enemigos infligen +{v} de daño.", new[] { 1, 1, 2 }, new[] { 25f, 50, 100 });
            Vow("v_horda", "La Horda", "+{v} de enemigos en cada encuentro.", new[] { 1, 2 }, new[] { 20f, 40 });
            Vow("v_armadura", "Piel de Shoggoth", "Los enemigos tienen {v} de probabilidad de llevar armadura.", new[] { 2, 2 }, new[] { 30f, 60 });
            Vow("v_escasez", "Escasez", "Los precios de los Mercaderes suben un {v}.", new[] { 1, 1 }, new[] { 40f, 80 });
            Vow("v_sinrespiro", "Sin Respiro", "Toda curación se reduce un {v}.", new[] { 1, 2 }, new[] { 30f, 60 });
            Vow("v_mente", "Mente Frágil", "Tu Lucidez máxima se reduce un {v}.", new[] { 1, 1 }, new[] { 25f, 50 });
            Vow("v_eleccion", "Ojos Ciegos", "Recibes {v} opción menos al elegir boones.", new[] { 3 }, new[] { 1f }, "");
            Vow("v_jefes", "Guardianes Implacables", "Los guardianes tienen +{v} de vida y atacan más rápido.", new[] { 3, 3 }, new[] { 25f, 50 });
            Vow("v_tiempo", "Reloj de Tindalos", "Debes cruzar cada región en {v} minutos o los Sabuesos te encuentran.", new[] { 3 }, new[] { 9f }, "");

            // -------------------- Reliquias --------------------
            Relic("rel_velo", "Velo del Guía", "El primer golpe que recibes en cada sala no causa daño.", "umr", m => m.firstHitShield = 1f);
            Relic("rel_llave", "Llave de Plata de Carter", "+40% de Oro Onírico.", "carter", m => m.goldMult *= 1.4f);
            Relic("rel_muneca", "Muñeca de Cera de Keziah", "+30 de Lucidez máxima.", "keziah", m => m.maxMagickBonus += 30);
            Relic("rel_diente", "Diente de Brown Jenkin", "+10% de probabilidad de esquivar.", "jenkin", m => m.dodgeChance += 0.1f);
            Relic("rel_moneda", "Moneda de Innsmouth", "Cthulhu aparece con más frecuencia y sus boones son de mayor rareza.", "zadok", m => { m.favoredGod = "cthulhu"; m.rarityBonus += 0.1f; });
            Relic("rel_reactivo", "Reactivo Verde de West", "Al caer por debajo del 30% de vida, te curas un 40% (una vez por región).", "west", m => m.lowHpRescue = 0.4f);
            Relic("rel_diario", "Diario de Wilbur", "+20% de daño contra guardianes.", "wilbur", m => m.bossDamageMult *= 1.2f);

            // -------------------- Familiares --------------------
            Familiars.Add(new FamiliarDef { id = "fam_gato", name = "Ébano, gato de Ulthar", desc = "+1 Último Umbral. Ataca a los enemigos cercanos.", apply = m => m.extraDeathDefiance += 1 });
            Familiars.Add(new FamiliarDef { id = "fam_zoog", name = "Pipo, el zoog", desc = "+25% de Oro Onírico y +15% de recursos.", apply = m => { m.goldMult *= 1.25f; m.resourceMult *= 1.15f; } });

            // -------------------- Presagios --------------------
            Quest("q_pickman", "El pintor de Boston", "Derrota a Richard Pickman en el Bosque Encantado.", s => s.Count("boss_pickman") > 0, C.Cost(Res.Ceniza, 20, Res.Ensueno, 10));
            Quest("q_dagon", "Bajo las olas", "Derrota a Padre Dagón y Madre Hidra en R'lyeh.", s => s.Count("boss_dagon") > 0, C.Cost(Res.Plata, 5, Res.Ensueno, 20));
            Quest("q_sacerdote", "El que no debe ser descrito", "Derrota al Sumo Sacerdote en la Meseta de Leng.", s => s.Count("boss_sacerdote") > 0, C.Cost(Res.Polvo, 2, Res.Ensueno, 30));
            Quest("q_nyar", "El Caos Reptante", "Derrota a Nyarlathotep en la Corte de Azathoth.", s => s.Count("boss_nyarlathotep") > 0, C.Cost(Res.Lagrima, 1, Res.Plata, 10));
            Quest("q_escape3", "Hijo pródigo de la Puerta", "Escapa tres veces.", s => s.Count("wins") >= 3, C.Cost(Res.Lagrima, 2), s => $"{Mathf.Min(3, s.Count("wins"))}/3");
            Quest("q_dioses", "Favor de los Primigenios", "Obtén un boon de cada uno de los seis dioses.", s => Gods.All.All(g => s.Count("boon_" + g.id) > 0), C.Cost(Res.Ensueno, 40), s => $"{Gods.All.Count(g => s.Count("boon_" + g.id) > 0)}/6");
            Quest("q_duo", "Alianzas impías", "Obtén un boon Dúo.", s => s.Count("duo") > 0, C.Cost(Res.Ceniza, 30, Res.Plata, 4));
            Quest("q_legend", "Un poder legendario", "Obtén un boon Legendario.", s => s.Count("legendary") > 0, C.Cost(Res.Ceniza, 30, Res.Polvo, 1));
            Quest("q_armas", "Maestría primordial", "Completa una incursión (hasta la muerte o la victoria) con cada arma.", s => Weapons.All.All(w => s.Count("run_" + w.id) > 0), C.Cost(Res.Polvo, 3), s => $"{Weapons.All.Count(w => s.Count("run_" + w.id) > 0)}/{Weapons.All.Count}");
            Quest("q_kills", "Cien horrores", "Derrota a 100 enemigos.", s => s.Count("kills") >= 100, C.Cost(Res.Huesos, 20), s => $"{Mathf.Min(100, s.Count("kills"))}/100");
            Quest("q_kills2", "Mil horrores", "Derrota a 1000 enemigos.", s => s.Count("kills") >= 1000, C.Cost(Res.Lagrima, 1, Res.Huesos, 50), s => $"{Mathf.Min(1000, s.Count("kills"))}/1000");
            Quest("q_cartas", "Estudiante del Grimorio", "Desbloquea 8 cartas del Grimorio.", s => s.cards.Count >= 8, C.Cost(Res.Ensueno, 30), s => $"{Mathf.Min(8, s.cards.Count)}/8");
            Quest("q_rituales", "Aprendiz del Caldero", "Completa 8 rituales.", s => s.rituals.Count >= 8, C.Cost(Res.Plata, 8), s => $"{Mathf.Min(8, s.rituals.Count)}/8");
            Quest("q_regalos", "Corazón de la Puerta", "Regala Licor de Luna a 4 allegados distintos.", s => s.affinity.Count(a => a.v > 0) >= 4, C.Cost(Res.Licor, 2), s => $"{Mathf.Min(4, s.affinity.Count(a => a.v > 0))}/4");
            Quest("q_miedo5", "Juramento menor", "Escapa con 5 o más de Pavor.", s => s.bestFear >= 5, C.Cost(Res.Lagrima, 2));
            Quest("q_miedo15", "Juramento mayor", "Escapa con 15 o más de Pavor.", s => s.bestFear >= 15, C.Cost(Res.Lagrima, 4));
            Quest("q_wilbur", "Rivalidad fraternal", "Derrota al Eco de Wilbur 3 veces.", s => s.Count("wilbur_defeated") >= 3, C.Cost(Res.Huesos, 30, Res.Licor, 1), s => $"{Mathf.Min(3, s.Count("wilbur_defeated"))}/3");
            Quest("q_azathoth", "Pacto con el Sultán Ciego", "Entra en 5 Puertas de Azathoth.", s => s.Count("chaos") >= 5, C.Cost(Res.Ensueno, 50), s => $"{Mathf.Min(5, s.Count("chaos"))}/5");
        }

        static void Card(string id, string num, string name, int grasp, string desc, Dictionary<string, int> unlock, Action<PlayerModifiers> apply, Color c, Action<RunState> onStart = null)
            => Cards.Add(new CardDef { id = id, numeral = num, name = name, grasp = grasp, desc = desc, unlock = unlock, apply = apply, onRunStart = onStart, color = c });

        static void Ritual(string id, string name, string desc, Dictionary<string, int> cost, Func<SaveData, bool> avail = null, string req = "")
            => Rituals.Add(new RitualDef { id = id, name = name, desc = desc, cost = cost, available = avail ?? (s => true), requirementText = req });

        static void Vow(string id, string name, string desc, int[] fear, float[] vals, string unit = "%")
            => Vows.Add(new VowDef { id = id, name = name, desc = desc, fearPerRank = fear, values = vals, unit = unit });

        static void Relic(string id, string name, string desc, string npc, Action<PlayerModifiers> apply)
            => Relics.Add(new RelicDef { id = id, name = name, desc = desc, npc = npc, apply = apply });

        static void Quest(string id, string name, string desc, Func<SaveData, bool> done, Dictionary<string, int> reward, Func<SaveData, string> progress = null)
            => Quests.Add(new QuestDef { id = id, name = name, desc = desc, done = done, reward = reward, progress = progress ?? (s => "") });

        public static float VowValue(string id)
        {
            var v = Vow(id); int r = Save.Data.Vow(id);
            if (v == null || r <= 0) return 0;
            return v.values[Mathf.Min(r, v.values.Length) - 1];
        }

        public static int TotalFear()
        {
            int f = 0;
            foreach (var v in Vows)
            {
                int r = Save.Data.Vow(v.id);
                for (int i = 0; i < r && i < v.fearPerRank.Length; i++) f += v.fearPerRank[i];
            }
            return f;
        }

        public static int GraspUsed()
        {
            int g = 0;
            foreach (var id in Save.Data.activeCards) { var c = Card(id); if (c != null) g += c.grasp; }
            return g;
        }
    }
}
