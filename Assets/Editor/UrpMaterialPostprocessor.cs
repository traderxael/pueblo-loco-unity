using UnityEditor;
using UnityEngine;

namespace PuebloLoco.EditorTools
{
    /// <summary>
    /// Convierte a URP/Lit los materiales que vienen embebidos dentro de los FBX
    /// (materialImportMode = Import via MaterialDescription).
    ///
    /// Importante: NO se puede cambiar el materialImportMode a "None" para apuntar
    /// a un material por defecto, porque se perderian los nombres de material
    /// (Lacquer, Stone, Gold, RoofTiles...) y con ellos toda la logica de tintado
    /// de GridBuildSystem.TintBuilding / TexFor. Aqui se conservan los nombres y
    /// solo se cambia el shader.
    /// </summary>
    public class UrpMaterialPostprocessor : AssetPostprocessor
    {
        const string BuiltInStandard = "Standard";
        const string LitShader = "Universal Render Pipeline/Lit";

        void OnPostprocessModel(GameObject root)
        {
            var lit = Shader.Find(LitShader);
            if (lit == null) return; // URP todavia no resuelto: no romper la importacion

            int changed = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (m.shader == null || m.shader.name != BuiltInStandard) continue;
                    mats[i] = Convert(m, lit);
                    dirty = true;
                    changed++;
                }
                if (dirty) r.sharedMaterials = mats;
            }

            if (changed > 0)
                Debug.Log($"[URP] {assetPath}: {changed} material(es) convertidos a URP/Lit");
        }

        /// <summary>Copia las propiedades del Standard a URP/Lit, respetando el nombre.</summary>
        static Material Convert(Material src, Shader lit)
        {
            var dst = new Material(lit)
            {
                name = src.name,
                mainTexture = src.mainTexture,
                mainTextureOffset = src.mainTextureOffset,
                mainTextureScale = src.mainTextureScale,
                color = src.color,
                globalIlluminationFlags = src.globalIlluminationFlags
            };

            CopyIfPresent(src, dst, "_Metallic");
            // El Standard usa _Glossiness; URP/Lit usa _Smoothness.
            CopyIfPresent(src, dst, "_Glossiness", "_Smoothness");
            CopyIfPresent(src, dst, "_BumpMap");
            CopyIfPresent(src, dst, "_BumpScale");
            CopyIfPresent(src, dst, "_EmissionColor");
            CopyIfPresent(src, dst, "_OcclusionMap");
            CopyIfPresent(src, dst, "_DetailMask");

            if (src.IsKeywordEnabled("_NORMALMAP") && dst.HasProperty("_BumpMap"))
                dst.EnableKeyword("_NORMALMAP");
            if (src.IsKeywordEnabled("_EMISSION") && dst.HasProperty("_EmissionColor"))
            {
                dst.EnableKeyword("_EMISSION");
                dst.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            return dst;
        }

        static void CopyIfPresent(Material src, Material dst, string name)
        {
            if (src.HasProperty(name) && dst.HasProperty(name)) dst.SetFloat(name, src.GetFloat(name));
        }

        static void CopyIfPresent(Material src, Material dst, string srcName, string dstName)
        {
            if (src.HasProperty(srcName) && dst.HasProperty(dstName)) dst.SetFloat(dstName, src.GetFloat(srcName));
        }
    }
}
