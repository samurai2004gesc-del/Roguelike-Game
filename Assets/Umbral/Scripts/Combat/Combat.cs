using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Resolución central de daño y consultas espaciales (arco, círculo, línea).
    /// Aquí se aplican los modificadores de boones, críticos, golpes por la espalda, armadura,
    /// vulnerabilidades por estados, esquiva del jugador y "Último Umbral" (desafío a la muerte).
    /// </summary>
    public static class Combat
    {
        public const float CritMultiplier = 3f;
        public static event Action<Combatant, Hit> OnDeath;
        public static event Action<Combatant, Hit, float> OnDamageDealt;   // objetivo, golpe, daño final

        static readonly List<Combatant> buffer = new List<Combatant>();

        public static void RaiseDeath(Combatant c, Hit h) => OnDeath?.Invoke(c, h);

        /// <summary>Calcula y aplica un golpe. Devuelve el daño final infligido (0 si se evitó).</summary>
        public static float Deal(Combatant target, Hit hit)
        {
            if (target == null || !target.Alive) return 0;
            if (target.Invulnerable && !target.isDummy) return 0;

            var mods = RunState.Current?.mods;
            float dmg = hit.damage;
            bool crit = false;
            bool fromPlayerSide = hit.source != null && hit.source.team == Team.Player;

            // ---------- Defensa del jugador ----------
            if (target.team == Team.Player && !target.isAlly)
            {
                var player = target as PlayerController;
                if (mods != null)
                {
                    if (mods.dodgeChance > 0 && UnityEngine.Random.value < mods.dodgeChance)
                    {
                        UIFloat.Text(target.transform.position + Vector3.up * target.height, "¡Esquiva!", new Color(0.7f, 0.9f, 1f), 0.9f);
                        Sfx.Play(SfxId.Dodge, target.transform.position);
                        target.invulnUntil = Time.time + 0.3f;
                        return 0;
                    }
                    dmg *= Mathf.Max(0.1f, 1f - mods.damageResist);
                    dmg *= RunState.Current.vowEnemyDamageMult;
                    dmg *= RunState.Current.chaosEnemyDamageMult;
                }
                if (hit.source != null && hit.source.status != null) dmg *= hit.source.status.OutgoingMult();
                if (player != null) dmg = player.ModifyIncoming(dmg, hit);
                if (dmg <= 0) return 0;
            }

            // ---------- Ofensiva del jugador ----------
            if (fromPlayerSide && target.team == Team.Enemy && mods != null)
            {
                if (!hit.secondary)
                {
                    dmg *= mods.SlotMult(hit.slot);
                    if (hit.omega) dmg *= mods.omegaDamageMult;
                }
                dmg *= mods.globalDamageMult;
                if (target.isBoss) dmg *= mods.bossDamageMult;
                if (target.armor > 0) dmg *= mods.armorDamageMult;
                if (mods.firstSecondsBonus > 0 && RoomDirector.EncounterTime < 10f) dmg *= 1f + mods.firstSecondsBonus;
                dmg *= mods.ConditionalMult(target, hit);

                // críticos (golpes por la espalda con las Colmillos de Tindalos, Nodens, Hastur...)
                if (hit.canCrit)
                {
                    float chance = mods.critChance + hit.extraCrit + mods.SlotCrit(hit.slot) + mods.CritVsTarget(target);
                    if (IsBackstab(target, hit.origin)) chance += mods.backstabCrit;
                    if (chance > 0 && UnityEngine.Random.value < chance) { crit = true; dmg *= CritMultiplier * mods.critDamageMult; }
                }
            }
            if (target.status != null) dmg *= target.status.IncomingMult(hit);
            if (hit.source != null && hit.source.team == Team.Enemy && target.team == Team.Enemy) dmg *= 1.2f; // fuego amigo por Locura

            dmg = Mathf.Max(0, dmg);
            if (target.isDummy) target.hp = Mathf.Max(1, target.hp);
            float dealt = target.ReceiveDamage(hit, dmg);

            // ---------- Retroalimentación ----------
            Vector3 numPos = target.transform.position + Vector3.up * (target.height + 0.2f);
            if (!hit.hidden)
            {
                Color c = crit ? new Color(1f, 0.85f, 0.2f) : Hit.ColorOf(hit.type);
                if (target.team == Team.Player && !target.isAlly) c = new Color(1f, 0.3f, 0.3f);
                string txt = Mathf.Max(1, Mathf.RoundToInt(dmg)).ToString() + (crit ? "!" : "");
                UIFloat.Number(numPos, txt, c, crit ? 1.45f : (hit.secondary ? 0.8f : 1f));
            }
            if (!hit.secondary)
            {
                Sfx.Play(crit ? SfxId.Crit : (target.armor > 0 ? SfxId.HitArmor : SfxId.Hit), target.transform.position);
                Fx.HitSpark(target.transform.position + Vector3.up * target.height * 0.5f, hit.dir, crit ? new Color(1f, 0.85f, 0.3f) : Hit.ColorOf(hit.type));
                if (!hit.noHitStop) HitStop.Do(crit ? 0.085f : (hit.omega ? 0.07f : 0.04f));
                if (crit) CameraRig.Shake(0.25f);
            }
            if (hit.knockback > 0) target.Knock(hit.dir, hit.knockback);

            OnDamageDealt?.Invoke(target, hit, dmg);

            // efectos de boons al golpear
            if (fromPlayerSide && target.team == Team.Enemy && mods != null)
                mods.FireOnHit(target, hit, dmg, crit);

            if (target.team == Team.Player && !target.isAlly)
            {
                CameraRig.Shake(0.35f);
                HitStop.Do(0.06f);
                Sfx.Play(SfxId.PlayerHurt, target.transform.position);
            }
            return dmg;
        }

        public static bool IsBackstab(Combatant target, Vector2 attackerPos)
        {
            if (target == null || target.isBoss) return false;
            Vector2 toAttacker = (attackerPos - target.Pos).SafeNormal(Vector2.up);
            return Vector2.Dot(target.facing, toAttacker) < -0.35f;
        }

        // ---------------- Consultas espaciales ----------------

        public static List<Combatant> InCircle(Vector2 center, float radius, Team hostileTo)
        {
            buffer.Clear();
            foreach (var c in Combatant.All)
            {
                if (c == null || !c.Alive || !c.targetable || c.team == hostileTo) continue;
                if ((c.Pos - center).sqrMagnitude <= (radius + c.radius) * (radius + c.radius)) buffer.Add(c);
            }
            return new List<Combatant>(buffer);
        }

        public static List<Combatant> InArc(Vector2 origin, Vector2 dir, float radius, float angleDeg, Team hostileTo)
        {
            var res = new List<Combatant>();
            dir = dir.SafeNormal(Vector2.up);
            float cos = Mathf.Cos(angleDeg * 0.5f * Mathf.Deg2Rad);
            foreach (var c in Combatant.All)
            {
                if (c == null || !c.Alive || !c.targetable || c.team == hostileTo) continue;
                Vector2 d = c.Pos - origin;
                float dist = d.magnitude;
                if (dist > radius + c.radius) continue;
                if (dist < c.radius + 0.3f) { res.Add(c); continue; }
                // margen angular por el radio del objetivo
                float margin = Mathf.Atan2(c.radius, dist);
                float ang = Mathf.Acos(Mathf.Clamp(Vector2.Dot(d / dist, dir), -1, 1));
                if (ang <= angleDeg * 0.5f * Mathf.Deg2Rad + margin) res.Add(c);
            }
            return res;
        }

        public static List<Combatant> InLine(Vector2 from, Vector2 to, float halfWidth, Team hostileTo)
        {
            var res = new List<Combatant>();
            foreach (var c in Combatant.All)
            {
                if (c == null || !c.Alive || !c.targetable || c.team == hostileTo) continue;
                if (U.DistToSegment(c.Pos, from, to, out _) <= halfWidth + c.radius) res.Add(c);
            }
            return res;
        }

        public static Combatant Nearest(Vector2 p, Team hostileTo, float maxDist = 999f, Combatant exclude = null, ICollection<Combatant> excludeSet = null)
        {
            Combatant best = null; float bd = maxDist * maxDist;
            foreach (var c in Combatant.All)
            {
                if (c == null || !c.Alive || !c.targetable || c.team == hostileTo || c == exclude) continue;
                if (excludeSet != null && excludeSet.Contains(c)) continue;
                float d = (c.Pos - p).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        public static int CountHostiles(Team hostileTo)
        {
            int n = 0;
            foreach (var c in Combatant.All)
                if (c != null && c.Alive && c.team != hostileTo && !c.isDummy && !c.isAlly) n++;
            return n;
        }

        /// <summary>Aplica un golpe a todos en un círculo. Devuelve cuántos fueron alcanzados.</summary>
        public static int Explode(Vector2 center, float radius, Hit template, Team hostileTo, float knock = 0f)
        {
            int n = 0;
            foreach (var c in InCircle(center, radius, hostileTo))
            {
                var h = template;
                h.dir = (c.Pos - center).SafeNormal(Vector2.up);
                h.knockback = knock > 0 ? knock : template.knockback;
                h.origin = center;
                Deal(c, h);
                n++;
            }
            return n;
        }
    }
}
