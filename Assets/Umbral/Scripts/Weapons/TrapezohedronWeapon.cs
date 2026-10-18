using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Trapezoedro Resplandeciente — dispara fragmentos con munición limitada que hay que recoger,
    /// y embiste para recuperarlos (como la Calavera Argéntea de Hades II).
    /// </summary>
    public class TrapezohedronWeapon : Weapon
    {
        static readonly Color Red = new Color(1f, 0.2f, 0.25f);
        static readonly Color Black = new Color(0.08f, 0.05f, 0.08f);

        class Shard { public GameObject go; public Vector2 pos; public float returnAt; }
        readonly List<Shard> dropped = new List<Shard>();
        public int ammo, maxAmmo = 3;
        int rushesLeft;
        float rushRefreshAt;
        Transform stone;

        public override float AttackOmegaCost => 25f;
        public override float SpecialOmegaCost => 30f;

        protected override void BuildModel(Transform pivot)
        {
            model = Kit.Empty(pivot, "Trapezoedro", new Vector3(0.6f, 1.3f, 0.4f));
            stone = Kit.Part(model, MeshGen.Rock(13, 0.35f, 0), Mats.Toon(Black, Red * 0.35f, 1f, Red), Vector3.zero, Vector3.one * 0.32f);
            Kit.Glow(model, Red, 0.15f, Vector3.zero, 3f);
        }

        public override void OnUpgradesChanged()
        {
            int newMax = M.Flag("tz_cuarto") ? 4 : 3;
            if (newMax > maxAmmo) ammo += newMax - maxAmmo;
            maxAmmo = newMax;
            if (ammo == 0 && dropped.Count == 0) ammo = maxAmmo;
        }

        public override void Init(PlayerController p, WeaponDef d)
        {
            base.Init(p, d);
            ammo = maxAmmo;
        }

        void Update()
        {
            if (stone != null) stone.Rotate(40 * Time.deltaTime, 90 * Time.deltaTime, 0);
            if (Time.time > rushRefreshAt) rushesLeft = M.Flag("tz_doble") ? 2 : 1;
            // recoger / retorno automático
            for (int i = dropped.Count - 1; i >= 0; i--)
            {
                var s = dropped[i];
                if (s.go == null) { dropped.RemoveAt(i); ammo = Mathf.Min(maxAmmo, ammo + 1); continue; }
                bool near = Vector2.Distance(s.pos, P.Pos) < 1.4f;
                if (near || Time.time >= s.returnAt) Collect(i, !near);
            }
        }

        void Collect(int i, bool flyBack)
        {
            var s = dropped[i];
            dropped.RemoveAt(i);
            if (s.go != null)
            {
                if (flyBack) Fx.Beam(s.go.transform.position, P.transform.position + Vector3.up, Red, 0.15f, 0.15f);
                Destroy(s.go);
            }
            ammo = Mathf.Min(maxAmmo, ammo + 1);
            Sfx.Play(SfxId.Pickup, P.transform.position, 0.4f, 1.6f);
        }

        public void ClearShards()
        {
            foreach (var s in dropped) if (s.go != null) Destroy(s.go);
            dropped.Clear();
            ammo = maxAmmo;
        }

        public override void Attack()
        {
            if (ammo <= 0) { UIFloat.Text(P.transform.position + Vector3.up * 2.4f, "Sin fragmentos", new Color(1f, 0.5f, 0.5f), 0.8f); busyUntil = Time.time + 0.25f; return; }
            Run(AttackCo());
        }

        IEnumerator AttackCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.32f / AtkSpd;
            moveMult = 0.5f;
            P.rig.Kick(0.2f);
            yield return new WaitForSeconds(0.06f);
            P.ConsumeEmpowered();
            FireShard(dir, 40f, false);
            Sfx.Play(SfxId.Projectile, P.transform.position, 1f, 0.7f);
            yield return new WaitForSeconds(0.15f);
            moveMult = 1f;
            current = null;
        }

        void FireShard(Vector2 dir, float dmg, bool omega)
        {
            if (ammo <= 0) return;
            ammo--;
            var h = MakeHit(dmg, MoveSlot.Attack, omega, 5f, dir, DamageType.Void);
            var pr = Shoot(dir, 22f, h, Red, 0.3f, 11f);
            if (M.Flag("tz_esquirla")) pr.pierce = 99;
            pr.onEnd = p => DropShard(p.pos);
        }

        void DropShard(Vector2 at)
        {
            if (World.Root == null) return;
            if (Arena.Current != null && !Arena.Current.IsWalkable(at, 0.2f)) at = Arena.Current.Resolve(at, 0.4f);
            var go = new GameObject("Fragmento");
            go.transform.SetParent(World.Root, false);
            go.transform.position = at.X0Z(0.4f);
            Kit.Part(go.transform, MeshGen.Crystal(5, 0.4f), Mats.Toon(Black, Red * 0.6f, 1f, Red), Vector3.zero, new Vector3(0.25f, 0.6f, 0.25f), new Vector3(Random.Range(-30, 30), Random.Range(0, 360), 0));
            Kit.Glow(go.transform, Red, 0.25f, Vector3.up * 0.2f, 2f);
            dropped.Add(new Shard { go = go, pos = at, returnAt = Time.time + (M.Flag("tz_iman") ? 1f : 3.5f) });
            if (M.Flag("tz_ardiente")) Hazards.Spawn("cthugha", at, 1.2f, 3f, 2, false);
        }

        public override void Special()
        {
            if (rushesLeft <= 0) return;
            Run(RushCo(false));
        }

        IEnumerator RushCo(bool omega)
        {
            rushesLeft--;
            rushRefreshAt = Time.time + 0.9f;
            FaceAim();
            Vector2 dir = Aim;
            float dist = (M.Flag("tz_morador") ? 7.5f : 5.5f) * (omega ? 1.2f : 1f);
            busyUntil = Time.time + 0.4f;
            moveMult = 0f;
            P.SetInvulnerable(0.25f);
            Sfx.Play(SfxId.Dash, P.transform.position, 1f, 0.6f);
            var hitSet = new HashSet<Combatant>();
            float t = 0, dur = 0.24f;
            Vector2 start = P.Pos;
            while (t < dur)
            {
                t += Time.deltaTime;
                Vector2 prev = P.Pos;
                P.MoveBy(dir * (dist / dur) * Time.deltaTime);
                Fx.Emit(FxKind.Smoke, P.transform.position + Vector3.up * 0.6f, Vector3.zero, new Color(0.15f, 0.05f, 0.1f, 0.8f), 1f, 0.5f);
                foreach (var c in Combat.InCircle(P.Pos, 1.2f, Team.Player))
                {
                    if (hitSet.Contains(c)) continue;
                    hitSet.Add(c);
                    var h = MakeHit(30, MoveSlot.Special, omega, 9f, dir);
                    Combat.Deal(c, h);
                    if (M.Flag("tz_morador"))
                    {
                        float v = M.Flag("tz_morador3") ? 0.35f : M.Flag("tz_morador2") ? 0.25f : 0.15f;
                        c.status.Apply(StatusType.Vulnerable, 1, 4f, v);
                    }
                }
                for (int i = dropped.Count - 1; i >= 0; i--) if (Vector2.Distance(dropped[i].pos, P.Pos) < 1.8f) Collect(i, false);
                yield return null;
            }
            if (omega)
            {
                Vector2 c = P.Pos;
                Fx.Ring(c.X0Z(), 3.2f, Red, 0.4f);
                Fx.Smoke(c.X0Z(0.5f), new Color(0.05f, 0.0f, 0.05f, 0.9f), 20, 2f);
                Fx.Burst(c.X0Z(1f), Red, 20, 8f, 0.35f, 0.5f, FxKind.Spark);
                Sfx.Play(SfxId.Explosion, c.X0Z());
                CameraRig.Shake(0.4f);
                foreach (var e in Combat.InCircle(c, 3.2f, Team.Player))
                {
                    var h = MakeHit(80, MoveSlot.Special, true, 10f, (e.Pos - c).SafeNormal(dir), DamageType.Void);
                    Combat.Deal(e, h);
                    e.status.Apply(StatusType.Vulnerable, 1, 4f, 0.25f);
                }
            }
            yield return new WaitForSeconds(0.1f);
            moveMult = 1f;
            current = null;
        }

        protected override void OmegaAttack() => Run(OmegaAttackCo());

        IEnumerator OmegaAttackCo()
        {
            FaceAim();
            Vector2 dir = Aim;
            busyUntil = Time.time + 0.4f;
            int n = Mathf.Max(1, ammo);
            if (ammo == 0) ammo = 1; // la carga Omega siempre lanza al menos uno
            Sfx.Play(SfxId.Explosion, P.transform.position, 0.6f, 1.6f);
            for (int i = 0; i < n; i++)
            {
                float a = n > 1 ? Mathf.Lerp(-20, 20, i / (float)(n - 1)) : 0;
                FireShard(dir.Rotate(a), 60f, true);
            }
            P.Knock(-dir, 6f);
            CameraRig.Shake(0.25f);
            yield return new WaitForSeconds(0.2f);
            current = null;
        }

        protected override void OmegaSpecial() { rushesLeft = Mathf.Max(rushesLeft, 1); Run(RushCo(true)); }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var s in dropped) if (s.go != null) Destroy(s.go);
        }
    }
}
