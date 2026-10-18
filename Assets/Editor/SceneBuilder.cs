using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PuebloLoco;

public static class SceneBuilder
{
    [MenuItem("Pueblo Loco/Build Main Scene")]
    public static void BuildMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var boot = new GameObject("Bootstrap");
        boot.AddComponent<Bootstrap>();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        Debug.Log("Scene built: Assets/Scenes/Main.unity");
    }

    public static void BuildSceneBatch()
    {
        BuildMainScene();
        EditorApplication.Exit(0);
    }
}
