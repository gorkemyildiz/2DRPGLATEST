using UnityEngine;

namespace Game.Data
{
    public enum RuneBranch
    {
        Defense = 0,
        Attack = 1,
        Utility = 2
    }

    [CreateAssetMenu(fileName = "NewRuneNode", menuName = "Game Data/Build/Rune Node")]
    public class RuneNodeData : ScriptableObject
    {
        [Header("Identity")]
        public string runeId = "rune_atk_1";
        public string displayName = "Strike";
        public RuneBranch branch = RuneBranch.Attack;

        [Header("Requirements")]
        public int requiredLevel = 1;
        public int maxRank = 5;

        [Header("Bonuses Per Rank")]
        public int attackBonusPerRank;
        public int healthBonusPerRank;
        [Tooltip("Added to skill cooldown reduction (0.02 = 2% per rank).")]
        [Range(0f, 0.2f)]
        public float cooldownReducePercentPerRank;
    }
}
