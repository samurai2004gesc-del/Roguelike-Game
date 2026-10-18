using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>Bestiario: los horrores de cada región. También alimenta las entradas del Necronomicón.</summary>
    public static class EnemyDB
    {
        public static readonly List<EnemyDef> All = new List<EnemyDef>();
        public static EnemyDef Get(string id) => All.FirstOrDefault(e => e.id == id);
        public static IEnumerable<EnemyDef> OfBiome(int b) => All.Where(e => e.biome == b && !e.id.StartsWith("boss_") && !e.eliteOnly && e.id != "shoggoth_blob");
        public static IEnumerable<EnemyDef> ElitesOf(int b) => All.Where(e => e.biome == b && e.eliteOnly);

        static EnemyDef Add(EnemyDef d) { All.Add(d); return d; }

        static EnemyDB()
        {
            // ================== I. EL BOSQUE ENCANTADO ==================
            Add(new EnemyDef
            {
                id = "zoog", name = "Zoog", biome = 0, hp = 26, speed = 5.2f, radius = 0.35f, height = 1.2f, threat = 1, aggression = 1.4f,
                move = MoveStyle.Chase, preferredRange = 0.9f, accent = new Color(1f, 0.4f, 0.25f), model = t => CharacterArt.Zoog(t, 0.85f),
                lore = "Pequeños seres pardos que habitan las madrigueras del Bosque Encantado. Beben vino lunar, conocen secretos del mundo de vigilia y, según dicen, no desprecian la carne de los soñadores incautos.",
                attacks = { new AttackDef { kind = AtkKind.Lunge, range = 3.2f, cooldown = 1.6f, windup = 0.5f, damage = 6, length = 3f, width = 0.7f, knock = 3, recover = 0.4f } }
            });
            Add(new EnemyDef
            {
                id = "ghoul", name = "Necrófago", biome = 0, hp = 62, speed = 3.9f, radius = 0.5f, height = 1.7f, threat = 2,
                move = MoveStyle.Chase, preferredRange = 1.4f, accent = new Color(0.8f, 0.95f, 0.4f), model = EnemyArt.Ghoul,
                lore = "Antaño humanos, los necrófagos se alimentan de los muertos en las criptas bajo el mundo. Hablan con farfulleos y glugluteos, y algunos recuerdan aún su vida en Boston.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Melee, range = 2.2f, cooldown = 1.5f, windup = 0.55f, damage = 10, radius = 2.3f, angle = 110, knock = 5, repeats = 2, repeatDelay = 0.15f },
                    new AttackDef { kind = AtkKind.Lunge, range = 5f, minRange = 2f, cooldown = 4f, windup = 0.6f, damage = 12, length = 4.5f, width = 0.9f, knock = 7, weight = 0.6f }
                }
            });
            Add(new EnemyDef
            {
                id = "nightgaunt", name = "Ángel Descarnado de la Noche", biome = 0, hp = 46, speed = 4.6f, radius = 0.5f, height = 2.1f, threat = 2, flying = true,
                move = MoveStyle.Hover, preferredRange = 5f, accent = new Color(0.6f, 0.6f, 1f), model = EnemyArt.Nightgaunt,
                lore = "Sin rostro, de piel negra y gomosa, con alas membranosas y cuernos curvados hacia dentro. No hablan: agarran a sus víctimas y les hacen cosquillas mientras las arrastran a las simas de Nodens.",
                attacks = { new AttackDef { kind = AtkKind.Charge, range = 8f, minRange = 2f, cooldown = 2.6f, windup = 0.8f, damage = 11, length = 9f, width = 0.9f, speed = 20f, knock = 8 } }
            });
            Add(new EnemyDef
            {
                id = "gug", name = "Gug", biome = 0, hp = 220, armor = 140, speed = 2.6f, radius = 1.1f, height = 3.6f, threat = 6, eliteOnly = true, knockResist = 0.8f,
                move = MoveStyle.Chase, preferredRange = 2.4f, accent = new Color(1f, 0.45f, 0.6f), model = EnemyArt.Gug,
                lore = "Gigantes peludos con una boca vertical que se abre de arriba abajo, desterrados al subsuelo por los Dioses de la Tierra a causa de sus abominables ritos. Sus cuatro antebrazos derriban torres.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Slam, range = 3.2f, cooldown = 3f, windup = 1.0f, damage = 18, radius = 3.6f, knock = 12 },
                    new AttackDef { kind = AtkKind.Melee, range = 3.5f, cooldown = 2f, windup = 0.7f, damage = 14, radius = 3.8f, angle = 150, knock = 9, repeats = 2, repeatDelay = 0.3f }
                }
            });
            Add(new EnemyDef
            {
                id = "boss_pickman", name = "Richard Upton Pickman", biome = 0, hp = 2600, speed = 4.4f, radius = 0.9f, height = 2.8f, threat = 0, knockResist = 1f,
                accent = new Color(1f, 0.5f, 0.3f), model = EnemyArt.Pickman,
                lore = "El pintor de Boston cuyos cuadros eran demasiado reales, porque los pintaba del natural. Se unió a los necrófagos y ahora custodia el paso del Bosque Encantado con pincel y garra."
            });

            // ================== II. R'LYEH HUNDIDA ==================
            Add(new EnemyDef
            {
                id = "deepone", name = "Profundo", biome = 1, hp = 78, speed = 3.8f, radius = 0.5f, height = 1.9f, threat = 2,
                move = MoveStyle.Chase, preferredRange = 2.2f, accent = new Color(0.4f, 1f, 0.8f), model = t => EnemyArt.DeepOne(t),
                lore = "Hombres-pez de ojos saltones y piel escamosa, inmortales salvo por violencia. Adoran a Padre Dagón y Madre Hidra, y a su señor Cthulhu que sueña.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Lunge, range = 3.8f, cooldown = 2f, windup = 0.6f, damage = 12, length = 3.8f, width = 0.6f, knock = 6 },
                    new AttackDef { kind = AtkKind.Melee, range = 2.4f, cooldown = 1.8f, windup = 0.5f, damage = 10, radius = 2.6f, angle = 70, knock = 4, weight = 0.8f }
                }
            });
            Add(new EnemyDef
            {
                id = "deeppriest", name = "Sacerdote de Dagón", biome = 1, hp = 58, speed = 3.2f, radius = 0.5f, height = 2.0f, threat = 2,
                move = MoveStyle.Kite, preferredRange = 7f, accent = new Color(0.3f, 0.95f, 0.85f), model = t => EnemyArt.DeepOne(t, true),
                lore = "Los híbridos de Innsmouth más ancianos dirigen la Orden Esotérica de Dagón. Su canto hace que el agua obedezca.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Projectile, range = 11f, cooldown = 2.2f, windup = 0.7f, damage = 9, count = 3, spread = 30, speed = 11f, knock = 3, length = 12 },
                    new AttackDef { kind = AtkKind.Lob, range = 10f, cooldown = 4.5f, windup = 0.8f, damage = 10, radius = 2f, status = StatusType.Lento, statusPower = 1, weight = 0.6f }
                }
            });
            Add(new EnemyDef
            {
                id = "polyp", name = "Pólipo Volante", biome = 1, hp = 60, speed = 3f, radius = 0.6f, height = 2f, threat = 2, flying = true,
                move = MoveStyle.Hover, preferredRange = 6f, accent = new Color(0.55f, 0.85f, 1f), model = EnemyArt.Polyp,
                lore = "Entidades parcialmente invisibles que gobernaron la Tierra antes que los Primigenios. Controlan el viento a voluntad y silban con un sonido que nadie olvida.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Radial, range = 9f, cooldown = 3f, windup = 0.9f, damage = 8, count = 10, speed = 8f, knock = 2, repeats = 2, repeatDelay = 0.35f, spread = 18, length = 10 },
                    new AttackDef { kind = AtkKind.Teleport, range = 12f, minRange = 4f, cooldown = 6f, windup = 0.6f, damage = 12, radius = 2.4f, angle = 120, knock = 10, weight = 0.5f }
                }
            });
            Add(new EnemyDef
            {
                id = "starspawn", name = "Engendro Estelar", biome = 1, hp = 280, armor = 160, speed = 2.4f, radius = 1f, height = 3.5f, threat = 6, eliteOnly = true, knockResist = 0.8f,
                move = MoveStyle.Chase, preferredRange = 3f, accent = new Color(0.4f, 1f, 0.5f), model = EnemyArt.StarSpawn,
                lore = "Vástagos de Cthulhu venidos de las estrellas, con su misma forma de pulpo alado en miniatura. Construyeron R'lyeh con ángulos que ningún geómetra humano aceptaría.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.SlamAtTarget, range = 9f, cooldown = 2.8f, windup = 0.9f, damage = 16, radius = 2.4f, knock = 8, repeats = 3, repeatDelay = 0.3f, summonId = "tentacle" },
                    new AttackDef { kind = AtkKind.Slam, range = 3f, cooldown = 3.5f, windup = 1f, damage = 20, radius = 4f, knock = 12 }
                }
            });
            Add(new EnemyDef { id = "boss_dagon", name = "Padre Dagón", biome = 1, hp = 2400, speed = 3.4f, radius = 1.2f, height = 4.5f, knockResist = 1f, accent = new Color(0.3f, 1f, 0.8f), model = EnemyArt.Dagon,
                lore = "Dios-pez de los filisteos y señor de los Profundos, colosal y antiquísimo. Gobierna el abismo junto a su consorte, Madre Hidra." });
            Add(new EnemyDef { id = "boss_hydra", name = "Madre Hidra", biome = 1, hp = 1700, speed = 3.8f, radius = 1f, height = 4f, knockResist = 1f, accent = new Color(0.5f, 1f, 1f), model = EnemyArt.Hydra,
                lore = "La consorte de Dagón, madre de incontables Profundos. Su canto bajo las olas arrastra a los marineros hacia las profundidades." });

            // ================== III. LA MESETA DE LENG ==================
            Add(new EnemyDef
            {
                id = "lengman", name = "Hombre de Leng", biome = 2, hp = 88, speed = 4f, radius = 0.5f, height = 2.0f, threat = 2,
                move = MoveStyle.Chase, preferredRange = 3f, accent = new Color(1f, 0.6f, 0.25f), model = EnemyArt.LengMan,
                lore = "Criaturas casi humanas de boca demasiado ancha, que ocultan cuernos bajo el turbante y pezuñas dentro de los zapatos. Comercian con los rubíes de las galeras negras... y con esclavos.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Charge, range = 7f, minRange = 2f, cooldown = 3f, windup = 0.75f, damage = 14, length = 7f, width = 0.7f, speed = 18f, knock = 9 },
                    new AttackDef { kind = AtkKind.Melee, range = 3f, cooldown = 1.6f, windup = 0.5f, damage = 11, radius = 3.2f, angle = 40, knock = 5 }
                }
            });
            Add(new EnemyDef
            {
                id = "moonbeast", name = "Bestia Lunar", biome = 2, hp = 105, speed = 2.6f, radius = 0.8f, height = 1.8f, threat = 3,
                move = MoveStyle.Kite, preferredRange = 6.5f, accent = new Color(0.75f, 1f, 0.4f), model = EnemyArt.MoonBeast,
                lore = "Grandes cosas blancuzcas y resbaladizas que pueden encogerse y estirarse a voluntad, con un muñón de tentáculos rosados en lugar de hocico. Gobiernan a los Hombres de Leng desde sus naves lunares.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Lob, range = 10f, cooldown = 2.4f, windup = 0.7f, damage = 10, radius = 2.2f, status = StatusType.Veneno, statusPower = 3, repeats = 2, repeatDelay = 0.4f },
                    new AttackDef { kind = AtkKind.Slam, range = 2.5f, cooldown = 3f, windup = 0.7f, damage = 12, radius = 2.8f, knock = 10, weight = 0.6f }
                }
            });
            Add(new EnemyDef
            {
                id = "lengspider", name = "Araña de Leng", biome = 2, hp = 60, speed = 5.6f, radius = 0.6f, height = 1.1f, threat = 2, aggression = 1.3f,
                move = MoveStyle.Kite, preferredRange = 4.5f, accent = new Color(0.9f, 0.4f, 1f), model = EnemyArt.Spider,
                lore = "Arañas púrpuras del tamaño de un poni que tejen en los valles de Leng. Los Hombres de Leng las temen más que a nada.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Web, range = 9f, cooldown = 3.5f, windup = 0.5f, damage = 0, radius = 2.4f },
                    new AttackDef { kind = AtkKind.Lunge, range = 5f, cooldown = 1.8f, windup = 0.45f, damage = 10, length = 5f, width = 0.7f, knock = 4, status = StatusType.Veneno, statusPower = 2 }
                }
            });
            Add(new EnemyDef
            {
                id = "shantak", name = "Shantak", biome = 2, hp = 260, armor = 120, speed = 4f, radius = 1f, height = 3f, threat = 5, eliteOnly = true, flying = true, knockResist = 0.7f,
                move = MoveStyle.Hover, preferredRange = 7f, accent = new Color(1f, 0.85f, 0.35f), model = EnemyArt.Shantak,
                lore = "Pájaros escamosos más grandes que elefantes, con cabeza de caballo. Sirven a los Otros Dioses y temen a los ángeles descarnados de la noche.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Charge, range = 12f, minRange = 3f, cooldown = 2.8f, windup = 0.9f, damage = 18, length = 12f, width = 1.3f, speed = 22f, knock = 12 },
                    new AttackDef { kind = AtkKind.Projectile, range = 10f, cooldown = 3f, windup = 0.6f, damage = 9, count = 5, spread = 60, speed = 12f, weight = 0.7f }
                }
            });
            Add(new EnemyDef { id = "boss_sacerdote", name = "El Sumo Sacerdote que No Debe Ser Descrito", biome = 2, hp = 3400, speed = 3f, radius = 1.1f, height = 4f, knockResist = 1f, accent = new Color(1f, 0.85f, 0.25f), model = EnemyArt.HighPriest,
                lore = "Habita el monasterio sin ventanas de la meseta de Leng, cubierto por una máscara de seda amarilla. Se dice que bajo la máscara no hay un rostro, sino algo que comunica directamente con los Otros Dioses." });

            // ================== IV. LA CORTE DE AZATHOTH ==================
            Add(new EnemyDef
            {
                id = "migo", name = "Mi-Go", biome = 3, hp = 80, speed = 4.4f, radius = 0.55f, height = 2f, threat = 2, flying = true,
                move = MoveStyle.Hover, preferredRange = 6f, accent = new Color(0.95f, 0.5f, 1f), model = EnemyArt.MiGo,
                lore = "Los Hongos de Yuggoth: crustáceos alados de cabeza convoluta que cambia de color para hablar. Extraen cerebros humanos y los guardan en cilindros para llevarlos entre las estrellas.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Beam, range = 10f, cooldown = 2.8f, windup = 0.9f, damage = 14, length = 11f, width = 0.5f, knock = 4 },
                    new AttackDef { kind = AtkKind.Projectile, range = 9f, cooldown = 2f, windup = 0.5f, damage = 8, count = 2, spread = 15, speed = 14f, repeats = 3, repeatDelay = 0.2f, weight = 0.8f }
                }
            });
            Add(new EnemyDef
            {
                id = "tindalos", name = "Sabueso de Tindalos", biome = 3, hp = 95, speed = 5f, radius = 0.55f, height = 1.5f, threat = 3, aggression = 1.4f,
                move = MoveStyle.Ambush, preferredRange = 2f, accent = new Color(0.3f, 0.8f, 1f), model = EnemyArt.Tindalos,
                lore = "Moran en los ángulos del tiempo y persiguen a quien se atreve a mirar demasiado lejos hacia el pasado. Emergen de cualquier esquina, sedientos, magros, con una lengua hueca.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Teleport, range = 14f, cooldown = 3.2f, windup = 0.45f, damage = 14, radius = 2.4f, angle = 110, knock = 6 },
                    new AttackDef { kind = AtkKind.Lunge, range = 4.5f, cooldown = 1.6f, windup = 0.4f, damage = 12, length = 4.5f, width = 0.7f, knock = 5 }
                }
            });
            Add(new EnemyDef
            {
                id = "piper", name = "Flautista Amorfo", biome = 3, hp = 85, speed = 2.6f, radius = 0.5f, height = 2.2f, threat = 3,
                move = MoveStyle.Kite, preferredRange = 7f, accent = new Color(1f, 0.7f, 0.3f), model = EnemyArt.Piper,
                lore = "Servidores sin forma de los Dioses Exteriores que danzan alrededor del trono de Azathoth tocando flautas enloquecedoras. Su música excita a todo lo que la oye.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Aura, range = 12f, cooldown = 5f, windup = 0.9f, damage = 8, radius = 8f },
                    new AttackDef { kind = AtkKind.Radial, range = 10f, cooldown = 3f, windup = 0.8f, damage = 8, count = 14, speed = 7f, spread = 12, repeats = 2, repeatDelay = 0.5f }
                }
            });
            Add(new EnemyDef
            {
                id = "shoggoth", name = "Shoggoth", biome = 3, hp = 340, armor = 200, speed = 2.6f, radius = 1.3f, height = 3.2f, threat = 7, eliteOnly = true, knockResist = 0.9f,
                move = MoveStyle.Chase, preferredRange = 2.5f, accent = new Color(0.4f, 1f, 0.5f), model = t => EnemyArt.Shoggoth(t), deathSpawn = "shoggoth_blob", deathSpawnCount = 3,
                lore = "Masas protoplásmicas creadas por los Antiguos como esclavos, que acabaron rebelándose. Gritan '¡Tekeli-li!' con las voces de sus amos muertos.",
                attacks =
                {
                    new AttackDef { kind = AtkKind.Slam, range = 3.2f, cooldown = 2.5f, windup = 0.9f, damage = 18, radius = 4f, knock = 10 },
                    new AttackDef { kind = AtkKind.Lunge, range = 7f, minRange = 3f, cooldown = 4f, windup = 1f, damage = 20, length = 7f, width = 1.4f, knock = 12 }
                }
            });
            Add(new EnemyDef
            {
                id = "shoggoth_blob", name = "Masa de Shoggoth", biome = 3, hp = 40, speed = 4.4f, radius = 0.45f, height = 0.9f, threat = 1,
                move = MoveStyle.Chase, preferredRange = 0.8f, accent = new Color(0.4f, 1f, 0.5f), model = t => EnemyArt.Shoggoth(t, 0.6f),
                lore = "Un fragmento de shoggoth que se desprendió y sigue vivo, y hambriento.",
                attacks = { new AttackDef { kind = AtkKind.Explode, range = 1.6f, cooldown = 3f, windup = 0.9f, damage = 14, radius = 2.2f, knock = 8 } }
            });
            Add(new EnemyDef { id = "boss_nyarlathotep", name = "Nyarlathotep", biome = 3, hp = 6000, speed = 3.5f, radius = 1.2f, height = 4.5f, knockResist = 1f, accent = new Color(1f, 0.3f, 0.2f), model = EnemyArt.Pharaoh,
                lore = "El Caos Reptante, alma y mensajero de los Dioses Exteriores, el de las Mil Formas. Guarda la Última Puerta para que nada de la sangre de Yog-Sothoth escape sin su permiso. Se divierte." });

            // ================== RIVAL ==================
            Add(new EnemyDef { id = "boss_wilbur", name = "Eco de Wilbur Whateley", biome = -1, hp = 900, speed = 4.2f, radius = 0.7f, height = 2.6f, knockResist = 0.9f, accent = new Color(1f, 0.6f, 0.3f), model = t => CharacterArt.Wilbur(t, true),
                lore = "El eco de tu hermano mayor, muerto en 1928 en la biblioteca de la Universidad Miskatonic cuando intentaba robar el Necronomicón. Él también quiere salir... a su manera." });

            Add(new EnemyDef { id = "dummy", name = "Monigote de Zadok", biome = -1, hp = 99999, speed = 0, radius = 0.6f, height = 2.2f, accent = Color.white, model = CharacterArt.Dummy,
                lore = "Zadok Allen lo talló con madera de barco. Dice que se parece a su abuelo. 'Golpéalo todo lo que quieras, cría, él ya no siente nada.'" });
        }
    }
}
