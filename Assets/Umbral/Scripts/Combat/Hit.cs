using UnityEngine;

namespace Umbral
{
    public enum Team { Player, Enemy }

    /// <summary>Ranura de movimiento, como en Hades II: cada dios mejora una ranura con un boon.</summary>
    public enum MoveSlot { None, Attack, Special, Cast, Sprint, Magick }

    /// <summary>Elemento del daño (determina el color del número y algunos efectos).</summary>
    public enum DamageType { Physical, Water, Madness, Earth, Lightning, Frost, Fire, Void }

    /// <summary>Descripción de un golpe. Se construye en el atacante y lo resuelve <see cref="Combat.Deal"/>.</summary>
    public struct Hit
    {
        public float damage;
        public Combatant source;
        public MoveSlot slot;
        public bool omega;
        public Vector2 dir;          // dirección del empuje
        public float knockback;      // fuerza de empuje (unidades/seg iniciales)
        public bool secondary;       // proviene de un efecto (estado, boon, eco): no dispara efectos al golpear
        public bool canCrit;
        public float extraCrit;      // probabilidad adicional de crítico
        public DamageType type;
        public bool ignoreArmor;
        public bool noHitStop;
        public bool hidden;          // sin número flotante
        public string tag;           // identificador para efectos encadenados
        public Vector2 origin;       // punto de origen del ataque (para golpes por la espalda)

        public static Hit Make(float dmg, Combatant src, MoveSlot slot, Vector2 dir, float knock = 0f)
        {
            return new Hit
            {
                damage = dmg, source = src, slot = slot, dir = dir, knockback = knock, canCrit = true,
                type = DamageType.Physical, origin = src != null ? src.Pos : Vector2.zero
            };
        }

        public static Hit Proc(float dmg, Combatant src, DamageType type, string tag = null)
        {
            return new Hit { damage = dmg, source = src, slot = MoveSlot.None, secondary = true, type = type, tag = tag, noHitStop = true, origin = src != null ? src.Pos : Vector2.zero };
        }

        public static Color ColorOf(DamageType t)
        {
            switch (t)
            {
                case DamageType.Water: return new Color(0.3f, 0.95f, 0.85f);
                case DamageType.Madness: return new Color(1f, 0.85f, 0.25f);
                case DamageType.Earth: return new Color(0.55f, 0.95f, 0.35f);
                case DamageType.Lightning: return new Color(0.75f, 0.7f, 1f);
                case DamageType.Frost: return new Color(0.7f, 0.92f, 1f);
                case DamageType.Fire: return new Color(1f, 0.55f, 0.15f);
                case DamageType.Void: return new Color(0.85f, 0.4f, 1f);
                default: return Color.white;
            }
        }
    }
}
