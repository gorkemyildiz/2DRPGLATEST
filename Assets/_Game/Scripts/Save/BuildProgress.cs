using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class RuneRankSave
    {
        public string runeId;
        public int rank;
    }

    [Serializable]
    public class BuildProgress
    {
        public List<RuneRankSave> runeRanks = new List<RuneRankSave>();
        public string activeSkillId1;
        public string activeSkillId2;
        public string passiveSkillId;

        public int GetRank(string runeId)
        {
            if (runeRanks == null || string.IsNullOrEmpty(runeId))
            {
                return 0;
            }

            for (int i = 0; i < runeRanks.Count; i++)
            {
                RuneRankSave entry = runeRanks[i];
                if (entry != null && entry.runeId == runeId)
                {
                    return Math.Max(0, entry.rank);
                }
            }

            return 0;
        }

        public void SetRank(string runeId, int rank)
        {
            if (string.IsNullOrEmpty(runeId))
            {
                return;
            }

            if (runeRanks == null)
            {
                runeRanks = new List<RuneRankSave>();
            }

            for (int i = 0; i < runeRanks.Count; i++)
            {
                if (runeRanks[i] != null && runeRanks[i].runeId == runeId)
                {
                    if (rank <= 0)
                    {
                        runeRanks.RemoveAt(i);
                    }
                    else
                    {
                        runeRanks[i].rank = rank;
                    }

                    return;
                }
            }

            if (rank > 0)
            {
                runeRanks.Add(new RuneRankSave { runeId = runeId, rank = rank });
            }
        }

        public BuildProgress Clone()
        {
            BuildProgress copy = new BuildProgress
            {
                activeSkillId1 = activeSkillId1,
                activeSkillId2 = activeSkillId2,
                passiveSkillId = passiveSkillId,
                runeRanks = new List<RuneRankSave>()
            };

            if (runeRanks != null)
            {
                for (int i = 0; i < runeRanks.Count; i++)
                {
                    RuneRankSave src = runeRanks[i];
                    if (src == null || string.IsNullOrEmpty(src.runeId) || src.rank <= 0)
                    {
                        continue;
                    }

                    copy.runeRanks.Add(new RuneRankSave
                    {
                        runeId = src.runeId,
                        rank = src.rank
                    });
                }
            }

            return copy;
        }

        public void CopyFrom(BuildProgress other)
        {
            if (other == null)
            {
                runeRanks = new List<RuneRankSave>();
                activeSkillId1 = null;
                activeSkillId2 = null;
                passiveSkillId = null;
                return;
            }

            BuildProgress clone = other.Clone();
            runeRanks = clone.runeRanks;
            activeSkillId1 = clone.activeSkillId1;
            activeSkillId2 = clone.activeSkillId2;
            passiveSkillId = clone.passiveSkillId;
        }
    }
}
