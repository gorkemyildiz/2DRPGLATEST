using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "EnhanceCatalog", menuName = "Game Data/Enhancement/Enhance Catalog")]
    public class EnhanceCatalog : ScriptableObject
    {
        public List<EnchantData> enchants = new List<EnchantData>();

        [Header("Star Costs")]
        public int[] starGoldCosts = { 30, 60, 120, 250, 500 };
        public int[] starScrapCosts = { 2, 4, 6, 10, 16 };
        public string scrapMaterialId = "mat_scrap";

        [Header("Star 3+ duplicate consume")]
        [Tooltip("Stars at or above this require consuming another copy of the same base item.")]
        public int duplicateRequiredFromStar = 3;

        public EnchantData GetEnchant(string enchantId)
        {
            if (enchants == null || string.IsNullOrEmpty(enchantId))
            {
                return null;
            }

            for (int i = 0; i < enchants.Count; i++)
            {
                if (enchants[i] != null && enchants[i].enchantId == enchantId)
                {
                    return enchants[i];
                }
            }

            return null;
        }

        public int GetStarGoldCost(int currentStars)
        {
            if (starGoldCosts == null || starGoldCosts.Length == 0)
            {
                return 50 * (currentStars + 1);
            }

            int index = Mathf.Clamp(currentStars, 0, starGoldCosts.Length - 1);
            return starGoldCosts[index];
        }

        public int GetStarScrapCost(int currentStars)
        {
            if (starScrapCosts == null || starScrapCosts.Length == 0)
            {
                return 2 * (currentStars + 1);
            }

            int index = Mathf.Clamp(currentStars, 0, starScrapCosts.Length - 1);
            return starScrapCosts[index];
        }
    }
}
