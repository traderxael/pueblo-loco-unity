using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        // icono por edificio para las tarjetas (estilo city-builder)
        static readonly System.Collections.Generic.Dictionary<string, string> Icons =
            new System.Collections.Generic.Dictionary<string, string>
        {
            {"casa","🏠"},{"kiosco","📰"},{"floreria","🌷"},{"panaderia","🥖"},{"taller","🔧"},
            {"casahan","🏠"},{"cafeteria","☕"},{"cine","🎬"},{"pagoda","🛕"},{"mercado","🏮"},
            {"muralla","🧱"},{"templo","⛩️"},{"granja","🌾"},{"herreria","⚒️"},{"posada","🏨"},
            {"campanario","🔔"},{"escuela","🏫"},{"hospital","🏥"},{"comisaria","🚓"},
            {"estadio","🏟️"},{"torre","🗼"},
        };

        public void Toggle()
        {
            visible = !visible;
            if (visible)
            {
                panel.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(SlideIn(100f, 560f));
                TutorialManager.Ensure()?.SignalShop();
                refreshTimer = 0f;
                Refresh();
            }
            else
            {
                Tooltip.Hide();
                panel.SetActive(false);
            }
        }

        /// <summary>La gaveta sube desde abajo con easing (0.22 s).</summary>
        IEnumerator SlideIn(float bottom, float top)
        {
            var rt = (RectTransform)panel.transform;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.22f;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // easeOutCubic
                rt.offsetMin = new Vector2(0f, Mathf.Lerp(-500f, bottom, e));
                rt.offsetMax = new Vector2(0f, Mathf.Lerp(-100f, top, e));
                yield return null;
            }
            rt.offsetMin = new Vector2(0f, bottom);
            rt.offsetMax = new Vector2(0f, top);
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
                bool poor = !locked && s.coins < def.cost;
                var baseColor = locked ? new Color(0.3f, 0.3f, 0.3f)
                              : poor ? new Color(0.28f, 0.22f, 0.18f)
                              : new Color(0.15f, 0.15f, 0.2f);
                var row = HUD.CreatePanel(def.def, listContent, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, baseColor);

                string icon = Icons.TryGetValue(def.def, out var ic) ? ic : "🏛️";
                string status = locked ? $"<color=#ffab40>🔒 Nv {def.level}</color>"
                              : poor ? $"<color=#ff5252>🪙 {def.cost:N0}</color>"
                              : $"<color=#ffd54f>🪙 {def.cost:N0}</color>";
                HUD.CreateText("Name", row,
                    $"<size=32>{icon}</size>  <size=16>{def.name} <color=#90a4ae>({def.size}x{def.size})</color>  {status}</size>",
                    16, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(12, 0));

                // tooltip + realce al pasar el mouse (estilo city-builder)
                string tipTitle = $"{icon} {def.name}";
                string tipBody = locked
                    ? $"🔒 Se desbloquea en el nivel {def.level}."
                    : $"Produce {def.coins:N0} 🪙 cada {def.interval}s · +{def.xp} XP\n" +
                      $"Costo: {def.cost:N0} 🪙 · Nivel {def.level} · Tamaño {def.size}x{def.size}" +
                      (poor ? $"\n<color=#ff5252>Te faltan {def.cost - s.coins:N0} 🪙</color>" : "");
                var img = row.GetComponent<Image>();
                var hi = baseColor * 1.35f; hi.a = baseColor.a;
                var trig = row.gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => { img.color = hi; Tooltip.Show(tipTitle, tipBody); });
                trig.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => { img.color = baseColor; Tooltip.Hide(); });
                trig.triggers.Add(exit);

                if (locked) continue;

                var defId = def.def;
                var btn = row.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (GridBuildSystem.Instance != null) GridBuildSystem.Instance.BeginPlacing(defId);
                    Tooltip.Hide();
                    Hide();
                });
            }
        }
    }
}
