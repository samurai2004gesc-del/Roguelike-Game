using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    public delegate void OnHitFn(Combatant target, Hit hit, float damage, bool crit);

    /// <summary>
    /// Todos los modificadores activos de la protagonista durante una incursión.
    /// Se reconstruye desde cero cada vez que cambian boones, cartas del Grimorio, reliquias, mejoras de arma o pactos.
    /// </summary>
    public class PlayerModifiers
    {
        public static readonly PlayerModifiers Default = new PlayerModifiers();

        // ---- Vida y defensa ----
        public float maxHpBonus;
        public float maxHpMult = 1f;
        public float damageResist;
        public float dodgeChance;
        public int extraDeathDefiance;
        public float healOnKill;
        public float healOnRoomClear;
        public float healMult = 1f;
        public float firstHitShield;           // reliquia del Guía: reduce el primer golpe de cada sala
        public float lowHpRescue;              // reactivo de West: cura al caer bajo 30%

        // ---- Lucidez (maná) ----
        public float maxMagickBonus;
        public float maxMagickMult = 1f;
        public float magickRegen;              // por segundo
        public float omegaCostMult = 1f;
        public float omegaChargeMult = 1f;     // >1 = carga más rápida
        public float hexChargeMult = 1f;
        public float hexPassiveCharge;         // carta "La Luna"

        // ---- Movilidad ----
        public float moveSpeedMult = 1f;
        public float sprintSpeedMult = 1f;
        public float dashDistanceMult = 1f;

        // ---- Ofensiva ----
        public float globalDamageMult = 1f;
        public readonly float[] slotMult = { 1, 1, 1, 1, 1, 1 };
        public readonly float[] slotCrit = new float[6];
        public float omegaDamageMult = 1f;
        public float bossDamageMult = 1f;
        public float armorDamageMult = 1f;
        public float firstSecondsBonus;
        public float critChance;
        public float critDamageMult = 1f;
        public float backstabCrit;
        public float critVsMad;
        public float attackSpeedMult = 1f;
        public float specialSpeedMult = 1f;
        public float fullMagickDamage;         // carta "El Caos Reptante"

        // ---- Signo Arcano (lanzamiento) ----
        public float castRadiusMult = 1f;
        public float castDurationMult = 1f;
        public int castCharges = 1;
        public float castVuln;                 // daño extra a enemigos atados (Hastur)

        // ---- Economía y recompensas ----
        public float goldMult = 1f;
        public float resourceMult = 1f;
        public int extraBoonChoices;           // carta/infusión
        public float rarityBonus;              // probabilidad extra de rareza
        public string favoredGod;              // reliquia: dios preferido

        // ---- Parámetros de estados ----
        public float mareaVuln = 0.15f;
        public float mareaSlow;
        public float mareaKnockMult = 1.5f;
        public float locuraWeak = 0.3f;
        public float locuraVuln;
        public float locuraTurncoat;           // probabilidad de que un enemigo con Locura ataque a sus aliados
        public float semillaDps = 6f;
        public float semillaBoundMult = 1f;
        public float retonoDuration = 6f;
        public float retonoDamage = 10f;
        public float sentenciaDamage = 40f;
        public int sentenciaChains;
        public float escarchaSlowPerStack = 0.05f;
        public float freezeDuration = 2f;
        public float frozenVuln;
        public float llamaDpsPerStack = 2f;
        public int llamaMaxStacks = 20;
        public float llamaVsMadMult = 1f;
        public float boundVuln;

        // ---- Ganchos (efectos de boones) ----
        public readonly List<OnHitFn>[] onHit = { new List<OnHitFn>(), new List<OnHitFn>(), new List<OnHitFn>(), new List<OnHitFn>(), new List<OnHitFn>(), new List<OnHitFn>() };
        public readonly List<OnHitFn> onAnyHit = new List<OnHitFn>();
        public readonly List<Action<Combatant, Hit>> onKill = new List<Action<Combatant, Hit>>();
        public readonly List<Action<Vector2, float>> onCastPlaced = new List<Action<Vector2, float>>();       // centro, radio
        public readonly List<Action<Vector2, float, float>> onCastTick = new List<Action<Vector2, float, float>>(); // centro, radio, dt
        public readonly List<Action<Vector2, Vector2>> onDash = new List<Action<Vector2, Vector2>>();        // desde, hasta
        public readonly List<Action<Vector2>> onSprintTick = new List<Action<Vector2>>();
        public readonly List<Action<float>> onPlayerHurt = new List<Action<float>>();
        public readonly List<Action<MoveSlot>> onOmega = new List<Action<MoveSlot>>();
        public readonly List<Action> onRoomStart = new List<Action>();
        public readonly List<Action> onRoomClear = new List<Action>();
        public readonly List<Action<Combatant, StatusType>> onStatusApplied = new List<Action<Combatant, StatusType>>();
        public readonly List<Action<Combatant, StatusType, int>> onStacks = new List<Action<Combatant, StatusType, int>>();
        public readonly List<Action<Combatant>> onFrozen = new List<Action<Combatant>>();
        public readonly List<Action<Combatant, float>> onBolt = new List<Action<Combatant, float>>();
        public readonly List<Action<Combatant>> onAllySpawn = new List<Action<Combatant>>();
        public readonly List<Action<Combatant, Hit>> onAllyHit = new List<Action<Combatant, Hit>>();
        public readonly List<Periodic> periodic = new List<Periodic>();
        public readonly List<Func<Combatant, Hit, float>> conditional = new List<Func<Combatant, Hit, float>>();

        /// <summary>Estado que aplica cada ranura (para dúos que preguntan "¿tu ataque aplica X?").</summary>
        public readonly StatusType?[] slotStatus = new StatusType?[6];

        // Banderas de dúos/legendarios
        public readonly HashSet<string> flags = new HashSet<string>();
        public bool Flag(string f) => flags.Contains(f);

        public class Periodic
        {
            public float interval;
            public float timer;
            public Action action;
        }

        public float SlotMult(MoveSlot s) => slotMult[(int)s];
        public float SlotCrit(MoveSlot s) => slotCrit[(int)s];

        public float CritVsTarget(Combatant t)
        {
            float c = 0;
            if (critVsMad > 0 && t.status != null && t.status.Has(StatusType.Locura)) c += critVsMad;
            if (Flag("st_ganzua") && t.status != null && t.status.Has(StatusType.Atado)) c += 0.25f;
            return c;
        }

        public float ConditionalMult(Combatant target, Hit hit)
        {
            float m = 1f;
            foreach (var f in conditional) m *= f(target, hit);
            if (fullMagickDamage > 0 && PlayerController.Instance != null && PlayerController.Instance.magick >= PlayerController.Instance.MaxMagick - 0.5f)
                m *= 1f + fullMagickDamage;
            return m;
        }

        public void FireOnHit(Combatant target, Hit hit, float dmg, bool crit)
        {
            if (hit.secondary) return;
            foreach (var f in onHit[(int)hit.slot]) { if (target == null) break; f(target, hit, dmg, crit); }
            foreach (var f in onAnyHit) { if (target == null) break; f(target, hit, dmg, crit); }
        }

        public void Tick(float dt)
        {
            foreach (var p in periodic)
            {
                p.timer += dt;
                if (p.timer >= p.interval) { p.timer = 0; p.action?.Invoke(); }
            }
        }
    }
}
