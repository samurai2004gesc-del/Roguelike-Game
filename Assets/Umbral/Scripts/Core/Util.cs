using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    /// <summary>Utilidades matemáticas sobre el plano XZ (el juego es 3D pero la jugabilidad es 2D, como en Hades).</summary>
    public static class U
    {
        public static Vector2 XZ(this Vector3 v) => new Vector2(v.x, v.z);
        public static Vector3 X0Z(this Vector2 v, float y = 0f) => new Vector3(v.x, y, v.y);
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static Vector2 Dir(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        public static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        /// <summary>Rotación Y que mira en la dirección XZ dada.</summary>
        public static Quaternion LookFlat(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);
        }

        public static Vector2 SafeNormal(this Vector2 v, Vector2 fallback)
        {
            float m = v.magnitude;
            return m > 0.0001f ? v / m : fallback;
        }

        public static float DistToSegment(Vector2 p, Vector2 a, Vector2 b, out Vector2 closest)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        public static Color WithA(this Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        public static T Pick<T>(this IList<T> list, System.Random rng) => list[rng.Next(list.Count)];

        public static void Shuffle<T>(this IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static float Range(this System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        public static bool Chance(this System.Random rng, float p) => rng.NextDouble() < p;

        public static Transform Child(this Transform t, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(t, false);
            return go.transform;
        }

        public static void DestroyChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        public static void SetLayerRecursive(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) c.gameObject.SetLayerRecursive(layer);
        }

        public static float Smooth01(float t) => t * t * (3f - 2f * t);
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }

    /// <summary>Temporizador simple basado en Time.time.</summary>
    public struct Cooldown
    {
        public float readyAt;
        public bool Ready => Time.time >= readyAt;
        public void Start(float seconds) => readyAt = Time.time + seconds;
        public float Remaining => Mathf.Max(0f, readyAt - Time.time);
    }
}
