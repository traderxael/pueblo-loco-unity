using UnityEngine;
using UnityEngine.UI;
using PuebloLoco.Core;
using PuebloLoco.Gameplay;

namespace PuebloLoco.UI
{
    public class HUD : MonoBehaviour
    {
        Text coinsText, donasText, levelText, questText, expandLabel;
        Image xpBar;
        Canvas canvas;

        void Start()
        {
            Build();
            GameManager.Instance.OnStateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2048, 1152);
            gameObject.AddComponent<GraphicRaycaster>();

            // panel superior
            var topPanel = CreatePanel("TopPanel", transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0), new Color(0, 0, 0, 0.5f));

            coinsText = CreateText("Coins", topPanel.transform, "🪙 500", 28, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(20, 0));
            donasText = CreateText("Donas", topPanel.transform, "🍩 30", 28, TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0), new Vector2(0.6f, 1), Vector2.zero);
            levelText = CreateText("Level", topPanel.transform, "Nv 1", 28, TextAnchor.MiddleRight,
                new Vector2(0.88f, 0), new Vector2(1, 1), Vector2.zero);

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

            // ── barra de acciones inferior, centrada (estilo city-builder) ──
            var actionBar = CreatePanel("ActionBar", transform,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-560, 10), new Vector2(560, 94), new Color(0, 0, 0, 0.55f));

            var btnDefs = new (string name, string label, Color c, float w)[]
            {
                ("CollectAllBtn", "💰 Recolectar", new Color(0.2f, 0.7f, 0.3f), 180f),
                ("BuildBtn",      "🔨 Construir",  new Color(0.2f, 0.5f, 0.9f), 160f),
                ("DecorBtn",      "🌿 Decorar",    new Color(0.2f, 0.6f, 0.35f), 150f),
                ("FriendsBtn",    "🧑 Amigos",     new Color(0.5f, 0.3f, 0.8f), 140f),
                ("QuestsBtn",     "📋 Misiones",   new Color(0.8f, 0.6f, 0.1f), 150f),
                ("ExpandBtn",     "⛏️ Ampliar",    new Color(0.45f, 0.4f, 0.2f), 220f),
            };
            float bx = 12f;
            foreach (var d in btnDefs)
            {
                var b = CreateButton(d.name, actionBar.transform, d.label,
                    new Vector2(0, 0), new Vector2(0, 0), new Vector2(bx, 15), new Vector2(d.w, 64), d.c);
                bx += d.w + 14f;
                switch (d.name)
                {
                    case "CollectAllBtn":
                        b.onClick.AddListener(() =>
                        {
                            var r = GameManager.Instance.CollectAll();
                            if (r.ok) TutorialManager.Ensure()?.SignalCollect();
                        });
                        break;
                    case "BuildBtn":
                        b.onClick.AddListener(() => BuildMenu.Instance?.Toggle());
                        break;
                    case "DecorBtn":
                        b.onClick.AddListener(() => DecorMenu.Instance?.Toggle());
                        break;
                    case "FriendsBtn":
                        b.onClick.AddListener(() => FriendsMenu.Instance?.Toggle());
                        break;
                    case "QuestsBtn":
                        b.onClick.AddListener(() => QuestsMenu.Instance?.Toggle());
                        break;
                    case "ExpandBtn":
                        expandLabel = b.GetComponentInChildren<Text>();
                        b.onClick.AddListener(() =>
                        {
                            if (GameManager.Instance == null) return;
                            var r = GameManager.Instance.ExpandLand();
                            if (r.ok) Toast.Show("⛏️ ¡Terreno ampliado!");
                            else Toast.Show("❌ " + r.reason);
                        });
                        break;
                }
            }
        }

        void UpdateExpandLabel(GameState s)
        {
            if (expandLabel == null) return;
            int next = s.landTier + 1;
            if (next >= Catalog.LAND_TIERS.Length)
            {
                expandLabel.text = "⛏️ Terreno máximo";
                return;
            }
            var tier = Catalog.LAND_TIERS[next];
            expandLabel.text = $"⛏️ {tier.size}×{tier.size} · {Compact(tier.cost)} 🪙";
        }

        /// <summary>Formato compacto de monedas tipo city-builder: 1.8M, 25k.</summary>
        static string Compact(long c)
        {
            if (c >= 1_000_000) return (c / 1_000_000.0).ToString("0.#") + "M";
            if (c >= 1_000) return (c / 1_000.0).ToString("0.#") + "k";
            return c.ToString();
        }

        void Refresh()
        {
            if (GameManager.Instance == null) return;
            var s = GameManager.Instance.State;
            if (s == null) return;
            coinsText.text = $"🪙 {s.coins:N0}";
            donasText.text = $"🍩 {s.donas:N0}";
            levelText.text = $"Nv {s.level}";
            int need = Economy.XpForLevel(s.level);
            xpBar.fillAmount = need > 0 ? (float)s.xp / need : 0;
            UpdateExpandLabel(s);
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
