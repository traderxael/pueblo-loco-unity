using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PuebloLoco.EditorTools
{
    /// <summary>
    /// Configura los FBX de personajes (rig generico + clips) y crea los
    /// Animator Controllers que usa el juego. Tambien compila el ejecutable.
    /// </summary>
    public class CharacterImportSettings : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Characters/")) return;

            var mi = assetImporter as ModelImporter;
            if (mi == null) return;

            mi.animationType = ModelImporterAnimationType.Generic;
            mi.importAnimation = true;
            mi.importBlendShapes = false;
            mi.animationCompression = ModelImporterAnimationCompression.Off;
            mi.importCameras = false;
            mi.importLights = false;

            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                string n = c.takeName;
                int bar = n.LastIndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                c.name = n;
                c.loopTime = n == "Idle" || n == "Walk" || n == "Run" || n == "Work";
            }
            mi.clipAnimations = clips;
        }
    }

    public static class CharacterPipeline
    {
        static readonly string[] Models = { "Villager", "Kid", "Elder", "SK_Sabio", "SK_Guardia" };

        [MenuItem("Pueblo Loco/1. Importar personajes y crear Animators")]
        public static void BuildControllers()
        {
            AssetDatabase.Refresh();

            // forzar reimport para que el postprocessor aplique rig/clips
            foreach (var m in Models)
                AssetDatabase.ImportAsset("Assets/Resources/Characters/" + m + ".fbx", ImportAssetOptions.ForceUpdate);

            Directory.CreateDirectory("Assets/Resources/Animations");
            foreach (var m in Models)
            {
                string fbx = "Assets/Resources/Characters/" + m + ".fbx";
                var clips = LoadClips(fbx);
                if (clips.Count == 0)
                {
                    Debug.LogError("[CHARS] no se encontraron clips en " + fbx);
                    continue;
                }
                var clipNames = new List<string>();
                foreach (var c in clips) clipNames.Add(c.name);
                var ctrl = BuildController(m, clips);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                string bounds = "?";
                if (prefab != null)
                {
                    var rs = prefab.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        Bounds b = rs[0].bounds;
                        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                        bounds = b.size.ToString("F2");
                    }
                }
                Debug.Log($"[CHARS] {m}: clips=[{string.Join(", ", clipNames)}] bounds={bounds} controller={(ctrl != null ? AssetDatabase.GetAssetPath(ctrl) : "ERROR")}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[CHARS] controllers listos");
        }

        static List<AnimationClip> LoadClips(string path)
        {
            var list = new List<AnimationClip>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = obj as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__")) list.Add(clip);
            }
            list.Sort((a, b) => string.Compare(a.name, b.name));
            return list;
        }

        /// <summary>Blender/Unity pueden dejar el take como "Rig|Idle"; nos quedamos con "Idle".</summary>
        static string CleanName(string n)
        {
            if (string.IsNullOrEmpty(n)) return n;
            int i = n.LastIndexOf('|');
            if (i >= 0) n = n.Substring(i + 1);
            return n.Trim();
        }

        static AnimatorController BuildController(string name, List<AnimationClip> clips)
        {
            string path = "Assets/Resources/Animations/" + name + ".controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Working", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Wave", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            foreach (var clip in clips)
            {
                string stateName = CleanName(clip.name);
                var st = sm.AddState(stateName);
                st.motion = clip;
                st.writeDefaultValues = false;
                states[stateName] = st;
            }

            AnimatorState idle, walk, run, work, wave;
            states.TryGetValue("Idle", out idle);
            states.TryGetValue("Walk", out walk);
            states.TryGetValue("Run", out run);
            states.TryGetValue("Work", out work);
            states.TryGetValue("Wave", out wave);

            if (idle != null) sm.defaultState = idle;

            AnimatorStateTransition T(AnimatorState from, AnimatorState to, float dur = 0.15f)
            {
                var t = from.AddTransition(to);
                t.hasExitTime = false;
                t.duration = dur;
                return t;
            }

            if (idle != null && walk != null)
            {
                var t = T(idle, walk, 0.12f);
                t.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");
                var t3 = T(walk, idle, 0.18f);
                t3.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
            }
            if (walk != null && run != null)
            {
                var t = T(walk, run, 0.12f);
                t.AddCondition(AnimatorConditionMode.Greater, 2.2f, "Speed");
                var t2 = T(run, walk, 0.15f);
                t2.AddCondition(AnimatorConditionMode.Less, 2.2f, "Speed");
            }
            if (idle != null && run != null)
            {
                var t = T(run, idle, 0.15f);
                t.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
            }
            if (walk != null && work != null)
            {
                var a = T(walk, work, 0.2f);
                a.AddCondition(AnimatorConditionMode.If, 0f, "Working");
            }
            if (idle != null && work != null)
            {
                var a = T(idle, work, 0.25f);
                a.AddCondition(AnimatorConditionMode.If, 0f, "Working");
                var b = T(work, idle, 0.25f);
                b.AddCondition(AnimatorConditionMode.IfNot, 0f, "Working");
            }

            if (wave != null)
            {
                var any = sm.AddAnyStateTransition(wave);
                any.hasExitTime = false;
                any.duration = 0.1f;
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0f, "Wave");
                var back = wave.AddTransition(idle != null ? idle : walk);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.2f;
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        [MenuItem("Pueblo Loco/2. Compilar ejecutable")]
        public static void BuildPlayerOnly()
        {
            BuildPlayer();
        }

        /// <summary>Entrada para batchmode: importa, crea animators y compila.</summary>
        public static void BuildCI()
        {
            int code = 0;
            try
            {
                BuildControllers();
                BuildScene();
                BuildPlayer();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[BUILD] fallo: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        static void BuildScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            string path = "Assets/Scenes/Main.unity";
            if (!File.Exists(path))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            Debug.Log("[BUILD] escena ok");
        }

        static void BuildPlayer()
        {
            Directory.CreateDirectory("Build");
            string exe = "Build/PuebloLoco.exe";
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            var sum = report.summary;
            Debug.Log($"[BUILD] resultado={sum.result} bytes={sum.totalSize} errores={sum.totalErrors} tiempo={sum.totalTime}");
            if (sum.result != BuildResult.Succeeded) throw new System.Exception("build fallo: " + sum.result);
        }
    }
}

