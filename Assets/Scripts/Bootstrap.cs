using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PuebloLoco.Gameplay;
using PuebloLoco.UI;

namespace PuebloLoco
{
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInit()
        {
            if (Object.FindAnyObjectByType<Bootstrap>() != null) return;
            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();
        }

        /// <summary>
        /// Sin EventSystem ningún botón de la UI responde (construir, saltar tutorial, etc.).
        /// La escena no lo trae y Bootstrap crea toda la UI por código, así que se
        /// asegura acá. activeInputHandler = 0 usa el Input Manager clásico.
        /// </summary>
        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(go);
        }


        /// <summary>
        /// UI compacta: sube la resolución de referencia de todas las canvases
        /// (los elementos quedan ~20% mas chicos y el texto tiene mas aire).
        /// </summary>
        static void NormalizeUI()
        {
            foreach (var cs in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                cs.referenceResolution = new Vector2(2048f, 1152f);
            }
        }
        void Awake()
        {
            EnsureEventSystem();

            // GameManager
            if (GameManager.Instance == null)
                new GameObject("GameManager").AddComponent<GameManager>();

            // Luz principal (sol)
            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                // URP trabaja con intensidad lineal y su atenuacion difiere de la de
                // Built-in: 0.82 dejaba la escena oscura con el sol a ~45 grados.
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.7f;
                lightGo.transform.rotation = Quaternion.Euler(52, -38, 0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.46f, 0.56f, 0.72f);
                RenderSettings.ambientEquatorColor = new Color(0.40f, 0.44f, 0.50f);
                RenderSettings.ambientGroundColor = new Color(0.24f, 0.26f, 0.30f);
            }

            // Cámara isométrica
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.53f, 0.75f, 0.92f); // cielo
            cam.fieldOfView = 45;
            // La camara se crea en runtime, asi que no hay donde agregarlo desde el
            // Inspector. URP lee sus ajustes de este componente.
            if (cam.GetComponent<UniversalAdditionalCameraData>() == null)
                cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            if (cam.GetComponent<IsometricCamera>() == null)
                cam.gameObject.AddComponent<IsometricCamera>();

            // Grid + UI
            if (FindAnyObjectByType<GridBuildSystem>() == null)
                new GameObject("GridBuildSystem").AddComponent<GridBuildSystem>();
            if (FindAnyObjectByType<HUD>() == null)
                new GameObject("HUD").AddComponent<HUD>();
            if (FindAnyObjectByType<BuildMenu>() == null)
                new GameObject("BuildMenu").AddComponent<BuildMenu>();
            if (FindAnyObjectByType<FriendsMenu>() == null)
                new GameObject("FriendsMenu").AddComponent<FriendsMenu>();
            if (FindAnyObjectByType<QuestsMenu>() == null)
                new GameObject("QuestsMenu").AddComponent<QuestsMenu>();
            if (FindAnyObjectByType<DecorMenu>() == null)
                new GameObject("DecorMenu").AddComponent<DecorMenu>();
            TutorialManager.Ensure();
            // tooltip flotante de las tarjetas (estilo city-builder)
            if (FindAnyObjectByType<Tooltip>() == null)
                new GameObject("Tooltip").AddComponent<Tooltip>();
            SetupPostFx();
            NormalizeUI();

            // Vecinos animados (modelos con esqueleto hechos en Blender)
            if (FindAnyObjectByType<VillagerManager>() == null)
                new GameObject("Villagers").AddComponent<VillagerManager>();
        }

        /// <summary>
        /// Volume global de post-procesado (tonemapping, bloom, SSAO, color).
        /// Se crea por codigo en vez de en la escena para no depender de que
        /// Main.unity tenga el Volume puesto: el perfil lo produce
        /// Assets/Editor/UrpSetup.cs en Resources/PostFX/.
        /// </summary>
        static void SetupPostFx()
        {
            if (FindAnyObjectByType<Volume>() != null) return;

            var go = new GameObject("PostFX Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;

            var profile = Resources.Load<VolumeProfile>("PostFX/PuebloLoco_PostFX");
            if (profile != null)
            {
                volume.sharedProfile = profile;
            }
            else
            {
                Debug.LogWarning("[URP] falta Resources/PostFX/PuebloLoco_PostFX; corre Pueblo Loco > URP > 0. Configurar URP completo");
            }

            DontDestroyOnLoad(go);
        }
    }
}
