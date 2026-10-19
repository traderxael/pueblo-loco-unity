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
            Application.runInBackground = true;
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

            yield return new WaitForSecondsRealtime(0.3f);

            // toma de acercamiento a la pagoda y los vecinos
            var cam = Camera.main;
            if (cam != null)
            {
                var iso = cam.GetComponent<IsometricCamera>();
                if (iso != null)
                {
                    Vector3 focus = GridBuildSystem.Instance != null
                        ? GridBuildSystem.Instance.GridToWorld(6, 7)
                        : new Vector3(0f, 0f, 0f);
                    iso.FocusOn(focus);
                    iso.SetDistance(7.5f);
                    yield return new WaitForSecondsRealtime(1.4f);
                    yield return StartCoroutine(Snap("06_closeup"));
                }
            }

            // capturas de los menus (UI compacta)
            var ui = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            string[] menues = { "build", "friends", "quests", "decor" };
            string[] nombres = { "07_menu_construir", "08_menu_amigos", "09_menu_misiones", "10_menu_decorar" };
            for (int m = 0; m < menues.Length; m++)
            {
                foreach (var b in ui)
                    if (b.name.ToLowerInvariant().Contains(menues[m])) { b.onClick.Invoke(); break; }
                yield return new WaitForSecondsRealtime(0.7f);
                yield return StartCoroutine(Snap(nombres[m]));
                foreach (var b in ui)
                    if (b.name.ToLowerInvariant().Contains(menues[m])) { b.onClick.Invoke(); break; }
                yield return new WaitForSecondsRealtime(0.3f);
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
            Debug.Log("[CAPTURE] " + name + " ok=" + (tex != null));
            yield return null;
        }

        void SeedTown()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogWarning("[CAPTURE] sin GameManager"); return; }

            var s = gm.State;
            s.coins = 250000;
            s.level = 12;
            s.landTier = 7; // terreno 20x20
            foreach (var c in s.characters) c.unlocked = true;

            s.buildings.Clear();
            var layout = new[]
            {
                ("casa", 2, 2), ("kiosco", 4, 3), ("panaderia", 6, 2),
                ("casahan", 3, 4), ("campanario", 2, 5), ("pagoda", 6, 6),
                ("mercado", 2, 8), ("templo", 8, 8), ("muralla", 10, 4),
                ("herreria", 10, 2), ("granja", 4, 10), ("posada", 10, 10)
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

            s.decor.Clear();
            var decor = new[]
            {
                ("farol", 1, 5), ("farol", 7, 2), ("estatua", 0, 3), ("estatua", 7, 7)
            };
            foreach (var d in decor)
            {
                s.decor.Add(new PlacedDecor
                {
                    id = GameState.NextId(),
                    def = d.Item1,
                    x = d.Item2,
                    y = d.Item3
                });
            }

            var luis = s.characters.Find(c => c.def == "luis");
            if (luis != null) luis.task = new ActiveTask { taskId = "regar", endAt = gm.NowMs + 300000, done = false };
            var carmen = s.characters.Find(c => c.def == "carmen");
            if (carmen != null) carmen.task = new ActiveTask { taskId = "hornear", endAt = gm.NowMs + 300000, done = false };
            var nina = s.characters.Find(c => c.def == "nina");
            if (nina != null) nina.task = new ActiveTask { taskId = "jugar", endAt = gm.NowMs + 300000, done = false };
            var sabio = s.characters.Find(c => c.def == "sabio");
            if (sabio != null) sabio.task = new ActiveTask { taskId = "estudiar", endAt = gm.NowMs + 300000, done = false };
            var guardia = s.characters.Find(c => c.def == "guardia");
            if (guardia != null) guardia.task = new ActiveTask { taskId = "patrullar", endAt = gm.NowMs + 300000, done = false };
            var comerciante = s.characters.Find(c => c.def == "comerciante");
            if (comerciante != null) comerciante.task = new ActiveTask { taskId = "vender", endAt = gm.NowMs + 300000, done = false };
            var campesino = s.characters.Find(c => c.def == "campesino");
            if (campesino != null) campesino.task = new ActiveTask { taskId = "sembrar", endAt = gm.NowMs + 300000, done = false };

            gm.NotifyChanged();
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.forceAll = true;
                VillagerManager.Instance.Sync();
            }
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            var botones = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            Debug.Log("[UI] eventSystem=" + (es != null) + " modulo=" + (es != null && es.currentInputModule != null) + " botones=" + botones.Length);
            var tutorial = PuebloLoco.UI.TutorialManager.Instance;
            if (tutorial != null) tutorial.EndTutorial(true);

            var cam = Camera.main;
            if (cam != null)
            {
                var iso = cam.GetComponent<IsometricCamera>();
                if (iso != null)
                {
                    iso.FocusOn(new Vector3(0f, 0f, 0f));
                    iso.SetDistance(29f);
                }
            }            Debug.Log("[CAPTURE] pueblo sembrado: edificios=" + s.buildings.Count + " personajes=" + s.characters.Count);
        }
    }
}


