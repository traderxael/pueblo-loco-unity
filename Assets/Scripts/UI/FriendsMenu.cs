using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    public class FriendsMenu : MonoBehaviour
    {
        public static FriendsMenu Instance { get; private set; }
        GameObject panel;
        bool visible;
        RectTransform listContent;
        float refreshTimer;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            panel = HUD.CreatePanel("FriendsPanel", transform,
                new Vector2(0.45f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.08f, 0.12f, 0.96f)).gameObject;
            panel.SetActive(false);

            HUD.CreateText("Title", panel.transform, "🧑 Vecinos", 32, TextAnchor.MiddleCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70));

            var closeBtn = HUD.CreateButton("Close", panel.transform, "✕",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -10), new Vector2(50, 50),
                new Color(0.8f, 0.2f, 0.2f));
            closeBtn.onClick.AddListener(Hide);

            // scroll list
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(panel.transform, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = new Vector2(0.03f, 0.03f); srt.anchorMax = new Vector2(0.97f, 0.92f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            listContent = (RectTransform)contentGo.transform;
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1);
            listContent.pivot = new Vector2(0.5f, 1);
            listContent.offsetMin = Vector2.zero; listContent.offsetMax = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = listContent;
            scroll.horizontal = false;

            GameManager.Instance.OnStateChanged += Refresh;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!visible) return;
            refreshTimer += Time.deltaTime;
            if (refreshTimer < 0.5f) return;
            refreshTimer = 0f;
            Refresh();
        }

        public void Toggle()
        {
            visible = !visible;
            panel.SetActive(visible);
            if (visible) { refreshTimer = 0f; Refresh(); }
        }

        public void Hide() { visible = false; panel.SetActive(false); }

        void Refresh()
        {
            if (!visible) return;
            foreach (Transform t in listContent) Destroy(t.gameObject);
            var s = GameManager.Instance.State;
            if (s == null) return;

            foreach (var cs in s.characters)
            {
                var def = Catalog.Character(cs.def);
                if (def == null) continue;

                bool locked = !cs.unlocked;
                var color = locked ? new Color(0.2f, 0.2f, 0.22f) : new Color(0.12f, 0.14f, 0.18f);
                var row = new GameObject(def.def, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 90;
                row.GetComponent<Image>().color = color;

                string status;
                if (locked) status = $"🔒 {def.donas} 🍩";
                else if (cs.task == null) status = "✅ Libre";
                else if (cs.task.done) status = "🎁 ¡Reclamar!";
                else
                {
                    long remain = System.Math.Max(0, cs.task.endAt - GameManager.Instance.NowMs);
                    status = "⏳ " + FormatRemain(remain);
                }

                HUD.CreateText("Name", row.transform, $"{def.name}   {status}",
                    22, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(15, 0));

                if (locked)
                {
                    var unlockDef = def;
                    var btn = row.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        var r = GameManager.Instance.UnlockCharacter(unlockDef.def);
                        if (!r.ok) Toast.Show($"❌ {r.reason}");
                        else Toast.Show($"🎉 ¡{unlockDef.name} se unió al barrio!");
                    });
                }
                else if (cs.task != null && cs.task.done)
                {
                    var charDef = cs.def;
                    var btn = row.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        var r = GameManager.Instance.CompleteTask(charDef);
                        if (r.ok)
                        {
                            Toast.Show($"💰 +{r.coins} 🪙  ⭐ +{r.xp} XP");
                            TutorialManager.Ensure()?.SignalTask();
                        }
                    });
                }
                else if (cs.task == null)
                {
                    // mini botones de tarea
                    float bx = -10;
                    foreach (var task in def.tasks)
                    {
                        var t = task;
                        var charDefId = def.def;
                        var tbtn = HUD.CreateButton($"T_{t.id}", row.transform, $"{t.name} ({FormatDur(t.sec)})",
                            new Vector2(1, 0), new Vector2(1, 1), new Vector2(bx, 0), new Vector2(200, 70),
                            new Color(0.2f, 0.5f, 0.85f));
                        var rt = (RectTransform)tbtn.transform;
                        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                        rt.anchoredPosition = new Vector2(bx - 95, 0);
                        rt.sizeDelta = new Vector2(190, 60);
                        tbtn.GetComponentInChildren<Text>().fontSize = 13;
                        tbtn.onClick.AddListener(() =>
                        {
                            var r = GameManager.Instance.StartTask(charDefId, t.id);
                            if (r.ok) { Toast.Show($"▶ {t.name} iniciada"); TutorialManager.Ensure()?.SignalTask(); }
                            else Toast.Show($"❌ {r.reason}");
                        });
                        bx -= 200;
                    }
                }
                else
                {
                    // tarea en curso: botón acelerar
                    var charDefId = cs.def;
                    int rush = Economy.RushCost(s, cs.def, GameManager.Instance.NowMs);
                    var rushBtn = HUD.CreateButton("Rush", row.transform, $"⚡ {rush} 🍩",
                        new Vector2(1, 0), new Vector2(1, 1), new Vector2(-110, 0), new Vector2(140, 60),
                        new Color(0.85f, 0.6f, 0.1f));
                    var rt = (RectTransform)rushBtn.transform;
                    rt.anchorMin = new Vector2(1, 0.15f); rt.anchorMax = new Vector2(1, 0.85f);
                    rt.anchoredPosition = new Vector2(-80, 0);
                    rt.sizeDelta = new Vector2(130, 55);
                    rushBtn.onClick.AddListener(() =>
                    {
                        var r = GameManager.Instance.RushTask(charDefId);
                        if (r.ok) Toast.Show($"⚡ ¡Tarea completada! +{r.coins} 🪙");
                        else Toast.Show($"❌ {r.reason}");
                    });
                }

                // tareas info
                if (locked || cs.task != null)
                    HUD.CreateText("Tasks", row.transform, TaskSummary(def),
                        14, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0.6f, 0.5f), new Vector2(15, 5));
            }
        }

        static string TaskSummary(CharacterDef def)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var t in def.tasks) parts.Add($"{t.name} → {t.coins}🪙/{FormatDur(t.sec)}");
            return string.Join("   ·   ", parts);
        }

        static string FormatDur(int sec)
        {
            if (sec >= 3600) return $"{sec / 3600}h";
            if (sec >= 60) return $"{sec / 60}m";
            return $"{sec}s";
        }

        /// <summary>Cuenta regresiva. TimeSpan.Minutes envuelve a los 60, así que
        /// las tareas de varias horas necesitan el total de horas.</summary>
        static string FormatRemain(long ms)
        {
            var ts = System.TimeSpan.FromMilliseconds(ms);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            return $"{ts.Minutes:00}:{ts.Seconds:00}";
        }
    }
}
