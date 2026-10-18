using UnityEditor;
using UnityEngine;

namespace PuebloLoco.EditorTools
{
    /// <summary>
    /// Crea Assets/Resources/URPLit.mat.
    ///
    /// Existe por una razón concreta: el juego arma los materiales en runtime con
    /// Shader.Find(...). Si ningún asset referencia el shader URP/Lit, Unity lo
    /// elimina del build (shader stripping) y todo se dibuja magenta en el
    /// ejecutable, aunque se vea bien en el editor. Tener un .mat en Resources
    /// garantiza que quede incluido.
    /// </summary>
    public static class UrpLitMaterial
    {
        public const string ResourcePath = "Assets/Resources/URPLit.mat";
        public const string ShaderName = "Universal Render Pipeline/Lit";
        public const string FallbackShaderName = "Standard";

        /// <summary>Shader del pipeline activo, con fallback al Standard de Built-in.</summary>
        public static Shader LitShader()
        {
            var s = Shader.Find(ShaderName);
            if (s != null) return s;
            s = Shader.Find(FallbackShaderName);
            if (s == null)
                Debug.LogError("[URP] no se encontro ni URP/Lit ni Standard; el material quedara invalido");
            return s;
        }

        [MenuItem("Pueblo Loco/URP/1. Crear material URP de referencia")]
        public static void Create()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[URP] el paquete Universal RP todavia no esta resuelto. Abrilo una vez en Unity y reintenta.");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<Material>(ResourcePath);
            if (existing != null && existing.shader == shader) return;

            Directory_EnsureFolder(System.IO.Path.GetDirectoryName(ResourcePath));
            var mat = new Material(shader) { name = "URPLit" };
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0f);

            if (existing != null) AssetDatabase.DeleteAsset(ResourcePath);
            AssetDatabase.CreateAsset(mat, ResourcePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] creado " + ResourcePath);
        }

        static void Directory_EnsureFolder(string absPath)
        {
            if (string.IsNullOrEmpty(absPath)) return;
            if (AssetDatabase.IsValidFolder(absPath)) return;
            string parent = System.IO.Path.GetDirectoryName(absPath).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(absPath);
            Directory_EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
