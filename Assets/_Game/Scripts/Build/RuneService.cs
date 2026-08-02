using System;
using UnityEngine;

namespace Game.Build
{
    public class RuneService : MonoBehaviour
    {
        [SerializeField] private Game.Data.RuneCatalog runeCatalog;
        [SerializeField] private Game.Rewards.RewardService rewardService;

        public event Action Changed;

        public Game.Data.RuneCatalog Catalog => runeCatalog;

        public void SetCatalog(Game.Data.RuneCatalog catalog)
        {
            if (catalog != null)
            {
                runeCatalog = catalog;
            }
        }

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            ResolveRefs();
        }

        private void ResolveRefs()
        {
            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }
        }

        public int GetCharacterLevel()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (rewardService != null && rewardService.Progress != null)
            {
                return Mathf.Max(1, rewardService.Progress.level);
            }

            return Mathf.Max(1, Game.Core.RuntimePlayerState.Progress.level);
        }

        public int GetTotalPoints()
        {
            return GetCharacterLevel();
        }

        public int GetSpent()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.ActiveBuild;
            if (progress == null || progress.runeRanks == null || runeCatalog == null)
            {
                return 0;
            }

            int spent = 0;
            for (int i = 0; i < progress.runeRanks.Count; i++)
            {
                Game.Save.RuneRankSave entry = progress.runeRanks[i];
                if (entry == null || entry.rank <= 0)
                {
                    continue;
                }

                if (runeCatalog.GetById(entry.runeId) == null)
                {
                    continue;
                }

                spent += entry.rank;
            }

            return spent;
        }

        public int GetRemaining()
        {
            return Mathf.Max(0, GetTotalPoints() - GetSpent());
        }

        public int GetBranchCap()
        {
            return Mathf.Max(0, GetCharacterLevel() / 2);
        }

        public int GetBranchSpent(Game.Data.RuneBranch branch)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (runeCatalog == null)
            {
                return 0;
            }

            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.ActiveBuild;
            if (progress == null || progress.runeRanks == null)
            {
                return 0;
            }

            int spent = 0;
            for (int i = 0; i < progress.runeRanks.Count; i++)
            {
                Game.Save.RuneRankSave entry = progress.runeRanks[i];
                if (entry == null || entry.rank <= 0)
                {
                    continue;
                }

                Game.Data.RuneNodeData node = runeCatalog.GetById(entry.runeId);
                if (node != null && node.branch == branch)
                {
                    spent += entry.rank;
                }
            }

            return spent;
        }

        public int GetRank(string runeId)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            return Game.Core.RuntimePlayerState.ActiveBuild.GetRank(runeId);
        }

        public bool CanAllocate(string runeId, out string failReason)
        {
            failReason = null;
            if (runeCatalog == null)
            {
                failReason = "Rune catalog missing";
                return false;
            }

            Game.Data.RuneNodeData node = runeCatalog.GetById(runeId);
            if (node == null)
            {
                failReason = "Unknown rune";
                return false;
            }

            int level = GetCharacterLevel();
            if (level < node.requiredLevel)
            {
                failReason = $"Requires level {node.requiredLevel}";
                return false;
            }

            int rank = GetRank(runeId);
            if (rank >= Mathf.Max(1, node.maxRank))
            {
                failReason = "Max rank";
                return false;
            }

            if (GetRemaining() <= 0)
            {
                failReason = "No points left";
                return false;
            }

            if (GetBranchSpent(node.branch) >= GetBranchCap())
            {
                failReason = $"Branch cap ({GetBranchCap()})";
                return false;
            }

            return true;
        }

        public bool TryAllocate(string runeId)
        {
            if (!CanAllocate(runeId, out string reason))
            {
                Debug.Log($"[RuneService] Allocate blocked: {reason}");
                return false;
            }

            int next = GetRank(runeId) + 1;
            Game.Core.RuntimePlayerState.ActiveBuild.SetRank(runeId, next);
            Changed?.Invoke();
            return true;
        }

        public bool TryRefund(string runeId)
        {
            int rank = GetRank(runeId);
            if (rank <= 0)
            {
                return false;
            }

            Game.Core.RuntimePlayerState.ActiveBuild.SetRank(runeId, rank - 1);
            Changed?.Invoke();
            return true;
        }

        public int GetTotalAttackBonus()
        {
            return SumBonus(Game.Core.RuntimePlayerState.ActiveBuild, (node, rank) => node.attackBonusPerRank * rank);
        }

        public int GetTotalHealthBonus()
        {
            return SumBonus(Game.Core.RuntimePlayerState.ActiveBuild, (node, rank) => node.healthBonusPerRank * rank);
        }

        public int GetTotalAttackBonusForClass(string classId)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress build = Game.Core.RuntimePlayerState.RosterProgress.GetOrCreateBuild(classId);
            return SumBonus(build, (node, rank) => node.attackBonusPerRank * rank);
        }

        public int GetTotalHealthBonusForClass(string classId)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress build = Game.Core.RuntimePlayerState.RosterProgress.GetOrCreateBuild(classId);
            return SumBonus(build, (node, rank) => node.healthBonusPerRank * rank);
        }

        public float GetCooldownReducePercent()
        {
            float total = 0f;
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (runeCatalog == null)
            {
                return 0f;
            }

            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.ActiveBuild;
            if (progress == null || progress.runeRanks == null)
            {
                return 0f;
            }

            for (int i = 0; i < progress.runeRanks.Count; i++)
            {
                Game.Save.RuneRankSave entry = progress.runeRanks[i];
                if (entry == null || entry.rank <= 0)
                {
                    continue;
                }

                Game.Data.RuneNodeData node = runeCatalog.GetById(entry.runeId);
                if (node == null)
                {
                    continue;
                }

                total += node.cooldownReducePercentPerRank * entry.rank;
            }

            return Mathf.Clamp01(total);
        }

        private int SumBonus(Game.Save.BuildProgress progress, Func<Game.Data.RuneNodeData, int, int> selector)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (runeCatalog == null || progress == null || progress.runeRanks == null)
            {
                return 0;
            }

            int sum = 0;
            for (int i = 0; i < progress.runeRanks.Count; i++)
            {
                Game.Save.RuneRankSave entry = progress.runeRanks[i];
                if (entry == null || entry.rank <= 0)
                {
                    continue;
                }

                Game.Data.RuneNodeData node = runeCatalog.GetById(entry.runeId);
                if (node == null)
                {
                    continue;
                }

                sum += selector(node, entry.rank);
            }

            return sum;
        }
    }
}
