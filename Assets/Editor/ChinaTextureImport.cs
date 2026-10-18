using UnityEditor;
using UnityEngine;

namespace PuebloLoco.EditorTools
{
    /// <summary>
    /// Importa las texturas CC0 de Poly Haven con el tipo correcto:
    /// *_nor -> NormalMap, *_rough -> textura de rugosidad (se usa como máscara).
    /// </summary>
    public class ChinaTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Resources/Textures/China")) return;

            var importer = (TextureImporter)assetImporter;
            if (assetPath.EndsWith("_nor.jpg"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 1024;
            }
            else if (assetPath.EndsWith("_rough.jpg"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.maxTextureSize = 512;
            }
            else if (assetPath.EndsWith("_diff.jpg"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Repeat;
            }
        }
    }
}
