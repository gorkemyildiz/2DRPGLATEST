using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewStage", menuName = "Game Data/Stage")]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        public string stageId = "stage_01";
        public string displayName = "Stage 1";
        public int stageIndex = 1;

        [Header("Waves")]
        public List<WaveData> waves = new List<WaveData>();

        [Header("Travel Settings")]
        public float travelDuration = 3f;
        public float delayAfterTravel = 1f;

        [Header("Completion Rewards")]
        public int completionGoldBonus = 50;
        public int completionExperienceBonus = 100;
    }
}
