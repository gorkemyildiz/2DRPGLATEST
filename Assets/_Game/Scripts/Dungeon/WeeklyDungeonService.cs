using System;
using System.Globalization;
using UnityEngine;

namespace Game.Dungeon
{
    public class WeeklyDungeonService : MonoBehaviour
    {
        [SerializeField] private Game.Data.WeeklyDungeonCatalog dungeonCatalog;
        [SerializeField] private Game.Crafting.CraftService craftService;

        public event Action Changed;

        public Game.Data.WeeklyDungeonCatalog Catalog => dungeonCatalog;

        public void SetCatalog(Game.Data.WeeklyDungeonCatalog catalog)
        {
            if (catalog != null)
            {
                dungeonCatalog = catalog;
            }
        }

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            ResolveRefs();
            if (dungeonCatalog == null)
            {
#if UNITY_EDITOR
                dungeonCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Data.WeeklyDungeonCatalog>(
                    "Assets/_Game/ScriptableObjects/Dungeon/WeeklyDungeonCatalog.asset");
#endif
            }

            EnsureCurrentWeek();
        }

        private void ResolveRefs()
        {
            if (craftService == null)
            {
                craftService = FindFirstObjectByType<Game.Crafting.CraftService>();
            }
        }

        public static string GetCurrentWeekKey()
        {
            DateTime utc = DateTime.UtcNow;
            Calendar cal = CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(utc, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return $"{utc.Year}-W{week:00}";
        }

        public void EnsureCurrentWeek()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            string key = GetCurrentWeekKey();
            Game.Save.WeeklyDungeonProgress progress = Game.Core.RuntimePlayerState.WeeklyDungeonProgress;
            if (progress.weekKey != key)
            {
                progress.weekKey = key;
                progress.entriesUsed = 0;
                Changed?.Invoke();
            }
        }

        public int GetMaxEntries()
        {
            return dungeonCatalog != null ? Mathf.Max(1, dungeonCatalog.maxEntriesPerWeek) : 10;
        }

        public int GetEntriesUsed()
        {
            EnsureCurrentWeek();
            return Game.Core.RuntimePlayerState.WeeklyDungeonProgress.entriesUsed;
        }

        public int GetEntriesRemaining()
        {
            return Mathf.Max(0, GetMaxEntries() - GetEntriesUsed());
        }

        public Game.Data.EnemyData GetCurrentBoss()
        {
            EnsureCurrentWeek();
            if (dungeonCatalog == null)
            {
                return null;
            }

            return dungeonCatalog.GetBossForWeek(Game.Core.RuntimePlayerState.WeeklyDungeonProgress.weekKey);
        }

        public int GetCourageStoneCount()
        {
            ResolveRefs();
            if (craftService != null)
            {
                return craftService.GetCourageStoneCount();
            }

            string id = dungeonCatalog != null ? dungeonCatalog.courageStoneMaterialId : "mat_courage_stone";
            return Game.Core.RuntimePlayerState.Materials.GetAmount(id);
        }

        public bool CanClaimWeeklyStone(out string reason)
        {
            reason = null;
            EnsureCurrentWeek();
            Game.Save.WeeklyDungeonProgress progress = Game.Core.RuntimePlayerState.WeeklyDungeonProgress;
            if (progress.lastClaimWeekKey == progress.weekKey)
            {
                reason = "Already claimed this week";
                return false;
            }

            return true;
        }

        public bool TryClaimWeeklyStone()
        {
            if (!CanClaimWeeklyStone(out string reason))
            {
                Debug.Log($"[WeeklyDungeon] Claim blocked: {reason}");
                return false;
            }

            ResolveRefs();
            int amount = dungeonCatalog != null ? Mathf.Max(1, dungeonCatalog.weeklyClaimStoneAmount) : 1;
            string id = dungeonCatalog != null ? dungeonCatalog.courageStoneMaterialId : "mat_courage_stone";
            if (craftService != null)
            {
                craftService.AddMaterial(id, amount);
            }
            else
            {
                Game.Core.RuntimePlayerState.Materials.Add(id, amount);
            }

            Game.Core.RuntimePlayerState.WeeklyDungeonProgress.lastClaimWeekKey =
                Game.Core.RuntimePlayerState.WeeklyDungeonProgress.weekKey;
            Changed?.Invoke();
            return true;
        }

        public bool CanEnter(out string reason)
        {
            reason = null;
            EnsureCurrentWeek();
            if (dungeonCatalog == null)
            {
                reason = "Dungeon catalog missing";
                return false;
            }

            if (dungeonCatalog.dungeonStage == null)
            {
                reason = "Dungeon stage missing";
                return false;
            }

            if (GetEntriesRemaining() <= 0)
            {
                reason = "Weekly entry limit reached";
                return false;
            }

            int cost = Mathf.Max(1, dungeonCatalog.courageStoneCost);
            if (GetCourageStoneCount() < cost)
            {
                reason = $"Need {cost} Courage Stone";
                return false;
            }

            return true;
        }

        public bool TryEnter()
        {
            if (!CanEnter(out string reason))
            {
                Debug.Log($"[WeeklyDungeon] Enter blocked: {reason}");
                return false;
            }

            ResolveRefs();
            int cost = Mathf.Max(1, dungeonCatalog.courageStoneCost);
            bool consumed = craftService != null
                ? craftService.TryConsumeCourageStone(cost)
                : Game.Core.RuntimePlayerState.Materials.TryConsume(dungeonCatalog.courageStoneMaterialId, cost);

            if (!consumed)
            {
                return false;
            }

            Game.Core.RuntimePlayerState.WeeklyDungeonProgress.entriesUsed += 1;
            Game.Data.EnemyData boss = GetCurrentBoss();
            Game.Core.BattleSession.SelectWeeklyDungeon(dungeonCatalog.dungeonStage, boss, dungeonCatalog);
            Changed?.Invoke();
            return true;
        }

        public void TryDropCourageStoneFromLayerBoss()
        {
            if (dungeonCatalog == null)
            {
                return;
            }

            if (UnityEngine.Random.value > dungeonCatalog.layerBossCourageDropChance)
            {
                return;
            }

            ResolveRefs();
            string id = dungeonCatalog.courageStoneMaterialId;
            if (craftService != null)
            {
                craftService.AddMaterial(id, 1);
            }
            else
            {
                Game.Core.RuntimePlayerState.Materials.Add(id, 1);
            }

            Debug.Log("[WeeklyDungeon] Courage Stone dropped from layer boss.");
            Changed?.Invoke();
        }
    }
}
