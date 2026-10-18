using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PuebloLoco.EditorTools
{
    /// <summary>
    /// Deja el proyecto en URP: crea el UniversalRenderPipelineAsset, el renderer
    /// con SSAO, el VolumeProfile de post-procesado y lo asigna a GraphicsSettings
    /// y a TODOS los niveles de calidad (en este proyecto son 6 y todos estaban
    /// vacios, asi que si se asigna solo el activo, los demás niveles siguen en
    /// Built-in y el juego se ve distinto segun la calidad elegida).
    ///
    /// Todo es idempotente: se puede volver a ejecutar sin duplicar nada.
    /// </summary>
    public static class UrpSetup
    {
        const string SettingsFolder = "Assets/Settings";
        const string PipelineAssetPath = SettingsFolder + "/URP-PuebloLoco.asset";
        const string RendererAssetPath = SettingsFolder + "/URP-PuebloLoco_Renderer.asset";
        // El perfil va en Resources para que Bootstrap lo cargue en runtime y no
        // haya que tocar Main.unity (que ademas puede estar abierta en otro editor).
        const string VolumeProfileResourceDir = "Assets/Resources/PostFX";
        const string VolumeProfileResourcePath = VolumeProfileResourceDir + "/PuebloLoco_PostFX.asset";
        const string VolumeProfileAssetPath = "Assets/Settings/PuebloLoco_PostFX.asset";

        [MenuItem("Pueblo Loco/URP/0. Configurar URP completo")]
        public static void SetupAll()
        {
            EnsureFolder(SettingsFolder);
            EnsureFolder(VolumeProfileResourceDir);

            UrpLitMaterial.Create();

            var rendererData = CreateOrLoadRenderer();
            AddSsao(rendererData);

            var pipelineAsset = CreateOrLoadPipeline(rendererData);
            if (pipelineAsset == null)
            {
                Debug.LogError("[URP] no se pudo crear el UniversalRenderPipelineAsset");
                return;
            }

            TunePipeline(pipelineAsset);
            AssignToGraphicsAndQuality(pipelineAsset);
            CreatePostFx();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[URP] configuracion completa. Asignado a " + QualitySettings.names.Length + " nivel(es) de calidad.");
        }

        /* ------------------------------------------------------------ renderer */

        static UniversalRendererData CreateOrLoadRenderer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            data.name = "URP-PuebloLoco_Renderer";
            AssetDatabase.CreateAsset(data, RendererAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] renderer creado: " + RendererAssetPath);
            return data;
        }

        /// <summary>SSAO en URP es un Renderer Feature, no solo un override del
        /// Volume. Sin esto el override no hace nada.</summary>
        static void AddSsao(UniversalRendererData rendererData)
        {
            foreach (var f in rendererData.rendererFeatures)
                if (f is ScreenSpaceAmbientOcclusion)
                {
                    Debug.Log("[URP] SSAO ya estaba en el renderer");
                    return;
                }

            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.name = "ScreenSpaceAmbientOcclusion";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] ScreenSpaceAmbientOcclusion agregado al renderer");
        }

        /* ------------------------------------------------------------ pipeline */

        static UniversalRenderPipelineAsset CreateOrLoadPipeline(UniversalRendererData rendererData)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (existing != null)
            {
                if (existing.GetType() == typeof(UniversalRenderPipelineAsset))
                    return existing;
            }

            var asset = UniversalRenderPipelineAsset.Create(rendererData);
            asset.name = "URP-PuebloLoco";
            AssetDatabase.CreateAsset(asset, PipelineAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] pipeline asset creado: " + PipelineAssetPath);
            return asset;
        }

        static void TunePipeline(UniversalRenderPipelineAsset asset)
        {
            // Sombras: el default de URP es 2048 con 4 cascades; para un pueblo
            // isometrico chico alcanza con menos distancia y mas resolucion.
            asset.shadowDistance = 60f;
            asset.shadowCascadeCount = 2;
            asset.supportsHDR = false;
            asset.msaaSampleCount = 2;
            asset.renderScale = 1f;
            EditorUtility.SetDirty(asset);
        }

        static void AssignToGraphicsAndQuality(RenderPipelineAsset asset)
        {
            GraphicsSettings.defaultRenderPipeline = asset;

            var names = QualitySettings.names;
            int previous = QualitySettings.GetQualityLevel();
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(previous, false);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------------------------------------ post fx */

        static void CreatePostFx()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfileResourcePath);
            if (profile == null) profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfileResourcePath);
            }

            // Tonemapping: sin esto las luces y el bloom queman la imagen.
            var tm = GetOrAdd<Tonemapping>(profile);
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.Neutral;

            // Bloom: faroles, ventanas y oro. threshold alto para que solo florezca
            // lo que de verdad emite.
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.0f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.6f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.7f;

            // SSAO: el override. Ojo, la clase del Volume se llama
            // ScreenSpaceAmbientOcclusionVolumeOverride; ScreenSpaceAmbientOcclusion
            // a secas es el RendererFeature. Ademas mode arranca en None e
            // IsActive() devuelve false si queda en None, asi que hay que ponerlo.
            // TODO(URP): en URP 17.6 la clase SSAO no resuelve en el assembly del editor.
            // Se deja desactivado para no bloquear la compilacion; reactivar cuando
            // se confirme el nombre/assembly correcto.
            // var ssao = GetOrAdd<ScreenSpaceAmbientOcclusionVolumeOverride>(profile);
            // ssao.mode = ScreenSpaceAmbientOcclusionMode.Standard;
            // ssao.intensity = 1.0f;
            // ssao.radius = 0.4f;

            // Un poco de saturacion y contraste para el aire estilizado.
            var ca = GetOrAdd<ColorAdjustments>(profile);
            ca.saturation.overrideState = true; ca.saturation.value = 10f;
            ca.contrast.overrideState = true; ca.contrast.value = 5f;

            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.overrideState = true; vig.intensity.value = 0.25f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// VolumeProfile no expone Get&lt;T&gt;(): la API real es Has/TryGet/Add, y Add
        /// tira si el componente ya existe. Add&lt;T&gt;(true) es ademas lo unico que
        /// marca overrideState=true, necesario porque el override de SSAO expone
        /// intensity/radius/mode como propiedades simples y sin eso el Volume los ignora.
        /// </summary>
        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing))
            {
                existing.SetAllOverridesTo(true);
                existing.active = true;
                return existing;
            }
            var created = profile.Add<T>(true);
            created.active = true;
            return created;
        }

        /* ------------------------------------------------------------ auto */

        const string SessionMarker = "Library/URP_SETUP_DONE";

        /// <summary>
        /// Corre solo, en cuanto Unity resuelve el paquete de URP y compila.
        /// Hace falta porque la migracion depende de dos cosas que solo existen
        /// dentro del editor: el UniversalRenderPipelineAsset y la conversion de
        /// los materiales embebidos de los FBX.
        /// </summary>
        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            if (Shader.Find(UrpLitMaterial.ShaderName) == null) return;
            if (File.Exists(SessionMarker)) return;

            EditorApplication.delayCall += () =>
            {
                if (File.Exists(SessionMarker)) return;
                try
                {
                    SetupAll();
                    File.WriteAllText(SessionMarker, "ok");
                    Debug.Log("[URP] auto-setup completado");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[URP] auto-setup fallo: " + e);
                }
            };
        }

        /// <summary>Entrada para -executeMethod en batchmode.</summary>
        public static void SetupCI()
        {
            int code = 0;
            try
            {
                SetupAll();
                Directory.CreateDirectory("Library");
                File.WriteAllText(SessionMarker, "ok");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[URP] fallo: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        static void EnsureFolder(string absPath)
        {
            if (AssetDatabase.IsValidFolder(absPath)) return;
            string parent = Path.GetDirectoryName(absPath).Replace('\\', '/');
            string leaf = Path.GetFileName(absPath);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
