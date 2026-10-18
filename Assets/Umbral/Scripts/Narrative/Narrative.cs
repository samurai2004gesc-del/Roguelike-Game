using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    public class Speaker
    {
        public string id, name, title;
        public Color color;
        public string relic;          // reliquia que regala al subir afinidad
        public string[] idle;         // líneas genéricas
        public string[] giftThanks;
    }

    public class DLine { public string who; public string text; public DLine(string w, string t) { who = w; text = t; } }

    public class DEntry
    {
        public string id, npc;
        public int priority;
        public Func<SaveData, bool> cond = s => true;
        public DLine[] lines;
        public Action onEnd;
    }

    /// <summary>Texto con marcas de género: {o} → o/a, {hijo}, {el}, {hermano}, {sobrino}, {un}, {lo}.</summary>
    public static class Lines
    {
        public static string G(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            bool f = Save.Data.female;
            return s.Replace("{o}", f ? "a" : "o").Replace("{hijo}", f ? "hija" : "hijo").Replace("{Hijo}", f ? "Hija" : "Hijo")
                    .Replace("{el}", f ? "ella" : "él").Replace("{hermano}", f ? "hermana" : "hermano").Replace("{Hermano}", f ? "Hermana" : "Hermano").Replace("{sobrino}", f ? "sobrina" : "sobrino")
                    .Replace("{un}", f ? "una" : "un").Replace("{lo}", f ? "la" : "lo").Replace("{pequeño}", f ? "pequeña" : "pequeño")
                    .Replace("{cachorro}", f ? "cachorra" : "cachorro").Replace("{nino}", f ? "niña" : "niño");
        }

        static readonly string[] merchant =
        {
            "Rubíes de Leng, especias de Kadath... ¿oro onírico? Ah, aceptamos oro onírico.",
            "Mis amos de la luna no saben que comercio contigo. Que siga así.",
            "Compra, compra. Las galeras zarpan antes de que salga la luna.",
            "No mires bajo mi turbante y yo no miraré bajo tu manto, {hijo} de la Puerta."
        };
        public static string Merchant() => G(merchant[UnityEngine.Random.Range(0, merchant.Length)]);
    }

    /// <summary>Base de datos de diálogos y personajes.</summary>
    public static class Dialogue
    {
        public static readonly Dictionary<string, Speaker> Speakers = new Dictionary<string, Speaker>();
        public static readonly List<DEntry> Entries = new List<DEntry>();

        public static Speaker Sp(string id) => Speakers.TryGetValue(id, out var s) ? s : new Speaker { id = id, name = id, color = Color.white };

        static void S(string id, string name, string title, Color c, string relic, string[] idle, string[] thanks)
            => Speakers[id] = new Speaker { id = id, name = name, title = title, color = c, relic = relic, idle = idle, giftThanks = thanks };

        static void E(string id, string npc, int prio, Func<SaveData, bool> cond, params DLine[] lines)
            => Entries.Add(new DEntry { id = id, npc = npc, priority = prio, cond = cond, lines = lines });

        static DLine L(string who, string t) => new DLine(who, t);
        const string Me = "sothis";

        /// <summary>Elige el diálogo de mayor prioridad no visto cuyo requisito se cumpla.</summary>
        public static DEntry Next(string npc)
        {
            var s = Save.Data;
            return Entries.Where(e => e.npc == npc && !s.seenDialogue.Contains(e.id) && e.cond(s)).OrderByDescending(e => e.priority).FirstOrDefault();
        }

        public static void MarkSeen(DEntry e) { if (e != null && !Save.Data.seenDialogue.Contains(e.id)) Save.Data.seenDialogue.Add(e.id); }

        public static string Idle(string npc)
        {
            var sp = Sp(npc);
            if (sp.idle == null || sp.idle.Length == 0) return "...";
            return Lines.G(sp.idle[UnityEngine.Random.Range(0, sp.idle.Length)]);
        }

        static bool Died(SaveData s) => s.Count("deaths") > 0;
        static bool Runs(SaveData s, int n) => s.Count("runs") >= n;

        static Dialogue()
        {
            // ------------------------------------------------------------ personajes
            S(Me, "Sothis Whateley", "Vástago de Yog-Sothoth", new Color(0.7f, 0.85f, 1f), null, null, null);
            S("umr", "'Umr at-Tawil", "El Guía, el Prolongado de Vida", new Color(0.8f, 0.7f, 1f), "rel_velo",
                new[] { "La Puerta está en todas partes, {hijo}. Incluso en ti.", "El tiempo es un círculo para la sangre de tu padre. Úsalo.", "Descansa. O no. Para mí es lo mismo: estoy en todos los momentos a la vez.", "Tus esferas brillan hoy. Él te observa." },
                new[] { "¿Licor de luna? Hace eones que nadie me ofrece nada. Gracias, {hijo}.", "Lo guardaré en un instante que nunca termina." });
            S("carter", "Randolph Carter", "El Soñador de Boston", new Color(0.6f, 0.8f, 1f), "rel_llave",
                new[] { "Recuerda: en el Bosque Encantado, nunca aceptes el vino de un zoog sin brindar primero.", "He cruzado la Puerta una vez. No te diré lo que vi. Aún no.", "Mis presagios no mienten, pero sí exageran.", "Kadath está ahí fuera, en algún lugar del frío desierto. Como tu salida." },
                new[] { "¡Vino de los zoogs! Me trae recuerdos de Ulthar. Gracias, {sobrino}.", "Por los viejos sueños, entonces." });
            S("keziah", "Keziah Mason", "La Bruja de los Ángulos", new Color(0.6f, 1f, 0.45f), "rel_muneca",
                new[] { "Los ángulos, {cachorro}. Todo está en los ángulos.", "¿Quieres aprender a doblar el espacio? Primero aprende a doblar tu orgullo.", "Mis cartas no muerden. Casi nunca.", "Brown Jenkin, deja de mordisquear el Grimorio." },
                new[] { "Hmph. Licor de luna. Sabes cómo ganarte a una bruja.", "Te lo agradezco, aunque no lo parezca. Así son las brujas." });
            S("jenkin", "Brown Jenkin", "Familiar de Keziah", new Color(1f, 0.8f, 0.4f), "rel_diente",
                new[] { "Ji ji ji. ¿Tienes queso? ¿Tienes dedos? Ji ji.", "La bruja duerme, yo vigilo. Yo siempre vigilo.", "He visto tu muerte cuarenta veces. La de ayer fue la más graciosa." },
                new[] { "¡Para mí, para mí! Ji ji ji. Toma un diente. Bueno, toma MI diente.", "Rico licor. Ahora tú hueles a luna. Ji ji." });
            S("zadok", "Zadok Allen", "El Viejo de Innsmouth", new Color(0.9f, 0.9f, 0.6f), "rel_moneda",
                new[] { "¡Fuera de aquí, todo es mentira! Bueno... casi todo.", "El viejo capitán Obed trajo la cosa del mar. Y luego todo se pudrió.", "Dale al monigote, dale. No se queja. Como mi abuelo.", "*hip* Te pareces a alguien que vi en la Isla del Diablo..." },
                new[] { "¿Licor? ¡Eso es mejor que el whisky! Bueno, no. Pero casi.", "Toma una moneda de Innsmouth. Que el mar te sea propicio... o que no te vea." });
            S("west", "Herbert West", "El Reanimador", new Color(0.5f, 1f, 0.4f), "rel_reactivo",
                new[] { "Huesos frescos. Necesito huesos frescos. Bueno, frescos es relativo.", "La muerte es solo un fallo químico. Uno que tú arreglas con mucha frecuencia.", "Fascinante. Tu tejido se regenera al cruzar la Puerta. ¿Me prestas un brazo?" },
                new[] { "Licor de luna... ¿qué compuestos contendrá? Gracias. Lo analizaré.", "Un regalo. Qué... humano. Toma, mi reactivo. No preguntes por el color." });
            S("wilbur", "Wilbur Whateley", "Tu hermano mayor", new Color(1f, 0.6f, 0.35f), "rel_diario",
                new[] { "Padre me eligió a mí primero. Recuérdalo.", "Abuelo Whateley nos enseñó los versos. A ti no te dio tiempo.", "Nuestro gemelo... no hablemos de nuestro gemelo." },
                new[] { "¿Licor? ¿Para mí? ... Eres rar{o}, {hermano}. Gracias.", "Toma mi diario. Lo escribí en un alfabeto que nadie entiende. Tú sí." });

            S("cthulhu", "Cthulhu", "El Gran Soñador", Gods.Get("cthulhu").color, null, null, null);
            S("hastur", "Hastur", "El Rey de Amarillo", Gods.Get("hastur").color, null, null, null);
            S("shub", "Shub-Niggurath", "La Cabra Negra", Gods.Get("shub").color, null, null, null);
            S("nodens", "Nodens", "Señor del Gran Abismo", Gods.Get("nodens").color, null, null, null);
            S("ithaqua", "Ithaqua", "El que Camina en el Viento", Gods.Get("ithaqua").color, null, null, null);
            S("cthugha", "Cthugha", "La Llama Viviente", Gods.Get("cthugha").color, null, null, null);
            S("azathoth", "Azathoth", "El Sultán Ciego e Idiota", new Color(1f, 0.3f, 0.8f), null, null, null);
            S("gatos", "Los Gatos de Ulthar", "Guardianes del umbral", new Color(0.6f, 1f, 0.4f), null, null, null);
            S("yog", "Yog-Sothoth", "La Llave y la Puerta", new Color(0.85f, 0.85f, 1f), null, null, null);
            S("boss_pickman", "Richard Upton Pickman", "El Pintor de Boston", new Color(1f, 0.55f, 0.3f), null, null, null);
            S("boss_dagon", "Padre Dagón", "Señor de los Profundos", new Color(0.3f, 1f, 0.8f), null, null, null);
            S("boss_hydra", "Madre Hidra", "Madre de los Profundos", new Color(0.5f, 1f, 1f), null, null, null);
            S("boss_sacerdote", "El Sumo Sacerdote", "Que No Debe Ser Descrito", new Color(1f, 0.85f, 0.25f), null, null, null);
            S("boss_nyarlathotep", "Nyarlathotep", "El Caos Reptante", new Color(1f, 0.3f, 0.25f), null, null, null);
            S("boss_wilbur", "Eco de Wilbur", "Tu hermano mayor", new Color(1f, 0.6f, 0.35f), null, null, null);

            // ------------------------------------------------------------ 'UMR AT-TAWIL
            E("umr_intro", "umr", 100, s => true,
                L("umr", "Despierta, {hijo} de la Puerta. Despierta, Sothis Whateley."),
                L(Me, "¿'Umr at-Tawil? ¿Otra vez he... soñado?"),
                L("umr", "No sueñas. Estás en el Umbral, el espacio entre los espacios, donde tu padre Yog-Sothoth es coterminal con todo tiempo y lugar."),
                L("umr", "Tu madre fue Lavinia Whateley, de Dunwich. Tus hermanos, Wilbur y el Otro, cruzaron hacia el mundo de vigilia... y allí murieron."),
                L(Me, "Y yo me quedé aquí. Atrapad{o} entre las esferas."),
                L("umr", "Quieres salir. Lo sé: lo he visto en todos los instantes. Para ello debes cruzar las Tierras del Sueño, la R'lyeh hundida, la meseta de Leng y la Corte de Azathoth."),
                L("umr", "Y en la Última Puerta te espera Nyarlathotep, que no deja salir a nadie de nuestra sangre sin pagar un precio."),
                L(Me, "Entonces pagaré. O le haré pagar a él."),
                L("umr", "Toma un arma del Altar, ve al Portal y cruza. Si mueres, la Puerta te traerá de vuelta. Para la sangre de Yog-Sothoth, la muerte es solo otra puerta."));
            E("umr_first_death", "umr", 90, s => Died(s) && s.Count("deaths") == 1,
                L("umr", "Has vuelto. Como dije."),
                L(Me, "Dolió. Dolió mucho más de lo que esperaba."),
                L("umr", "El dolor es un maestro paciente. Los dioses ya te han visto. Algunos te ofrecerán su favor la próxima vez."),
                L("umr", "Usa lo que traigas de vuelta: la ceniza estelar para el Grimorio de Keziah, la plata para el Caldero. Nada se pierde del todo."));
            E("umr_caldero", "umr", 80, s => s.Count("runs") >= 1 && s.rituals.Count == 0,
                L("umr", "Ese caldero burbujea desde antes de que los soles se encendieran. Con los materiales adecuados, los rituales cambian lo que encontrarás ahí fuera."),
                L(Me, "¿Como invocar mercaderes? ¿O gatos?"),
                L("umr", "Exactamente. Incluso los gatos de Ulthar responden a un buen ritual."));
            E("umr_pickman", "umr", 70, s => s.Count("boss_pickman") >= 1,
                L("umr", "Pickman ha caído ante ti. Pintará esta derrota, sin duda. Del natural."),
                L(Me, "Ese hombre era... ¿humano, alguna vez?"),
                L("umr", "Todos lo fueron alguna vez, salvo tú y yo. Ahora, el mar te espera. R'lyeh no perdona a los que nadan mal."));
            E("umr_dagon", "umr", 70, s => s.Count("boss_dagon") >= 1,
                L("umr", "Dagón e Hidra. Has vencido a los padres de los Profundos. Cthulhu se agitó en su sueño."),
                L(Me, "¿Eso es bueno?"),
                L("umr", "Para ti, sí. Para el mundo de vigilia... ya veremos."));
            E("umr_sacerdote", "umr", 70, s => s.Count("boss_sacerdote") >= 1,
                L("umr", "Has mirado bajo la máscara de seda amarilla."),
                L(Me, "No había nada. Solo... una puerta. Como yo."),
                L("umr", "Ahora entiendes por qué no debe ser descrito."));
            E("umr_win1", "umr", 95, s => s.Count("wins") >= 1,
                L("umr", "Cruzaste la Última Puerta."),
                L(Me, "Vi las colinas de Dunwich al atardecer. Olía a lluvia y a chotacabras. Y luego... el amanecer me arrastró de vuelta."),
                L("umr", "La vigilia rechaza lo que no pertenece a ella. Aún. Cada vez que cruces, el mundo te recordará un poco más."),
                L(Me, "Entonces cruzaré hasta que me recuerde por completo."));
            E("umr_win3", "umr", 96, s => s.Count("wins") >= 3,
                L("umr", "Tres veces has cruzado. Tu padre ha... sonreído, si es que las esferas sonríen."),
                L("umr", "Existe un ritual: Abrir la Última Puerta. Con las lágrimas de Yog-Sothoth, la Puerta podría quedarse abierta para ti."),
                L(Me, "¿Y para quién más?"),
                L("umr", "Esa, {hijo}, es la pregunta correcta."));
            E("umr_epilogo", "umr", 99, s => s.HasRitual("r_epilogo"),
                L("yog", "...SOTHIS..."),
                L(Me, "¿Padre?"),
                L("yog", "LA LLAVE ES TUYA. LA PUERTA ERES TÚ. CRUZA CUANDO QUIERAS. VUELVE CUANDO QUIERAS."),
                L("umr", "Ha hablado. En eones no lo había hecho con palabras."),
                L(Me, "Entonces ya no huyo. Solo... viajo."),
                L("umr", "Bienvenid{o} a casa, Sothis Whateley. Las dos casas."));
            E("umr_wilbur", "umr", 60, s => s.Count("wilbur_defeated") >= 1,
                L("umr", "El eco de Wilbur te ha buscado. Es solo un eco, pero un eco con resentimiento."),
                L(Me, "Él tuvo una vida allá fuera. Yo nunca la tuve."),
                L("umr", "Él tuvo una muerte allá fuera. Recuérdalo también."));

            // ------------------------------------------------------------ RANDOLPH CARTER
            E("carter_intro", "carter", 100, s => true,
                L("carter", "¡Ah, la criatura de la Puerta! Randolph Carter, a tu servicio. Soñador, escritor mediocre y, durante un tiempo, rey de Ilek-Vad."),
                L(Me, "Usted cruzó la Última Puerta, ¿verdad?"),
                L("carter", "La crucé, sí. Y me vi a mí mismo multiplicado en mil Carters. No lo recomiendo antes del desayuno."),
                L("carter", "Llevo una lista de presagios. Cosas que las estrellas indican que harás. Cúmplelas, y el Umbral te recompensará."));
            E("carter_zoogs", "carter", 50, s => s.Count("kill_zoog") >= 10,
                L("carter", "He oído que has tenido problemas con los zoogs."),
                L(Me, "Muerden. Mucho."),
                L("carter", "Una vez les gané una discusión en su propio consejo. Luego intentaron comerme. Son así: rencorosos, pero de buen paladar."));
            E("carter_dagon", "carter", 60, s => s.Count("boss_dagon") >= 1,
                L("carter", "¿Dagón? ¿El de verdad? Un amigo mío escribió sobre él. Después se tiró por la ventana. Cosas del oficio."),
                L(Me, "No es un consuelo, señor Carter."),
                L("carter", "No pretendía serlo. Pretendía ser una advertencia sobre el oficio."));
            E("carter_wins", "carter", 70, s => s.Count("wins") >= 1,
                L("carter", "¡Lo lograste! ¿Cómo es el mundo de vigilia ahora? ¿Siguen los tranvías en Providence?"),
                L(Me, "Solo vi colinas, piedras antiguas y un cielo enorme."),
                L("carter", "Ah, Dunwich. No es el mejor sitio para volver, pero es tu sitio."));

            // ------------------------------------------------------------ KEZIAH MASON
            E("keziah_intro", "keziah", 100, s => true,
                L("keziah", "Así que tú eres la cría que tiene a todo el Umbral revuelto."),
                L(Me, "¿Y usted es...?"),
                L("keziah", "Keziah Mason, de Arkham. Me colgaron por bruja en 1692 y escapé por un ángulo. Este es mi Grimorio."),
                L("keziah", "Cada carta guarda un poder. Gástate la ceniza estelar en despertarlas y llévalas contigo, pero tu mente solo aguanta un tanto: tu Capacidad."),
                L(Me, "¿Y cómo amplío esa capacidad?"),
                L("keziah", "Con ensueño. Mucho ensueño. Y menos preguntas."));
            E("keziah_cards", "keziah", 50, s => s.cards.Count >= 5,
                L("keziah", "Cinco cartas despiertas. No está mal para alguien que no sabe lo que es un teseracto."),
                L(Me, "Sé lo que es un teseracto."),
                L("keziah", "Dibújamelo, entonces. ...Eso pensaba."));
            E("keziah_dagas", "keziah", 60, s => s.HasRitual("r_dagas"),
                L("keziah", "Te has quedado los Colmillos de Tindalos. Cuidado: los Sabuesos huelen ese metal desde el principio del tiempo."),
                L(Me, "¿Me perseguirán?"),
                L("keziah", "Te perseguirán de todos modos. Por lo menos ahora podrás devolverles el mordisco."));

            // ------------------------------------------------------------ BROWN JENKIN
            E("jenkin_intro", "jenkin", 100, s => true,
                L("jenkin", "¡Ji ji ji! La cría nueva. La cría de los globos. ¿Puedo morderte? Solo un poquito."),
                L(Me, "No."),
                L("jenkin", "¡Ji! Pregunto siempre. Algún día alguien dirá que sí."));
            E("jenkin_deaths", "jenkin", 50, s => s.Count("deaths") >= 5,
                L("jenkin", "Cinco veces muert{o}. Las cuento. Las cuento con los dientes."),
                L(Me, "Qué tierno."),
                L("jenkin", "¡Ji ji! Nadie me había llamado tierno. Te morderé menos."));

            // ------------------------------------------------------------ ZADOK ALLEN
            E("zadok_intro", "zadok", 100, s => true,
                L("zadok", "*hip* ¿Eh? ¿Quién anda ahí? ¿Otro de esos Profundos?"),
                L(Me, "Soy Sothis. Vivo aquí... más o menos."),
                L("zadok", "Zadok Allen, noventa y seis años, de Innsmouth. Vi cosas que te helarían la sangre, ¡si es que tienes sangre!"),
                L("zadok", "Si quieres practicar, ese monigote de ahí aguanta lo que le eches. Le puse la cara de mi abuelo."));
            E("zadok_rlyeh", "zadok", 60, s => s.Count("boss_dagon") >= 1,
                L("zadok", "¿Has matado a Padre Dagón? ¿A DAGÓN? *hip*"),
                L(Me, "Volverá, según me dicen."),
                L("zadok", "Claro que volverá. Siempre vuelven. Como las mareas. Como mi resaca."));

            // ------------------------------------------------------------ HERBERT WEST
            E("west_intro", "west", 100, s => true,
                L("west", "No te muevas. ...Fascinante. Tus tejidos tienen una composición imposible."),
                L(Me, "Hola a usted también, doctor."),
                L("west", "Herbert West, Universidad Miskatonic, promoción de 1903. Reanimador. Te compraré huesos de necrófago, de profundo, de lo que sea."),
                L("west", "A cambio, te daré... recursos. No preguntes de dónde salen."));

            // ------------------------------------------------------------ WILBUR (en el santuario)
            E("wilbur_intro", "wilbur", 100, s => s.Count("wilbur_defeated") >= 1,
                L("wilbur", "Así que este es el Umbral. Más frío de lo que el abuelo contaba."),
                L(Me, "Wilbur. ¿Eres... tú? ¿O solo el eco?"),
                L("wilbur", "¿Hay diferencia? Morí en una biblioteca, mordido por un perro guardián, intentando robar un libro. Eso es todo lo que queda de mí: el rencor y el deseo de salir."),
                L(Me, "Entonces queremos lo mismo."),
                L("wilbur", "No, {hermano}. Tú quieres salir. Yo quería abrir la puerta para que entraran ELLOS. Esa es la diferencia."));

            // ------------------------------------------------------------ JEFES: presentación
            E("boss_pickman_1", "boss_pickman", 100, s => s.Count("boss_pickman") == 0,
                L("boss_pickman", "Vaya, vaya. Un modelo nuevo. Siempre quise pintar a un vástago de los Dioses Exteriores."),
                L(Me, "Apártate, Pickman. Tengo que pasar."),
                L("boss_pickman", "Todo el que pasa por aquí acaba en un lienzo. O en un estómago. Mis amigos y yo no somos exigentes."));
            E("boss_pickman_2", "boss_pickman", 90, s => s.Count("boss_pickman") >= 1,
                L("boss_pickman", "¡Otra vez tú! Aún no he terminado tu retrato. Me falta el gesto de dolor."),
                L(Me, "Ese gesto lo vas a poner tú."));
            E("boss_dagon_1", "boss_dagon", 100, s => s.Count("boss_dagon") == 0,
                L("boss_dagon", "¿QUIÉN PERTURBA EL ABISMO DE R'LYEH?"),
                L("boss_hydra", "Es la cría de la Puerta, mi amor. Huele a esferas y a miedo."),
                L("boss_dagon", "ENTONCES SERÁ ALIMENTO PARA NUESTROS HIJOS."));
            E("boss_dagon_2", "boss_dagon", 90, s => s.Count("boss_dagon") >= 1,
                L("boss_hydra", "Ha vuelto, Dagón. Te dije que volvería."),
                L("boss_dagon", "Y LA HUNDIREMOS DE NUEVO."));
            E("boss_sacerdote_1", "boss_sacerdote", 100, s => s.Count("boss_sacerdote") == 0,
                L("boss_sacerdote", "..."),
                L(Me, "¿No vas a decir nada?"),
                L("boss_sacerdote", "Lo que soy no se dice. Lo que soy se ve. Y quien lo ve... deja de ver."));
            E("boss_sacerdote_2", "boss_sacerdote", 90, s => s.Count("boss_sacerdote") >= 1,
                L("boss_sacerdote", "Has mirado. Y sigues entero. Los Otros Dioses están... intrigados."));
            E("boss_nyar_1", "boss_nyarlathotep", 100, s => s.Count("boss_nyarlathotep") == 0,
                L("boss_nyarlathotep", "Bienvenid{o}, {sobrino}. Llevo observándote desde que eras un rumor en el vientre de Lavinia."),
                L(Me, "Nyarlathotep. Apártate de la Puerta."),
                L("boss_nyarlathotep", "¿Apartarme? Soy el alma y el mensajero de los Dioses Exteriores. Yo SOY el camino. Y hoy el camino tiene ganas de jugar."));
            E("boss_nyar_2", "boss_nyarlathotep", 90, s => s.Count("boss_nyarlathotep") >= 1,
                L("boss_nyarlathotep", "Otra vez aquí. ¿Sabes cuántas formas tengo, {sobrino}? Mil. ¿Sabes cuántas veces puedes morir tú? Las que yo quiera."),
                L(Me, "Entonces quiero que esta sea la última."));
            E("boss_wilbur_1", "boss_wilbur", 100, s => s.Count("wilbur_defeated") == 0,
                L("boss_wilbur", "{Hermano}. Por fin. El abuelo me habló de ti, ¿sabes? El que se quedó dentro."),
                L(Me, "¿Wilbur? ¡Wilbur, espera!"),
                L("boss_wilbur", "Solo uno de los dos saldrá por la Puerta. Y ya sabes quién era el favorito de padre."));
            E("boss_wilbur_2", "boss_wilbur", 90, s => s.Count("wilbur_defeated") >= 1,
                L("boss_wilbur", "Otra vez nos encontramos. ¿Me dejarás ganar esta vez, {hermano}?"),
                L(Me, "Nunca te dejé ganar. Ganaste tú solo, una vez. Y mira cómo acabó."));
        }
    }
}
