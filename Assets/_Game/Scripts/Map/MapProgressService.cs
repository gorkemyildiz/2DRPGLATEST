using Game.Data;
using Game.Save;
using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Unlocks next map nodes after a successful clear.
    /// Flow: levels 1-9 → next level; level 10 → layer boss; layer boss → next chapter (or next layer).
    /// </summary>
    public static class MapProgressService
    {
        public static void RegisterLevelCleared(LevelData level, MapCatalog catalog)
        {
            if (level == null)
            {
                return;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;

            if (!progress.IsLevelUnlocked(level.layerIndex, level.chapterIndex, level.levelIndex))
            {
                Debug.LogWarning(
                    $"[MapProgress] Cleared locked level L{level.layerIndex}-C{level.chapterIndex}-Lv{level.levelIndex}; ignoring unlock advance.");
                return;
            }

            int layer = level.layerIndex;
            int chapter = level.chapterIndex;
            int clearedLevel = level.levelIndex;

            if (clearedLevel < 10)
            {
                RaiseUnlock(progress, layer, chapter, clearedLevel + 1);
            }
            else
            {
                // Chapter boss (Lv10) cleared → layer boss required before next chapter/layer.
                progress.pendingLayerBossChapter = chapter;
                Debug.Log(
                    $"[MapProgress] Chapter {chapter} complete — layer boss pending (no next chapter yet).");
            }

            Debug.Log(
                $"[MapProgress] Cleared L{layer}-C{chapter}-Lv{clearedLevel}. " +
                $"Unlocked L{progress.unlockedLayer}-C{progress.unlockedChapter}-Lv{progress.unlockedLevel}. " +
                $"PendingLayerBossChapter={progress.pendingLayerBossChapter}");
        }

        public static void RegisterLayerBossCleared(LayerData layer, MapCatalog catalog)
        {
            if (layer == null)
            {
                return;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;

            int finishedChapter = progress.pendingLayerBossChapter > 0
                ? progress.pendingLayerBossChapter
                : Mathf.Max(1, progress.unlockedChapter);
            progress.pendingLayerBossChapter = 0;

            if (finishedChapter > progress.layerBossClearedChapterUpTo)
            {
                progress.layerBossClearedChapterUpTo = finishedChapter;
            }

            int chapterCount = Mathf.Max(1, layer.ChapterCount);

            if (finishedChapter < chapterCount)
            {
                // Next chapter in same layer — LB node stays visually open.
                RaiseUnlock(progress, layer.layerIndex, finishedChapter + 1, 1);
                Debug.Log(
                    $"[MapProgress] Layer boss cleared after C{finishedChapter} → unlocked C{finishedChapter + 1} Lv1.");
            }
            else
            {
                // Entire layer done.
                if (layer.layerIndex > progress.layerBossClearedUpTo)
                {
                    progress.layerBossClearedUpTo = layer.layerIndex;
                }

                int nextLayer = layer.layerIndex + 1;
                if (catalog != null && catalog.GetLayer(nextLayer) != null)
                {
                    RaiseUnlock(progress, nextLayer, 1, 1);
                    progress.layerBossClearedChapterUpTo = 0; // reset visual for new layer
                    Debug.Log($"[MapProgress] Layer boss cleared — unlocked Layer {nextLayer}.");
                }
                else
                {
                    Debug.Log($"[MapProgress] Layer boss cleared — no next layer in catalog.");
                }
            }
        }

        /// <summary>
        /// Resolves the level that was just fought (session first, then persisted active node).
        /// </summary>
        public static LevelData ResolveClearedLevel(MapCatalog catalog)
        {
            if (Game.Core.BattleSession.SelectedLevel != null)
            {
                return Game.Core.BattleSession.SelectedLevel;
            }

            Game.Core.BattleSession.TryRestoreFromProgress(catalog);
            return Game.Core.BattleSession.SelectedLevel;
        }

        public static LayerData ResolveClearedLayerBoss(MapCatalog catalog)
        {
            if (Game.Core.BattleSession.IsLayerBossBattle && Game.Core.BattleSession.SelectedLayerBoss != null)
            {
                return Game.Core.BattleSession.SelectedLayerBoss;
            }

            Game.Core.BattleSession.TryRestoreFromProgress(catalog);
            if (Game.Core.BattleSession.IsLayerBossBattle)
            {
                return Game.Core.BattleSession.SelectedLayerBoss;
            }

            return null;
        }

        /// <summary>
        /// Next fight after the run that just cleared (based on cleared node, not unlock tip).
        /// </summary>
        public static bool TryGetAutoContinueTarget(
            MapCatalog catalog,
            out LevelData nextLevel,
            out LayerData nextLayerBoss)
        {
            nextLevel = null;
            nextLayerBoss = null;

            if (catalog == null)
            {
                Debug.LogWarning("[MapProgress] Auto-continue failed: MapCatalog is null.");
                return false;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;

            // Just cleared layer boss → next chapter / next layer stage.
            LayerData clearedBoss = ResolveClearedLayerBoss(catalog);
            if (clearedBoss != null && Game.Core.BattleSession.IsLayerBossBattle)
            {
                // RegisterLayerBossCleared already ran and advanced unlock tip.
                nextLevel = catalog.GetLevel(
                    progress.unlockedLayer,
                    progress.unlockedChapter,
                    progress.unlockedLevel);
                if (nextLevel == null)
                {
                    Debug.Log("[MapProgress] Auto-continue: no stage after layer boss.");
                }

                return nextLevel != null;
            }

            LevelData cleared = ResolveClearedLevel(catalog);
            if (cleared == null)
            {
                Debug.LogWarning("[MapProgress] Auto-continue failed: no SelectedLevel / active battle node.");
                return false;
            }

            // Same chapter, next level
            if (cleared.levelIndex < 10)
            {
                nextLevel = catalog.GetLevel(cleared.layerIndex, cleared.chapterIndex, cleared.levelIndex + 1);
                if (nextLevel == null)
                {
                    Debug.LogWarning(
                        $"[MapProgress] Missing LevelData L{cleared.layerIndex}-C{cleared.chapterIndex}-Lv{cleared.levelIndex + 1}");
                }

                return nextLevel != null;
            }

            // Chapter Lv10 cleared → MUST fight layer boss next (never skip to next chapter).
            LayerData layer = catalog.GetLayer(cleared.layerIndex);
            if (layer == null)
            {
                Debug.LogWarning($"[MapProgress] Missing LayerData L{cleared.layerIndex}");
                return false;
            }

            if (progress.pendingLayerBossChapter <= 0)
            {
                progress.pendingLayerBossChapter = cleared.chapterIndex;
            }

            nextLayerBoss = layer;
            bool ok = nextLayerBoss.layerBossBattle != null;
            if (!ok)
            {
                Debug.LogWarning($"[MapProgress] Layer {layer.layerIndex} has no layerBossBattle assigned.");
            }
            else
            {
                Debug.Log($"[MapProgress] Auto-continue → Layer Boss after C{cleared.chapterIndex}.");
            }

            return ok;
        }

        private static void RaiseUnlock(MapProgress progress, int layer, int chapter, int level)
        {
            if (Compare(layer, chapter, level, progress.unlockedLayer, progress.unlockedChapter, progress.unlockedLevel) > 0)
            {
                progress.unlockedLayer = layer;
                progress.unlockedChapter = chapter;
                progress.unlockedLevel = level;
            }
        }

        private static int Compare(int l1, int c1, int v1, int l2, int c2, int v2)
        {
            if (l1 != l2)
            {
                return l1.CompareTo(l2);
            }

            if (c1 != c2)
            {
                return c1.CompareTo(c2);
            }

            return v1.CompareTo(v2);
        }
    }
}
