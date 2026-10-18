using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Mallas procedurales: discos, anillos, sectores, arcos de tajo, suelos flotantes, rocas, tentáculos.</summary>
    public static class MeshGen
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        static readonly Dictionary<PrimitiveType, Mesh> prims = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Primitive(PrimitiveType t)
        {
            if (prims.TryGetValue(t, out var m) && m != null) return m;
            var go = GameObject.CreatePrimitive(t);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            prims[t] = m;
            return m;
        }

        public static Mesh Sphere => Primitive(PrimitiveType.Sphere);
        public static Mesh Cube => Primitive(PrimitiveType.Cube);
        public static Mesh Capsule => Primitive(PrimitiveType.Capsule);
        public static Mesh Cylinder => Primitive(PrimitiveType.Cylinder);
        public static Mesh Quad => Primitive(PrimitiveType.Quad);

        /// <summary>Disco plano en XZ de radio 1 (UV centradas).</summary>
        public static Mesh Disc(int seg = 48)
        {
            string k = "disc" + seg;
            if (cache.TryGetValue(k, out var m)) return m;
            return cache[k] = Sector(360f, seg, 0f);
        }

        /// <summary>Sector circular (cono de ataque) en XZ, apuntando a +Z. innerRadius en [0,1).</summary>
        public static Mesh Sector(float angleDeg, int seg = 32, float inner = 0f)
        {
            string k = "sector" + angleDeg + "_" + seg + "_" + inner;
            if (cache.TryGetValue(k, out var cached)) return cached;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float half = angleDeg * 0.5f;
            for (int i = 0; i <= seg; i++)
            {
                float a = (-half + angleDeg * i / seg + 90f) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                // en XZ con +Z como frente: girar para que el centro apunte a +Z
                d = new Vector3(-d.x, 0, d.z);
                v.Add(d * inner); v.Add(d);
                uv.Add(new Vector2(0.5f + d.x * inner * 0.5f, 0.5f + d.z * inner * 0.5f));
                uv.Add(new Vector2(0.5f + d.x * 0.5f, 0.5f + d.z * 0.5f));
                if (i < seg)
                {
                    int b = i * 2;
                    tri.Add(b); tri.Add(b + 1); tri.Add(b + 3);
                    tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
                }
            }
            var m = new Mesh { name = k };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            FixWinding(m);
            m.RecalculateNormals(); m.RecalculateBounds();
            return cache[k] = m;
        }

        public static Mesh Ring(float inner = 0.85f, int seg = 64) => Sector(360f, seg, inner);

        /// <summary>Rectángulo en XZ: ancho 1 centrado en X, largo 1 desde z=0 hasta z=1.</summary>
        public static Mesh Rect()
        {
            if (cache.TryGetValue("rect", out var m)) return m;
            m = new Mesh { name = "rect" };
            m.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(-0.5f, 0, 1) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals();
            return cache["rect"] = m;
        }

        /// <summary>Arco de tajo (media luna) en XZ con UV.x a lo largo del arco y UV.y del interior al exterior.</summary>
        public static Mesh SlashArc(float angleDeg, float inner, int seg = 24)
        {
            string k = "slash" + angleDeg + "_" + inner;
            if (cache.TryGetValue(k, out var m)) return m;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float half = angleDeg * 0.5f;
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float a = (-half + angleDeg * t) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                // la media luna es más gruesa en el centro
                float thick = Mathf.Lerp(1f, inner, Mathf.Sin(t * Mathf.PI));
                v.Add(d * thick); v.Add(d);
                uv.Add(new Vector2(t, 0)); uv.Add(new Vector2(t, 1));
                if (i < seg)
                {
                    int b = i * 2;
                    tri.Add(b); tri.Add(b + 2); tri.Add(b + 1);
                    tri.Add(b + 1); tri.Add(b + 2); tri.Add(b + 3);
                }
            }
            m = new Mesh { name = k };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            FixWinding(m);
            m.RecalculateNormals(); m.RecalculateBounds();
            return cache[k] = m;
        }

        /// <summary>Asegura que los triángulos miren hacia +Y (visibles desde la cámara cenital).</summary>
        static void FixWinding(Mesh m)
        {
            var v = m.vertices; var t = m.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (n.y < 0) { (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]); }
            }
            m.triangles = t;
        }

        /// <summary>
        /// Losa flotante: la cara superior es el polígono (triangulado en abanico desde el centroide,
        /// válido para polígonos estrellados) y un faldón irregular que cae hacia el vacío.
        /// </summary>
        public static Mesh FloatingSlab(IList<Vector2> poly, float depth, float uvScale, int seed)
        {
            var rng = new System.Random(seed);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); var col = new List<Color>();
            Vector2 c = Vector2.zero;
            foreach (var p in poly) c += p;
            c /= poly.Count;
            // cara superior
            v.Add(c.X0Z()); uv.Add(c * uvScale); col.Add(Color.white);
            for (int i = 0; i < poly.Count; i++) { v.Add(poly[i].X0Z()); uv.Add(poly[i] * uvScale); col.Add(Color.white); }
            for (int i = 0; i < poly.Count; i++)
            {
                tri.Add(0); tri.Add(1 + (i + 1) % poly.Count); tri.Add(1 + i);
            }
            // faldón: varias capas que se estrechan hacia abajo como roca colgante
            int rings = 3;
            int baseIdx = v.Count;
            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r / rings;
                float y = -depth * t;
                for (int i = 0; i <= poly.Count; i++)
                {
                    Vector2 p = poly[i % poly.Count];
                    float shrink = Mathf.Lerp(1f, 0.55f + rng.Range(-0.1f, 0.1f), t * t);
                    Vector2 q = c + (p - c) * shrink;
                    float jitter = r == 0 ? 0 : rng.Range(-0.4f, 0.4f);
                    v.Add(new Vector3(q.x + jitter, y - (r == rings ? rng.Range(0, depth * 0.4f) : 0), q.y + jitter));
                    uv.Add(new Vector2(i * 0.35f, y * 0.25f));
                    float shade = Mathf.Lerp(0.85f, 0.25f, t);
                    col.Add(new Color(shade, shade, shade, 1));
                }
            }
            int ring = poly.Count + 1;
            for (int r = 0; r < rings; r++)
                for (int i = 0; i < poly.Count; i++)
                {
                    int a = baseIdx + r * ring + i, b = a + 1, d = a + ring, e = d + 1;
                    tri.Add(a); tri.Add(b); tri.Add(e);
                    tri.Add(a); tri.Add(e); tri.Add(d);
                }
            var m = new Mesh { name = "slab" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.SetColors(col);
            m.RecalculateNormals(); m.RecalculateBounds();
            // las normales de la cara superior deben mirar arriba aunque el abanico cambie de orientación
            var tris = m.triangles; var verts = m.vertices;
            for (int i = 0; i < poly.Count * 3; i += 3)
            {
                Vector3 n = Vector3.Cross(verts[tris[i + 1]] - verts[tris[i]], verts[tris[i + 2]] - verts[tris[i]]);
                if (n.y < 0) { (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]); }
            }
            m.triangles = tris;
            m.RecalculateNormals();
            var normals = m.normals;
            for (int i = 0; i <= poly.Count; i++) normals[i] = Vector3.up;
            m.normals = normals;
            return m;
        }

        /// <summary>Roca deformada (icosfera con ruido), base para monolitos, corales, cristales.</summary>
        public static Mesh Rock(int seed, float roughness = 0.25f, int subdiv = 1)
        {
            string k = "rock" + seed + "_" + roughness + "_" + subdiv;
            if (cache.TryGetValue(k, out var m)) return m;
            m = Icosphere(subdiv);
            var v = m.vertices;
            var rng = new System.Random(seed);
            float ox = rng.Range(0, 100), oy = rng.Range(0, 100);
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 n = v[i].normalized;
                float noise = Mathf.PerlinNoise(n.x * 1.7f + ox, n.y * 1.7f + n.z * 1.3f + oy);
                v[i] = n * (1f + (noise - 0.5f) * 2f * roughness);
            }
            m.vertices = v;
            m.RecalculateNormals(); m.RecalculateBounds();
            m.name = k;
            return cache[k] = m;
        }

        /// <summary>Icosfera de caras planas (sin vértices compartidos) para un look tallado.</summary>
        public static Mesh Icosphere(int subdiv)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            var faces = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            for (int s = 0; s < subdiv; s++)
            {
                var nf = new List<int>();
                var mid = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                    if (mid.TryGetValue(key, out int idx)) return idx;
                    verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                    return mid[key] = verts.Count - 1;
                }
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = nf;
            }
            // desindexar para caras planas
            var fv = new List<Vector3>(); var ft = new List<int>();
            for (int i = 0; i < faces.Count; i++) { fv.Add(verts[faces[i]]); ft.Add(i); }
            var m = new Mesh { name = "ico" };
            m.SetVertices(fv); m.SetTriangles(ft, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Tubo que se estrecha, con forma de tentáculo, a lo largo de +Y (longitud 1).</summary>
        public static Mesh Tentacle(int seg = 12, int sides = 7, float baseRadius = 0.18f, float curl = 0.5f)
        {
            string k = "tent" + seg + "_" + sides + "_" + baseRadius + "_" + curl;
            if (cache.TryGetValue(k, out var m)) return m;
            var v = new List<Vector3>(); var tri = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float r = Mathf.Lerp(baseRadius, 0.01f, Mathf.Pow(t, 0.8f));
                Vector3 center = new Vector3(Mathf.Sin(t * 2.4f) * curl * t, t, Mathf.Sin(t * 3.7f) * curl * 0.3f * t);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    v.Add(center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
                    uv.Add(new Vector2((float)s / sides, t));
                }
            }
            for (int i = 0; i < seg; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a = i * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                    tri.Add(a); tri.Add(c); tri.Add(b);
                    tri.Add(b); tri.Add(c); tri.Add(d);
                }
            m = new Mesh { name = k };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return cache[k] = m;
        }

        /// <summary>Cono (punta en +Y), útil para cuernos, púas, sombreros de bruja, cristales.</summary>
        public static Mesh Cone(int sides = 10)
        {
            string k = "cone" + sides;
            if (cache.TryGetValue(k, out var m)) return m;
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int s = 0; s < sides; s++)
            {
                float a0 = s * Mathf.PI * 2f / sides, a1 = (s + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                int b = v.Count;
                v.Add(p0); v.Add(new Vector3(0, 1, 0)); v.Add(p1);
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
                b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(Vector3.zero);
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            }
            m = new Mesh { name = k };
            m.SetVertices(v); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return cache[k] = m;
        }

        /// <summary>Manto/túnica acampanada: tronco de cono con borde inferior ondulado. Altura 1, base radio 0.5.</summary>
        public static Mesh Robe(int sides = 14, float topRadius = 0.18f, float wave = 0.06f)
        {
            string k = "robe" + sides + "_" + topRadius + "_" + wave;
            if (cache.TryGetValue(k, out var m)) return m;
            var v = new List<Vector3>(); var tri = new List<int>();
            int rows = 4;
            for (int r = 0; r <= rows; r++)
            {
                float t = (float)r / rows;
                float rad = Mathf.Lerp(0.5f, topRadius, Mathf.Pow(t, 0.9f));
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    float y = t + (r == 0 ? Mathf.Sin(a * 5f) * wave : 0f);
                    v.Add(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad));
                }
            }
            for (int r = 0; r < rows; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                    tri.Add(a); tri.Add(c); tri.Add(b);
                    tri.Add(b); tri.Add(c); tri.Add(d);
                }
            m = new Mesh { name = k };
            m.SetVertices(v); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return cache[k] = m;
        }

        /// <summary>Prisma alargado (cristal / obelisco) de n lados con punta.</summary>
        public static Mesh Crystal(int sides = 6, float tip = 0.25f)
        {
            string k = "crystal" + sides + "_" + tip;
            if (cache.TryGetValue(k, out var m)) return m;
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int s = 0; s < sides; s++)
            {
                float a0 = s * Mathf.PI * 2f / sides, a1 = (s + 1) * Mathf.PI * 2f / sides;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                Vector3 t0 = b0 + Vector3.up * (1f - tip), t1 = b1 + Vector3.up * (1f - tip);
                int i = v.Count;
                v.AddRange(new[] { b0, t0, t1, b1 });
                tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                i = v.Count;
                v.AddRange(new[] { t0, Vector3.up, t1 });
                tri.AddRange(new[] { i, i + 1, i + 2 });
            }
            m = new Mesh { name = k };
            m.SetVertices(v); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return cache[k] = m;
        }
    }
}
