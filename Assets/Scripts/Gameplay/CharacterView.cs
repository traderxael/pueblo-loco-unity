using UnityEngine;
using PuebloLoco.Core;

namespace PuebloLoco.Gameplay
{
    /// <summary>Click sobre un vecino: saluda y abre el menú de Amigos.</summary>
    public class CharacterView : MonoBehaviour
    {
        public string charDefId;
        VillagerAI ai;

        public void Setup(string defId, GameObject model)
        {
            charDefId = defId;
            ai = GetComponent<VillagerAI>();
        }

        void Update()
        {
            if (!Input.GetMouseButtonDown(0) || Camera.main == null) return;
            // Mientras se está colocando un edificio, el clic es del fantasma:
            // saludar además dispararía dos acciones con un solo click.
            if (GridBuildSystem.Instance != null && GridBuildSystem.Instance.IsPlacing) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 100f)) return;
            if (hit.transform != transform && !hit.transform.IsChildOf(transform)) return;

            if (ai == null) ai = GetComponent<VillagerAI>();
            if (ai != null) ai.PlayWave();
            UI.FriendsMenu.Instance?.Toggle();
        }
    }
}
