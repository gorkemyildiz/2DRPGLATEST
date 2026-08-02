using Game.Save;

namespace Game.Data
{
    public static class ItemEnhanceUtil
    {
        public static int GetBaseEnchantSlots(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return 1;
                case ItemRarity.Epic: return 2;
                case ItemRarity.Legendary: return 3;
                default: return 0;
            }
        }

        public static int GetEnchantSlotCount(ItemData item, ItemInstanceSave instance)
        {
            if (item == null)
            {
                return 0;
            }

            int slots = GetBaseEnchantSlots(item.rarity);
            if (instance != null && instance.stars >= 5)
            {
                slots += 1;
            }

            return slots;
        }

        public static int GetAttackBonus(ItemData item, ItemInstanceSave instance, EnhanceCatalog catalog)
        {
            if (item == null)
            {
                return 0;
            }

            int stars = instance != null ? System.Math.Max(0, instance.stars) : 0;
            int starBonus = stars * System.Math.Max(1, item.attackBonus / 5 + (item.attackBonus > 0 ? 1 : 0));
            int total = item.attackBonus + starBonus;

            if (instance != null && instance.enchantIds != null && catalog != null)
            {
                for (int i = 0; i < instance.enchantIds.Count; i++)
                {
                    EnchantData enchant = catalog.GetEnchant(instance.enchantIds[i]);
                    if (enchant != null)
                    {
                        total += enchant.attackBonus;
                    }
                }
            }

            return total;
        }

        public static int GetHealthBonus(ItemData item, ItemInstanceSave instance, EnhanceCatalog catalog)
        {
            if (item == null)
            {
                return 0;
            }

            int stars = instance != null ? System.Math.Max(0, instance.stars) : 0;
            int starBonus = stars * System.Math.Max(1, item.healthBonus / 5 + (item.healthBonus > 0 ? 1 : 0));
            int total = item.healthBonus + starBonus;

            if (instance != null && instance.enchantIds != null && catalog != null)
            {
                for (int i = 0; i < instance.enchantIds.Count; i++)
                {
                    EnchantData enchant = catalog.GetEnchant(instance.enchantIds[i]);
                    if (enchant != null)
                    {
                        total += enchant.healthBonus;
                    }
                }
            }

            return total;
        }

        public static string FormatStars(int stars)
        {
            stars = System.Math.Max(0, System.Math.Min(5, stars));
            if (stars <= 0)
            {
                return string.Empty;
            }

            return new string('★', stars);
        }
    }
}
