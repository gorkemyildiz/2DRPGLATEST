using System;
using System.Collections.Generic;

namespace Game.Rewards
{
    [Serializable]
    public class PlayerProgress
    {
        public int level = 1;
        public int currentExperience = 0;
        public int gold = 0;
        public int totalEnemiesKilled = 0;
        public int highestCompletedStage = 0;
        public List<string> ownedItemIds = new List<string>();

        public PlayerProgress()
        {
            level = 1;
            currentExperience = 0;
            gold = 0;
            totalEnemiesKilled = 0;
            highestCompletedStage = 0;
            ownedItemIds = new List<string>();
        }
    }
}
