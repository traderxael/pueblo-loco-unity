using System;
using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        GameObject card, arrow;
        Text cardText, skipBtn;
        RectTransform arrowRect;
        float checkTimer;
        bool flagShop, flagCollect, flagTask, flagQuests;

        public static TutorialManager Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("TutorialManager");
                go.AddComponent<TutorialManager>();
            }
            return Instance;
        }

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            // tarjeta del tutorial (parte inferior)
            var cardRt = HUD.CreatePanel("TutCard", transform,
                new Vector2(0.1f, 0), new Vector2(0.9f, 0),
                new Vector2(0, 20), new Vector2(0, 140), new Color(0.05f, 0.05f, 0.1f, 0.92f));
            card = cardRt.gameObject;
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.7f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            cardText = HUD.CreateText("TutText", card.transform, "", 26, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.25f), new Vector2(0.98f, 1), new Vector2(0, 0));
            cardText.supportRichText = true;

            var skip = HUD.CreateButton("Skip", card.transform, "Saltar tutorial ➜",
                new Vector2(0.75f, 0), new Vector2(1, 0.28f), Vector2.zero, new Vector2(0, 0),
                new Color(0.25f, 0.25f, 0.3f, 0.9f));
            var skipLabel = skip.GetComponentInChildren<Text>();
            skipLabel.fontSize = 16;
            skipLabel.color = new Color(0.8f, 0.8f, 0.85f);
            skip.onClick.AddListener(() => EndTutorial(true));

            // flecha animada
            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowGo.transform.SetParent(transform, false);
            arrowRect = (RectTransform)arrowGo.transform;
            arrowRect.sizeDelta = new Vector2(48, 48);
            var img = arrowGo.GetComponent<Image>();
            img.color = new Color(1f, 0.9f, 0.2f);
            // forma de flecha con triángulo
            var text = HUD.CreateText("ArrowGlyph", arrowGo.transform, "▼", 42, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero);
            text.color = new Color(1f, 0.9f, 0.2f);
            Destroy(img);
            arrow = arrowGo;
        }

        void Update()
        {
            var s = GameManager.Instance.State;
            if (s == null || s.tutorial < 0) { HideAll(); return; }

            checkTimer += Time.deltaTime;
            if (checkTimer < 0.25f) return;
            checkTimer = 0;

            var step = GetStep(s.tutorial);
            if (step == null) { EndTutorial(false); return; }

            // ¿se cumplió el paso actual?
            if (StepDone(s))
            {
                s.tutorial++;
                flagShop = flagCollect = flagTask = flagQuests = false;
                step = GetStep(s.tutorial);
                if (step == null) { EndTutorial(false); return; }
                GameManager.Instance.Save();
            }

            card.SetActive(true);
            cardText.text = step.text;
            PositionArrow(step.target);
        }

        TutorialStep GetStep(int i) => i switch
        {
            0 => new TutorialStep("🏗️ Toca <b>Construir</b> y elige la <b>Casa</b>", "BuildBtn"),
            1 => new TutorialStep("📦 Coloca la <b>Casa</b> en una zona <color=#4caf50>verde</color> del terreno", null),
            2 => new TutorialStep("💰 Toca <b>Recolectar</b> o la burbuja de monedas para cobrar", "CollectAllBtn"),
            3 => new TutorialStep("🧑 Toca a <b>Luis</b> y dale una tarea para ganar monedas", null),
            4 => new TutorialStep("📋 ¡Sigue la <b>misión</b> en la esquina superior izquierda!", null),
            _ => null
        };

        bool StepDone(GameState s)
        {
            switch (s.tutorial)
            {
                case 0: return flagShop;
                case 1: return s.buildings.Count >= 2;
                case 2: return flagCollect;
                case 3: return flagTask;
                case 4: return flagQuests || s.questIndex > 0;
                default: return true;
            }
        }

        void PositionArrow(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) { arrow.SetActive(false); return; }
            var target = FindTarget(targetName);
            if (target == null) { arrow.SetActive(false); return; }
            var rt = (RectTransform)target.transform;
            Vector2 screenPos = rt.TransformPoint(rt.rect.center);
            arrowRect.position = screenPos + new Vector2(0, -50);
            arrow.SetActive(true);
            // rebote
            arrowRect.anchoredPosition = new Vector2(0, Mathf.Sin(Time.time * 4) * 8);
        }

        GameObject FindTarget(string name)
        {
            // buscar en toda la jerarquía de UI
            var all = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rt in all)
                if (rt.name == name && rt.gameObject.activeInHierarchy)
                    return rt.gameObject;
            return null;
        }

        void HideAll()
        {
            if (card != null) card.SetActive(false);
            if (arrow != null) arrow.SetActive(false);
        }

        public void EndTutorial(bool skipped)
        {
            var s = GameManager.Instance.State;
            if (s != null) s.tutorial = -1;
            HideAll();
            if (!skipped)
                Toast.Show("🎓 ¡Tutorial completo! Ya eres alcalde de Pueblo Loco");
            GameManager.Instance?.Save();
        }

        /* señales desde otros sistemas */
        public void SignalShop() => flagShop = true;
        public void SignalCollect() => flagCollect = true;
        public void SignalTask() => flagTask = true;
        public void SignalQuests() => flagQuests = true;

        class TutorialStep
        {
            public string text, target;
            public TutorialStep(string text, string target) { this.text = text; this.target = target; }
        }
    }
}
