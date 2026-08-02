using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewChest", menuName = "Game Data/Chest")]
    public class ChestData : ScriptableObject
    {
        [Header("Identity")]
        public string chestId = "chest_001";
        public string displayName = "Wooden Chest";
        public ChestTier chestTier = ChestTier.Normal;

        [Header("Contents")]
        public List<ItemData> possibleItems = new List<ItemData>();
        public int minimumItems = 1;
        public int maximumItems = 3;
    }
}
