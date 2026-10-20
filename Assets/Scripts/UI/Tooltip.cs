using UnityEngine;
using UnityEngine.UI;

namespace PuebloLoco.UI
{
    /// <summary>
    /// Tooltip flotante estilo city-builder: aparece al pasar el mouse sobre las
    /// tarjetas de Construir/Decorar y sigue el cursor (canvas de 2048x1152).
    /// </summary>
    public class Tooltip : MonoBehaviour
    {
        public static Tooltip Instance { get; private set; }

        RectTransform panel;
        Text titleText, bodyText;

        void Awake()
        {
            Instance = this;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2048, 1152);

            panel = HUD.CreatePanel("TipPanel", transform, Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, new Color(0.05f, 0.05f, 0.08f, 0.97f));
            panel.sizeDelta = new Vector2(470, 132);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.55f, 0.9f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);

            titleText = HUD.CreateText("Title", panel, "", 19, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, Vector2.zero);
            titleText.color = new Color(1f, 0.88f, 0.55f);
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0, 0.55f); trt.anchorMax = new Vector2(1, 1f);
            trt.offsetMin = new Vector2(16, 2); trt.offsetMax = new Vector2(-16, -10);

            bodyText = HUD.CreateText("Body", panel, "", 15, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, Vector2.zero);
            bodyText.color = new Color(0.9f, 0.94f, 1f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            var brt = (RectTransform)bodyText.transform;
            brt.anchorMin = new Vector2(0, 0f); brt.anchorMax = new Vector2(1, 0.55f);
            brt.offsetMin = new Vector2(16, 8); brt.offsetMax = new Vector2(-16, -2);

            panel.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show(string title, string body)
        {
            if (Instance == null) return;
            Instance.titleText.text = title;
            Instance.bodyText.text = body;
            Instance.panel.gameObject.SetActive(true);
            Instance.Reposition();
        }

        public static void Hide()
        {
            if (Instance == null || Instance.panel == null) return;
            Instance.panel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (panel != null && panel.gameObject.activeSelf) Reposition();
        }

        void Reposition()
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, Input.mousePosition, null, out local);
            float w = panel.sizeDelta.x, h = panel.sizeDelta.y;
            // a la derecha-arriba del cursor, con inversion en los bordes
            float x = local.x + 22f + w * 0.5f;
            float y = local.y + 14f + h * 0.5f;
            if (x + w * 0.5f > 1010f) x = local.x - 22f - w * 0.5f;
            if (y + h * 0.5f > 566f) y = local.y - 14f - h * 0.5f;
            panel.localPosition = new Vector3(
                Mathf.Clamp(x, -1010f + w * 0.5f, 1010f - w * 0.5f),
                Mathf.Clamp(y, -570f + h * 0.5f, 570f - h * 0.5f), 0f);
        }
    }
}
