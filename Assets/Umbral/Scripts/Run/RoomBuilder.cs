using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Genera la geometría y el arte de cada sala: una isla de piedra flotando sobre el vacío cósmico,
    /// con bordes decorados según la región, obstáculos, luces, partículas ambientales y el plano del abismo.
    /// </summary>
    public static class RoomBuilder
    {
        public class Layout
        {
            public Arena arena;
            public Vector2 entry;
            public List<Vector2> doorSlots = new List<Vector2>();
            public Vector2 center;
            public Vector2 up;   // dirección "arriba de la pantalla" en el mundo
        }

        static float Yaw => CameraRig.Instance != null ? CameraRig.Instance.yaw : 45f;
        static Vector2 S2W(Vector2 screen) => screen.Rotate(-Yaw);

        /// <summary>Crea el polígono de una sala en coordenadas de pantalla y lo gira al mundo.</summary>
        public static Layout Generate(System.Random rng, float w, float h, int shape, int obstacles, bool pits)
        {
            var pts = new List<Vector2>();
            float hw = w * 0.5f, hh = h * 0.5f;
            switch (shape)
            {
                case 0: // octógono irregular
                    for (int i = 0; i < 8; i++)
                    {
                        float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                        pts.Add(new Vector2(Mathf.Cos(a) * hw * 1.08f, Mathf.Sin(a) * hh * 1.08f));
                    }
                    break;
                case 1: // cruz ancha
                    {
                        float cx = hw * 0.55f, cy = hh * 0.55f;
                        pts.AddRange(new[] { new Vector2(-cx, -hh), new Vector2(cx, -hh), new Vector2(cx, -cy), new Vector2(hw, -cy), new Vector2(hw, cy), new Vector2(cx, cy), new Vector2(cx, hh), new Vector2(-cx, hh), new Vector2(-cx, cy), new Vector2(-hw, cy), new Vector2(-hw, -cy), new Vector2(-cx, -cy) });
                    }
                    break;
                case 2: // elipse
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i * Mathf.PI * 2 / 14;
                        pts.Add(new Vector2(Mathf.Cos(a) * hw, Mathf.Sin(a) * hh));
                    }
                    break;
                case 3: // rombo truncado
                    pts.AddRange(new[] { new Vector2(0, -hh), new Vector2(hw * 0.6f, -hh * 0.75f), new Vector2(hw, 0), new Vector2(hw * 0.6f, hh * 0.75f), new Vector2(0, hh), new Vector2(-hw * 0.6f, hh * 0.75f), new Vector2(-hw, 0), new Vector2(-hw * 0.6f, -hh * 0.75f) });
                    break;
                default: // rectángulo de esquinas cortadas
                    {
                        float c = Mathf.Min(hw, hh) * 0.35f;
                        pts.AddRange(new[] { new Vector2(-hw + c, -hh), new Vector2(hw - c, -hh), new Vector2(hw, -hh + c), new Vector2(hw, hh - c), new Vector2(hw - c, hh), new Vector2(-hw + c, hh), new Vector2(-hw, hh - c), new Vector2(-hw, -hh + c) });
                    }
                    break;
            }
            // subdividir y deformar los bordes para un contorno orgánico
            var detailed = new List<Vector2>();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                int segs = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b) / 2.2f));
                for (int s = 0; s < segs; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, (float)s / segs);
                    if (s > 0) p += p.normalized * rng.Range(-0.45f, 0.45f);
                    detailed.Add(p);
                }
            }

            var layout = new Layout();
            var world = new List<Vector2>();
            foreach (var p in detailed) world.Add(S2W(p));
            var arena = new Arena();
            arena.SetPolygon(world);
            layout.arena = arena;
            layout.up = S2W(Vector2.up);
            layout.center = Vector2.zero;
            layout.entry = S2W(new Vector2(0, -hh + 2.2f));

            // puertas: repartidas por el borde superior
            int slots = 3;
            for (int i = 0; i < slots; i++)
            {
                float x = (i - (slots - 1) / 2f) * (w * 0.3f);
                Vector2 sp = new Vector2(x, hh + 2f);
                // proyectar hacia dentro hasta tocar el polígono
                Vector2 wp = S2W(sp);
                for (int k = 0; k < 60 && !arena.InsidePolygon(wp); k++) wp = Vector2.MoveTowards(wp, Vector2.zero, 0.25f);
                layout.doorSlots.Add(wp);
            }

            // obstáculos
            for (int i = 0; i < obstacles; i++)
            {
                for (int tries = 0; tries < 30; tries++)
                {
                    var p = new Vector2(rng.Range(-hw * 0.7f, hw * 0.7f), rng.Range(-hh * 0.55f, hh * 0.6f));
                    var wp = S2W(p);
                    float r = rng.Range(0.6f, 1.2f);
                    if (Vector2.Distance(wp, layout.entry) < 4f || !arena.IsWalkable(wp, r + 1.5f)) continue;
                    bool clash = false;
                    foreach (var o in arena.obstacles) if (Vector2.Distance(o.center, wp) < o.radius + r + 2.2f) clash = true;
                    foreach (var d in layout.doorSlots) if (Vector2.Distance(d, wp) < 4f) clash = true;
                    if (clash) continue;
                    arena.obstacles.Add(new Arena.Obstacle(wp, r, true));
                    break;
                }
            }
            if (pits)
            {
                int n = rng.Next(1, 3);
                for (int i = 0; i < n; i++)
                {
                    var p = S2W(new Vector2(rng.Range(-hw * 0.5f, hw * 0.5f), rng.Range(-hh * 0.3f, hh * 0.4f)));
                    float r = rng.Range(1.2f, 2f);
                    if (Vector2.Distance(p, layout.entry) < 5f || !arena.IsWalkable(p, r + 1.5f)) continue;
                    bool clash = false;
                    foreach (var o in arena.obstacles) if (Vector2.Distance(o.center, p) < o.radius + r + 2f) clash = true;
                    if (clash) continue;
                    arena.obstacles.Add(new Arena.Obstacle(p, r, false));
                }
            }
            return layout;
        }

        // =================================================================
        // Ambientación
        // =================================================================
        public static Light sun;
        static ParticleSystem motes;

        public static void ApplyLighting(BiomeDef b, bool dark = false)
        {
            if (sun == null)
            {
                var go = new GameObject("Sun");
                Object.DontDestroyOnLoad(go);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.75f;
            }
            sun.color = b.sun;
            sun.intensity = dark ? b.sunIntensity * 0.25f : b.sunIntensity;
            sun.transform.rotation = Quaternion.Euler(b.sunAngles.x, b.sunAngles.y, 0);
            var sh = new UnityEngine.Rendering.SphericalHarmonicsL2();
            sh.AddAmbientLight(dark ? b.ambient * 0.3f : b.ambient);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = b.ambient;
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = false;
            CameraRig.Instance?.SetGrading(b.vignette, b.saturation, dark ? -0.6f : b.exposure);
        }

        public static void BuildVoid(BiomeDef b, Vector2 center, bool globes = true)
        {
            var m = Mats.Void(b.voidA, b.voidB, b.voidC, globes);
            m.SetVector("_Center", new Vector4(center.x, 0, center.y, 0));
            var go = Fx.MeshObj("Abismo", MeshGen.Quad, m, new Vector3(center.x, -38f, center.y), Quaternion.Euler(90, 0, 0), Vector3.one * 600f);
            go.transform.SetParent(World.Root, true);
        }

        public static void BuildMotes(BiomeDef b, Vector2 center, float size)
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(World.Root, false);
            go.transform.position = center.X0Z(2f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor = b.motes;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 30;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(size, 4f, size);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Unlit(Color.white, Mats.Blend.Additive, Tex.SoftCircle, 2.5f);
            ps.Play();
        }

        /// <summary>Losa del suelo con textura pintada y venas brillantes.</summary>
        public static void BuildFloor(BiomeDef b, Arena arena, int seed)
        {
            var tex = Tex.Floor(b.id, b.floorA, b.floorB, b.crack, 4f, seed % 3 + b.id.Length * 7, b.organicFloor);
            var veins = Tex.RuneVeins(b.id, b.id.Length * 13, 0.45f);
            var mat = Mats.ToonTextured("floor_" + b.id, tex, Color.white, new Vector2(1, 1), veins, b.veins * 0.9f);
            var mesh = MeshGen.FloatingSlab(arena.polygon, 7f, 1f / 7f, seed);
            var go = new GameObject("Suelo");
            go.transform.SetParent(World.Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.receiveShadows = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // borde iluminado (línea del precipicio)
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.Unlit(b.veins.WithA(0.5f), Mats.Blend.Additive, null, 1.4f);
            lr.loop = true;
            lr.positionCount = arena.polygon.Count;
            for (int i = 0; i < arena.polygon.Count; i++) lr.SetPosition(i, arena.polygon[i].X0Z(0.02f));
            lr.widthMultiplier = 0.08f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Decoración del borde, obstáculos, fosas, islas flotantes y luces.</summary>
        public static void Dress(BiomeDef b, Arena arena, System.Random rng, bool lights = true)
        {
            var parent = World.Child("Decorado");
            // obstáculos
            foreach (var o in arena.obstacles)
            {
                if (o.blocksProjectiles) Props.Obstacle(parent, b, o.center, o.radius, rng);
                else Props.Pit(parent, b, o.center, o.radius);
            }
            // borde: rocas, árboles, columnas, corales... colocados justo fuera del suelo
            float acc = 0;
            int lightCount = 0;
            for (int i = 0; i < arena.polygon.Count; i++)
            {
                Vector2 a = arena.polygon[i], c = arena.polygon[(i + 1) % arena.polygon.Count];
                float len = Vector2.Distance(a, c);
                Vector2 dir = (c - a).normalized;
                Vector2 outward = new Vector2(dir.y, -dir.x);
                for (float t = 0; t < len; t += 1.4f)
                {
                    acc += 1.4f;
                    Vector2 p = a + dir * t + outward * rng.Range(0.2f, 1.4f);
                    // evitar tapar la vista: menos decoración en el borde inferior de la pantalla
                    Vector2 up = S2W(Vector2.up);
                    float facingCam = Vector2.Dot(outward, up);
                    if (facingCam < -0.4f && rng.NextDouble() < 0.75) continue;
                    if (rng.NextDouble() < 0.55)
                        Props.EdgeDecor(parent, b, p, rng, facingCam < -0.4f);
                    if (lights && acc > 7f && lightCount < 6 && facingCam > -0.2f)
                    {
                        acc = 0; lightCount++;
                        Props.LightSource(parent, b, a + dir * t + outward * 0.4f, rng);
                    }
                }
            }
            // islas flotantes lejanas
            for (int i = 0; i < 16; i++)
            {
                Vector2 p = Random.insideUnitCircle.normalized * rng.Range(20f, 55f);
                float s = rng.Range(1.5f, 6f);
                var t = Kit.Part(parent, MeshGen.Rock(rng.Next(0, 50), 0.35f, 1), Mats.Toon(Color.Lerp(b.floorA, b.crack, 0.4f), null, 1f), p.X0Z(rng.Range(-18f, -4f)), new Vector3(s, s * 0.6f, s), new Vector3(rng.Range(0, 30), rng.Range(0, 360), rng.Range(0, 30)), "isla", false);
                t.gameObject.AddComponent<Floater>().amp = rng.Range(0.2f, 0.6f);
            }
            Props.Backdrop(parent, b, rng);
        }
    }

    /// <summary>Movimiento suave de flotación para islas y objetos.</summary>
    public class Floater : MonoBehaviour
    {
        public float amp = 0.3f, speed = 0.5f, rot = 3f;
        Vector3 basePos; float ph;
        void Start() { basePos = transform.localPosition; ph = Random.value * 10f; }
        void Update()
        {
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed + ph) * amp;
            transform.Rotate(0, rot * Time.deltaTime, 0, Space.World);
        }
    }
}
