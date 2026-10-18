using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Física ligera del juego. Toda sala (y el santuario) es un polígono caminable sobre el plano XZ,
    /// flotando sobre el vacío, con obstáculos circulares (pilares, monolitos, fosas).
    /// No se usa PhysX: los personajes son círculos que se resuelven contra este espacio.
    /// </summary>
    public class Arena
    {
        public static Arena Current;

        public readonly List<Vector2> polygon = new List<Vector2>(); // sentido antihorario
        public readonly List<Obstacle> obstacles = new List<Obstacle>();

        public struct Obstacle
        {
            public Vector2 center;
            public float radius;
            public bool blocksProjectiles;
            public Obstacle(Vector2 c, float r, bool blocks) { center = c; radius = r; blocksProjectiles = blocks; }
        }

        public Bounds2 bounds;
        public struct Bounds2 { public Vector2 min, max; public Vector2 Center => (min + max) * 0.5f; public Vector2 Size => max - min; }

        public void SetPolygon(IList<Vector2> pts)
        {
            polygon.Clear();
            polygon.AddRange(pts);
            if (SignedArea() < 0) polygon.Reverse();
            var b = new Bounds2 { min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue) };
            foreach (var p in polygon) { b.min = Vector2.Min(b.min, p); b.max = Vector2.Max(b.max, p); }
            bounds = b;
        }

        float SignedArea()
        {
            float a = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i]; var q = polygon[(i + 1) % polygon.Count];
                a += p.x * q.y - q.x * p.y;
            }
            return a * 0.5f;
        }

        public bool InsidePolygon(Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i]; var b = polygon[j];
                if (((a.y > p.y) != (b.y > p.y)) && (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-6f) + a.x))
                    inside = !inside;
            }
            return inside;
        }

        public float DistanceToEdge(Vector2 p, out Vector2 closest, out Vector2 inwardNormal)
        {
            float best = float.MaxValue; closest = p; inwardNormal = Vector2.up;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
                float d = U.DistToSegment(p, a, b, out var c);
                if (d < best)
                {
                    best = d; closest = c;
                    var e = (b - a).normalized;
                    inwardNormal = new Vector2(-e.y, e.x);
                }
            }
            return best;
        }

        /// <summary>¿Puede un círculo de radio r estar en p?</summary>
        public bool IsWalkable(Vector2 p, float r = 0f)
        {
            if (!InsidePolygon(p)) return false;
            if (r > 0 && DistanceToEdge(p, out _, out _) < r) return false;
            foreach (var o in obstacles)
                if ((p - o.center).sqrMagnitude < (o.radius + r) * (o.radius + r)) return false;
            return true;
        }

        /// <summary>¿Choca un proyectil en p? (sale del suelo o golpea un obstáculo sólido)</summary>
        public bool BlocksProjectile(Vector2 p)
        {
            if (!InsidePolygon(p))
            {
                // margen: los proyectiles vuelan un poco sobre el vacío antes de desaparecer
                return DistanceToEdge(p, out _, out _) > 3f;
            }
            foreach (var o in obstacles)
                if (o.blocksProjectiles && (p - o.center).sqrMagnitude < o.radius * o.radius) return true;
            return false;
        }

        /// <summary>Empuja un círculo hasta quedar dentro del espacio caminable.</summary>
        public Vector2 Resolve(Vector2 p, float r)
        {
            for (int iter = 0; iter < 3; iter++)
            {
                foreach (var o in obstacles)
                {
                    Vector2 d = p - o.center;
                    float min = o.radius + r;
                    float m = d.magnitude;
                    if (m < min)
                    {
                        Vector2 n = m > 0.0001f ? d / m : Vector2.right;
                        p = o.center + n * min;
                    }
                }
                bool inside = InsidePolygon(p);
                float dist = DistanceToEdge(p, out var c, out var nIn);
                if (!inside)
                    p = c + nIn * (r + 0.01f);
                else if (dist < r)
                    p = c + (p - c).normalized * r;
            }
            return p;
        }

        /// <summary>Movimiento con barrido en pasos pequeños para no atravesar obstáculos.</summary>
        public Vector2 Move(Vector2 from, Vector2 delta, float r)
        {
            float len = delta.magnitude;
            if (len < 1e-5f) return Resolve(from, r);
            int steps = Mathf.Clamp(Mathf.CeilToInt(len / 0.25f), 1, 40);
            Vector2 p = from;
            Vector2 step = delta / steps;
            for (int i = 0; i < steps; i++) p = Resolve(p + step, r);
            return p;
        }

        /// <summary>Recorre una línea y devuelve el último punto caminable (para embestidas y teletransportes).</summary>
        public Vector2 Raycast(Vector2 from, Vector2 dir, float maxDist, float r)
        {
            Vector2 last = from;
            int steps = Mathf.CeilToInt(maxDist / 0.2f);
            for (int i = 1; i <= steps; i++)
            {
                Vector2 p = from + dir * (maxDist * i / steps);
                if (!IsWalkable(p, r)) break;
                last = p;
            }
            return last;
        }

        public Vector2 RandomPoint(System.Random rng, float r, float minDistFrom = 0f, Vector2 avoid = default)
        {
            for (int i = 0; i < 200; i++)
            {
                var p = new Vector2(rng.Range(bounds.min.x, bounds.max.x), rng.Range(bounds.min.y, bounds.max.y));
                if (!IsWalkable(p, r + 0.3f)) continue;
                if (minDistFrom > 0 && Vector2.Distance(p, avoid) < minDistFrom) continue;
                return p;
            }
            return Resolve(bounds.Center, r);
        }
    }
}
