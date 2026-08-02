using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Forces inventory item icons to reimport as uncompressed 512 sprites.
    /// Old Library cache was stuck on tiny compressed textures (~35KB).
    /// </summary>
    public static class InventoryIconImportFix
    {
        private static readonly string[] IconPaths =
        {
            "Assets/_Game/Art/UI/Inventory/Icons/Icon_Sword.png",
            "Assets/_Game/Art/UI/Inventory/Icons/Icon_Ring.png",
            "Assets/_Game/Art/UI/Inventory/Icons/Icon_Crystal.png",
        };

        [InitializeOnLoadMethod]
        private static void AutoFixOnLoad()
        {
            EditorApplication.delayCall += EnsureImportSettings;
        }

        [MenuItem("Game/Inventory/Reimport Item Icons (Force HD)")]
        public static void ReimportMenu()
        {
            EnsureImportSettings();
            foreach (string path in IconPaths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.Refresh();
            Debug.Log("[InventoryIcons] Forced HD reimport of item icons.");
        }

        private static void EnsureImportSettings()
        {
            bool dirty = false;
            for (int i = 0; i < IconPaths.Length; i++)
            {
                string path = IconPaths[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.maxTextureSize != 512)
                {
                    importer.maxTextureSize = 512;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                TextureImporterPlatformSettings plat = importer.GetDefaultPlatformTextureSettings();
                if (plat.maxTextureSize != 512
                    || plat.format != TextureImporterFormat.RGBA32
                    || plat.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    plat.name = "DefaultTexturePlatform";
                    plat.overridden = true;
                    plat.maxTextureSize = 512;
                    plat.format = TextureImporterFormat.RGBA32;
                    plat.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(plat);
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    dirty = true;
                    Debug.Log($"[InventoryIcons] Reimported HD settings: {path}");
                }
            }

            if (dirty)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
