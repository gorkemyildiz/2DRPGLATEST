using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewLayer", menuName = "Game Data/Map/Layer")]
    public class LayerData : ScriptableObject
    {
        [Header("Identity")]
        public string layerId = "layer_01";
        public string displayName = "Layer 1";
        public int layerIndex = 1;

        [Header("Chapters")]
        public List<ChapterData> chapters = new List<ChapterData>();

        [Header("Layer Boss")]
        public StageData layerBossBattle;
        public string layerBossDisplayName = "Layer Boss";
        public ChestData layerBossChest;

        [Header("Entry")]
        [Tooltip("When true, entering the layer boss consumes one Summon Stone.")]
        public bool requireSummonStone;

        public ChapterData GetChapter(int chapterIndex)
        {
            if (chapters == null)
            {
                return null;
            }

            for (int i = 0; i < chapters.Count; i++)
            {
                ChapterData chapter = chapters[i];
                if (chapter != null && chapter.chapterIndex == chapterIndex)
                {
                    return chapter;
                }
            }

            return null;
        }

        public int ChapterCount => chapters != null ? chapters.Count : 0;
    }
}
