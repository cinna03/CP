using System.IO;
using Capstone.App.Data;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.EditorTools
{
    /// <summary>
    /// Sets the catalogue pictures to 1024 and ASTC so the iPhone list stays light.
    /// </summary>
    public static class CatalogueTextureOptimizer
    {
        const string ShelfPath = "Assets/Resources/CatalogueShelf.asset";

        [MenuItem("Tools/Capstone/Optimize Catalogue Textures for iPhone")]
        public static void Optimize()
        {
            CatalogueShelf shelf = AssetDatabase.LoadAssetAtPath<CatalogueShelf>(ShelfPath);
            if (shelf == null || shelf.Works == null)
            {
                Debug.LogWarning("Catalogue shelf was not found, so no pictures were changed.");
                return;
            }

            int changed = 0;
            for (int i = 0; i < shelf.Works.Count; i++)
            {
                ArtworkData work = shelf.Works[i];
                if (work == null || work.ArtworkSprite == null || work.ArtworkSprite.texture == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(work.ArtworkSprite.texture);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                TextureImporterPlatformSettings phone = importer.GetPlatformTextureSettings("iPhone");
                phone.overridden = true;
                phone.maxTextureSize = 1024;
                phone.format = TextureImporterFormat.ASTC_6x6;
                phone.compressionQuality = 50;
                importer.SetPlatformTextureSettings(phone);
                importer.SaveAndReimport();
                changed++;
            }

            Debug.Log("Optimized " + changed + " catalogue pictures for iPhone at 1024 ASTC.");
        }
    }
}
