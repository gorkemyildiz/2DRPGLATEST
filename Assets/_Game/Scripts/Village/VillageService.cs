using System;
using UnityEngine;

namespace Game.Village
{
    public class VillageService : MonoBehaviour
    {
        [SerializeField] private Game.Data.VillageCatalog villageCatalog;
        [SerializeField] private Game.Rewards.RewardService rewardService;

        public event Action BuildingsChanged;

        public Game.Data.VillageCatalog Catalog => villageCatalog;

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Core.RuntimePlayerState.VillageProgress.EnsureDefaults();

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }
        }

        public void SetCatalog(Game.Data.VillageCatalog catalog)
        {
            if (catalog != null)
            {
                villageCatalog = catalog;
            }
        }

        public int GetBuildingLevel(Game.Data.VillageBuildingType type)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            return Game.Core.RuntimePlayerState.VillageProgress.GetLevel(type);
        }

        public int GetTotalGoldPerHour()
        {
            return GetRateGold(Game.Data.VillageBuildingType.Mine);
        }

        public int GetTotalExperiencePerHour()
        {
            return GetRateExp(Game.Data.VillageBuildingType.Library);
        }

        public float GetCraftSpeedBonus()
        {
            if (villageCatalog == null)
            {
                return 0f;
            }

            Game.Data.VillageBuildingData data = villageCatalog.GetByType(Game.Data.VillageBuildingType.Blacksmith);
            if (data == null)
            {
                return 0f;
            }

            return data.GetCraftSpeedBonus(GetBuildingLevel(Game.Data.VillageBuildingType.Blacksmith));
        }

        public int GetRateGold(Game.Data.VillageBuildingType type)
        {
            Game.Data.VillageBuildingData data = villageCatalog != null ? villageCatalog.GetByType(type) : null;
            if (data == null)
            {
                return 0;
            }

            return data.GetGoldPerHour(GetBuildingLevel(type));
        }

        public int GetRateExp(Game.Data.VillageBuildingType type)
        {
            Game.Data.VillageBuildingData data = villageCatalog != null ? villageCatalog.GetByType(type) : null;
            if (data == null)
            {
                return 0;
            }

            return data.GetExperiencePerHour(GetBuildingLevel(type));
        }

        public bool CanUpgrade(Game.Data.VillageBuildingType type, out string failReason)
        {
            failReason = null;
            if (villageCatalog == null)
            {
                failReason = "Village catalog missing";
                return false;
            }

            Game.Data.VillageBuildingData data = villageCatalog.GetByType(type);
            if (data == null)
            {
                failReason = "Building data missing";
                return false;
            }

            int level = GetBuildingLevel(type);
            if (level >= data.maxLevel)
            {
                failReason = "Max level";
                return false;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            int requiredLevel = data.GetRequiredPlayerLevel(level);
            int playerLevel = Game.Core.RuntimePlayerState.Progress.level;
            if (playerLevel < requiredLevel)
            {
                failReason = $"Need player Lv.{requiredLevel}";
                return false;
            }

            int cost = data.GetUpgradeGoldCost(level);
            if (Game.Core.RuntimePlayerState.Progress.gold < cost)
            {
                failReason = $"Need {cost} gold";
                return false;
            }

            return true;
        }

        public bool TryUpgrade(Game.Data.VillageBuildingType type)
        {
            if (!CanUpgrade(type, out string reason))
            {
                Debug.LogWarning($"[VillageService] Upgrade failed ({type}): {reason}");
                return false;
            }

            Game.Data.VillageBuildingData data = villageCatalog.GetByType(type);
            int level = GetBuildingLevel(type);
            int cost = data.GetUpgradeGoldCost(level);

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (rewardService != null)
            {
                if (!rewardService.TrySpendGold(cost))
                {
                    Debug.LogWarning("[VillageService] Spend gold failed.");
                    return false;
                }
            }
            else
            {
                Game.Core.RuntimePlayerState.Progress.gold -= cost;
            }

            Game.Core.RuntimePlayerState.VillageProgress.SetLevel(type, level + 1);
            BuildingsChanged?.Invoke();
            Game.Save.GameSaveController.SaveGame();

            Debug.Log($"[VillageService] Upgraded {type} to Lv.{level + 1} for {cost} gold.");
            return true;
        }
    }
}
