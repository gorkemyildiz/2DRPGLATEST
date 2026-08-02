using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "SkillCatalog", menuName = "Game Data/Build/Skill Catalog")]
    public class SkillCatalog : ScriptableObject
    {
        public List<SkillData> skills = new List<SkillData>();

        [Header("Slot Unlock Levels")]
        public int activeSlot1UnlockLevel = 1;
        public int activeSlot2UnlockLevel = 5;
        public int passiveSlotUnlockLevel = 10;

        public SkillData GetById(string skillId)
        {
            if (skills == null || string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null && skills[i].skillId == skillId)
                {
                    return skills[i];
                }
            }

            return null;
        }

        public List<SkillData> GetByKind(SkillKind kind)
        {
            List<SkillData> result = new List<SkillData>();
            if (skills == null)
            {
                return result;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null && skills[i].kind == kind)
                {
                    result.Add(skills[i]);
                }
            }

            return result;
        }
    }
}
