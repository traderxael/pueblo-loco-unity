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

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0.45f, 1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            panel.SetActive(false);

            HUD.CreateText("Title", panel.transform, "🔨 Construir", 32, TextAnchor.MiddleCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70));

            var closeBtn = HUD.CreateButton("Close", panel.transform, "✕",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -10), new Vector2(50, 50),
                new Color(0.8f, 0.2f, 0.2f));
            closeBtn.onClick.AddListener(Hide);

            // lista de edificios del catálogo
            float y = -90;
            foreach (var def in Catalog.BUILDINGS)
            {
                var s = GameManager.Instance.State;
                bool locked = s.level < def.level;
                var color = locked ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.15f, 0.15f, 0.2f);
                var row = HUD.CreatePanel(def.def, panel.transform, new Vector2(0.05f, 1), new Vector2(0.95f, 1),
                    new Vector2(0, y - 70), new Vector2(0, y), color);
                y -= 80;

                string status = locked ? $"🔒 Nv {def.level}" : $"🪙 {def.cost:N0}";
                HUD.CreateText("Name", row, $"{def.name}  ({def.size}x{def.size})  {status}",
                    20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(15, 0));

                if (!locked)
                {
                    var defId = def.def;
                    var btn = row.gameObject.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        GridBuildSystem.Instance.BeginPlacing(defId);
                        Hide();
                    });
                }
            }
        }

        public void Toggle()
        {
            visible = !visible;
            panel.SetActive(visible);
            if (visible) TutorialManager.Ensure()?.SignalShop();
        }

        public void Hide() { visible = false; panel.SetActive(false); }
    }
}
