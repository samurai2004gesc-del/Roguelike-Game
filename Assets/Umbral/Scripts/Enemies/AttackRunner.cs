using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Ejecuta ataques enemigos descritos por <see cref="AttackDef"/>: siempre con aviso previo (zona roja que se llena),
    /// para que la lectura y el esprint sean la clave del combate, como en Hades.
    /// </summary>
    public static class AttackRunner
    {
        public static Color ProjColor = new Color(1f, 0.35f, 0.4f);

        static bool IsVictim(Enemy e, Combatant c)
        {
            if (c == null || c == e || !c.Alive || !c.targetable) return false;
            if (e.status.Has(StatusType.Traidor)) return c.team == Team.Enemy && !c.isDummy;
            return c.team == Team.Player && !c.isAlly;
        }

        static IEnumerable<Combatant> Victims(Enemy e)
        {
            foreach (var c in Combatant.All.ToArray()) if (IsVictim(e, c)) yield return c;
        }

        public static void HitArea(Enemy e, AttackDef a, Vector2 center, float radius, float dmgMult = 1f)
        {
            foreach (var c in Victims(e))
                if ((c.Pos - center).sqrMagnitude <= (radius + c.radius * 0.6f) * (radius + c.radius * 0.6f))
                    Strike(e, a, c, (c.Pos - center).SafeNormal(e.facing), dmgMult);
        }

        public static void HitCone(Enemy e, AttackDef a, Vector2 origin, Vector2 dir, float radius, float angle)
        {
            foreach (var c in Victims(e))
            {
                Vector2 d = c.Pos - origin;
                float dist = d.magnitude;
                if (dist > radius + c.radius * 0.6f) continue;
                if (dist > 0.5f && Vector2.Angle(dir, d) > angle * 0.5f + 8f) continue;
                Strike(e, a, c, d.SafeNormal(dir));
            }
        }

        public static void HitLine(Enemy e, AttackDef a, Vector2 from, Vector2 to, float halfWidth)
        {
            foreach (var c in Victims(e))
                if (U.DistToSegment(c.Pos, from, to, out _) <= halfWidth + c.radius * 0.6f)
                    Strike(e, a, c, (to - from).SafeNormal(e.facing));
        }

        public static void Strike(Enemy e, AttackDef a, Combatant c, Vector2 dir, float dmgMult = 1f)
        {
            var h = Hit.Make(a.damage * e.damageMult * dmgMult, e, MoveSlot.None, dir, a.knock);
            h.canCrit = false;
            h.type = DamageType.Physical;
            Combat.Deal(c, h);
            if (a.status.HasValue) c.status.Apply(a.status.Value, 1, 2.5f, a.statusPower);
        }

        static IEnumerator Windup(Enemy e, AttackDef a, Combatant tgt, Telegraph tele, bool follow, float dur)
        {
            float t = 0;
            while (t < dur)
            {
                if (e == null || e.dead || !e.status.CanAct) { if (tele != null) tele.Kill(); yield break; }
                t += Time.deltaTime;
                if (a.tracking && tgt != null && t < dur * 0.65f)
                {
                    Vector2 d = (tgt.Pos - e.Pos).SafeNormal(e.facing);
                    e.facing = Vector2.Lerp(e.facing, d, Time.deltaTime * 10f).normalized;
                    e.transform.rotation = U.LookFlat(e.facing);
                    if (tele != null && follow)
                    {
                        tele.transform.position = e.Pos.X0Z(0.04f);
                        tele.transform.rotation = U.LookFlat(e.facing);
                    }
                }
                if (Random.value < 0.25f) Fx.Emit(FxKind.Glow, e.transform.position + Vector3.up * e.height * 0.8f, Random.insideUnitSphere, e.def.accent, 0.25f, 0.3f);
                yield return null;
            }
        }

        static float Speed(Enemy e) => RunState.Current != null && e.isBoss ? RunState.Current.vowBossSpeed : 1f;

        public static IEnumerator Run(Enemy e, AttackDef a, Combatant tgt)
        {
            float sp = Speed(e) * e.status.ActionSpeedMult();
            float windup = a.windup / sp;
            for (int rep = 0; rep < Mathf.Max(1, a.repeats); rep++)
            {
                if (e == null || e.dead) yield break;
                if (tgt == null || !tgt.Alive) yield break;
                Vector2 dir = (tgt.Pos - e.Pos).SafeNormal(e.facing);
                e.facing = dir; e.transform.rotation = U.LookFlat(dir);
                e.rig.Kick(-0.15f);
                if (!string.IsNullOrEmpty(a.sfx) && System.Enum.TryParse<SfxId>(a.sfx, out var sid)) Sfx.Play(sid, e.transform.position, 0.7f);
                switch (a.kind)
                {
                    case AtkKind.Melee:
                        {
                            var tele = Telegraph.Cone(e.Pos, dir, a.radius, a.angle, windup); e.telegraphs.Add(tele);
                            yield return Windup(e, a, tgt, tele, true, windup);
                            if (e == null || e.dead || !e.status.CanAct) yield break;
                            e.rig.Swing(-80, 80, 0.12f); e.rig.Kick(0.3f);
                            Fx.Slash(e.transform.position, e.facing, a.radius, a.angle, e.def.accent, 0.18f, e.height * 0.5f);
                            Sfx.Play(SfxId.Swing, e.transform.position, 0.6f, 0.8f);
                            HitCone(e, a, e.Pos, e.facing, a.radius, a.angle);
                            break;
                        }
                    case AtkKind.Lunge:
                    case AtkKind.Charge:
                        {
                            bool charge = a.kind == AtkKind.Charge;
                            var tele = Telegraph.Line(e.Pos, dir, a.length, a.width * 2f, windup); e.telegraphs.Add(tele);
                            yield return Windup(e, a, tgt, tele, true, windup);
                            if (e == null || e.dead || !e.status.CanAct) yield break;
                            Vector2 d = e.facing;
                            float dur = charge ? a.length / Mathf.Max(1f, a.speed) : 0.22f;
                            float spd = a.length / dur;
                            var hitSet = new HashSet<Combatant>();
                            Sfx.Play(SfxId.Dash, e.transform.position, 0.7f, 0.7f);
                            for (float t = 0; t < dur; t += Time.deltaTime)
                            {
                                if (e == null || e.dead || !e.status.CanAct) yield break;
                                Vector2 before = e.Pos;
                                e.MoveBy(d * spd * Time.deltaTime);
                                if (Random.value < 0.5f) Fx.Emit(FxKind.Smoke, e.transform.position + Vector3.up * 0.3f, Vector3.zero, new Color(0.2f, 0.15f, 0.2f, 0.6f), 0.8f, 0.4f);
                                foreach (var c in Victims(e))
                                {
                                    if (hitSet.Contains(c)) continue;
                                    if ((c.Pos - e.Pos).magnitude <= e.radius + c.radius + a.width * 0.5f) { hitSet.Add(c); Strike(e, a, c, d); }
                                }
                                if ((e.Pos - before).magnitude < spd * Time.deltaTime * 0.3f) { if (charge) { CameraRig.Shake(0.2f); e.status.Apply(StatusType.Aturdido, 1, 0.6f); } break; }
                                yield return null;
                            }
                            break;
                        }
                    case AtkKind.Slam:
                    case AtkKind.SlamAtTarget:
                    case AtkKind.Explode:
                        {
                            Vector2 at = a.kind == AtkKind.SlamAtTarget ? tgt.Pos : e.Pos;
                            var tele = Telegraph.Circle(at, a.radius, windup, a.kind == AtkKind.Explode ? new Color(1f, 0.5f, 0.1f) : (Color?)null); e.telegraphs.Add(tele);
                            yield return Windup(e, a, tgt, null, false, windup);
                            if (e == null || e.dead || !e.status.CanAct) { if (tele != null) tele.Kill(); yield break; }
                            if (a.kind == AtkKind.Slam) at = e.Pos;
                            e.rig.Kick(0.4f);
                            Fx.Ring(at.X0Z(), a.radius, e.def.accent, 0.35f);
                            Fx.GroundFlash(at.X0Z(), a.radius, e.def.accent, 0.3f);
                            Fx.Smoke(at.X0Z(0.2f), new Color(0.2f, 0.15f, 0.2f, 0.7f), 10, 1.4f);
                            Sfx.Play(SfxId.Explosion, at.X0Z(), 0.7f, a.kind == AtkKind.Explode ? 1.2f : 0.8f);
                            CameraRig.Shake(0.25f);
                            HitArea(e, a, at, a.radius);
                            if (a.kind == AtkKind.SlamAtTarget && a.summonId == "tentacle") Props.SpawnEruptionVisual("enemy", at, a.radius);
                            if (a.kind == AtkKind.Explode) { e.Die(Hit.Proc(9999, null, DamageType.Physical)); yield break; }
                            break;
                        }
                    case AtkKind.Projectile:
                    case AtkKind.Radial:
                        {
                            float w = windup;
                            e.rig.Flash(e.def.accent, 0.3f);
                            yield return Windup(e, a, tgt, null, false, w);
                            if (e == null || e.dead || !e.status.CanAct) yield break;
                            Vector2 d = a.tracking && tgt != null ? (tgt.Pos - e.Pos).SafeNormal(e.facing) : e.facing;
                            e.facing = d; e.transform.rotation = U.LookFlat(d);
                            e.rig.Kick(0.25f);
                            Sfx.Play(SfxId.Projectile, e.transform.position, 0.6f, 0.8f);
                            int n = Mathf.Max(1, a.count);
                            for (int i = 0; i < n; i++)
                            {
                                Vector2 pd;
                                if (a.kind == AtkKind.Radial) pd = d.Rotate(i * 360f / n + rep * a.spread);
                                else pd = n > 1 ? d.Rotate(Mathf.Lerp(-a.spread * 0.5f, a.spread * 0.5f, i / (float)(n - 1))) : d;
                                FireEnemyProjectile(e, a, pd);
                            }
                            break;
                        }
                    case AtkKind.Lob:
                    case AtkKind.Web:
                        {
                            Vector2 at = tgt.Pos + (tgt is PlayerController pc ? pc.velocity * 0.3f : Vector2.zero);
                            if (Arena.Current != null) at = Arena.Current.Resolve(at, 0.2f);
                            e.rig.Kick(0.3f);
                            var tele = Telegraph.Circle(at, a.radius, windup + 0.5f, a.kind == AtkKind.Web ? new Color(0.85f, 0.85f, 0.9f) : (Color?)null); e.telegraphs.Add(tele);
                            yield return Windup(e, a, tgt, null, false, windup * 0.5f);
                            if (e == null || e.dead || !e.status.CanAct) { if (tele != null) tele.Kill(); yield break; }
                            Runner.Run(LobCo(e, a, e.Pos, at, windup * 0.5f + 0.5f));
                            break;
                        }
                    case AtkKind.Summon:
                        {
                            e.rig.Flash(new Color(0.6f, 0.3f, 1f), 0.5f);
                            yield return Windup(e, a, tgt, null, false, windup);
                            if (e == null || e.dead) yield break;
                            var sd = EnemyDB.Get(a.summonId);
                            if (sd != null && RoomDirector.AliveCount < 14)
                                for (int i = 0; i < a.count; i++)
                                {
                                    Vector2 p = e.Pos + Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.5f);
                                    if (Arena.Current != null) p = Arena.Current.Resolve(p, 0.5f);
                                    RoomDirector.Register(Enemy.Spawn(sd, p, false, 0.7f));
                                }
                            Sfx.Play(SfxId.Whisper, e.transform.position, 0.7f);
                            break;
                        }
                    case AtkKind.Teleport:
                        {
                            Fx.Smoke(e.transform.position + Vector3.up * 0.5f, new Color(0.1f, 0.1f, 0.2f, 0.8f), 10, 1.2f);
                            e.rig.gameObject.SetActive(false);
                            e.targetable = false;
                            yield return new WaitForSeconds(0.35f / sp);
                            if (e == null || e.dead) yield break;
                            Vector2 behind = tgt.Pos - tgt.facing.SafeNormal(Vector2.up) * (a.radius * 0.6f + tgt.radius);
                            if (Arena.Current != null) behind = Arena.Current.Resolve(behind, e.radius);
                            e.Teleport(behind);
                            e.rig.gameObject.SetActive(true);
                            e.targetable = true;
                            Fx.Smoke(e.transform.position + Vector3.up * 0.5f, new Color(0.1f, 0.2f, 0.3f, 0.8f), 10, 1.2f);
                            Fx.Burst(e.transform.position + Vector3.up, e.def.accent, 12, 5f, 0.3f, 0.4f, FxKind.Spark);
                            Vector2 d = (tgt.Pos - e.Pos).SafeNormal(e.facing);
                            e.facing = d; e.transform.rotation = U.LookFlat(d);
                            var tele = Telegraph.Cone(e.Pos, d, a.radius, a.angle, windup); e.telegraphs.Add(tele);
                            yield return Windup(e, a, tgt, tele, true, windup);
                            if (e == null || e.dead || !e.status.CanAct) yield break;
                            Fx.Slash(e.transform.position, e.facing, a.radius, a.angle, e.def.accent, 0.18f, 0.8f);
                            HitCone(e, a, e.Pos, e.facing, a.radius, a.angle);
                            break;
                        }
                    case AtkKind.Beam:
                        {
                            var tele = Telegraph.Line(e.Pos, dir, a.length, a.width * 2f, windup); e.telegraphs.Add(tele);
                            yield return Windup(e, a, tgt, tele, true, windup);
                            if (e == null || e.dead || !e.status.CanAct) yield break;
                            Vector2 end = e.Pos + e.facing * a.length;
                            Fx.Beam(e.Pos.X0Z(1f), end.X0Z(1f), e.def.accent, a.width * 1.5f, 0.3f);
                            Fx.Beam(e.Pos.X0Z(1f), end.X0Z(1f), Color.white, a.width * 0.5f, 0.25f);
                            Sfx.Play(SfxId.Magic, e.transform.position, 0.8f, 0.6f);
                            CameraRig.Shake(0.15f);
                            HitLine(e, a, e.Pos, end, a.width);
                            break;
                        }
                    case AtkKind.Aura:
                        {
                            e.rig.Flash(e.def.accent, 0.5f);
                            yield return Windup(e, a, tgt, null, false, windup);
                            if (e == null || e.dead) yield break;
                            Fx.Ring(e.transform.position, a.radius, e.def.accent, 0.5f);
                            Sfx.Play(SfxId.Whisper, e.transform.position, 0.8f, 1.4f);
                            foreach (var other in Enemy.Alive_.ToArray())
                                if (other != null && other != e && (other.Pos - e.Pos).magnitude < a.radius) other.StartCoroutine(Buff(other, 4f));
                            HitArea(e, a, e.Pos, a.radius * 0.5f);
                            break;
                        }
                }
                if (rep < a.repeats - 1) yield return new WaitForSeconds(a.repeatDelay / sp);
            }
            if (e != null) e.telegraphs.RemoveAll(t => t == null);
            yield return new WaitForSeconds(a.recover / sp);
        }

        static IEnumerator Buff(Enemy e, float dur)
        {
            e.speedMult = 1.4f; e.damageMult *= 1.3f;
            for (float t = 0; t < dur && e != null; t += Time.deltaTime)
            {
                if (Random.value < 0.2f) Fx.Emit(FxKind.Glow, e.transform.position + Vector3.up * e.height, Vector3.up * 2f, new Color(1f, 0.6f, 0.2f), 0.25f, 0.4f);
                yield return null;
            }
            if (e != null) { e.speedMult = 1f; e.damageMult /= 1.3f; }
        }

        public static Projectile FireEnemyProjectile(Enemy e, AttackDef a, Vector2 dir)
        {
            var h = Hit.Make(a.damage * e.damageMult, e, MoveSlot.None, dir, a.knock);
            h.canCrit = false;
            var team = e.status.Has(StatusType.Traidor) ? Team.Player : Team.Enemy;
            var p = Projectile.Fire(e, team, e.Pos + dir * (e.radius + 0.3f), dir, a.speed, h, e.def.accent, 0.3f, a.length > 0 ? a.length + 6f : 14f);
            p.height = Mathf.Min(1.2f, e.height * 0.6f);
            if (a.status.HasValue)
            {
                var st = a.status.Value; float pw = a.statusPower;
                p.onHit = (pr, c) => { Combat.Deal(c, pr.hit); c.status.Apply(st, 1, 2.5f, pw); };
            }
            return p;
        }

        static IEnumerator LobCo(Enemy e, AttackDef a, Vector2 from, Vector2 to, float flight)
        {
            var orb = Kit.Glow(World.Root, e != null ? e.def.accent : ProjColor, 0.5f, from.X0Z(1f), 2.5f);
            float t = 0;
            while (t < flight)
            {
                t += Time.deltaTime;
                float k = t / flight;
                if (orb != null) orb.position = Vector3.Lerp(from.X0Z(1f), to.X0Z(0.3f), k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 3f;
                yield return null;
            }
            if (orb != null) Object.Destroy(orb.gameObject);
            if (World.Root == null || e == null) yield break;
            Fx.GroundFlash(to.X0Z(), a.radius, e.def.accent, 0.3f);
            Fx.Burst(to.X0Z(0.3f), e.def.accent, 12, 5f, 0.3f, 0.5f);
            Sfx.Play(a.kind == AtkKind.Web ? SfxId.Whisper : SfxId.Water, to.X0Z(), 0.7f);
            if (a.kind == AtkKind.Web)
            {
                Hazards.Enemy(to, a.radius, 4f, 0, new Color(0.85f, 0.85f, 0.9f), StatusType.Lento);
            }
            else
            {
                HitArea(e, a, to, a.radius);
                if (a.status.HasValue) Hazards.Enemy(to, a.radius * 0.9f, 3.5f, Mathf.Max(1, a.statusPower), e.def.accent, a.status.Value);
            }
        }
    }
}
