using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Elementos de las infusiones (como Tierra/Agua/Aire/Fuego/Éter en Hades II).</summary>
    public enum Element { None, Tierra, Agua, Aire, Fuego, Eter }

    public class GodDef
    {
        public string id, name, title;
        public Element element;
        public Color color;
        public StatusType curse;
        public string curseName, curseDesc;
        public string[] offerLines;     // lo que dice al ofrecer boones
        public string lore;
        public char glyph;              // símbolo para iconos

        public static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Tierra: return "Tierra";
                case Element.Agua: return "Agua";
                case Element.Aire: return "Aire";
                case Element.Fuego: return "Fuego";
                case Element.Eter: return "Éter";
            }
            return "";
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Tierra: return new Color(0.6f, 0.85f, 0.35f);
                case Element.Agua: return new Color(0.3f, 0.8f, 1f);
                case Element.Aire: return new Color(0.85f, 0.95f, 0.95f);
                case Element.Fuego: return new Color(1f, 0.5f, 0.2f);
                case Element.Eter: return new Color(0.85f, 0.6f, 1f);
            }
            return Color.gray;
        }
    }

    /// <summary>
    /// Los Primigenios y Dioses Exteriores que, como los olímpicos de Hades II, ofrecen su favor a la progenie de Yog-Sothoth.
    /// Cada uno tiene una maldición (estado) que define su estilo de juego.
    /// </summary>
    public static class Gods
    {
        public static readonly List<GodDef> All = new List<GodDef>();
        static readonly Dictionary<string, GodDef> byId = new Dictionary<string, GodDef>();

        public static GodDef Get(string id) => id != null && byId.TryGetValue(id, out var g) ? g : null;

        static Gods()
        {
            Add(new GodDef
            {
                id = "cthulhu", name = "Cthulhu", title = "El Gran Soñador de R'lyeh", element = Element.Agua,
                color = new Color(0.15f, 0.85f, 0.75f), curse = StatusType.Marea, curseName = "Marea", glyph = 'C',
                curseDesc = "Los enemigos con Marea son arrastrados más lejos y reciben +15% de daño.",
                offerLines = new[]
                {
                    "Ph'nglui mglw'nafh... ah, la cría de la Puerta. Sueño contigo desde antes de que nacieras.",
                    "Las aguas recuerdan todo, pequeñ{o}. Deja que te arrastren hacia la orilla.",
                    "Que las mareas de R'lyeh rompan contra tus enemigos.",
                    "Mi sueño es largo, pero mis tentáculos llegan lejos. Toma.",
                    "Cuando las estrellas estén en posición, tú ya no estarás aquí. Huye bien."
                },
                lore = "Sacerdote de los Primigenios, duerme muerto pero soñando en la ciudad sumergida de R'lyeh. Sus sueños tocan las mentes sensibles... y la tuya es muy sensible."
            });
            Add(new GodDef
            {
                id = "hastur", name = "Hastur", title = "El Inefable, Rey de Amarillo", element = Element.Aire,
                color = new Color(0.98f, 0.82f, 0.2f), curse = StatusType.Locura, curseName = "Locura", glyph = 'H',
                curseDesc = "Los enemigos con Locura infligen -30% de daño.",
                offerLines = new[]
                {
                    "¿Has visto el Signo Amarillo? Ahora lo llevas tú.",
                    "En Carcosa, las sombras se alargan. Las tuyas también, criatura.",
                    "Quítate la máscara... ah, no llevas ninguna. Qué decepción.",
                    "Que tus enemigos lean la obra prohibida y pierdan la razón.",
                    "Junto al lago de Hali te espero. O quizá no."
                },
                lore = "Aquel Que No Debe Ser Nombrado, habitante de Carcosa junto al lago de Hali. Su obra teatral enloquece a quien lee el segundo acto."
            });
            Add(new GodDef
            {
                id = "shub", name = "Shub-Niggurath", title = "La Cabra Negra de los Bosques", element = Element.Tierra,
                color = new Color(0.45f, 0.85f, 0.25f), curse = StatusType.Semilla, curseName = "Semilla", glyph = 'S',
                curseDesc = "Inflige daño cada segundo. Si el enemigo muere con Semilla, brota un Retoño Oscuro que lucha a tu lado.",
                offerLines = new[]
                {
                    "¡Iä! Mil crías tengo, y aun así me alegra ver una ajena.",
                    "Todo lo que muere, alimenta. Todo lo que alimenta, brota.",
                    "Mis retoños te seguirán un rato, cachorr{o} de la Puerta.",
                    "La tierra es fértil donde caen tus enemigos.",
                    "Crece, criatura. Crece y devora."
                },
                lore = "La Cabra Negra de los Bosques con Mil Crías, deidad de fertilidad monstruosa. Sus Retoños Oscuros son árboles que caminan sobre pezuñas."
            });
            Add(new GodDef
            {
                id = "nodens", name = "Nodens", title = "Señor del Gran Abismo", element = Element.Eter,
                color = new Color(0.72f, 0.62f, 1f), curse = StatusType.Sentencia, curseName = "Sentencia", glyph = 'N',
                curseDesc = "El siguiente golpe que reciba un enemigo con Sentencia invoca un rayo del Gran Abismo.",
                offerLines = new[]
                {
                    "Soy enemigo del Caos Reptante. Eso nos hace aliados, ¿no crees?",
                    "Los ángeles descarnados de la noche cazan para mí. Hoy cazan para ti.",
                    "Un Dios Arquetípico no suele ayudar a la sangre de los Exteriores. Hoy haré una excepción.",
                    "Que el trueno del abismo marque a tus presas.",
                    "Mi carro de conchas te espera más allá de la Puerta."
                },
                lore = "Dios Arquetípico, Señor del Gran Abismo, que surca el éter en un carro de concha tirado por delfines. Antiguo rival de Nyarlathotep."
            });
            Add(new GodDef
            {
                id = "ithaqua", name = "Ithaqua", title = "El que Camina en el Viento", element = Element.Aire,
                color = new Color(0.68f, 0.9f, 1f), curse = StatusType.Escarcha, curseName = "Escarcha", glyph = 'I',
                curseDesc = "Ralentiza al enemigo. Con 10 acumulaciones queda Congelado.",
                offerLines = new[]
                {
                    "El viento del norte sopla para ti, cría de la Puerta.",
                    "Camino entre las estrellas sin dejar huella. Tú dejarás escarcha.",
                    "¿Frío? Pronto lo sentirán ellos.",
                    "Los wendigos aúllan tu nombre. Es un cumplido.",
                    "El hielo conserva. El hielo espera. Como yo."
                },
                lore = "El Wendigo de los vientos helados, que arrastra a sus víctimas por los cielos del norte. Sus pasos gigantes quedan grabados en la nieve."
            });
            Add(new GodDef
            {
                id = "cthugha", name = "Cthugha", title = "La Llama Viviente", element = Element.Fuego,
                color = new Color(1f, 0.45f, 0.12f), curse = StatusType.Llama, curseName = "Llama Viva", glyph = 'F',
                curseDesc = "Cada acumulación inflige 2 de daño por segundo (hasta 20).",
                offerLines = new[]
                {
                    "Desde Fomalhaut ardo. Y ahora tú también.",
                    "El fuego purifica. O al menos carboniza.",
                    "Que ardan los que bloquean tu camino.",
                    "Mis vampiros de fuego bailan cuando luchas. Me gusta eso.",
                    "Ni Nyarlathotep soporta mis llamas. Recuérdalo."
                },
                lore = "Masa de fuego viviente que mora cerca de la estrella Fomalhaut, acompañada por los Vampiros de Fuego. Enemigo natural de Nyarlathotep."
            });
        }

        static void Add(GodDef g) { All.Add(g); byId[g.id] = g; }
    }
}
