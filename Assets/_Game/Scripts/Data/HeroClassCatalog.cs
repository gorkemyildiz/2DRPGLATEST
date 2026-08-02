using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "HeroClassCatalog", menuName = "Game Data/Roster/Hero Class Catalog")]
    public class HeroClassCatalog : ScriptableObject
    {
        public string defaultClassId = "fighter";
        public List<HeroClassData> classes = new List<HeroClassData>();

        [Header("Party Slots (editable later)")]
        [Tooltip("Max heroes that can fight together.")]
        public int maxPartySlots = 3;
        [Tooltip("Gold to unlock each slot. Index 0 is free/always open. Placeholder costs.")]
        public int[] partySlotGoldCosts = { 0, 1000, 5000 };

        [Header("Demo")]
        [Tooltip("If true, all classes unlock immediately.")]
        public bool unlockAllForDemo;

        public int GetMaxPartySlots()
        {
            return Mathf.Clamp(maxPartySlots, 1, 3);
        }

        public int GetPartySlotUnlockCost(int slotIndex)
        {
            if (slotIndex <= 0)
            {
                return 0;
            }

            if (partySlotGoldCosts == null || slotIndex >= partySlotGoldCosts.Length)
            {
                return slotIndex == 1 ? 1000 : 5000;
            }

            return Mathf.Max(0, partySlotGoldCosts[slotIndex]);
        }

        public HeroClassData GetById(string classId)
        {
            if (classes == null || string.IsNullOrEmpty(classId))
            {
                return null;
            }

            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i] != null && classes[i].classId == classId)
                {
                    return classes[i];
                }
            }

            return null;
        }

        public HeroClassData GetDefault()
        {
            HeroClassData def = GetById(defaultClassId);
            if (def != null)
            {
                return def;
            }

            return classes != null && classes.Count > 0 ? classes[0] : null;
        }

        public bool AllowsSkill(string classId, string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            HeroClassData data = GetById(classId);
            if (data == null)
            {
                return true;
            }

            if (data.allowedSkillIds == null || data.allowedSkillIds.Count == 0)
            {
                return true;
            }

            return data.allowedSkillIds.Contains(skillId);
        }
    }
}
