using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    public class QuestsMenu : MonoBehaviour
    {
        public static QuestsMenu Instance { get; private set; }
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

            panel = HUD.CreatePanel("QuestsPanel", transform,
                new Vector2(0.3f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.08f, 0.12f, 0.96f)).gameObject;
            panel.SetActive(false);

            HUD.CreateText("Title", panel.transform, "📋 Misiones", 32, TextAnchor.MiddleCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70));

            var closeBtn = HUD.CreateButton("Close", panel.transform, "✕",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -10), new Vector2(50, 50),
                new Color(0.8f, 0.2f, 0.2f));
            closeBtn.onClick.AddListener(Hide);

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
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
            if (visible)
            {
                TutorialManager.Ensure()?.SignalQuests();
                refreshTimer = 0f;
                Refresh();
            }
        }

        public void Hide() { visible = false; panel.SetActive(false); }

        void Refresh()
        {
            if (!visible) return;
            foreach (Transform t in listContent) Destroy(t.gameObject);
            var s = GameManager.Instance.State;
            if (s == null) return;

            // ── Misión activa ──
            var q = Economy.ActiveQuest(s);
            if (q != null)
            {
                var row = CreateRow("ActiveQuest", new Color(0.15f, 0.25f, 0.15f), 130);
                HUD.CreateText("Name", row.transform, $"⭐ {q.name}  ({s.questIndex + 1}/{Catalog.QUESTS.Length})",
                    22, TextAnchor.UpperLeft, new Vector2(0, 0.6f), new Vector2(1, 1), new Vector2(15, 5));
                HUD.CreateText("Story", row.transform, q.story,
                    16, TextAnchor.LowerLeft, new Vector2(0, 0.3f), new Vector2(1, 0.6f), new Vector2(15, 0));
                string rw = "";
                if (q.reward.donas > 0) rw += $"{q.reward.donas}🍩 ";
                if (q.reward.xp > 0) rw += $"{q.reward.xp}⭐";
                HUD.CreateText("Reward", row.transform, $"Recompensa: {rw}",
                    14, TextAnchor.LowerRight, new Vector2(0.5f, 0), new Vector2(1, 0.35f), new Vector2(0, 5));
                // objetivos
                string objs = "";
                foreach (var o in q.objs)
                {
                    bool done = Economy.ObjectiveDone(s, o);
                    objs += (done ? "✅ " : "⬜ ") + ObjectiveLabel(o) + "\n";
                }
                HUD.CreateText("Objs", row.transform, objs.TrimEnd(),
                    14, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0.6f, 0.35f), new Vector2(15, 5));
            }
            else
            {
                var row = CreateRow("AllDone", new Color(0.2f, 0.35f, 0.2f), 70);
                HUD.CreateText("Msg", row.transform, "🏆 ¡Todas las misiones completadas! Eres leyenda local",
                    20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero);
            }

            // ── Misiones diarias ──
            var dailies = Economy.DailyProgressList(s, GameManager.Instance.NowMs);
            if (dailies.Count > 0)
            {
                var header = CreateRow("DailyHeader", new Color(0.2f, 0.15f, 0.3f), 45);
                HUD.CreateText("H", header.transform, "📅 Diarias (rotan a medianoche)",
                    18, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(15, 0));
                foreach (var d in dailies)
                {
                    var row = CreateRow($"Daily_{d.def.id}", new Color(0.12f, 0.12f, 0.18f), 65);
                    HUD.CreateText("Name", row.transform,
                        $"{(d.done ? "✅" : "⬜")} {d.def.name}  ·  {d.progress}/{d.goal}  ·  +{d.def.donas}🍩",
                        17, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0.75f, 1), new Vector2(15, 0));
                    HUD.CreateText("Desc", row.transform, d.def.desc,
                        13, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0.75f, 0.5f), new Vector2(15, 0));
                    if (d.done && !d.claimed)
                    {
                        var id = d.def.id;
                        var btn = HUD.CreateButton("Claim", row.transform, "🎁 Reclamar",
                            new Vector2(1, 0), new Vector2(1, 1), new Vector2(-15, 0), new Vector2(140, 55),
                            new Color(0.2f, 0.7f, 0.3f));
                        var rt = (RectTransform)btn.transform;
                        rt.anchorMin = new Vector2(1, 0.15f); rt.anchorMax = new Vector2(1, 0.85f);
                        rt.anchoredPosition = new Vector2(-80, 0); rt.sizeDelta = new Vector2(130, 50);
                        btn.onClick.AddListener(() =>
                        {
                            var r = GameManager.Instance.ClaimDaily(id);
                            if (r.ok) Toast.Show($"🍩 +{r.donas} donas");
                            else Toast.Show($"❌ {r.reason}");
                        });
                    }
                }
            }

            // ── Logros ──
            var achHeader = CreateRow("AchHeader", new Color(0.3f, 0.2f, 0.1f), 45);
            HUD.CreateText("H", achHeader.transform, $"🏆 Logros ({s.achievements.Count}/{Catalog.ACHIEVEMENTS.Length})",
                18, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(15, 0));

            foreach (var a in Catalog.ACHIEVEMENTS)
            {
                bool unlocked = s.HasAchievement(a.id);
                var row = CreateRow($"Ach_{a.id}",
                    unlocked ? new Color(0.15f, 0.25f, 0.15f) : new Color(0.1f, 0.1f, 0.14f), 40);
                string label = unlocked ? "🏆" : "🔒";
                HUD.CreateText("A", row.transform, $"{label} {a.name} — {a.desc}" +
                    (a.donas > 0 && unlocked ? $"  +{a.donas}🍩" : ""),
                    15, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(15, 0));
            }
        }

        RectTransform CreateRow(string name, Color color, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(listContent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        static string ObjectiveLabel(QuestObjective o) => o.type switch
        {
            "build" => $"Construye: {Catalog.Building(o.def)?.name ?? o.def}",
            "unlock" => $"Desbloquea: {Catalog.Character(o.def)?.name ?? o.def}",
            "level" => $"Alcanza nivel {o.level}",
            "coinsEarned" => $"Recolecta {o.amount} monedas",
            "tasksDone" => $"Completa {o.amount} tareas",
            "buildings" => $"Ten {o.amount} edificios",
            "decor" => $"Coloca {o.amount} adornos",
            _ => o.type
        };
    }
}
