using UnityEngine;

namespace Game.Data
{
    public enum SkillKind
    {
        Active = 0,
        Passive = 1
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "Game Data/Build/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "skill_power_strike";
        public string displayName = "Power Strike";
        public SkillKind kind = SkillKind.Active;

        [Header("Requirements")]
        public int requiredLevel = 1;

        [Header("Active")]
        [Tooltip("Seconds between auto-casts.")]
        public float cooldown = 6f;
        [Tooltip("Damage = hero ATK * this multiplier.")]
        public float damageMultiplier = 1.8f;

        [Header("Passive")]
        public int attackBonus;
        public int healthBonus;
    }
}
