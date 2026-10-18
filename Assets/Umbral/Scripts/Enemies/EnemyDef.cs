using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    public enum MoveStyle { Chase, Kite, Hover, Ambush, Stationary }

    public enum AtkKind { Melee, Lunge, Charge, Slam, SlamAtTarget, Projectile, Radial, Lob, Summon, Teleport, Beam, Aura, Explode, Web }

    public class AttackDef
    {
        public AtkKind kind;
        public float range = 2.5f;        // distancia a la que se decide usarlo
        public float minRange;            // no usarlo si el objetivo está demasiado cerca
        public float cooldown = 2.5f;
        public float windup = 0.7f;
        public float recover = 0.5f;
        public float damage = 10f;
        public float radius = 2f;         // área/alcance del golpe
        public float angle = 100f;        // cono
        public float width = 1f;          // línea
        public float length = 6f;         // línea / embestida
        public int count = 1;
        public float spread = 0f;
        public float speed = 12f;
        public float knock = 6f;
        public string summonId;
        public StatusType? status;
        public float statusPower;
        public float weight = 1f;
        public bool tracking = true;
        public int repeats = 1;           // ráfagas
        public float repeatDelay = 0.25f;
        public string sfx;
    }

    public class EnemyDef
    {
        public string id, name, lore;
        public int biome;
        public float hp = 50, armor;
        public float speed = 3.5f;
        public float radius = 0.5f, height = 1.6f;
        public float knockResist;
        public MoveStyle move = MoveStyle.Chase;
        public float preferredRange = 1.5f;
        public float aggression = 1f;      // >1 ataca más a menudo
        public int threat = 1;             // coste en el presupuesto de oleadas
        public bool flying;
        public bool eliteOnly;
        public string deathSpawn; public int deathSpawnCount;
        public List<AttackDef> attacks = new List<AttackDef>();
        public Func<Transform, ModelRig> model;
        public Color accent = Color.red;
    }
}
