using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewChapter", menuName = "Game Data/Map/Chapter")]
    public class ChapterData : ScriptableObject
    {
        [Header("Identity")]
        public string chapterId = "L1_C1";
        public string displayName = "Chapter 1";
        public int layerIndex = 1;
        public int chapterIndex = 1;

        [Header("Levels (10)")]
        public List<LevelData> levels = new List<LevelData>();

        public LevelData GetLevel(int levelIndex)
        {
            if (levels == null)
            {
                return null;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                LevelData level = levels[i];
                if (level != null && level.levelIndex == levelIndex)
                {
                    return level;
                }
            }

            return null;
        }
    }
}
