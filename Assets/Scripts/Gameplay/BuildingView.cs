using UnityEngine;
using PuebloLoco.Core;

namespace PuebloLoco.Gameplay
{
    public class BuildingView : MonoBehaviour
    {
        public string buildingId;
        public BuildingDef def;

        GameObject bubble;
        TextMesh label;
        float checkTimer;

        void Start()
        {
            // aparición con rebote
            StartCoroutine(PopIn());

            // burbuja de monedas
            var bubbleGo = new GameObject("Bubble");
            bubbleGo.transform.SetParent(transform);
            bubbleGo.transform.localPosition = Vector3.up * (def.size * 1.4f);
            bubble = bubbleGo;

            label = bubbleGo.AddComponent<TextMesh>();
            label.text = "";
            // Sin fuente el TextMesh no dibuja nada: el número nunca aparecía.
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.yellow;
        }

        System.Collections.IEnumerator PopIn()
        {
            Vector3 final = transform.localScale;
            float t = 0f;
            transform.localScale = final * 0.05f;
            while (t < 0.42f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.42f);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.12f;
                transform.localScale = final * e * overshoot;
                yield return null;
            }
            transform.localScale = final;
        }

        void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer < 0.5f) return;
            checkTimer = 0;

            if (GameManager.Instance == null || label == null) return;
            var s = GameManager.Instance.State;
            var b = s.buildings.Find(x => x.id == buildingId);
            if (b == null) { label.text = ""; return; }

            Economy.AccrueBuilding(b, GameManager.Instance.NowMs);
            if (b.ready > 0)
            {
                label.text = b.ready.ToString();
                // flotar
                bubble.transform.localPosition = Vector3.up * (def.size * 1.4f + Mathf.Sin(Time.time * 2) * 0.05f);
                // click para cobrar
                if (Input.GetMouseButtonDown(0) && Camera.main != null &&
                    (GridBuildSystem.Instance == null || !GridBuildSystem.Instance.IsPlacing))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out var hit, 100f))
                    {
                        bool mine = hit.transform == transform || hit.transform.IsChildOf(transform);
                        if (mine) GameManager.Instance.CollectBuilding(buildingId);
                    }
                }
            }
            else
            {
                label.text = "";
            }
        }
    }
}
