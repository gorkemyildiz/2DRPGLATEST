using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Game Data/Map/Level")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public string levelId = "L1_C1_01";
        public string displayName = "Level 1";

        [Header("Map Coordinates")]
        public int layerIndex = 1;
        public int chapterIndex = 1;
        [Range(1, 10)] public int levelIndex = 1;

        [Header("Battle")]
        public StageData battleContent;

        [Header("Boss Flags")]
        public bool isChapterBoss;

        [Header("Rewards")]
        public ChestTier completionChestTier = ChestTier.Normal;
        public ChestData completionChest;
    }
}
