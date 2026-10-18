using UnityEngine;
using PuebloLoco.Gameplay;
using PuebloLoco.UI;

namespace PuebloLoco
{
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInit()
        {
            if (Object.FindFirstObjectByType<Bootstrap>() != null) return;
            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();
        }

        void Awake()
        {
            // GameManager
            if (GameManager.Instance == null)
                new GameObject("GameManager").AddComponent<GameManager>();

            // Luz principal (sol)
            if (FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.82f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.65f;
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
            if (cam.GetComponent<IsometricCamera>() == null)
                cam.gameObject.AddComponent<IsometricCamera>();

            // Grid + UI
            if (FindFirstObjectByType<GridBuildSystem>() == null)
                new GameObject("GridBuildSystem").AddComponent<GridBuildSystem>();
            if (FindFirstObjectByType<HUD>() == null)
                new GameObject("HUD").AddComponent<HUD>();
            if (FindFirstObjectByType<BuildMenu>() == null)
                new GameObject("BuildMenu").AddComponent<BuildMenu>();
            if (FindFirstObjectByType<FriendsMenu>() == null)
                new GameObject("FriendsMenu").AddComponent<FriendsMenu>();
            if (FindFirstObjectByType<QuestsMenu>() == null)
                new GameObject("QuestsMenu").AddComponent<QuestsMenu>();
            TutorialManager.Ensure();

            // Vecinos animados (modelos con esqueleto hechos en Blender)
            if (FindFirstObjectByType<VillagerManager>() == null)
                new GameObject("Villagers").AddComponent<VillagerManager>();
        }
    }
}
