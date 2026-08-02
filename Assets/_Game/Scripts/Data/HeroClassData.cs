using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewHeroClass", menuName = "Game Data/Roster/Hero Class")]
    public class HeroClassData : ScriptableObject
    {
        [Header("Identity")]
        public string classId = "fighter";
        public string displayName = "Fighter";

        [Header("Base Stats")]
        public int baseAttackDamage = 28;
        public int baseMaxHealth = 200;

        [Header("Unlock")]
        [Tooltip("Player level required to unlock (Fighter should be 1).")]
        public int unlockLevel = 1;
        public int unlockGoldCost;

        [Header("Skills")]
        [Tooltip("Skill ids this class may equip. Empty = all skills allowed.")]
        public List<string> allowedSkillIds = new List<string>();

        [Header("Visual")]
        [Tooltip("Optional. If null, hub/battle keep the default Hero prefab.")]
        public GameObject heroPrefab;
    }
}
