using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Umbral
{
    /// <summary>
    /// Cámara isométrica (3/4 en diagonal, como Hades) con seguimiento suave, anticipación hacia donde apuntas,
    /// temblor por "trauma" y posprocesado: bloom, viñeta, aberración cromática que late al recibir daño.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance;
        public Camera cam;
        public Transform target;
        public float pitch = 52f, yaw = 45f, distance = 25f, fov = 24f;
        public float lookAhead = 1.6f;

        Vector3 focus;
        float trauma;
        float zoom = 1f, zoomTarget = 1f;
        Volume volume;
        ChromaticAberration chroma;
        Vignette vignette;
        ColorAdjustments color;
        Bloom bloom;
        float hurtPulse;
        Vector2 extraOffset;

        public static void Create(Transform parent)
        {
            var go = new GameObject("CameraRig");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<CameraRig>();
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(go.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.01f, 0.04f);
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 220f;
            cam.fieldOfView = Instance.fov;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            Instance.cam = cam;
            Instance.BuildVolume();
        }

        void BuildVolume()
        {
            var vgo = new GameObject("PostFX");
            vgo.transform.SetParent(transform, false);
            volume = vgo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.72f);

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.08f, 0.0f, 0.12f));

            color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(14f);
            color.saturation.Override(18f);
            color.postExposure.Override(0.15f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(0.08f);

            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.18f);
        }

        public static void Shake(float amount)
        {
            if (Instance == null || !GameSettings.ScreenShake) return;
            Instance.trauma = Mathf.Min(1f, Instance.trauma + amount);
        }

        public static void HurtPulse() { if (Instance != null) Instance.hurtPulse = 1f; }

        public void SetTarget(Transform t, bool snap = true)
        {
            target = t;
            if (snap && t != null) { focus = t.position; Apply(Vector3.zero); }
        }

        public void SetZoom(float z) => zoomTarget = z;
        public void SetOffset(Vector2 o) => extraOffset = o;

        public void SetGrading(Color vignetteColor, float saturation, float exposure)
        {
            if (vignette != null) vignette.color.Override(vignetteColor);
            if (color != null) { color.saturation.Override(saturation); color.postExposure.Override(exposure); }
        }

        /// <summary>Punto del suelo (y=0) bajo la posición de pantalla.</summary>
        public Vector2 ScreenToGround(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.y) < 1e-4f) return focus.XZ();
            float t = -ray.origin.y / ray.direction.y;
            return (ray.origin + ray.direction * t).XZ();
        }

        /// <summary>Convierte una dirección de entrada (stick/WASD) a dirección de mundo según el giro de la cámara.</summary>
        public Vector2 InputToWorld(Vector2 input)
        {
            if (input.sqrMagnitude < 1e-5f) return Vector2.zero;
            return input.Rotate(-yaw);
        }

        void LateUpdate()
        {
            if (target != null)
            {
                Vector3 desired = target.position;
                var pc = target.GetComponent<PlayerController>();
                if (pc != null) desired += (pc.aimDir * lookAhead).X0Z();
                desired += extraOffset.X0Z();
                focus = Vector3.Lerp(focus, desired, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            }
            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));

            trauma = Mathf.Max(0, trauma - Time.unscaledDeltaTime * 1.6f);
            float sh = trauma * trauma;
            float tt = Time.unscaledTime * 25f;
            Vector3 shake = new Vector3(Mathf.PerlinNoise(tt, 0) - 0.5f, 0, Mathf.PerlinNoise(0, tt) - 0.5f) * sh * 1.6f;
            Apply(shake);

            hurtPulse = Mathf.Max(0, hurtPulse - Time.unscaledDeltaTime * 2.5f);
            if (chroma != null) chroma.intensity.Override(0.08f + hurtPulse * 0.7f);
            if (vignette != null) vignette.intensity.Override(0.38f + hurtPulse * 0.15f);
        }

        void Apply(Vector3 shake)
        {
            if (cam == null) return;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 pos = focus + shake - rot * Vector3.forward * distance * zoom;
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
        }
    }
}
