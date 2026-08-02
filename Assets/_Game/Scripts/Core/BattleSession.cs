using Game.Data;
using Game.Save;

namespace Game.Core
{
    /// <summary>
    /// Carries the selected map node from Hub into BattleDemo for one run.
    /// Also mirrors selection into MapProgress so unlock/auto-continue survive scene loads.
    /// </summary>
    public static class BattleSession
    {
        public static LevelData SelectedLevel { get; private set; }
        public static LayerData SelectedLayerBoss { get; private set; }
        public static bool IsLayerBossBattle { get; private set; }
        public static bool IsWeeklyDungeonBattle { get; private set; }
        public static StageData SelectedWeeklyStage { get; private set; }
        public static EnemyData SelectedWeeklyBoss { get; private set; }
        public static WeeklyDungeonCatalog WeeklyDungeonCatalog { get; private set; }
        public static MapCatalog ActiveCatalog { get; private set; }

        public static void SetCatalog(MapCatalog catalog)
        {
            if (catalog != null)
            {
                ActiveCatalog = catalog;
            }
        }

        public static void SelectLevel(LevelData level, MapCatalog catalog = null)
        {
            ClearWeekly();
            SelectedLevel = level;
            SelectedLayerBoss = null;
            IsLayerBossBattle = false;
            if (catalog != null)
            {
                ActiveCatalog = catalog;
            }

            if (level != null)
            {
                RuntimePlayerState.EnsureInitialized();
                RuntimePlayerState.MapProgress.SetActiveLevel(
                    level.layerIndex,
                    level.chapterIndex,
                    level.levelIndex);
            }
        }

        public static void SelectLayerBoss(LayerData layer, MapCatalog catalog = null)
        {
            ClearWeekly();
            SelectedLevel = null;
            SelectedLayerBoss = layer;
            IsLayerBossBattle = layer != null;
            if (catalog != null)
            {
                ActiveCatalog = catalog;
            }

            if (layer != null)
            {
                RuntimePlayerState.EnsureInitialized();
                RuntimePlayerState.MapProgress.SetActiveLayerBoss(layer.layerIndex);
            }
        }

        public static void SelectWeeklyDungeon(
            StageData stage,
            EnemyData boss,
            WeeklyDungeonCatalog catalog)
        {
            SelectedLevel = null;
            SelectedLayerBoss = null;
            IsLayerBossBattle = false;
            IsWeeklyDungeonBattle = true;
            SelectedWeeklyStage = stage;
            SelectedWeeklyBoss = boss;
            WeeklyDungeonCatalog = catalog;

            RuntimePlayerState.EnsureInitialized();
            RuntimePlayerState.MapProgress.ClearActiveBattleNode();
        }

        public static void Clear(bool clearActiveProgressNode = true)
        {
            SelectedLevel = null;
            SelectedLayerBoss = null;
            IsLayerBossBattle = false;
            ClearWeekly();

            if (clearActiveProgressNode)
            {
                RuntimePlayerState.EnsureInitialized();
                RuntimePlayerState.MapProgress.ClearActiveBattleNode();
            }
        }

        private static void ClearWeekly()
        {
            IsWeeklyDungeonBattle = false;
            SelectedWeeklyStage = null;
            SelectedWeeklyBoss = null;
            WeeklyDungeonCatalog = null;
        }

        /// <summary>
        /// Rebuilds SelectedLevel / layer boss from MapProgress.active* when statics were lost.
        /// </summary>
        public static bool TryRestoreFromProgress(MapCatalog catalog)
        {
            if (IsWeeklyDungeonBattle && SelectedWeeklyStage != null)
            {
                return true;
            }

            if (SelectedLevel != null || (IsLayerBossBattle && SelectedLayerBoss != null))
            {
                return true;
            }

            MapCatalog useCatalog = catalog != null ? catalog : ActiveCatalog;
            if (useCatalog == null)
            {
                return false;
            }

            RuntimePlayerState.EnsureInitialized();
            MapProgress progress = RuntimePlayerState.MapProgress;
            if (progress == null || !progress.HasActiveBattleNode)
            {
                return false;
            }

            if (progress.activeIsLayerBoss)
            {
                LayerData layer = useCatalog.GetLayer(progress.activeLayer);
                if (layer == null)
                {
                    return false;
                }

                SelectLayerBoss(layer, useCatalog);
                return true;
            }

            LevelData level = useCatalog.GetLevel(
                progress.activeLayer,
                progress.activeChapter,
                progress.activeLevel);
            if (level == null)
            {
                return false;
            }

            SelectLevel(level, useCatalog);
            return true;
        }

        public static StageData ResolveBattleContent(StageData fallback)
        {
            if (IsWeeklyDungeonBattle && SelectedWeeklyStage != null)
            {
                return SelectedWeeklyStage;
            }

            if (IsLayerBossBattle && SelectedLayerBoss != null && SelectedLayerBoss.layerBossBattle != null)
            {
                return SelectedLayerBoss.layerBossBattle;
            }

            if (SelectedLevel != null && SelectedLevel.battleContent != null)
            {
                return SelectedLevel.battleContent;
            }

            return fallback;
        }

        public static string ResolveDisplayName(string fallback)
        {
            if (IsWeeklyDungeonBattle)
            {
                if (WeeklyDungeonCatalog != null && !string.IsNullOrEmpty(WeeklyDungeonCatalog.displayName))
                {
                    string bossName = SelectedWeeklyBoss != null ? SelectedWeeklyBoss.displayName : "Boss";
                    return $"{WeeklyDungeonCatalog.displayName}: {bossName}";
                }

                return "Weekly Dungeon";
            }

            if (IsLayerBossBattle && SelectedLayerBoss != null)
            {
                return string.IsNullOrEmpty(SelectedLayerBoss.layerBossDisplayName)
                    ? SelectedLayerBoss.displayName + " Boss"
                    : SelectedLayerBoss.layerBossDisplayName;
            }

            if (SelectedLevel != null)
            {
                return SelectedLevel.displayName;
            }

            return fallback;
        }
    }
}
