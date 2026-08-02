using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Roster
{
    public class RosterService : MonoBehaviour
    {
        [SerializeField] private Game.Data.HeroClassCatalog classCatalog;
        [SerializeField] private Game.Rewards.RewardService rewardService;

        public event Action ClassChanged;
        public event Action RosterChanged;

        public Game.Data.HeroClassCatalog Catalog => classCatalog;

        public void SetCatalog(Game.Data.HeroClassCatalog catalog)
        {
            if (catalog != null)
            {
                classCatalog = catalog;
            }
        }

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            ResolveRefs();
            if (classCatalog == null)
            {
#if UNITY_EDITOR
                classCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Data.HeroClassCatalog>(
                    "Assets/_Game/ScriptableObjects/Roster/HeroClassCatalog.asset");
#endif
            }

            ApplyDemoUnlocks();
            Game.Core.RuntimePlayerState.SyncActiveBuildFromRoster();
        }

        private void ResolveRefs()
        {
            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }
        }

        public Game.Data.HeroClassData GetActiveClass()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            string id = Game.Core.RuntimePlayerState.RosterProgress.selectedClassId;
            Game.Data.HeroClassData data = classCatalog != null ? classCatalog.GetById(id) : null;
            return data != null ? data : (classCatalog != null ? classCatalog.GetDefault() : null);
        }

        public Game.Data.HeroClassData GetClass(string classId)
        {
            return classCatalog != null ? classCatalog.GetById(classId) : null;
        }

        public string GetSelectedClassId()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            return Game.Core.RuntimePlayerState.RosterProgress.selectedClassId;
        }

        public int GetMaxPartySlots()
        {
            return classCatalog != null
                ? classCatalog.GetMaxPartySlots()
                : Game.Save.RosterProgress.MaxPartySlots;
        }

        public int GetUnlockedPartySlots()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Core.RuntimePlayerState.RosterProgress.EnsurePartyDefaults(
                classCatalog != null ? classCatalog.defaultClassId : "fighter");
            return Game.Core.RuntimePlayerState.RosterProgress.unlockedPartySlots;
        }

        public int GetPartySlotUnlockCost(int slotIndex)
        {
            return classCatalog != null ? classCatalog.GetPartySlotUnlockCost(slotIndex) : (slotIndex == 1 ? 1000 : 5000);
        }

        public string GetPartyClassId(int slotIndex)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            return Game.Core.RuntimePlayerState.RosterProgress.GetPartyClassId(slotIndex);
        }

        public List<string> GetFilledPartyClassIds()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.RosterProgress progress = Game.Core.RuntimePlayerState.RosterProgress;
            progress.EnsurePartyDefaults(classCatalog != null ? classCatalog.defaultClassId : "fighter");

            List<string> filled = new List<string>();
            int unlocked = Mathf.Min(progress.unlockedPartySlots, GetMaxPartySlots());
            for (int i = 0; i < unlocked; i++)
            {
                string id = progress.GetPartyClassId(i);
                if (!string.IsNullOrEmpty(id))
                {
                    filled.Add(id);
                }
            }

            if (filled.Count == 0)
            {
                string fallback = progress.selectedClassId;
                if (string.IsNullOrEmpty(fallback) && classCatalog != null)
                {
                    fallback = classCatalog.defaultClassId;
                }

                if (!string.IsNullOrEmpty(fallback))
                {
                    filled.Add(fallback);
                }
            }

            return filled;
        }

        public bool IsPartySlotUnlocked(int slotIndex)
        {
            return GetUnlockedPartySlots() > slotIndex && slotIndex >= 0;
        }

        public bool CanUnlockNextPartySlot(out string reason)
        {
            reason = null;
            int unlocked = GetUnlockedPartySlots();
            int max = GetMaxPartySlots();
            if (unlocked >= max)
            {
                reason = "All party slots unlocked";
                return false;
            }

            int cost = GetPartySlotUnlockCost(unlocked);
            int gold = GetGold();
            if (gold < cost)
            {
                reason = $"Needs {cost} gold";
                return false;
            }

            return true;
        }

        public bool TryUnlockNextPartySlot()
        {
            if (!CanUnlockNextPartySlot(out string reason))
            {
                Debug.Log($"[RosterService] Party slot unlock blocked: {reason}");
                return false;
            }

            int nextIndex = GetUnlockedPartySlots();
            int cost = GetPartySlotUnlockCost(nextIndex);
            if (cost > 0)
            {
                SpendGold(cost);
            }

            Game.Core.RuntimePlayerState.RosterProgress.unlockedPartySlots = nextIndex + 1;
            Game.Core.RuntimePlayerState.RosterProgress.EnsurePartyDefaults(
                classCatalog != null ? classCatalog.defaultClassId : "fighter");
            RosterChanged?.Invoke();
            return true;
        }

        public bool CanAssignToPartySlot(int slotIndex, string classId, out string reason)
        {
            reason = null;
            if (!IsPartySlotUnlocked(slotIndex))
            {
                reason = "Slot locked";
                return false;
            }

            if (string.IsNullOrEmpty(classId))
            {
                reason = "No class";
                return false;
            }

            if (!IsUnlocked(classId))
            {
                reason = "Class locked";
                return false;
            }

            if (classCatalog != null && classCatalog.GetById(classId) == null)
            {
                reason = "Unknown class";
                return false;
            }

            // Unique class in party (MVP).
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.RosterProgress progress = Game.Core.RuntimePlayerState.RosterProgress;
            progress.EnsurePartyDefaults();
            for (int i = 0; i < progress.unlockedPartySlots; i++)
            {
                if (i == slotIndex)
                {
                    continue;
                }

                if (progress.GetPartyClassId(i) == classId)
                {
                    reason = "Already in party";
                    return false;
                }
            }

            return true;
        }

        public bool TryAssignToPartySlot(int slotIndex, string classId)
        {
            if (!CanAssignToPartySlot(slotIndex, classId, out string reason))
            {
                Debug.Log($"[RosterService] Assign blocked: {reason}");
                return false;
            }

            Game.Core.RuntimePlayerState.RosterProgress.SetPartyClassId(slotIndex, classId);

            // Keep build-edit focus on assigned class.
            Game.Core.RuntimePlayerState.PushActiveBuildIntoRoster();
            Game.Core.RuntimePlayerState.RosterProgress.selectedClassId = classId;
            Game.Core.RuntimePlayerState.SyncActiveBuildFromRoster();

            ClassChanged?.Invoke();
            RosterChanged?.Invoke();
            return true;
        }

        public bool TryClearPartySlot(int slotIndex)
        {
            if (slotIndex <= 0)
            {
                Debug.Log("[RosterService] Slot 0 cannot be cleared.");
                return false;
            }

            if (!IsPartySlotUnlocked(slotIndex))
            {
                return false;
            }

            Game.Core.RuntimePlayerState.RosterProgress.SetPartyClassId(slotIndex, string.Empty);
            RosterChanged?.Invoke();
            return true;
        }

        public bool IsUnlocked(string classId)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (classCatalog != null && classCatalog.unlockAllForDemo)
            {
                return true;
            }

            return Game.Core.RuntimePlayerState.RosterProgress.IsUnlocked(classId);
        }

        public bool CanUnlock(string classId, out string reason)
        {
            reason = null;
            if (classCatalog == null)
            {
                reason = "Class catalog missing";
                return false;
            }

            Game.Data.HeroClassData data = classCatalog.GetById(classId);
            if (data == null)
            {
                reason = "Unknown class";
                return false;
            }

            if (IsUnlocked(classId))
            {
                reason = "Already unlocked";
                return false;
            }

            int level = GetPlayerLevel();
            if (level < data.unlockLevel)
            {
                reason = $"Requires level {data.unlockLevel}";
                return false;
            }

            if (GetGold() < data.unlockGoldCost)
            {
                reason = $"Needs {data.unlockGoldCost} gold";
                return false;
            }

            return true;
        }

        public bool TryUnlock(string classId)
        {
            if (!CanUnlock(classId, out string reason))
            {
                Debug.Log($"[RosterService] Unlock blocked: {reason}");
                return false;
            }

            Game.Data.HeroClassData data = classCatalog.GetById(classId);
            if (data.unlockGoldCost > 0)
            {
                SpendGold(data.unlockGoldCost);
            }

            Game.Core.RuntimePlayerState.RosterProgress.Unlock(classId);
            RosterChanged?.Invoke();
            return true;
        }

        public bool TrySelect(string classId)
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (string.IsNullOrEmpty(classId))
            {
                return false;
            }

            if (!IsUnlocked(classId))
            {
                Debug.Log($"[RosterService] Class locked: {classId}");
                return false;
            }

            if (classCatalog != null && classCatalog.GetById(classId) == null)
            {
                return false;
            }

            // Prefer existing party slot for this class, else first empty unlocked slot, else slot 0.
            Game.Save.RosterProgress progress = Game.Core.RuntimePlayerState.RosterProgress;
            progress.EnsurePartyDefaults(classCatalog != null ? classCatalog.defaultClassId : "fighter");
            int targetSlot = 0;
            bool foundExisting = false;
            for (int i = 0; i < progress.unlockedPartySlots; i++)
            {
                if (progress.GetPartyClassId(i) == classId)
                {
                    targetSlot = i;
                    foundExisting = true;
                    break;
                }
            }

            if (!foundExisting)
            {
                for (int i = 0; i < progress.unlockedPartySlots; i++)
                {
                    if (string.IsNullOrEmpty(progress.GetPartyClassId(i)))
                    {
                        targetSlot = i;
                        break;
                    }
                }
            }

            return TryAssignToPartySlot(targetSlot, classId);
        }

        public bool AllowsSkill(string skillId)
        {
            return classCatalog == null || classCatalog.AllowsSkill(GetSelectedClassId(), skillId);
        }

        private void ApplyDemoUnlocks()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Core.RuntimePlayerState.RosterProgress.EnsureDefaults(
                classCatalog != null ? classCatalog.defaultClassId : "fighter");

            if (classCatalog == null || classCatalog.classes == null)
            {
                return;
            }

            if (classCatalog.unlockAllForDemo)
            {
                for (int i = 0; i < classCatalog.classes.Count; i++)
                {
                    if (classCatalog.classes[i] != null)
                    {
                        Game.Core.RuntimePlayerState.RosterProgress.Unlock(classCatalog.classes[i].classId);
                    }
                }
            }
        }

        private int GetPlayerLevel()
        {
            if (rewardService != null && rewardService.Progress != null)
            {
                return Mathf.Max(1, rewardService.Progress.level);
            }

            return Mathf.Max(1, Game.Core.RuntimePlayerState.Progress.level);
        }

        private int GetGold()
        {
            if (rewardService != null && rewardService.Progress != null)
            {
                return rewardService.Progress.gold;
            }

            return Game.Core.RuntimePlayerState.Progress.gold;
        }

        private void SpendGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (rewardService != null && rewardService.Progress != null)
            {
                rewardService.Progress.gold = Mathf.Max(0, rewardService.Progress.gold - amount);
                return;
            }

            Game.Core.RuntimePlayerState.Progress.gold =
                Mathf.Max(0, Game.Core.RuntimePlayerState.Progress.gold - amount);
        }
    }
}
