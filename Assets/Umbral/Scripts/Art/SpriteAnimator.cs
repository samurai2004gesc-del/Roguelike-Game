using System.Collections;
using UnityEngine;

namespace Umbral
{
    /// <summary>
    /// Anima las figuras pintadas como marionetas de papel (estilo "paper puppet"):
    /// respiración en reposo, saltitos y balanceo al caminar, inclinación hacia delante,
    /// anticipación y estocada al atacar, retroceso al recibir daño, estela al esprintar,
    /// aleteo de criaturas voladoras y ondulación de túnicas y tentáculos.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public class SpriteAnimator : MonoBehaviour
    {
        public class Profile
        {
            public float hop = 0.05f;        // altura del saltito al caminar (fracción de la altura)
            public float stepRate = 9f;      // pasos por segundo a velocidad máxima
            public float sway = 0.05f;       // balanceo lateral
            public float lean = 0.1f;        // inclinación al avanzar
            public float breathe = 0.018f;   // respiración en reposo
            public float wave;               // ondulación de túnica/tentáculos
            public float waveHeight = 0.45f;
            public bool flies;
            public float flyHeight = 0.5f;
            public float flap;               // aleteo (escala horizontal)
            public float scuttle;            // temblor rápido (arañas, insectos)
        }

        public static Profile ProfileFor(string id)
        {
            var p = new Profile();
            switch (id)
            {
                case "sothis_f": case "sothis_m": case "keziah": case "deeppriest":
                    p.wave = 0.035f; p.waveHeight = 0.4f; p.hop = 0.035f; break;
                case "umr": case "boss_sacerdote":
                    p.wave = 0.05f; p.waveHeight = 0.6f; p.flies = true; p.flyHeight = 0.35f; p.hop = 0; break;
                case "wilbur": case "boss_wilbur":
                    p.wave = 0.04f; p.waveHeight = 0.3f; p.hop = 0.04f; break;
                case "piper": case "polyp":
                    p.wave = 0.07f; p.waveHeight = 0.6f; p.flies = true; p.flyHeight = 0.4f; p.hop = 0; break;
                case "shoggoth": case "shoggoth_blob": case "nyar_caos": case "retono":
                    p.wave = 0.06f; p.waveHeight = 0.5f; p.hop = 0.02f; p.sway = 0.08f; p.breathe = 0.04f; break;
                case "nightgaunt": case "shantak": case "migo": case "nyar_morador":
                    p.flies = true; p.flyHeight = 0.7f; p.flap = 0.09f; p.hop = 0; p.sway = 0.04f; break;
                case "zoog": case "fam_zoog": case "jenkin": case "fam_gato":
                    p.hop = 0.1f; p.stepRate = 13f; p.sway = 0.03f; break;
                case "lengspider": case "tindalos":
                    p.hop = 0.03f; p.stepRate = 16f; p.scuttle = 0.012f; p.lean = 0.14f; break;
                case "gug": case "starspawn": case "moonbeast": case "boss_dagon":
                    p.hop = 0.025f; p.stepRate = 5f; p.sway = 0.06f; p.breathe = 0.025f; break;
                case "boss_hydra":
                    p.wave = 0.06f; p.waveHeight = 0.45f; p.hop = 0; break;
                case "dummy":
                    p.hop = 0; p.sway = 0; p.lean = 0; p.breathe = 0.005f; break;
            }
            return p;
        }

        public Profile profile;
        ModelRig rig;
        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        Texture2D tex;
        float phase, leanSmooth, waveTime, ghostTimer, seed;
        static readonly int ShearId = Shader.PropertyToID("_Shear"), BendId = Shader.PropertyToID("_Bend"),
            StretchId = Shader.PropertyToID("_Stretch"), OffsetId = Shader.PropertyToID("_Offset"),
            WaveId = Shader.PropertyToID("_Wave"), WaveTimeId = Shader.PropertyToID("_WaveTime");

        ArtIA.Frames frames;
        Mesh frameMesh;
        int currentFrame = -1;

        /// <summary>Activa la animación por poses (hoja de 3x2) además de la deformación.</summary>
        public void SetFrames(ArtIA.Frames f, Mesh m) { frames = f; frameMesh = m; ShowFrame(0); }

        void ShowFrame(int i)
        {
            if (frames == null || i == currentFrame) return;
            currentFrame = i;
            ArtIA.ApplyUV(frameMesh, frames.uv[i]);
            transform.localScale = new Vector3(frames.size[i].x, frames.size[i].y, 1f);
        }

        int ChooseFrame(float k, bool dashing, float sinceSwing, float sinceHit)
        {
            if (dashing || sinceHit < 0.18f) return 5;
            if (sinceSwing < 0.07f) return 3;
            if (sinceSwing < 0.3f) return 4;
            if (k > 0.15f) return Mathf.Sin(phase) >= 0 ? 1 : 2;
            return 0;
        }

        public void Setup(ModelRig r, MeshRenderer renderer, Profile p, Texture2D t)
        {
            rig = r; mr = renderer; profile = p; tex = t;
            mpb = new MaterialPropertyBlock();
            seed = Random.value * 10f;
        }

        void LateUpdate()
        {
            if (rig == null || mr == null) return;
            if (rig.Frozen) return;
            float dt = Time.deltaTime, now = Time.time;
            var p = profile;
            float speed = rig.velocity.magnitude;
            float k = Mathf.Clamp01(speed / Mathf.Max(0.5f, rig.speedRef));
            var player = rig.transform.parent != null ? rig.transform.parent.GetComponent<PlayerController>() : null;
            bool dashing = (player != null && player.IsDashing) || speed > rig.speedRef * 1.9f;

            // ciclo de paso
            if (k > 0.05f) phase += dt * p.stepRate * Mathf.Lerp(0.6f, 1f, k);
            else phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, dt * 6f);
            float step = Mathf.Sin(phase);
            leanSmooth = Mathf.Lerp(leanSmooth, k, 1f - Mathf.Exp(-10f * dt));

            float shear = p.sway * step * k + p.lean * leanSmooth;
            float bend = 0f;
            float sx = 1f, sy = 1f;
            float ox = 0f, oy = 0f;

            // reposo: respiración
            float breath = Mathf.Sin((now + seed) * 2.4f) * p.breathe * (1f - k * 0.7f);
            sy += breath; sx -= breath * 0.5f;
            bend += Mathf.Sin((now + seed) * 1.3f) * 0.01f;

            // caminar: saltito y aplastado al aterrizar
            oy += Mathf.Abs(step) * p.hop * k;
            float land = 1f - Mathf.Abs(step);
            sy -= land * p.hop * 0.6f * k; sx += land * p.hop * 0.4f * k;
            if (p.scuttle > 0 && k > 0.05f) ox += Mathf.Sin(now * 60f + seed) * p.scuttle;

            // aleteo
            if (p.flap > 0) { float f = Mathf.Sin((now + seed) * 10f); sx += f * p.flap; sy -= f * p.flap * 0.3f; }

            // muelle de estirar/aplastar del esqueleto (golpes, ataques, saltos)
            sy *= 1f + rig.Squash * 0.9f;
            sx *= 1f - rig.Squash * 0.6f;

            // ataque: anticipación hacia atrás y estocada hacia delante
            float a = now - rig.lastSwingTime;
            if (a < 0.32f)
            {
                float env = a < 0.07f ? -(a / 0.07f) * 0.6f : 1f - (a - 0.07f) / 0.25f;
                shear += 0.28f * env;
                bend += 0.1f * env;
                if (env > 0) { sx += 0.12f * env; ox += 0.06f * env; }
            }

            // daño recibido: retroceso y temblor
            float h = now - rig.lastHitTime;
            if (h < 0.25f)
            {
                float rec = 1f - h / 0.25f;
                shear -= 0.15f * rec;
                ox += Mathf.Sin(h * 90f) * 0.03f * rec;
            }

            // esprint / embestida: estirado y estela de imágenes residuales
            if (dashing)
            {
                sx *= 1.22f; sy *= 0.9f; shear += 0.18f;
                ghostTimer -= dt;
                if (ghostTimer <= 0) { ghostTimer = 0.035f; SpawnGhost(player != null); }
            }

            if (frames != null) ShowFrame(ChooseFrame(k, dashing, now - rig.lastSwingTime, now - rig.lastHitTime));
            waveTime += dt * (1f + k * 1.5f);
            mr.GetPropertyBlock(mpb);
            mpb.SetFloat(ShearId, shear);
            mpb.SetFloat(BendId, bend);
            mpb.SetVector(StretchId, new Vector4(sx, sy, 0, 0));
            mpb.SetVector(OffsetId, new Vector4(ox, oy, 0, 0));
            mpb.SetVector(WaveId, new Vector4(p.wave * (1f + k), 7f, 3.5f, p.waveHeight));
            mpb.SetFloat(WaveTimeId, waveTime);
            mr.SetPropertyBlock(mpb);
        }

        void SpawnGhost(bool isPlayer)
        {
            if (World.Root == null) return;
            var mf = GetComponent<MeshFilter>();
            var go = new GameObject("Estela");
            go.transform.SetParent(World.Root, false);
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.transform.localScale = transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            Color c = isPlayer ? PlayerController.IridColor() : new Color(1f, 0.4f, 0.4f);
            var mat = Mats.NewUnlit(c.WithA(0.55f), Mats.Blend.Additive, tex, 1.4f, false);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Runner.Run(FadeGhost(go, mat));
        }

        static IEnumerator FadeGhost(GameObject go, Material mat)
        {
            Color c0 = mat.GetColor(Mats.BaseColorId);
            for (float t = 0; t < 0.28f && go != null; t += Time.deltaTime)
            {
                mat.SetColor(Mats.BaseColorId, c0.WithA(c0.a * (1f - t / 0.28f)));
                yield return null;
            }
            if (go != null) Object.Destroy(go);
            Object.Destroy(mat);
        }
    }
}
