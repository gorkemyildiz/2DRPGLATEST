using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewVillageBuilding", menuName = "Game Data/Village/Building")]
    public class VillageBuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string buildingId = "building_library";
        public string displayName = "Library";
        public VillageBuildingType buildingType = VillageBuildingType.Library;
        public Sprite icon;
        public Sprite plazaSprite;

        [Header("Levels")]
        public int maxLevel = 10;

        [Header("Passive Rates (per hour at level 1)")]
        public int baseGoldPerHour;
        public int baseExperiencePerHour;

        [Header("Per Level Scaling")]
        public int goldPerHourPerLevel = 20;
        public int experiencePerHourPerLevel = 10;

        [Header("Blacksmith (Phase 3 craft uses this)")]
        [Range(0f, 2f)] public float craftSpeedBonusPerLevel = 0.05f;

        [Header("Upgrade Costs")]
        public int baseUpgradeGoldCost = 50;
        public int upgradeGoldCostPerLevel = 40;
        public int requiredPlayerLevelPerBuildingLevel = 1;

        public int GetGoldPerHour(int buildingLevel)
        {
            int level = Mathf.Max(1, buildingLevel);
            return baseGoldPerHour + goldPerHourPerLevel * (level - 1);
        }

        public int GetExperiencePerHour(int buildingLevel)
        {
            int level = Mathf.Max(1, buildingLevel);
            return baseExperiencePerHour + experiencePerHourPerLevel * (level - 1);
        }

        public float GetCraftSpeedBonus(int buildingLevel)
        {
            int level = Mathf.Max(1, buildingLevel);
            return craftSpeedBonusPerLevel * (level - 1);
        }

        public int GetUpgradeGoldCost(int currentLevel)
        {
            int level = Mathf.Max(1, currentLevel);
            return baseUpgradeGoldCost + upgradeGoldCostPerLevel * (level - 1);
        }

        public int GetRequiredPlayerLevel(int currentLevel)
        {
            int nextLevel = Mathf.Max(1, currentLevel) + 1;
            return Mathf.Max(1, requiredPlayerLevelPerBuildingLevel * nextLevel);
        }
    }
}
