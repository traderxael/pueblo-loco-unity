using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    /// <summary>Compra de adornos. Sin esto las misiones q10-q12 nunca se completan.</summary>
    public class DecorMenu : MonoBehaviour
    {
        public static DecorMenu Instance { get; private set; }
        GameObject panel;
        bool visible;
        RectTransform listContent;
        float refreshTimer;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var panelRt = HUD.CreatePanel("DecorPanel", transform,
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 100), new Vector2(0, 480),
                new Color(0.08f, 0.08f, 0.12f, 0.96f));
            panel = panelRt.gameObject;
            panel.SetActive(false);

            HUD.CreateText("Title", panel.transform, "🌿 Decorar", 24, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(20, -58));

            var closeBtn = HUD.CreateButton("Close", panel.transform, "✕",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -9), new Vector2(38, 38),
                new Color(0.8f, 0.2f, 0.2f));
            closeBtn.onClick.AddListener(Hide);

            // grilla de tarjetas de adornos (estilo city-builder)
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(panel.transform, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = new Vector2(0.01f, 0.02f); srt.anchorMax = new Vector2(0.99f, 0.86f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            listContent = (RectTransform)contentGo.transform;
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1);
            listContent.pivot = new Vector2(0.5f, 1);
            listContent.offsetMin = Vector2.zero; listContent.offsetMax = Vector2.zero;

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(330, 84);
            grid.spacing = new Vector2(12, 12);
            grid.padding = new RectOffset(12, 12, 12, 12);

            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = listContent;
            scroll.horizontal = false;
        }

        public void Toggle()
        {
            visible = !visible;
            panel.SetActive(visible);
            if (visible) { refreshTimer = 0f; Refresh(); }
        }

        public void Hide()
        {
            visible = false;
            panel.SetActive(false);
        }

        void Update()
        {
            if (!visible) return;
            refreshTimer += Time.deltaTime;
            if (refreshTimer < 0.5f) return;
            refreshTimer = 0f;
            Refresh();
        }

        void Refresh()
        {
            if (!visible || listContent == null) return;
            foreach (Transform t in listContent) Destroy(t.gameObject);
            var s = GameManager.Instance != null ? GameManager.Instance.State : null;
            if (s == null) return;

            foreach (var def in Catalog.DECOR)
            {
                int owned = 0;
                foreach (var d in s.decor) if (d.def == def.def) owned++;

                bool locked = s.level < def.level;
                bool poor = s.coins < def.cost;
                var rowColor = locked ? new Color(0.22f, 0.22f, 0.25f)
                             : poor ? new Color(0.28f, 0.22f, 0.18f)
                             : new Color(0.12f, 0.16f, 0.14f);
                var row = CreateRow(def.def, rowColor, 80);

                string status = locked ? $"🔒 Nv {def.level}" : $"🪙 {def.cost:N0}";
                string ownedTxt = owned > 0 ? $"   x{owned}" : "";
                HUD.CreateText("Name", row, $"{def.name}{ownedTxt}   {status}",
                    17, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(15, 0));

                if (locked) continue;

                var defId = def.def;
                var btn = HUD.CreateButton("Buy", row, "Comprar",
                    new Vector2(1, 0), new Vector2(1, 1), new Vector2(-15, 0), new Vector2(124, 46),
                    poor ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.2f, 0.7f, 0.3f));
                var rt = (RectTransform)btn.transform;
                rt.anchorMin = new Vector2(1, 0.15f); rt.anchorMax = new Vector2(1, 0.85f);
                rt.anchoredPosition = new Vector2(-90, 0);
                rt.sizeDelta = new Vector2(140, 50);
                btn.onClick.AddListener(() =>
                {
                    var r = GameManager.Instance.BuyDecor(defId);
                    if (r.ok) Toast.Show("🌿 Adorno colocado");
                    else Toast.Show("❌ " + r.reason);
                });
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
    }
}
