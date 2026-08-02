using UnityEngine;

namespace Game.Data
{
    public enum EnchantCategory
    {
        Attack = 0,
        Defense = 1,
        Utility = 2
    }

    [CreateAssetMenu(fileName = "NewEnchant", menuName = "Game Data/Enhancement/Enchant")]
    public class EnchantData : ScriptableObject
    {
        [Header("Identity")]
        public string enchantId = "enchant_atk_1";
        public string displayName = "Sharp";
        public EnchantCategory category = EnchantCategory.Attack;

        [Header("Requirements")]
        [Tooltip("Enchant can only be applied to items of this rarity or higher.")]
        public ItemRarity requiredRarity = ItemRarity.Rare;

        [Header("Bonuses")]
        public int attackBonus;
        public int healthBonus;

        [Header("Clear Cost")]
        public int clearGoldCost = 50;
    }
}
