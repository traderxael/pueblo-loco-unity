using System.Collections;
using System.IO;
using UnityEngine;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.Dev
{
    /// <summary>
    /// Modo captura para verificar el juego sin interacción:
    /// arma un pueblo de ejemplo, deja a los vecinos animados y guarda PNGs.
    /// Uso: PuebloLoco.exe -shotdir "ruta"
    /// </summary>
    public class DevCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-shotdir") < 0) return;
            var go = new GameObject("DevCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<DevCapture>();
        }

        string Dir
        {
            get
            {
                var a = System.Environment.GetCommandLineArgs();
                int i = System.Array.IndexOf(a, "-shotdir");
                if (i >= 0 && i + 1 < a.Length) return a[i + 1];
                return Path.Combine(Application.dataPath, "..", "Shots");
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            Debug.Log("[CAPTURE] carpeta " + Dir);
            yield return new WaitForSecondsRealtime(1.2f);

            SeedTown();
            yield return new WaitForSecondsRealtime(1.0f);

            float[] marks = { 0f, 1.5f, 3f, 4.5f, 6f, 8f };
            float t = 0f;
            int idx = 0;
            while (idx < marks.Length)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                if (t >= marks[idx])
                {
                    yield return Snap(string.Format("{0:00}_town", idx));
                    idx++;
                }
            }

            Debug.Log("[CAPTURE] fin");
            Application.Quit();
        }

        IEnumerator Snap(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex != null)
            {
                File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
                Destroy(tex);
            }
            Debug.Log("[CAPTURE] " + name);
            yield return null;
        }

        void SeedTown()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogWarning("[CAPTURE] sin GameManager"); return; }

            var s = gm.State;
            s.coins = 250000;
            s.level = 12;
            s.landTier = 1; // terreno 8x8
            foreach (var c in s.characters) c.unlocked = true;

            s.buildings.Clear();
            var layout = new[]
            {
                ("casa", 2, 2), ("kiosco", 5, 2), ("panaderia", 3, 5),
                ("casa", 6, 6), ("floreria", 1, 6), ("cafeteria", 6, 1)
            };
            foreach (var l in layout)
            {
                s.buildings.Add(new PlacedBuilding
                {
                    id = GameState.NextId(),
                    def = l.Item1,
                    x = l.Item2,
                    y = l.Item3,
                    lastAt = gm.NowMs,
                    ready = 0
                });
            }

            var luis = s.characters.Find(c => c.def == "luis");
            if (luis != null) luis.task = new ActiveTask { taskId = "regar", endAt = gm.NowMs + 300000, done = false };
            var carmen = s.characters.Find(c => c.def == "carmen");
            if (carmen != null) carmen.task = new ActiveTask { taskId = "hornear", endAt = gm.NowMs + 300000, done = false };
            var nina = s.characters.Find(c => c.def == "nina");
            if (nina != null) nina.task = new ActiveTask { taskId = "jugar", endAt = gm.NowMs + 300000, done = false };

            gm.NotifyChanged();
            if (GridBuildSystem.Instance != null) GridBuildSystem.Instance.RebuildAll();
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.forceAll = true;
                VillagerManager.Instance.Sync();
            }
            var tutorial = PuebloLoco.UI.TutorialManager.Instance;
            if (tutorial != null) tutorial.EndTutorial(true);

            var cam = Camera.main;
            if (cam != null)
            {
                var iso = cam.GetComponent<IsometricCamera>();
                if (iso != null)
                {
                    iso.FocusOn(new Vector3(0f, 0f, 0f));
                    iso.SetDistance(6.2f);
                }
            }
            Debug.Log("[CAPTURE] pueblo sembrado: edificios=" + s.buildings.Count + " personajes=" + s.characters.Count);
        }
    }
}
