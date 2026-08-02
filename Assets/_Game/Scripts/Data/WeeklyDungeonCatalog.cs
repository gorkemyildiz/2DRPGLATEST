using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "WeeklyDungeonCatalog", menuName = "Game Data/Dungeon/Weekly Dungeon Catalog")]
    public class WeeklyDungeonCatalog : ScriptableObject
    {
        [Header("Entry")]
        public int maxEntriesPerWeek = 10;
        public int courageStoneCost = 1;
        public string courageStoneMaterialId = "mat_courage_stone";

        [Header("Battle")]
        public StageData dungeonStage;
        public string displayName = "Weekly Dungeon";

        [Header("Boss Rotation (weekly index % count)")]
        public List<EnemyData> bossRotation = new List<EnemyData>();

        [Header("Rewards")]
        public ChestData dungeonChest;

        [Header("Stone Sources")]
        [Range(0f, 1f)] public float layerBossCourageDropChance = 0.25f;
        public int weeklyClaimStoneAmount = 1;

        public EnemyData GetBossForWeek(string weekKey)
        {
            if (bossRotation == null || bossRotation.Count == 0)
            {
                return null;
            }

            int index = Mathf.Abs(weekKey != null ? weekKey.GetHashCode() : 0) % bossRotation.Count;
            return bossRotation[index];
        }
    }
}
