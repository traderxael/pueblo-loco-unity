using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    public class HUD : MonoBehaviour
    {
        Text coinsText, donasText, levelText, xpText, questText;
        Image xpBar;
        Canvas canvas;

        void Start()
        {
            Build();
            GameManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            // panel superior
            var topPanel = CreatePanel("TopPanel", transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0), new Color(0, 0, 0, 0.5f));

            coinsText = CreateText("Coins", topPanel.transform, "🪙 500", 28, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(20, 0));
            donasText = CreateText("Donas", topPanel.transform, "🍩 30", 28, TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0), new Vector2(0.6f, 1), Vector2.zero);
            levelText = CreateText("Level", topPanel.transform, "Nv 1", 28, TextAnchor.MiddleRight,
                new Vector2(0.88f, 0), new Vector2(1, 1), new Vector2(0, -20));

            // barra de XP
            var xpBg = CreatePanel("XPBG", topPanel.transform, new Vector2(0.62f, 0.30f),
                new Vector2(0.86f, 0.52f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            var xpFill = CreatePanel("XPFill", xpBg.transform, Vector2.zero, Vector2.one,
                new Vector2(2, 2), new Vector2(-2, -2), new Color(0.3f, 0.8f, 0.3f, 1f));
            xpBar = xpFill.GetComponent<Image>();
            xpBar.type = Image.Type.Filled;
            xpBar.fillMethod = Image.FillMethod.Horizontal;

            // misión activa (debajo del panel superior para no taparlo)
            questText = CreateText("Quest", transform, "", 22, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0.62f, 1), new Vector2(20, -92));
            var qrt = questText.rectTransform;
            qrt.offsetMin = new Vector2(20f, -172f);
            qrt.offsetMax = new Vector2(-20f, -92f);
            questText.color = new Color(1, 1, 0.8f);

            // botón recolectar todo
            var collectBtn = CreateButton("CollectAllBtn", transform, "💰 Recolectar todo",
                new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(250, 70),
                new Color(0.2f, 0.7f, 0.3f));
            collectBtn.onClick.AddListener(() =>
            {
                var r = GameManager.Instance.CollectAll();
                if (r.ok) TutorialManager.Ensure()?.SignalCollect();
            });

            // menú construcción
            var buildBtn = CreateButton("BuildBtn", transform, "🔨 Construir",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(200, 70),
                new Color(0.2f, 0.5f, 0.9f));
            buildBtn.onClick.AddListener(() => BuildMenu.Instance.Toggle());

            // amigos
            var friendsBtn = CreateButton("FriendsBtn", transform, "🧑 Amigos",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(230, 20), new Vector2(170, 70),
                new Color(0.5f, 0.3f, 0.8f));
            friendsBtn.onClick.AddListener(() => FriendsMenu.Instance.Toggle());

            // misiones
            var questsBtn = CreateButton("QuestsBtn", transform, "📋 Misiones",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(410, 20), new Vector2(170, 70),
                new Color(0.8f, 0.6f, 0.1f));
            questsBtn.onClick.AddListener(() => QuestsMenu.Instance.Toggle());
        }

        void Refresh()
        {
            var s = GameManager.Instance.State;
            coinsText.text = $"🪙 {s.coins:N0}";
            donasText.text = $"🍩 {s.donas:N0}";
            levelText.text = $"Nv {s.level}";
            int need = Economy.XpForLevel(s.level);
            xpBar.fillAmount = need > 0 ? (float)s.xp / need : 0;
            xpText = xpText; // keep ref
            var q = Economy.ActiveQuest(s);
            questText.text = q != null ? $"📋 {q.name}\n<size=18>{q.story}</size>" : "🏆 ¡Misiones completadas!";
        }

        /* helpers UI */
        public static RectTransform CreatePanel(string name, Transform parent, Vector2 aMin, Vector2 aMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Text CreateText(string name, Transform parent, string content, int size,
            TextAnchor anchor, Vector2 aMin, Vector2 aMax, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offset; rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public static Button CreateButton(string name, Transform parent, string label,
            Vector2 aMin, Vector2 aMax, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.anchoredPosition = offset + size * 0.5f;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            var btn = go.GetComponent<Button>();
            var txt = CreateText("Label", go.transform, label, 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero);
            return btn;
        }
    }
}
