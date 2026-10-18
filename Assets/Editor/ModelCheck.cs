using UnityEditor;
using UnityEngine;

namespace PuebloLoco.EditorTools
{
    /// <summary>Diagnóstico: mide los modelos legacy como los ve Unity.</summary>
    public static class ModelCheck
    {
        public static void LogAll()
        {
            string[] names = { "GrassTile", "PathTile", "Tree", "Casa", "Kiosco", "Panaderia", "Luis" };
            foreach (var n in names)
            {
                var prefab = Resources.Load<GameObject>(n);
                if (prefab == null)
                {
                    Debug.Log("[MODEL] " + n + " FALTA");
                    continue;
                }
                var rs = prefab.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0)
                {
                    Debug.Log("[MODEL] " + n + " sin renderers");
                    continue;
                }
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                Debug.Log($"[MODEL] {n} size={b.size.ToString("F2")} rot={prefab.transform.rotation.eulerAngles.ToString("F1")} hijos={prefab.transform.childCount}");
            }
            EditorApplication.Exit(0);
        }
    }
}
