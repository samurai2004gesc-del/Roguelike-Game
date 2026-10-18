using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Umbral.EditorTools
{
    /// <summary>
    /// Configura el proyecto automáticamente la primera vez que se abre:
    /// crea el asset de URP (Forward, HDR, sombras suaves), la escena principal y los ajustes de jugador.
    /// También se puede lanzar a mano desde el menú «Umbral».
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string SettingsDir = "Assets/Umbral/Settings";
        const string ScenePath = "Assets/Umbral/Scenes/Umbral.unity";
        const string RendererPath = SettingsDir + "/UmbralRenderer.asset";
        const string PipelinePath = SettingsDir + "/UmbralURP.asset";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) return;
                if (GraphicsSettings.defaultRenderPipeline == null || !File.Exists(ScenePath)) Configure();
            };
        }

        [MenuItem("Umbral/Configurar proyecto", priority = 0)]
        public static void Configure()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            // ---------------- URP ----------------
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }
            rendererData.renderingMode = RenderingMode.Forward;
            rendererData.depthPrimingMode = DepthPrimingMode.Disabled;
            if (rendererData.postProcessData == null)
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            EditorUtility.SetDirty(rendererData);

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, PipelinePath);
            }
            urp.supportsHDR = true;
            urp.msaaSampleCount = 1;
            urp.renderScale = 1f;
            urp.shadowDistance = 70f;
            urp.shadowCascadeCount = 2;
            urp.mainLightShadowmapResolution = 2048;
            urp.maxAdditionalLightsCount = 8;
            var so = new SerializedObject(urp);
            SetProp(so, "m_SoftShadowsSupported", true);
            SetProp(so, "m_AdditionalLightsRenderingMode", 1); // por píxel
            SetProp(so, "m_MainLightRenderingMode", 1);
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_RequireDepthTexture", false);
            SetProp(so, "m_RequireOpaqueTexture", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);

            // ---------------- ajustes de jugador ----------------
            PlayerSettings.companyName = "Umbral Studio";
            PlayerSettings.productName = "UMBRAL";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            // ---------------- escena ----------------
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("UMBRAL");
                go.AddComponent<Game>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Umbral] Proyecto configurado: URP + escena principal listos. Abre " + ScenePath + " y pulsa Play.");
        }

        static void SetProp(SerializedObject so, string name, object value)
        {
            var p = so.FindProperty(name);
            if (p == null) return;
            switch (value)
            {
                case bool b: p.boolValue = b; break;
                case int i:
                    if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i;
                    else p.intValue = i;
                    break;
                case float f: p.floatValue = f; break;
            }
        }

        [MenuItem("Umbral/Abrir escena principal", priority = 1)]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) Configure();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Umbral/Construir para Windows", priority = 20)]
        public static void BuildWindows()
        {
            Configure();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/UMBRAL.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Umbral] Build: " + report.summary.result + " — " + report.summary.totalErrors + " errores");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Umbral/Borrar partida guardada", priority = 40)]
        static void DeleteSave()
        {
            Save.Delete();
            PlayerPrefs.DeleteAll();
            Debug.Log("[Umbral] Partida borrada.");
        }

        /// <summary>Punto de entrada para modo batch: configura y sale.</summary>
        public static void ConfigureBatch()
        {
            Configure();
            EditorApplication.Exit(0);
        }
    }
}
