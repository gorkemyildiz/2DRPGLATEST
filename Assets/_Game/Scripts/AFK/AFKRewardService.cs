using System;
using UnityEngine;

namespace Game.AFK
{
    [Serializable]
    public class AFKReward
    {
            public int goldEarned;
            public int experienceEarned;
            public int scrapEarned;
            public float afkDurationSeconds;
            public bool chestDropped;
            public bool wasClaimed;

            public AFKReward()
            {
                goldEarned = 0;
                experienceEarned = 0;
                scrapEarned = 0;
                afkDurationSeconds = 0;
                chestDropped = false;
                wasClaimed = false;
            }
        }

    public class AFKRewardService : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maximumAFKDurationHours = 8f;
        [SerializeField] private float afkEfficiency = 0.30f;
        [SerializeField] private float minimumAfkSeconds = 60f;

        [Header("Fallback Rates (per hour) if village missing")]
        [SerializeField] private int baseGoldPerHour = 100;
        [SerializeField] private int baseExperiencePerHour = 50;

        [Header("AFK Chest")]
        [SerializeField] [Range(0f, 1f)] private float chestChancePerHour = 0.25f;
        [SerializeField] private Game.Inventory.ChestService chestService;
        [SerializeField] private Game.Data.CraftCatalog craftCatalog;
        [SerializeField] private int scrapPerAfkHour = 4;

        [Header("Refs")]
        [SerializeField] private Game.Village.VillageService villageService;
        [SerializeField] private Game.Crafting.CraftService craftService;

        public AFKReward CalculateAFKReward(DateTime lastSaveTime)
        {
            AFKReward reward = new AFKReward();

            if (Game.Core.RuntimePlayerState.HasClaimedAfkThisPlaySession)
            {
                Debug.Log("[AFKRewardService] AFK already claimed this play session.");
                return reward;
            }

            if (lastSaveTime == DateTime.MinValue)
            {
                Debug.Log("[AFKRewardService] No valid last save time. No AFK rewards.");
                return reward;
            }

            TimeSpan afkDuration = DateTime.UtcNow - lastSaveTime;
            if (afkDuration.TotalSeconds < minimumAfkSeconds)
            {
                Debug.Log("[AFKRewardService] AFK duration too short. No rewards.");
                return reward;
            }

            float afkHours = Mathf.Min((float)afkDuration.TotalHours, maximumAFKDurationHours);

            int goldPerHour = baseGoldPerHour;
            int expPerHour = baseExperiencePerHour;

            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (villageService != null)
            {
                goldPerHour = Mathf.Max(goldPerHour, villageService.GetTotalGoldPerHour());
                expPerHour = Mathf.Max(expPerHour, villageService.GetTotalExperiencePerHour());
            }

            // afkYield = activeEquivalent * 0.30
            reward.afkDurationSeconds = (float)afkDuration.TotalSeconds;
            reward.goldEarned = Mathf.FloorToInt(goldPerHour * afkHours * afkEfficiency);
            reward.experienceEarned = Mathf.FloorToInt(expPerHour * afkHours * afkEfficiency);
            reward.scrapEarned = Mathf.FloorToInt(scrapPerAfkHour * afkHours * afkEfficiency);

            float chestRollChance = Mathf.Clamp01(chestChancePerHour * afkHours);
            reward.chestDropped = UnityEngine.Random.value <= chestRollChance;
            reward.wasClaimed = false;

            Debug.Log(
                $"[AFKRewardService] AFK: {reward.goldEarned}g / {reward.experienceEarned}xp / " +
                $"scrap={reward.scrapEarned} chest={reward.chestDropped} for {afkHours:F2}h @ {afkEfficiency:P0}.");

            return reward;
        }

        public void ClaimReward(AFKReward reward, Game.Rewards.RewardService rewardService)
        {
            if (reward == null || reward.wasClaimed)
            {
                return;
            }

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (rewardService == null)
            {
                Debug.LogError("[AFKRewardService] RewardService missing.");
                return;
            }

            if (reward.goldEarned > 0)
            {
                rewardService.AddGold(reward.goldEarned);
            }

            if (reward.experienceEarned > 0)
            {
                rewardService.AddExperience(reward.experienceEarned);
            }

            if (reward.scrapEarned > 0)
            {
                if (craftService == null)
                {
                    craftService = FindFirstObjectByType<Game.Crafting.CraftService>();
                }

                string scrapId = "mat_scrap";
                if (craftCatalog != null && craftCatalog.scrapMaterial != null)
                {
                    scrapId = craftCatalog.scrapMaterial.materialId;
                }
                else if (craftService != null && craftService.Catalog != null && craftService.Catalog.scrapMaterial != null)
                {
                    scrapId = craftService.Catalog.scrapMaterial.materialId;
                }

                if (craftService != null)
                {
                    craftService.AddMaterial(scrapId, reward.scrapEarned);
                }
                else
                {
                    Game.Core.RuntimePlayerState.EnsureInitialized();
                    Game.Core.RuntimePlayerState.Materials.Add(scrapId, reward.scrapEarned);
                }
            }

            if (reward.chestDropped)
            {
                if (chestService == null)
                {
                    chestService = FindFirstObjectByType<Game.Inventory.ChestService>();
                }

                if (chestService != null)
                {
                    chestService.TryOpenTierChest(Game.Data.ChestTier.Normal);
                }
            }

            reward.wasClaimed = true;
            Game.Core.RuntimePlayerState.HasClaimedAfkThisPlaySession = true;
            Debug.Log($"[AFKRewardService] Claimed AFK: {reward.goldEarned}g {reward.experienceEarned}xp scrap={reward.scrapEarned} chest={reward.chestDropped}");
        }
    }
}
