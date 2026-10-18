using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Catálogo de boones. Estructura calcada de Hades II:
    ///  · 5 boones "de ranura" por dios (Ataque, Especial, Signo/lanzamiento, Esprint, Lucidez/ganancia) — solo uno por ranura.
    ///  · Pasivos que potencian la maldición del dios.
    ///  · Infusión (requiere N boones de un elemento), Legendario (requiere pasivo) y Dúos (requieren dos dioses).
    ///  · Gatos de Ulthar (equivalente a Hermes) y Bendiciones/Maldiciones de Azathoth (equivalente al Caos).
    /// </summary>
    public static class BoonDB
    {
        public static readonly List<BoonDef> All = new List<BoonDef>();
        static readonly Dictionary<string, BoonDef> byId = new Dictionary<string, BoonDef>();
        public static BoonDef Get(string id) => id != null && byId.TryGetValue(id, out var b) ? b : null;

        static PlayerController P => PlayerController.Instance;
        static float Pct(float v) => v / 100f;

        static BoonDef Add(BoonDef b) { All.Add(b); byId[b.id] = b; if (b.element == Element.None && b.God != null && b.kind != BoonKind.Duo) b.element = b.God.element; return b; }

        static BoonDef CoreHit(string god, MoveSlot slot, string id, string name, string extra, float[] vals)
        {
            return Add(new BoonDef
            {
                id = id, god = god, name = name, slot = slot, kind = BoonKind.Core, values = vals, unit = "%",
                desc = $"Tu {BoonInstance.SlotName(slot)} inflige +{{v}} de daño y aplica {Gods.Get(god).curseName}." + extra,
                apply = (m, v, b) =>
                {
                    m.slotMult[(int)slot] *= 1f + Pct(v);
                    m.slotStatus[(int)slot] = Gods.Get(god).curse;
                    m.onHit[(int)slot].Add((t, h, d, c) => BoonFx.Curse(god, t, h, slot == MoveSlot.Special ? 2 : 1));
                }
            });
        }

        static BoonDB()
        {
            // =====================================================================
            // CTHULHU — Marea (Agua)
            // =====================================================================
            CoreHit("cthulhu", MoveSlot.Attack, "cth_attack", "Garra del Soñador", "", new[] { 40f, 50, 60, 70 });
            Add(new BoonDef
            {
                id = "cth_special", god = "cthulhu", name = "Ola de R'lyeh", slot = MoveSlot.Special, kind = BoonKind.Core, values = new[] { 50f, 60, 70, 80 }, unit = "%",
                desc = "Tu Especial inflige +{v} de daño, aplica Marea y desata una ola alrededor del impacto.",
                apply = (m, v, b) =>
                {
                    m.slotMult[(int)MoveSlot.Special] *= 1 + Pct(v);
                    m.slotStatus[(int)MoveSlot.Special] = StatusType.Marea;
                    m.onHit[(int)MoveSlot.Special].Add((t, h, d, c) =>
                    {
                        BoonFx.Curse("cthulhu", t, h);
                        if (h.tag != "ola") BoonFx.Wave("cthulhu", t.Pos, 2.2f, 15f + v * 0.2f, 4f);
                    });
                }
            });
            Add(new BoonDef
            {
                id = "cth_cast", god = "cthulhu", name = "Tumba Sumergida", slot = MoveSlot.Cast, kind = BoonKind.Core, values = new[] { 60f, 75, 90, 105 },
                desc = "Tu Signo Arcano desata una marea al formarse que inflige {v} de daño y aplica Marea.",
                apply = (m, v, b) => { m.slotStatus[(int)MoveSlot.Cast] = StatusType.Marea; m.onCastPlaced.Add((c, r) => BoonFx.Wave("cthulhu", c, r * 1.1f, v, 8f)); }
            });
            Add(new BoonDef
            {
                id = "cth_sprint", god = "cthulhu", name = "Marea Negra", slot = MoveSlot.Sprint, kind = BoonKind.Core, values = new[] { 30f, 40, 50, 60 },
                desc = "Al esprintar dejas una ola que inflige {v} de daño, empuja y aplica Marea.",
                apply = (m, v, b) => m.onDash.Add((from, to) => BoonFx.Wave("cthulhu", to, 2.4f, v, 7f))
            });
            Add(new BoonDef
            {
                id = "cth_magick", god = "cthulhu", name = "Ganancia del Sueño Profundo", slot = MoveSlot.Magick, kind = BoonKind.Core, values = new[] { 3f, 4, 5, 6 },
                desc = "Recuperas {v} de Lucidez cada vez que aplicas Marea.",
                apply = (m, v, b) => m.onStatusApplied.Add((t, s) => { if (s == StatusType.Marea) BoonFx.Restore(v); })
            });
            Add(new BoonDef
            {
                id = "cth_abrazo", god = "cthulhu", name = "Abrazo Abisal", values = new[] { 10f, 15, 20, 25 }, unit = "%", requiresCurse = StatusType.Marea,
                desc = "Los enemigos con Marea reciben +{v} de daño adicional.",
                apply = (m, v, b) => m.mareaVuln += Pct(v)
            });
            Add(new BoonDef
            {
                id = "cth_llamada", god = "cthulhu", name = "Llamada de las Profundidades", values = new[] { 40f, 55, 70, 85 }, requiresCurse = StatusType.Marea,
                desc = "Los enemigos que mueren con Marea estallan en una ola que inflige {v} de daño.",
                apply = (m, v, b) => m.onKill.Add((t, h) => { if (t.status.Has(StatusType.Marea)) BoonFx.Wave("cthulhu", t.Pos, 3f, v, 6f); })
            });
            Add(new BoonDef
            {
                id = "cth_coral", god = "cthulhu", name = "Coral Resistente", values = new[] { 8f, 11, 14, 17 }, unit = "%",
                desc = "Recibes -{v} de daño de todas las fuentes.",
                apply = (m, v, b) => m.damageResist += Pct(v)
            });
            Add(new BoonDef
            {
                id = "cth_infusion", god = "cthulhu", name = "Presión Abisal", kind = BoonKind.Infusion, infusionElement = Element.Agua, infusionCost = 3, values = new[] { 30f }, unit = "%", levelable = false,
                desc = "INFUSIÓN (3 Agua): los enemigos con Marea quedan {v} más lentos.",
                apply = (m, v, b) => m.mareaSlow += Pct(v)
            });
            Add(new BoonDef
            {
                id = "cth_legend", god = "cthulhu", name = "R'lyeh Asciende", kind = BoonKind.Legendary, values = new[] { 150f }, levelable = false,
                requiresAny = new[] { "cth_abrazo", "cth_llamada" },
                desc = "LEGENDARIO: cada 6 s, un tentáculo de Cthulhu brota bajo un enemigo e inflige {v} de daño.",
                apply = (m, v, b) => m.periodic.Add(new PlayerModifiers.Periodic { interval = 6f, action = () => { var e = BoonFx.RandomEnemy(); if (e != null) BoonFx.Eruption("cthulhu", e.Pos, 2.2f, v, 0.6f); } })
            });

            // =====================================================================
            // HASTUR — Locura (Aire)
            // =====================================================================
            CoreHit("hastur", MoveSlot.Attack, "has_attack", "Signo Amarillo", "", new[] { 40f, 50, 60, 70 });
            CoreHit("hastur", MoveSlot.Special, "has_special", "Lamento de Carcosa", "", new[] { 60f, 70, 80, 90 });
            Add(new BoonDef
            {
                id = "has_cast", god = "hastur", name = "Velo del Rey", slot = MoveSlot.Cast, kind = BoonKind.Core, values = new[] { 20f, 26, 32, 38 }, unit = "%",
                desc = "Los enemigos atados por tu Signo enloquecen y reciben +{v} de daño.",
                apply = (m, v, b) =>
                {
                    m.slotStatus[(int)MoveSlot.Cast] = StatusType.Locura;
                    m.boundVuln += Pct(v);
                    m.onCastTick.Add((c, r, dt) => { foreach (var t in Combat.InCircle(c, r, Team.Player)) if (!t.status.Has(StatusType.Locura)) BoonFx.Curse("hastur", t, default); });
                }
            });
            Add(new BoonDef
            {
                id = "has_sprint", god = "hastur", name = "Paso Inefable", slot = MoveSlot.Sprint, kind = BoonKind.Core, values = new[] { 50f, 65, 80, 95 }, unit = "%",
                desc = "Tras esprintar, tu siguiente Ataque o Especial inflige +{v} de daño y aplica Locura.",
                apply = (m, v, b) =>
                {
                    m.onDash.Add((f, t) => { if (P != null) P.empoweredNext = Pct(v); });
                    m.onAnyHit.Add((t, h, d, c) => { if (h.tag == "empowered") BoonFx.Curse("hastur", t, h); });
                }
            });
            Add(new BoonDef
            {
                id = "has_magick", god = "hastur", name = "Ganancia de Hali", slot = MoveSlot.Magick, kind = BoonKind.Core, values = new[] { 40f, 55, 70, 85 }, unit = "%",
                desc = "Al recibir daño recuperas Lucidez igual al {v} del daño recibido... y algo de cordura.",
                apply = (m, v, b) => m.onPlayerHurt.Add(d => BoonFx.Restore(d * Pct(v) * 2f))
            });
            Add(new BoonDef
            {
                id = "has_mascara", god = "hastur", name = "La Máscara Pálida", values = new[] { 10f, 15, 20, 25 }, unit = "%", requiresCurse = StatusType.Locura,
                desc = "La Locura reduce el daño enemigo un {v} adicional.",
                apply = (m, v, b) => m.locuraWeak = Mathf.Min(0.8f, m.locuraWeak + Pct(v))
            });
            Add(new BoonDef
            {
                id = "has_rey", god = "hastur", name = "El Rey de Amarillo", values = new[] { 20f, 30, 40, 50 }, unit = "%", requiresCurse = StatusType.Locura,
                desc = "Al aplicar Locura hay un {v} de probabilidad de que el enemigo se vuelva Traidor y ataque a sus aliados.",
                apply = (m, v, b) => m.locuraTurncoat += Pct(v)
            });
            Add(new BoonDef
            {
                id = "has_hali", god = "hastur", name = "Orillas de Hali", values = new[] { 8f, 10, 12, 14 }, unit = "%", requiresCurse = StatusType.Locura,
                desc = "+{v} de probabilidad de crítico contra enemigos con Locura.",
                apply = (m, v, b) => m.critVsMad += Pct(v)
            });
            Add(new BoonDef
            {
                id = "has_infusion", god = "hastur", name = "Signo Expansivo", kind = BoonKind.Infusion, infusionElement = Element.Aire, infusionCost = 3, values = new[] { 4f }, levelable = false,
                desc = "INFUSIÓN (3 Aire): cuando un enemigo con Locura muere, contagia la Locura a todos en {v} m.",
                apply = (m, v, b) => m.onKill.Add((t, h) => { if (t.status.Has(StatusType.Locura)) foreach (var e in Combat.InCircle(t.Pos, v, Team.Player)) BoonFx.Curse("hastur", e, default); })
            });
            Add(new BoonDef
            {
                id = "has_legend", god = "hastur", name = "La Obra Prohibida", kind = BoonKind.Legendary, values = new[] { 100f }, levelable = false,
                requiresAny = new[] { "has_mascara", "has_rey", "has_hali" },
                desc = "LEGENDARIO: los enemigos que mueren con Locura leen el segundo acto y estallan en {v} de daño de locura.",
                apply = (m, v, b) => m.onKill.Add((t, h) => { if (t.status.Has(StatusType.Locura)) BoonFx.Wave("hastur", t.Pos, 3.2f, v, 3f); })
            });

            // =====================================================================
            // SHUB-NIGGURATH — Semilla (Tierra)
            // =====================================================================
            CoreHit("shub", MoveSlot.Attack, "shub_attack", "Zarpa de la Cabra Negra", "", new[] { 40f, 50, 60, 70 });
            CoreHit("shub", MoveSlot.Special, "shub_special", "Espinas del Bosque", "", new[] { 50f, 60, 70, 80 });
            Add(new BoonDef
            {
                id = "shub_cast", god = "shub", name = "Raíces Negras", slot = MoveSlot.Cast, kind = BoonKind.Core, values = new[] { 20f, 26, 32, 38 },
                desc = "Tu Signo hace brotar raíces que infligen {v} de daño por segundo y aplican Semilla.",
                apply = (m, v, b) =>
                {
                    m.slotStatus[(int)MoveSlot.Cast] = StatusType.Semilla;
                    float acc = 0;
                    m.onCastTick.Add((c, r, dt) =>
                    {
                        acc += dt;
                        if (acc < 0.5f) return;
                        acc = 0;
                        foreach (var t in Combat.InCircle(c, r, Team.Player))
                        {
                            Combat.Deal(t, Hit.Proc(v * 0.5f, P, DamageType.Earth, "raices"));
                            BoonFx.Curse("shub", t, default);
                        }
                        Fx.Burst(c.X0Z(0.2f), Gods.Get("shub").color, 6, 3f, 0.25f, 0.5f);
                    });
                }
            });
            Add(new BoonDef
            {
                id = "shub_sprint", god = "shub", name = "Pezuñas del Bosque", slot = MoveSlot.Sprint, kind = BoonKind.Core, values = new[] { 12f, 16, 20, 24 },
                desc = "Al esprintar dejas un rastro de espinas que inflige {v} de daño y aplica Semilla.",
                apply = (m, v, b) => m.onSprintTick.Add(pos => Hazards.Spawn("shub", pos, 1.1f, 2.5f, v, true))
            });
            Add(new BoonDef
            {
                id = "shub_magick", god = "shub", name = "Ganancia Fértil", slot = MoveSlot.Magick, kind = BoonKind.Core, values = new[] { 12f, 16, 20, 24 },
                desc = "Cada Retoño Oscuro que brota te devuelve {v} de Lucidez.",
                apply = (m, v, b) => m.onAllySpawn.Add(a => BoonFx.Restore(v))
            });
            Add(new BoonDef
            {
                id = "shub_crias", god = "shub", name = "Mil Crías", values = new[] { 30f, 45, 60, 75 }, unit = "%", requiresCurse = StatusType.Semilla,
                desc = "Tus Retoños Oscuros duran y golpean un {v} más.",
                apply = (m, v, b) => { m.retonoDuration *= 1 + Pct(v); m.retonoDamage *= 1 + Pct(v); }
            });
            Add(new BoonDef
            {
                id = "shub_savia", god = "shub", name = "Savia Negra", values = new[] { 1f, 2, 3, 4 },
                desc = "Recuperas {v} de vida por cada enemigo derrotado.",
                apply = (m, v, b) => m.healOnKill += v
            });
            Add(new BoonDef
            {
                id = "shub_corteza", god = "shub", name = "Corteza Antigua", values = new[] { 20f, 30, 40, 50 },
                desc = "+{v} de vida máxima.",
                apply = (m, v, b) => m.maxHpBonus += v
            });
            Add(new BoonDef
            {
                id = "shub_podredumbre", god = "shub", name = "Podredumbre Fértil", values = new[] { 50f, 75, 100, 125 }, unit = "%", requiresCurse = StatusType.Semilla,
                desc = "La Semilla inflige un {v} más de daño.",
                apply = (m, v, b) => m.semillaDps *= 1 + Pct(v)
            });
            Add(new BoonDef
            {
                id = "shub_infusion", god = "shub", name = "Raíz Profunda", kind = BoonKind.Infusion, infusionElement = Element.Tierra, infusionCost = 3, values = new[] { 3f }, levelable = false,
                desc = "INFUSIÓN (3 Tierra): la Semilla inflige x{v} de daño a enemigos Atados.",
                apply = (m, v, b) => m.semillaBoundMult = v
            });
            Add(new BoonDef
            {
                id = "shub_legend", god = "shub", name = "¡Iä! ¡Shub-Niggurath!", kind = BoonKind.Legendary, values = new[] { 80f }, levelable = false,
                requiresAny = new[] { "shub_crias", "shub_podredumbre" },
                desc = "LEGENDARIO: tus Retoños estallan al marchitarse e infligen {v} de daño.",
                apply = (m, v, b) => m.flags.Add("retono_explota")
            });

            // =====================================================================
            // NODENS — Sentencia (Éter)
            // =====================================================================
            CoreHit("nodens", MoveSlot.Attack, "nod_attack", "Lanza del Abismo", "", new[] { 40f, 50, 60, 70 });
            Add(new BoonDef
            {
                id = "nod_special", god = "nodens", name = "Tridente Estelar", slot = MoveSlot.Special, kind = BoonKind.Core, values = new[] { 25f, 32, 39, 46 },
                desc = "Tu Especial lanza un rayo encadenado que salta entre 3 enemigos e inflige {v} de daño, aplicando Sentencia.",
                apply = (m, v, b) =>
                {
                    m.slotStatus[(int)MoveSlot.Special] = StatusType.Sentencia;
                    m.onHit[(int)MoveSlot.Special].Add((t, h, d, c) =>
                    {
                        BoonFx.Curse("nodens", t, h);
                        var set = new HashSet<Combatant> { t }; Vector2 from = t.Pos;
                        for (int i = 0; i < 3; i++)
                        {
                            var n = Combat.Nearest(from, Team.Player, 6f, null, set);
                            if (n == null) break;
                            set.Add(n);
                            Fx.Lightning(from.X0Z(1f), n.transform.position + Vector3.up, Gods.Get("nodens").color, 0.15f);
                            Combat.Deal(n, Hit.Proc(v, P, DamageType.Lightning, "cadena"));
                            BoonFx.Curse("nodens", n, default);
                            from = n.Pos;
                        }
                    });
                }
            });
            Add(new BoonDef
            {
                id = "nod_cast", god = "nodens", name = "Carro de Conchas", slot = MoveSlot.Cast, kind = BoonKind.Core, values = new[] { 25f, 32, 39, 46 },
                desc = "Mientras tu Signo esté activo, caen rayos sobre los enemigos atrapados infligiendo {v} de daño.",
                apply = (m, v, b) =>
                {
                    m.slotStatus[(int)MoveSlot.Cast] = StatusType.Sentencia;
                    float acc = 0;
                    m.onCastTick.Add((c, r, dt) =>
                    {
                        acc += dt; if (acc < 0.7f) return; acc = 0;
                        foreach (var t in Combat.InCircle(c, r, Team.Player))
                        {
                            Fx.Lightning(t.transform.position + Vector3.up * 8, t.transform.position, Gods.Get("nodens").color, 0.2f);
                            Combat.Deal(t, Hit.Proc(v, P, DamageType.Lightning, "carro"));
                            Sfx.Play(SfxId.Thunder, t.transform.position, 0.5f);
                        }
                    });
                }
            });
            Add(new BoonDef
            {
                id = "nod_sprint", god = "nodens", name = "Corcel del Vacío", slot = MoveSlot.Sprint, kind = BoonKind.Core, values = new[] { 40f, 50, 60, 70 },
                desc = "Al terminar tu esprint estalla un trueno que inflige {v} de daño y aplica Sentencia.",
                apply = (m, v, b) => m.onDash.Add((f, t) =>
                {
                    Fx.Lightning(t.X0Z(9f), t.X0Z(), Gods.Get("nodens").color, 0.3f);
                    BoonFx.Wave("nodens", t, 2.6f, v, 2f);
                })
            });
            Add(new BoonDef
            {
                id = "nod_magick", god = "nodens", name = "Ganancia del Cazador", slot = MoveSlot.Magick, kind = BoonKind.Core, values = new[] { 2f, 2.5f, 3f, 3.5f },
                desc = "Recuperas {v} de Lucidez por segundo.",
                apply = (m, v, b) => m.magickRegen += v
            });
            Add(new BoonDef
            {
                id = "nod_juicio", god = "nodens", name = "Juicio Antiguo", values = new[] { 40f, 60, 80, 100 }, unit = "%", requiresCurse = StatusType.Sentencia,
                desc = "Los rayos de Sentencia infligen +{v} de daño.",
                apply = (m, v, b) => m.sentenciaDamage *= 1 + Pct(v)
            });
            Add(new BoonDef
            {
                id = "nod_ojo", god = "nodens", name = "Ojo del Cazador", values = new[] { 6f, 8, 10, 12 }, unit = "%",
                desc = "+{v} de probabilidad de crítico.",
                apply = (m, v, b) => m.critChance += Pct(v)
            });
            Add(new BoonDef
            {
                id = "nod_guardian", god = "nodens", name = "Guardián del Sueño", values = new[] { 15f, 20, 25, 30 }, unit = "%",
                desc = "+{v} de daño contra jefes y guardianes.",
                apply = (m, v, b) => m.bossDamageMult *= 1 + Pct(v)
            });
            Add(new BoonDef
            {
                id = "nod_infusion", god = "nodens", name = "Cadena Divina", kind = BoonKind.Infusion, infusionElement = Element.Eter, infusionCost = 2, values = new[] { 2f }, levelable = false,
                desc = "INFUSIÓN (2 Éter): los rayos de Sentencia saltan a {v} enemigos más.",
                apply = (m, v, b) => m.sentenciaChains += Mathf.RoundToInt(v)
            });
            Add(new BoonDef
            {
                id = "nod_legend", god = "nodens", name = "Rival del Caos", kind = BoonKind.Legendary, values = new[] { 45f }, levelable = false,
                requiresAny = new[] { "nod_juicio", "nod_guardian" },
                desc = "LEGENDARIO: cada 2,5 s cae un rayo sobre un enemigo al azar ({v} de daño, aplica Sentencia).",
                apply = (m, v, b) => m.periodic.Add(new PlayerModifiers.Periodic
                {
                    interval = 2.5f, action = () =>
                    {
                        var e = BoonFx.RandomEnemy(); if (e == null) return;
                        Fx.Lightning(e.transform.position + Vector3.up * 10, e.transform.position, Gods.Get("nodens").color, 0.35f);
                        Sfx.Play(SfxId.Thunder, e.transform.position, 0.6f);
                        Combat.Deal(e, Hit.Proc(v, P, DamageType.Lightning, "rival"));
                        BoonFx.Curse("nodens", e, default);
                    }
                })
            });

            // =====================================================================
            // ITHAQUA — Escarcha (Aire)
            // =====================================================================
            CoreHit("ithaqua", MoveSlot.Attack, "ith_attack", "Viento Gélido", "", new[] { 40f, 50, 60, 70 });
            CoreHit("ithaqua", MoveSlot.Special, "ith_special", "Aliento Ártico", "", new[] { 50f, 60, 70, 80 });
            Add(new BoonDef
            {
                id = "ith_cast", god = "ithaqua", name = "Ventisca", slot = MoveSlot.Cast, kind = BoonKind.Core, values = new[] { 10f, 14, 18, 22 },
                desc = "Tu Signo se convierte en una ventisca: {v} de daño por segundo y 4 Escarcha por segundo.",
                apply = (m, v, b) =>
                {
                    m.slotStatus[(int)MoveSlot.Cast] = StatusType.Escarcha;
                    float acc = 0;
                    m.onCastTick.Add((c, r, dt) =>
                    {
                        acc += dt; if (acc < 0.5f) return; acc = 0;
                        foreach (var t in Combat.InCircle(c, r, Team.Player))
                        {
                            Combat.Deal(t, Hit.Proc(v * 0.5f, P, DamageType.Frost, "ventisca"));
                            t.status.Apply(StatusType.Escarcha, 2, 5f);
                        }
                        for (int i = 0; i < 6; i++) Fx.Emit(FxKind.Star, (c + Random.insideUnitCircle * r).X0Z(1.5f), new Vector3(2, -1, 1), new Color(0.8f, 0.95f, 1f), 0.2f, 0.7f);
                    });
                }
            });
            Add(new BoonDef
            {
                id = "ith_sprint", god = "ithaqua", name = "Caminante del Viento", slot = MoveSlot.Sprint, kind = BoonKind.Core, values = new[] { 20f, 26, 32, 38 }, unit = "%",
                desc = "Esprintas un {v} más rápido y tu esprint aplica Escarcha a los enemigos que atraviesas.",
                apply = (m, v, b) =>
                {
                    m.sprintSpeedMult *= 1 + Pct(v);
                    m.onDash.Add((f, t) => { foreach (var e in Combat.InLine(f, t, 1f, Team.Player)) e.status.Apply(StatusType.Escarcha, 3, 5f); });
                }
            });
            Add(new BoonDef
            {
                id = "ith_magick", god = "ithaqua", name = "Ganancia Boreal", slot = MoveSlot.Magick, kind = BoonKind.Core, values = new[] { 20f, 26, 32, 38 },
                desc = "Recuperas {v} de Lucidez cada vez que congelas a un enemigo.",
                apply = (m, v, b) => m.onFrozen.Add(t => BoonFx.Restore(v))
            });
            Add(new BoonDef
            {
                id = "ith_eterno", god = "ithaqua", name = "Hielo Eterno", values = new[] { 1f, 1.5f, 2f, 2.5f }, requiresCurse = StatusType.Escarcha,
                desc = "La congelación dura +{v} s.",
                apply = (m, v, b) => m.freezeDuration += v
            });
            Add(new BoonDef
            {
                id = "ith_rafaga", god = "ithaqua", name = "Ráfaga", values = new[] { 10f, 13, 16, 19 }, unit = "%",
                desc = "Tu Ataque y Especial son un {v} más rápidos.",
                apply = (m, v, b) => { m.attackSpeedMult *= 1 + Pct(v); m.specialSpeedMult *= 1 + Pct(v); }
            });
            Add(new BoonDef
            {
                id = "ith_wendigo", god = "ithaqua", name = "Hambre del Wendigo", values = new[] { 30f, 40, 50, 60 }, unit = "%", requiresCurse = StatusType.Escarcha,
                desc = "Los enemigos congelados reciben +{v} de daño.",
                apply = (m, v, b) => m.frozenVuln += Pct(v)
            });
            Add(new BoonDef
            {
                id = "ith_infusion", god = "ithaqua", name = "Ojos de Hielo", kind = BoonKind.Infusion, infusionElement = Element.Aire, infusionCost = 3, values = new[] { 8f }, unit = "%", levelable = false,
                desc = "INFUSIÓN (3 Aire): cualquier golpe tiene un {v} de probabilidad de congelar al instante.",
                apply = (m, v, b) => m.onAnyHit.Add((t, h, d, c) => { if (Random.value < Pct(v)) t.status.Apply(StatusType.Escarcha, 10, 5f); })
            });
            Add(new BoonDef
            {
                id = "ith_legend", god = "ithaqua", name = "La Gran Ventisca", kind = BoonKind.Legendary, values = new[] { 120f }, levelable = false,
                requiresAny = new[] { "ith_eterno", "ith_wendigo" },
                desc = "LEGENDARIO: los enemigos que mueren congelados se hacen añicos: {v} de daño y 5 Escarcha a su alrededor.",
                apply = (m, v, b) => m.onKill.Add((t, h) =>
                {
                    if (!t.status.Has(StatusType.Congelado)) return;
                    Fx.Burst(t.transform.position + Vector3.up, new Color(0.7f, 0.95f, 1f), 30, 10f, 0.3f, 0.6f, FxKind.Spark);
                    foreach (var e in Combat.InCircle(t.Pos, 3.5f, Team.Player))
                    {
                        Combat.Deal(e, Hit.Proc(v, P, DamageType.Frost, "anicos"));
                        e.status.Apply(StatusType.Escarcha, 5, 5f);
                    }
                })
            });

            // =====================================================================
            // CTHUGHA — Llama Viva (Fuego)
            // =====================================================================
            CoreHit("cthugha", MoveSlot.Attack, "cthg_attack", "Lengua de Fuego", "", new[] { 40f, 50, 60, 70 });
            CoreHit("cthugha", MoveSlot.Special, "cthg_special", "Llamarada de Fomalhaut", "", new[] { 50f, 60, 70, 80 });
            Add(new BoonDef
            {
                id = "cthg_cast", god = "cthugha", name = "Corona Solar", slot = MoveSlot.Cast, kind = BoonKind.Core, values = new[] { 80f, 100, 120, 140 },
                desc = "Tu Signo estalla tras 1 s infligiendo {v} de daño y aplicando 6 Llama Viva.",
                apply = (m, v, b) =>
                {
                    m.slotStatus[(int)MoveSlot.Cast] = StatusType.Llama;
                    m.onCastPlaced.Add((c, r) => Runner.Delay(1f, () =>
                    {
                        if (World.Root == null) return;
                        BoonFx.Wave("cthugha", c, r * 1.15f, v, 3f, false);
                        foreach (var t in Combat.InCircle(c, r * 1.15f, Team.Player)) t.status.Apply(StatusType.Llama, 6, 6f);
                        Fx.Light(c.X0Z(1.5f), Gods.Get("cthugha").color, 10f, 8f, 0.5f);
                        CameraRig.Shake(0.3f);
                    }));
                }
            });
            Add(new BoonDef
            {
                id = "cthg_sprint", god = "cthugha", name = "Paso Ígneo", slot = MoveSlot.Sprint, kind = BoonKind.Core, values = new[] { 3f, 4, 5, 6 },
                desc = "Al esprintar dejas un rastro de fuego que aplica {v} Llama Viva por segundo.",
                apply = (m, v, b) => m.onSprintTick.Add(pos => Hazards.Spawn("cthugha", pos, 1.1f, 2.5f, v, true))
            });
            Add(new BoonDef
            {
                id = "cthg_magick", god = "cthugha", name = "Ganancia Ardiente", slot = MoveSlot.Magick, kind = BoonKind.Core, values = new[] { 0.6f, 0.8f, 1f, 1.2f },
                desc = "Recuperas {v} de Lucidez por cada acumulación de Llama Viva que infliges.",
                apply = (m, v, b) => m.onStacks.Add((t, s, n) => { if (s == StatusType.Llama) BoonFx.Restore(v * n); })
            });
            Add(new BoonDef
            {
                id = "cthg_incendio", god = "cthugha", name = "Incendio", values = new[] { 50f, 75, 100, 125 }, unit = "%", requiresCurse = StatusType.Llama,
                desc = "La Llama Viva inflige un {v} más de daño.",
                apply = (m, v, b) => m.llamaDpsPerStack *= 1 + Pct(v)
            });
            Add(new BoonDef
            {
                id = "cthg_pira", god = "cthugha", name = "Pira Funeraria", values = new[] { 50f, 65, 80, 95 }, unit = "%", requiresCurse = StatusType.Llama,
                desc = "Al morir, los enemigos en llamas propagan el {v} de sus acumulaciones a los cercanos.",
                apply = (m, v, b) => m.onKill.Add((t, h) =>
                {
                    int st = t.status.Stacks(StatusType.Llama); if (st <= 0) return;
                    int spread = Mathf.Max(1, Mathf.RoundToInt(st * Pct(v)));
                    Fx.Ring(t.transform.position, 3f, Gods.Get("cthugha").color, 0.35f);
                    foreach (var e in Combat.InCircle(t.Pos, 3f, Team.Player)) e.status.Apply(StatusType.Llama, spread, 6f);
                })
            });
            Add(new BoonDef
            {
                id = "cthg_calor", god = "cthugha", name = "Calor Interno", values = new[] { 20f, 27, 34, 41 }, unit = "%",
                desc = "Tus movimientos Omega infligen +{v} de daño.",
                apply = (m, v, b) => m.omegaDamageMult *= 1 + Pct(v)
            });
            Add(new BoonDef
            {
                id = "cthg_infusion", god = "cthugha", name = "Sol Negro", kind = BoonKind.Infusion, infusionElement = Element.Fuego, infusionCost = 2, values = new[] { 10f }, levelable = false,
                desc = "INFUSIÓN (2 Fuego): la Llama Viva puede acumularse +{v} veces más.",
                apply = (m, v, b) => m.llamaMaxStacks += Mathf.RoundToInt(v)
            });
            Add(new BoonDef
            {
                id = "cthg_legend", god = "cthugha", name = "La Estrella Fomalhaut", kind = BoonKind.Legendary, values = new[] { 140f }, levelable = false,
                requiresAny = new[] { "cthg_incendio", "cthg_pira" },
                desc = "LEGENDARIO: cada 25 acumulaciones de Llama Viva que infliges, cae un meteoro ({v} de daño).",
                apply = (m, v, b) =>
                {
                    int count = 0;
                    m.onStacks.Add((t, s, n) =>
                    {
                        if (s != StatusType.Llama) return;
                        count += n;
                        if (count >= 25 && t != null) { count = 0; BoonFx.Eruption("cthugha", t.Pos, 2.6f, v, 0.7f); }
                    });
                }
            });

            // =====================================================================
            // DÚOS
            // =====================================================================
            Duo("duo_sueno_carcosa", "El Sueño de Carcosa", "cthulhu", "hastur", "Aplicar Locura también aplica Marea.", 0, (m, v, b) => m.flags.Add("duo_sueno_carcosa"));
            Duo("duo_mar_helado", "Mar Helado", "cthulhu", "ithaqua", "Aplicar Marea también aplica 2 Escarcha.", 0, (m, v, b) => m.flags.Add("duo_mar_helado"));
            Duo("duo_fuego_amarillo", "Fuego Amarillo", "hastur", "cthugha", "La Llama Viva inflige x{v} de daño a enemigos con Locura.", 2f, (m, v, b) => m.llamaVsMadMult = v);
            Duo("duo_bosque_llamas", "Bosque en Llamas", "shub", "cthugha", "Tus Retoños Oscuros aplican 3 Llama Viva con cada golpe.", 3f, (m, v, b) => m.onAllyHit.Add((t, h) => t.status.Apply(StatusType.Llama, 3, 6f)));
            Duo("duo_tormenta_abisal", "Tormenta Abisal", "nodens", "cthulhu", "Los rayos de Sentencia aplican Marea y crean una ola de {v} de daño.", 30f, (m, v, b) => m.onBolt.Add((t, d) => BoonFx.Wave("cthulhu", t.Pos, 2.4f, v, 5f)));
            Duo("duo_relampago_glacial", "Relámpago Glacial", "nodens", "ithaqua", "Los rayos de Sentencia aplican {v} Escarcha.", 4f, (m, v, b) => m.onBolt.Add((t, d) => t.status.Apply(StatusType.Escarcha, Mathf.RoundToInt(v), 5f)));
            Duo("duo_progenie_demente", "Progenie Demente", "shub", "hastur", "Tus Retoños aplican Locura y brotan también de enemigos que mueren con Locura.", 0, (m, v, b) => { m.flags.Add("retono_locura"); m.onAllyHit.Add((t, h) => BoonFx.Curse("hastur", t, h)); });
            Duo("duo_choque_termico", "Choque Térmico", "ithaqua", "cthugha", "Golpear a un enemigo congelado que tenga Llama Viva provoca una explosión de {v} de daño.", 100f, (m, v, b) => m.onAnyHit.Add((t, h, d, c) =>
            {
                if (t.status.Has(StatusType.Congelado) && t.status.Has(StatusType.Llama))
                {
                    t.status.Remove(StatusType.Congelado);
                    BoonFx.Wave("cthugha", t.Pos, 2.5f, v, 4f, false);
                }
            }));
            Duo("duo_caceria_salvaje", "Cacería Salvaje", "nodens", "shub", "Los golpes de tus Retoños aplican Sentencia.", 0, (m, v, b) => m.onAllyHit.Add((t, h) => BoonFx.Curse("nodens", t, h)));
            Duo("duo_abismo_helado", "Abismo Congelado", "cthulhu", "cthugha", "Las olas de Cthulhu dejan vapor hirviente: +{v} Llama Viva a los empapados.", 3f, (m, v, b) => m.onStatusApplied.Add((t, s) => { if (s == StatusType.Marea) t.status.Apply(StatusType.Llama, Mathf.RoundToInt(v), 6f); }));

            // =====================================================================
            // GATOS DE ULTHAR (como Hermes)
            // =====================================================================
            Cat("cat_pasos", "Pasos de Gato", "Te mueves un {v} más rápido.", new[] { 10f, 14, 18, 22 }, "%", (m, v, b) => m.moveSpeedMult *= 1 + Pct(v));
            Cat("cat_reflejos", "Reflejos Felinos", "{v} de probabilidad de esquivar cualquier golpe.", new[] { 8f, 11, 14, 17 }, "%", (m, v, b) => m.dodgeChance += Pct(v));
            Cat("cat_zarpazo", "Zarpazo Veloz", "Tu Ataque es un {v} más rápido.", new[] { 10f, 14, 18, 22 }, "%", (m, v, b) => m.attackSpeedMult *= 1 + Pct(v));
            Cat("cat_salto", "Salto de Tejado", "Tu esprint recorre un {v} más de distancia.", new[] { 20f, 28, 36, 44 }, "%", (m, v, b) => m.dashDistanceMult *= 1 + Pct(v));
            Cat("cat_lenguas", "Lenguas de Ulthar", "Obtienes un {v} más de Oro Onírico.", new[] { 30f, 40, 50, 60 }, "%", (m, v, b) => m.goldMult *= 1 + Pct(v));
            Cat("cat_ronroneo", "Ronroneo", "Al limpiar una sala recuperas {v} de vida.", new[] { 5f, 7, 9, 11 }, "", (m, v, b) => m.healOnRoomClear += v);
            Cat("cat_vidas", "Siete Vidas", "+{v} Último Umbral (resurrección).", new[] { 1f, 1, 1, 1 }, "", (m, v, b) => m.extraDeathDefiance += 1, weight: 0.4f, levelable: false);
            Cat("cat_signo", "Garras en el Signo", "Tu Signo Arcano dura un {v} más.", new[] { 30f, 40, 50, 60 }, "%", (m, v, b) => m.castDurationMult *= 1 + Pct(v));

            // =====================================================================
            // AZATHOTH — Bendiciones (permanentes) y Maldiciones (temporales)
            // =====================================================================
            Chaos("chaos_b_attack", "Pulso del Sultán: Ataque", "Tu Ataque inflige +{v} de daño.", new[] { 30f, 40, 50, 60 }, "%", (m, v, b) => m.slotMult[(int)MoveSlot.Attack] *= 1 + Pct(v));
            Chaos("chaos_b_special", "Pulso del Sultán: Especial", "Tu Especial inflige +{v} de daño.", new[] { 30f, 40, 50, 60 }, "%", (m, v, b) => m.slotMult[(int)MoveSlot.Special] *= 1 + Pct(v));
            Chaos("chaos_b_cast", "Pulso del Sultán: Signo", "Tu Signo cubre un {v} más de área.", new[] { 25f, 35, 45, 55 }, "%", (m, v, b) => m.castRadiusMult *= 1 + Pct(v));
            Chaos("chaos_b_crit", "Ojo de la Flauta", "+{v} de probabilidad de crítico.", new[] { 5f, 7, 9, 11 }, "%", (m, v, b) => m.critChance += Pct(v));
            Chaos("chaos_b_hp", "Carne del Vacío", "+{v} de vida máxima.", new[] { 25f, 35, 45, 55 }, "", (m, v, b) => m.maxHpBonus += v);
            Chaos("chaos_b_magick", "Mente Desbordada", "+{v} de Lucidez máxima.", new[] { 30f, 40, 50, 60 }, "", (m, v, b) => m.maxMagickBonus += v);
            Chaos("chaos_b_gold", "Tesoro Idiota", "Obtienes un {v} más de Oro Onírico.", new[] { 50f, 70, 90, 110 }, "%", (m, v, b) => m.goldMult *= 1 + Pct(v));
            Chaos("chaos_b_omega", "Gaita del Caos", "Tus movimientos Omega cuestan un {v} menos.", new[] { 20f, 26, 32, 38 }, "%", (m, v, b) => m.omegaCostMult *= 1 - Pct(v));

            ChaosCurse("chaos_c_dmg", "Maldición: Fauces", "Los enemigos infligen +50% de daño.", m => m.globalDamageMult *= 1f, extraEnemyDmg: 0.5f);
            ChaosCurse("chaos_c_weak", "Maldición: Brazos Inertes", "Tu Ataque inflige -40% de daño.", m => m.slotMult[(int)MoveSlot.Attack] *= 0.6f);
            ChaosCurse("chaos_c_mind", "Maldición: Mente Rota", "Tu Lucidez máxima se reduce un 50%.", m => m.maxMagickMult *= 0.5f);
            ChaosCurse("chaos_c_slow", "Maldición: Lodo Estelar", "Te mueves un 25% más lento.", m => m.moveSpeedMult *= 0.75f);
            ChaosCurse("chaos_c_cast", "Maldición: Signo Roto", "Tu Signo dura un 60% menos.", m => m.castDurationMult *= 0.4f);
        }

        static void Duo(string id, string name, string g1, string g2, string desc, float v, System.Action<PlayerModifiers, float, BoonInstance> apply)
        {
            Add(new BoonDef { id = id, name = name, god = g1, kind = BoonKind.Duo, requiresGods = new[] { g1, g2 }, desc = "DÚO: " + desc, values = new[] { v }, levelable = false, apply = apply });
        }

        static void Cat(string id, string name, string desc, float[] vals, string unit, System.Action<PlayerModifiers, float, BoonInstance> apply, float weight = 1f, bool levelable = true)
        {
            Add(new BoonDef { id = id, name = name, god = "gatos", kind = BoonKind.Cat, desc = desc, values = vals, unit = unit, apply = apply, weight = weight, levelable = levelable });
        }

        static void Chaos(string id, string name, string desc, float[] vals, string unit, System.Action<PlayerModifiers, float, BoonInstance> apply)
        {
            Add(new BoonDef { id = id, name = name, god = "azathoth", kind = BoonKind.Chaos, desc = desc, values = vals, unit = unit, apply = apply, levelable = true });
        }

        public static readonly Dictionary<string, float> chaosEnemyDamage = new Dictionary<string, float>();

        static void ChaosCurse(string id, string name, string desc, System.Action<PlayerModifiers> apply, float extraEnemyDmg = 0f)
        {
            if (extraEnemyDmg > 0) chaosEnemyDamage[id] = extraEnemyDmg;
            Add(new BoonDef { id = id, name = name, god = "azathoth_curse", kind = BoonKind.Chaos, desc = desc, values = new[] { 0f }, levelable = false, weight = 0, apply = (m, v, b) => apply(m) });
        }

        public static IEnumerable<BoonDef> ChaosBlessings => All.Where(b => b.god == "azathoth");
        public static IEnumerable<BoonDef> ChaosCurses => All.Where(b => b.god == "azathoth_curse");
        public static IEnumerable<BoonDef> CatBoons => All.Where(b => b.kind == BoonKind.Cat);
        public static IEnumerable<BoonDef> OfGod(string god) => All.Where(b => b.god == god && b.kind != BoonKind.Duo);
        public static IEnumerable<BoonDef> DuosOf(string god) => All.Where(b => b.kind == BoonKind.Duo && b.requiresGods.Contains(god));
    }
}
