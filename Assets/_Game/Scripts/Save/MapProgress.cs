using System;

namespace Game.Save
{
    [Serializable]
    public class MapProgress
    {
        /// <summary>Furthest unlocked layer (1-based).</summary>
        public int unlockedLayer = 1;

        /// <summary>Furthest unlocked chapter within unlockedLayer.</summary>
        public int unlockedChapter = 1;

        /// <summary>Furthest unlocked level within unlockedChapter (1-10).</summary>
        public int unlockedLevel = 1;

        /// <summary>Highest layer whose final layer-boss was fully cleared.</summary>
        public int layerBossClearedUpTo;

        /// <summary>
        /// After clearing a chapter's level 10, layer boss is required before the next chapter/layer.
        /// 0 = none pending. Otherwise = chapter index that was just finished.
        /// </summary>
        public int pendingLayerBossChapter;

        /// <summary>
        /// Highest chapter whose layer boss was beaten on the current layer.
        /// Used so the LB node stays visually open after a kill.
        /// </summary>
        public int layerBossClearedChapterUpTo;

        /// <summary>
        /// Last selected battle node (persists across scene loads when BattleSession statics are lost).
        /// </summary>
        public int activeLayer;

        public int activeChapter;
        public int activeLevel;
        public bool activeIsLayerBoss;

        public MapProgress()
        {
            unlockedLayer = 1;
            unlockedChapter = 1;
            unlockedLevel = 1;
            layerBossClearedUpTo = 0;
            pendingLayerBossChapter = 0;
            layerBossClearedChapterUpTo = 0;
            activeLayer = 0;
            activeChapter = 0;
            activeLevel = 0;
            activeIsLayerBoss = false;
        }

        public bool HasActiveBattleNode =>
            activeIsLayerBoss
                ? activeLayer > 0
                : activeLayer > 0 && activeChapter > 0 && activeLevel > 0;

        public void SetActiveLevel(int layer, int chapter, int level)
        {
            activeLayer = layer;
            activeChapter = chapter;
            activeLevel = level;
            activeIsLayerBoss = false;
        }

        public void SetActiveLayerBoss(int layer)
        {
            activeLayer = layer;
            activeChapter = 0;
            activeLevel = 0;
            activeIsLayerBoss = true;
        }

        public void ClearActiveBattleNode()
        {
            activeLayer = 0;
            activeChapter = 0;
            activeLevel = 0;
            activeIsLayerBoss = false;
        }

        public bool IsLevelUnlocked(int layer, int chapter, int level)
        {
            if (layer < unlockedLayer)
            {
                return true;
            }

            if (layer > unlockedLayer)
            {
                return false;
            }

            if (chapter < unlockedChapter)
            {
                return true;
            }

            if (chapter > unlockedChapter)
            {
                return false;
            }

            return level <= unlockedLevel;
        }

        /// <summary>Layer boss node is fightable when a chapter-10 clear is waiting on it.</summary>
        public bool IsLayerBossUnlocked(int layer)
        {
            return layer > 0
                   && layer == unlockedLayer
                   && pendingLayerBossChapter > 0;
        }

        public bool IsLayerBossCleared(int layer)
        {
            return layer > 0 && layer <= layerBossClearedUpTo;
        }

        /// <summary>True if LB for this chapter (or the whole layer) was already beaten.</summary>
        public bool HasClearedLayerBossForChapter(int chapter)
        {
            if (chapter <= 0)
            {
                return false;
            }

            return layerBossClearedChapterUpTo >= chapter || layerBossClearedUpTo > 0;
        }
    }
}
