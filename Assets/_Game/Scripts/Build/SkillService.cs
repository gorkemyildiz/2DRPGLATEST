using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Build
{
    public enum SkillSlot
    {
        Active1 = 0,
        Active2 = 1,
        Passive = 2
    }

    public class SkillService : MonoBehaviour
    {
        [SerializeField] private Game.Data.SkillCatalog skillCatalog;
        [SerializeField] private Game.Rewards.RewardService rewardService;

        public event Action Changed;

        public Game.Data.SkillCatalog Catalog => skillCatalog;

        public void SetCatalog(Game.Data.SkillCatalog catalog)
        {
            if (catalog != null)
            {
                skillCatalog = catalog;
            }
        }

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            ResolveRefs();
        }

        private void ResolveRefs()
        {
            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }
        }

        public int GetCharacterLevel()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (rewardService != null && rewardService.Progress != null)
            {
                return Mathf.Max(1, rewardService.Progress.level);
            }

            return Mathf.Max(1, Game.Core.RuntimePlayerState.Progress.level);
        }

        public int GetSlotUnlockLevel(SkillSlot slot)
        {
            if (skillCatalog == null)
            {
                return slot == SkillSlot.Active1 ? 1 : (slot == SkillSlot.Active2 ? 5 : 10);
            }

            switch (slot)
            {
                case SkillSlot.Active1: return Mathf.Max(1, skillCatalog.activeSlot1UnlockLevel);
                case SkillSlot.Active2: return Mathf.Max(1, skillCatalog.activeSlot2UnlockLevel);
                default: return Mathf.Max(1, skillCatalog.passiveSlotUnlockLevel);
            }
        }

        public bool IsSlotUnlocked(SkillSlot slot)
        {
            return GetCharacterLevel() >= GetSlotUnlockLevel(slot);
        }

        public string GetEquippedId(SkillSlot slot)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.ActiveBuild;
            if (progress == null)
            {
                return null;
            }

            switch (slot)
            {
                case SkillSlot.Active1: return progress.activeSkillId1;
                case SkillSlot.Active2: return progress.activeSkillId2;
                default: return progress.passiveSkillId;
            }
        }

        public Game.Data.SkillData GetEquipped(SkillSlot slot)
        {
            return skillCatalog != null ? skillCatalog.GetById(GetEquippedId(slot)) : null;
        }

        public bool CanEquip(string skillId, SkillSlot slot, out string failReason)
        {
            failReason = null;
            if (skillCatalog == null)
            {
                failReason = "Skill catalog missing";
                return false;
            }

            if (!IsSlotUnlocked(slot))
            {
                failReason = $"Slot unlocks at level {GetSlotUnlockLevel(slot)}";
                return false;
            }

            Game.Data.SkillData skill = skillCatalog.GetById(skillId);
            if (skill == null)
            {
                failReason = "Unknown skill";
                return false;
            }

            if (GetCharacterLevel() < skill.requiredLevel)
            {
                failReason = $"Requires level {skill.requiredLevel}";
                return false;
            }

            bool wantActive = slot == SkillSlot.Active1 || slot == SkillSlot.Active2;
            if (wantActive && skill.kind != Game.Data.SkillKind.Active)
            {
                failReason = "Active slot needs an active skill";
                return false;
            }

            if (!wantActive && skill.kind != Game.Data.SkillKind.Passive)
            {
                failReason = "Passive slot needs a passive skill";
                return false;
            }

            Game.Roster.RosterService roster = FindFirstObjectByType<Game.Roster.RosterService>();
            if (roster != null && !roster.AllowsSkill(skillId))
            {
                failReason = "Skill not available for this class";
                return false;
            }

            return true;
        }

        public bool TryEquip(string skillId, SkillSlot slot)
        {
            if (!CanEquip(skillId, slot, out string reason))
            {
                Debug.Log($"[SkillService] Equip blocked: {reason}");
                return false;
            }

            // Unequip from other slots if same skill id.
            ClearSkillFromOtherSlots(skillId, slot);
            SetEquippedId(slot, skillId);
            Changed?.Invoke();
            return true;
        }

        public bool TryUnequip(SkillSlot slot)
        {
            if (string.IsNullOrEmpty(GetEquippedId(slot)))
            {
                return false;
            }

            SetEquippedId(slot, null);
            Changed?.Invoke();
            return true;
        }

        public List<Game.Data.SkillData> GetEquippedActives()
        {
            List<Game.Data.SkillData> actives = new List<Game.Data.SkillData>();
            Game.Data.SkillData a1 = GetEquipped(SkillSlot.Active1);
            Game.Data.SkillData a2 = GetEquipped(SkillSlot.Active2);
            if (a1 != null)
            {
                actives.Add(a1);
            }

            if (a2 != null)
            {
                actives.Add(a2);
            }

            return actives;
        }

        public int GetPassiveAttackBonus()
        {
            Game.Data.SkillData passive = GetEquipped(SkillSlot.Passive);
            return passive != null ? Mathf.Max(0, passive.attackBonus) : 0;
        }

        public int GetPassiveHealthBonus()
        {
            Game.Data.SkillData passive = GetEquipped(SkillSlot.Passive);
            return passive != null ? Mathf.Max(0, passive.healthBonus) : 0;
        }

        public int GetPassiveAttackBonusForClass(string classId)
        {
            return GetPassiveBonusForClass(classId, atk: true);
        }

        public int GetPassiveHealthBonusForClass(string classId)
        {
            return GetPassiveBonusForClass(classId, atk: false);
        }

        public List<Game.Data.SkillData> GetEquippedActivesForClass(string classId)
        {
            List<Game.Data.SkillData> actives = new List<Game.Data.SkillData>();
            if (string.IsNullOrEmpty(classId) || skillCatalog == null)
            {
                return actives;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.RosterProgress.GetOrCreateBuild(classId);
            AddActive(actives, progress.activeSkillId1);
            AddActive(actives, progress.activeSkillId2);
            return actives;
        }

        private void AddActive(List<Game.Data.SkillData> actives, string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            Game.Data.SkillData skill = skillCatalog.GetById(skillId);
            if (skill != null)
            {
                actives.Add(skill);
            }
        }

        private int GetPassiveBonusForClass(string classId, bool atk)
        {
            if (string.IsNullOrEmpty(classId) || skillCatalog == null)
            {
                return 0;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.RosterProgress.GetOrCreateBuild(classId);
            if (string.IsNullOrEmpty(progress.passiveSkillId))
            {
                return 0;
            }

            Game.Data.SkillData passive = skillCatalog.GetById(progress.passiveSkillId);
            if (passive == null)
            {
                return 0;
            }

            return atk ? Mathf.Max(0, passive.attackBonus) : Mathf.Max(0, passive.healthBonus);
        }

        private void ClearSkillFromOtherSlots(string skillId, SkillSlot keep)
        {
            if (GetEquippedId(SkillSlot.Active1) == skillId && keep != SkillSlot.Active1)
            {
                SetEquippedId(SkillSlot.Active1, null);
            }

            if (GetEquippedId(SkillSlot.Active2) == skillId && keep != SkillSlot.Active2)
            {
                SetEquippedId(SkillSlot.Active2, null);
            }

            if (GetEquippedId(SkillSlot.Passive) == skillId && keep != SkillSlot.Passive)
            {
                SetEquippedId(SkillSlot.Passive, null);
            }
        }

        private void SetEquippedId(SkillSlot slot, string skillId)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.BuildProgress progress = Game.Core.RuntimePlayerState.ActiveBuild;
            if (progress == null)
            {
                return;
            }

            switch (slot)
            {
                case SkillSlot.Active1:
                    progress.activeSkillId1 = skillId;
                    break;
                case SkillSlot.Active2:
                    progress.activeSkillId2 = skillId;
                    break;
                default:
                    progress.passiveSkillId = skillId;
                    break;
            }
        }
    }
}
