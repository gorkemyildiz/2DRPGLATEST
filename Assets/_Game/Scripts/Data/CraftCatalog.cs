using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "CraftCatalog", menuName = "Game Data/Crafting/Craft Catalog")]
    public class CraftCatalog : ScriptableObject
    {
        public List<MaterialData> materials = new List<MaterialData>();
        public List<CraftRecipeData> recipes = new List<CraftRecipeData>();

        [Header("Disassemble Yields (by rarity index)")]
        public MaterialData scrapMaterial;
        public int[] scrapByRarity = { 1, 2, 3, 5, 8 };

        [Header("Merge")]
        public int mergeInputCount = 9;
        [Range(0f, 1f)] public float chancePlusOneRarity = 0.25f;
        [Range(0f, 1f)] public float chancePlusTwoRarity = 0.05f;
        public List<ItemData> mergeResultPool = new List<ItemData>();

        [Header("Special")]
        public MaterialData summonStone;
        public MaterialData courageStone;
        public CraftRecipeData summonStoneRecipe;

        public MaterialData GetMaterial(string materialId)
        {
            if (materials == null || string.IsNullOrEmpty(materialId))
            {
                return null;
            }

            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i] != null && materials[i].materialId == materialId)
                {
                    return materials[i];
                }
            }

            return null;
        }

        public ItemData FindMergeResult(EquipmentSlot slot, ItemRarity rarity, int levelBand)
        {
            if (mergeResultPool == null)
            {
                return null;
            }

            ItemData fallback = null;
            for (int i = 0; i < mergeResultPool.Count; i++)
            {
                ItemData item = mergeResultPool[i];
                if (item == null)
                {
                    continue;
                }

                if (item.equipmentSlot == slot && item.levelBand == levelBand)
                {
                    if (item.rarity == rarity)
                    {
                        return item;
                    }

                    if (fallback == null && (int)item.rarity <= (int)rarity)
                    {
                        fallback = item;
                    }
                }
            }

            return fallback;
        }
    }
}
