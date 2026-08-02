using System;
using UnityEngine;

namespace Game.Rewards
{
    public class RewardService : MonoBehaviour
    {
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private UnityEngine.UI.Text goldText;
        [SerializeField] private UnityEngine.UI.Text experienceText;
        [SerializeField] private UnityEngine.UI.Text levelText;
        [SerializeField] private Game.Inventory.ChestService chestService;

        public event Action<int> GoldChanged;
        public event Action<int, int> ExperienceChanged;
        public event Action<int> LevelUp;
        public event Action<string> RewardGranted;

        public PlayerProgress Progress => playerProgress;

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            playerProgress = Game.Core.RuntimePlayerState.Progress;

            if (chestService == null)
            {
                chestService = FindFirstObjectByType<Game.Inventory.ChestService>();
            }
        }

        private void Start()
        {
            RefreshUI();
        }

        public void BindUI(UnityEngine.UI.Text gold, UnityEngine.UI.Text experience, UnityEngine.UI.Text level)
        {
            if (gold != null)
            {
                goldText = gold;
            }

            if (experience != null)
            {
                experienceText = experience;
            }

            if (level != null)
            {
                levelText = level;
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            UpdateGoldUI();
            UpdateExperienceUI();
            UpdateLevelUI();
        }

        public void AddGold(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[RewardService] AddGold called with negative amount: {amount}. Ignoring.");
                return;
            }

            playerProgress.gold += amount;
            GoldChanged?.Invoke(playerProgress.gold);
            UpdateGoldUI();

            Debug.Log($"[RewardService] Gold added: {amount}. Total: {playerProgress.gold}");
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (playerProgress.gold < amount)
            {
                return false;
            }

            playerProgress.gold -= amount;
            GoldChanged?.Invoke(playerProgress.gold);
            UpdateGoldUI();
            return true;
        }

        public void AddExperience(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[RewardService] AddExperience called with negative amount: {amount}. Ignoring.");
                return;
            }

            playerProgress.currentExperience += amount;

            Debug.Log($"[RewardService] Experience added: {amount}. Current: {playerProgress.currentExperience}");

            CheckLevelUp();

            ExperienceChanged?.Invoke(playerProgress.currentExperience, GetRequiredExperience());
            UpdateExperienceUI();
            UpdateLevelUI();
        }

        public void GrantEnemyRewards(Game.Data.EnemyData enemyData)
        {
            if (enemyData == null)
            {
                Debug.LogWarning("[RewardService] GrantEnemyRewards called with null enemyData.");
                return;
            }

            playerProgress.totalEnemiesKilled++;

            AddGold(enemyData.goldReward);
            AddExperience(enemyData.experienceReward);

            if (enemyData.chestDropChance > 0)
            {
                if (chestService != null)
                {
                    bool dropped = chestService.TryDropFromChance(enemyData.chestDropChance);
                    if (dropped)
                    {
                        RewardGranted?.Invoke($"Chest from {enemyData.displayName}");
                    }
                }
                else
                {
                    float roll = UnityEngine.Random.value;
                    if (roll <= enemyData.chestDropChance)
                    {
                        Debug.Log($"[RewardService] Chest dropped from {enemyData.displayName}! (ChestService missing)");
                        RewardGranted?.Invoke($"Chest from {enemyData.displayName}");
                    }
                }
            }
        }

        private void CheckLevelUp()
        {
            int requiredExp = GetRequiredExperience();

            while (playerProgress.currentExperience >= requiredExp)
            {
                playerProgress.currentExperience -= requiredExp;
                playerProgress.level++;

                Debug.Log($"[RewardService] Level Up! New level: {playerProgress.level}");

                LevelUp?.Invoke(playerProgress.level);

                requiredExp = GetRequiredExperience();
            }
        }

        public int GetRequiredExperience()
        {
            return 100 + ((playerProgress.level - 1) * 50);
        }

        private void UpdateGoldUI()
        {
            if (goldText != null)
            {
                goldText.text = playerProgress.gold.ToString();
            }
        }

        private void UpdateExperienceUI()
        {
            if (experienceText != null)
            {
                experienceText.text = $"EXP: {playerProgress.currentExperience}/{GetRequiredExperience()}";
            }
        }

        private void UpdateLevelUI()
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{playerProgress.level}";
            }
        }
    }
}
