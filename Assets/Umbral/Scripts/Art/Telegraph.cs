using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Aviso de ataque en el suelo (zona roja que se llena), como los indicadores de Hades II.
    /// Formas: círculo, cono y línea/rectángulo.
    /// </summary>
    public class Telegraph : MonoBehaviour
    {
        public enum Shape { Circle, Cone, Line }

        Material outlineMat, fillMat;
        Transform fill;
        Shape shape;
        float duration, t;
        bool autoDestroy;
        Color color;

        public static Color EnemyColor = new Color(1f, 0.18f, 0.25f);
        public static Color BossColor = new Color(1f, 0.35f, 0.1f);

        public static Telegraph Circle(Vector2 center, float radius, float duration, Color? color = null)
        {
            var t = Make("TeleCircle", Shape.Circle, color ?? EnemyColor, duration);
            t.transform.position = center.X0Z(0.04f);
            t.transform.localScale = Vector3.one * radius;
            t.Build(MeshGen.Disc(), MeshGen.Ring(0.92f));
            return t;
        }

        public static Telegraph Cone(Vector2 origin, Vector2 dir, float radius, float angle, float duration, Color? color = null)
        {
            var t = Make("TeleCone", Shape.Cone, color ?? EnemyColor, duration);
            t.transform.position = origin.X0Z(0.04f);
            t.transform.rotation = U.LookFlat(dir);
            t.transform.localScale = Vector3.one * radius;
            t.Build(MeshGen.Sector(angle, 32), MeshGen.Sector(angle, 32, 0.93f));
            return t;
        }

        public static Telegraph Line(Vector2 from, Vector2 dir, float length, float width, float duration, Color? color = null)
        {
            var t = Make("TeleLine", Shape.Line, color ?? EnemyColor, duration);
            t.transform.position = from.X0Z(0.04f);
            t.transform.rotation = U.LookFlat(dir);
            t.transform.localScale = new Vector3(width, 1, length);
            t.Build(MeshGen.Rect(), null);
            return t;
        }

        static Telegraph Make(string name, Shape s, Color c, float duration)
        {
            var go = new GameObject(name);
            go.transform.SetParent(World.Root, false);
            var t = go.AddComponent<Telegraph>();
            t.shape = s; t.color = c; t.duration = Mathf.Max(0.01f, duration); t.autoDestroy = true;
            return t;
        }

        void Build(Mesh fillMesh, Mesh outlineMesh)
        {
            outlineMat = Mats.NewUnlit(color.WithA(0.28f), Mats.Blend.Alpha, null, 1f, false);
            fillMat = Mats.NewUnlit(color.WithA(0.45f), Mats.Blend.Additive, null, 1.4f, false);
            // base translúcida
            AddMesh("Base", fillMesh, outlineMat, Vector3.one, 0f);
            if (outlineMesh != null) AddMesh("Edge", outlineMesh, fillMat, Vector3.one, 0.005f);
            fill = AddMesh("Fill", fillMesh, fillMat, Vector3.zero, 0.01f).transform;
        }

        GameObject AddMesh(string n, Mesh m, Material mat, Vector3 scale, float y)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, y, 0);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (shape == Shape.Line) fill.localScale = new Vector3(1, 1, k);
            else fill.localScale = Vector3.one * k;
            if (fillMat != null)
            {
                float pulse = k > 0.85f ? 0.6f + 0.4f * Mathf.Sin(Time.time * 50f) : 0.45f;
                fillMat.SetColor(Mats.BaseColorId, color.WithA(pulse));
            }
            if (autoDestroy && t >= duration + 0.05f) Destroy(gameObject);
        }

        public void Kill() { if (this != null && gameObject != null) Destroy(gameObject); }

        void OnDestroy()
        {
            if (outlineMat != null) Destroy(outlineMat);
            if (fillMat != null) Destroy(fillMat);
        }
    }
}
