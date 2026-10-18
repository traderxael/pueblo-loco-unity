using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PuebloLoco.UI
{
    public class Toast : MonoBehaviour
    {
        static Toast instance;
        Text label;
        CanvasGroup group;
        Coroutine current;

        public static void Show(string message, float duration = 3f)
        {
            if (instance == null)
            {
                var go = new GameObject("Toast");
                go.AddComponent<Toast>();
            }
            instance.ShowInternal(message, duration);
        }

        void Awake()
        {
            instance = this;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;

            var panel = HUD.CreatePanel("ToastPanel", transform,
                new Vector2(0.2f, 1), new Vector2(0.8f, 1),
                new Vector2(0, -220), new Vector2(0, -140), new Color(0.05f, 0.05f, 0.08f, 0.9f));
            label = HUD.CreateText("ToastLabel", panel, "", 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(10, 0));
            label.supportRichText = true;
        }

        void ShowInternal(string msg, float duration)
        {
            label.text = msg;
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(FadeRoutine(duration));
        }

        IEnumerator FadeRoutine(float duration)
        {
            // fade in
            float t = 0;
            while (t < 0.25f) { t += Time.deltaTime; group.alpha = t / 0.25f; yield return null; }
            group.alpha = 1;
            // espera
            yield return new WaitForSeconds(duration);
            // fade out
            t = 0;
            while (t < 0.4f) { t += Time.deltaTime; group.alpha = 1 - t / 0.4f; yield return null; }
            group.alpha = 0;
        }
    }
}
