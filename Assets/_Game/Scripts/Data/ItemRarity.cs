namespace Game.Data
{
    /// <summary>
    /// 5 item tiers: Sıradan, Yaygın, Nadir, Efsanevi, Ölümsüz.
    /// </summary>
    public enum ItemRarity
    {
        Common = 0,      // Sıradan
        Uncommon = 1,    // Yaygın
        Rare = 2,        // Nadir
        Epic = 3,        // Efsanevi
        Legendary = 4    // Ölümsüz
    }

    public static class ItemRarityUtil
    {
        public static string GetDisplayName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "Sıradan";
                case ItemRarity.Uncommon: return "Yaygın";
                case ItemRarity.Rare: return "Nadir";
                case ItemRarity.Epic: return "Efsanevi";
                case ItemRarity.Legendary: return "Ölümsüz";
                default: return rarity.ToString();
            }
        }

        public static ItemRarity Clamp(ItemRarity rarity)
        {
            int v = (int)rarity;
            if (v < 0) return ItemRarity.Common;
            if (v > (int)ItemRarity.Legendary) return ItemRarity.Legendary;
            return rarity;
        }

        public static ItemRarity Raise(ItemRarity rarity, int steps)
        {
            return Clamp((ItemRarity)((int)rarity + steps));
        }
    }
}
