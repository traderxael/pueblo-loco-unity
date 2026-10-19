using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    public class BuildMenu : MonoBehaviour
    {
        public static BuildMenu Instance { get; private set; }
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

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rt = (RectTransform)panel.transform;
            // gaveta inferior estilo city-builder: ancho completo, sobre la barra
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(0, 100); rt.offsetMax = new Vector2(0, 560);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.96f);
            panel.SetActive(false);

            HUD.CreateText("Title", panel.transform, "🔨 Construir", 24, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(20, -58));

            var closeBtn = HUD.CreateButton("Close", panel.transform, "✕",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -9), new Vector2(38, 38),
                new Color(0.8f, 0.2f, 0.2f));
            closeBtn.onClick.AddListener(Hide);

            // grilla de tarjetas de edificios (estilo city-builder)
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(panel.transform, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = new Vector2(0.01f, 0.02f); srt.anchorMax = new Vector2(0.99f, 0.88f);
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
            if (visible)
            {
                TutorialManager.Ensure()?.SignalShop();
                refreshTimer = 0f;
                Refresh();
            }
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

        /// <summary>El nivel se relee en cada refresco: si no, un edificio bloqueado
        /// queda gris para siempre aunque el jugador suba de nivel.</summary>
        void Refresh()
        {
            if (!visible || listContent == null) return;
            foreach (Transform t in listContent) Destroy(t.gameObject);
            if (GameManager.Instance == null) return;
            var s = GameManager.Instance.State;
            if (s == null) return;

            foreach (var def in Catalog.BUILDINGS)
            {
                bool locked = s.level < def.level;
                var color = locked ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.15f, 0.15f, 0.2f);
                var row = HUD.CreatePanel(def.def, listContent, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, color);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;

                string status = locked ? $"🔒 Nv {def.level}" : $"🪙 {def.cost:N0}";
                HUD.CreateText("Name", row, $"{def.name}  ({def.size}x{def.size})  {status}",
                    16, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(15, 0));

                if (locked) continue;

                var defId = def.def;
                var btn = row.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (GridBuildSystem.Instance != null) GridBuildSystem.Instance.BeginPlacing(defId);
                    Hide();
                });
            }
        }
    }
}
