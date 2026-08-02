using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "Game Data/Crafting/Material")]
    public class MaterialData : ScriptableObject
    {
        [Header("Identity")]
        public string materialId = "mat_scrap";
        public string displayName = "Scrap";
        public Sprite icon;

        [Header("Flags")]
        public bool isSummonStone;
        public bool isCourageStone;
        public bool isMergeBooster;

        [Header("Merge Booster")]
        [Tooltip("If booster, which rarity tier it boosts.")]
        public ItemRarity boostsRarity = ItemRarity.Common;
        [Range(0f, 1f)] public float successBonus = 0.1f;
    }
}
