using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Game Data/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "item_001";
        public string displayName = "Item";

        [Header("Visuals")]
        public Sprite icon;

        [Header("Properties")]
        public ItemRarity rarity = ItemRarity.Common;
        public EquipmentSlot equipmentSlot = EquipmentSlot.Weapon;

        [Header("Level Band (merge group)")]
        [Tooltip("Items must share this band to merge (9→1).")]
        public int levelBand = 1;

        [Header("Bonuses")]
        public int attackBonus = 0;
        public int healthBonus = 0;
    }
}
