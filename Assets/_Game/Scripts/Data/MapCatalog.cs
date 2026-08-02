using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "MapCatalog", menuName = "Game Data/Map/Map Catalog")]
    public class MapCatalog : ScriptableObject
    {
        [SerializeField] private List<LayerData> layers = new List<LayerData>();

        [Header("Chest Tier Defaults")]
        public ChestData normalChest;
        public ChestData levelBossChest;
        public ChestData chapterBossChest;
        public ChestData layerBossChest;
        public ChestData dungeonBossChest;

        public IReadOnlyList<LayerData> Layers => layers;

        public void SetLayers(List<LayerData> newLayers)
        {
            layers = newLayers ?? new List<LayerData>();
        }

        public LayerData GetLayer(int layerIndex)
        {
            if (layers == null)
            {
                return null;
            }

            for (int i = 0; i < layers.Count; i++)
            {
                LayerData layer = layers[i];
                if (layer != null && layer.layerIndex == layerIndex)
                {
                    return layer;
                }
            }

            return null;
        }

        public LevelData GetLevel(int layerIndex, int chapterIndex, int levelIndex)
        {
            LayerData layer = GetLayer(layerIndex);
            if (layer == null)
            {
                return null;
            }

            ChapterData chapter = layer.GetChapter(chapterIndex);
            if (chapter == null)
            {
                return null;
            }

            return chapter.GetLevel(levelIndex);
        }

        public ChestData GetChestForTier(ChestTier tier)
        {
            switch (tier)
            {
                case ChestTier.LevelBoss:
                    return levelBossChest != null ? levelBossChest : normalChest;
                case ChestTier.ChapterBoss:
                    return chapterBossChest != null ? chapterBossChest : levelBossChest;
                case ChestTier.LayerBoss:
                    return layerBossChest != null ? layerBossChest : chapterBossChest;
                case ChestTier.DungeonBoss:
                    return dungeonBossChest != null ? dungeonBossChest : layerBossChest;
                default:
                    return normalChest;
            }
        }
    }
}
