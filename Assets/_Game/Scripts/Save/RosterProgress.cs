using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class ClassBuildSave
    {
        public string classId;
        public BuildProgress build = new BuildProgress();
    }

    [Serializable]
    public class RosterProgress
    {
        public string selectedClassId = "fighter";
        public List<string> unlockedClassIds = new List<string>();
        public List<ClassBuildSave> classBuilds = new List<ClassBuildSave>();

        // Party: unlockedPartySlots starts at 1; partyClassIds[i] is class in slot i (empty = vacant).
        public int unlockedPartySlots = 1;
        public List<string> partyClassIds = new List<string>();

        public const int MaxPartySlots = 3;

        public void EnsureDefaults(string defaultClassId = "fighter")
        {
            if (string.IsNullOrEmpty(selectedClassId))
            {
                selectedClassId = defaultClassId;
            }

            if (unlockedClassIds == null)
            {
                unlockedClassIds = new List<string>();
            }

            if (classBuilds == null)
            {
                classBuilds = new List<ClassBuildSave>();
            }

            if (!unlockedClassIds.Contains(selectedClassId))
            {
                unlockedClassIds.Add(selectedClassId);
            }

            EnsureBuildSlot(selectedClassId);
            EnsurePartyDefaults(defaultClassId);
        }

        public void EnsurePartyDefaults(string defaultClassId = "fighter")
        {
            unlockedPartySlots = Math.Max(1, Math.Min(MaxPartySlots, unlockedPartySlots <= 0 ? 1 : unlockedPartySlots));

            if (partyClassIds == null)
            {
                partyClassIds = new List<string>();
            }

            while (partyClassIds.Count < MaxPartySlots)
            {
                partyClassIds.Add(string.Empty);
            }

            if (partyClassIds.Count > MaxPartySlots)
            {
                partyClassIds.RemoveRange(MaxPartySlots, partyClassIds.Count - MaxPartySlots);
            }

            if (string.IsNullOrEmpty(partyClassIds[0]))
            {
                partyClassIds[0] = string.IsNullOrEmpty(selectedClassId) ? defaultClassId : selectedClassId;
            }

            if (string.IsNullOrEmpty(selectedClassId))
            {
                selectedClassId = partyClassIds[0];
            }
        }

        public bool IsPartySlotUnlocked(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < unlockedPartySlots;
        }

        public string GetPartyClassId(int slotIndex)
        {
            EnsurePartyDefaults();
            if (slotIndex < 0 || slotIndex >= partyClassIds.Count)
            {
                return string.Empty;
            }

            return partyClassIds[slotIndex] ?? string.Empty;
        }

        public void SetPartyClassId(int slotIndex, string classId)
        {
            EnsurePartyDefaults();
            if (slotIndex < 0 || slotIndex >= MaxPartySlots || !IsPartySlotUnlocked(slotIndex))
            {
                return;
            }

            partyClassIds[slotIndex] = classId ?? string.Empty;
        }

        public bool IsUnlocked(string classId)
        {
            return !string.IsNullOrEmpty(classId) && unlockedClassIds != null && unlockedClassIds.Contains(classId);
        }

        public void Unlock(string classId)
        {
            if (string.IsNullOrEmpty(classId))
            {
                return;
            }

            if (unlockedClassIds == null)
            {
                unlockedClassIds = new List<string>();
            }

            if (!unlockedClassIds.Contains(classId))
            {
                unlockedClassIds.Add(classId);
            }

            EnsureBuildSlot(classId);
        }

        public BuildProgress GetOrCreateBuild(string classId)
        {
            EnsureBuildSlot(classId);
            for (int i = 0; i < classBuilds.Count; i++)
            {
                if (classBuilds[i] != null && classBuilds[i].classId == classId)
                {
                    if (classBuilds[i].build == null)
                    {
                        classBuilds[i].build = new BuildProgress();
                    }

                    return classBuilds[i].build;
                }
            }

            return new BuildProgress();
        }

        public void EnsureBuildSlot(string classId)
        {
            if (string.IsNullOrEmpty(classId))
            {
                return;
            }

            if (classBuilds == null)
            {
                classBuilds = new List<ClassBuildSave>();
            }

            for (int i = 0; i < classBuilds.Count; i++)
            {
                if (classBuilds[i] != null && classBuilds[i].classId == classId)
                {
                    if (classBuilds[i].build == null)
                    {
                        classBuilds[i].build = new BuildProgress();
                    }

                    return;
                }
            }

            classBuilds.Add(new ClassBuildSave
            {
                classId = classId,
                build = new BuildProgress()
            });
        }

        public RosterProgress Clone()
        {
            RosterProgress copy = new RosterProgress
            {
                selectedClassId = selectedClassId,
                unlockedClassIds = new List<string>(),
                classBuilds = new List<ClassBuildSave>(),
                unlockedPartySlots = unlockedPartySlots,
                partyClassIds = new List<string>()
            };

            if (unlockedClassIds != null)
            {
                copy.unlockedClassIds.AddRange(unlockedClassIds);
            }

            if (classBuilds != null)
            {
                for (int i = 0; i < classBuilds.Count; i++)
                {
                    ClassBuildSave src = classBuilds[i];
                    if (src == null || string.IsNullOrEmpty(src.classId))
                    {
                        continue;
                    }

                    copy.classBuilds.Add(new ClassBuildSave
                    {
                        classId = src.classId,
                        build = src.build != null ? src.build.Clone() : new BuildProgress()
                    });
                }
            }

            if (partyClassIds != null)
            {
                copy.partyClassIds.AddRange(partyClassIds);
            }

            copy.EnsurePartyDefaults(selectedClassId);
            return copy;
        }

        public void CopyFrom(RosterProgress other)
        {
            if (other == null)
            {
                selectedClassId = "fighter";
                unlockedClassIds = new List<string> { "fighter" };
                classBuilds = new List<ClassBuildSave>();
                unlockedPartySlots = 1;
                partyClassIds = new List<string> { "fighter", string.Empty, string.Empty };
                EnsureDefaults();
                return;
            }

            RosterProgress clone = other.Clone();
            selectedClassId = clone.selectedClassId;
            unlockedClassIds = clone.unlockedClassIds;
            classBuilds = clone.classBuilds;
            unlockedPartySlots = clone.unlockedPartySlots;
            partyClassIds = clone.partyClassIds;
            EnsureDefaults(selectedClassId);
        }
    }

    [Serializable]
    public class WeeklyDungeonProgress
    {
        public string weekKey = string.Empty;
        public int entriesUsed;
        public string lastClaimWeekKey = string.Empty;

        public WeeklyDungeonProgress Clone()
        {
            return new WeeklyDungeonProgress
            {
                weekKey = weekKey,
                entriesUsed = entriesUsed,
                lastClaimWeekKey = lastClaimWeekKey
            };
        }

        public void CopyFrom(WeeklyDungeonProgress other)
        {
            if (other == null)
            {
                weekKey = string.Empty;
                entriesUsed = 0;
                lastClaimWeekKey = string.Empty;
                return;
            }

            weekKey = other.weekKey ?? string.Empty;
            entriesUsed = Math.Max(0, other.entriesUsed);
            lastClaimWeekKey = other.lastClaimWeekKey ?? string.Empty;
        }
    }
}
