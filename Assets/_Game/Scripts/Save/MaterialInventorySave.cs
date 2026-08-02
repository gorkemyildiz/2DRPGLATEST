using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class MaterialStackSave
    {
        public string materialId;
        public int amount;
    }

    [Serializable]
    public class MaterialInventorySave
    {
        public List<MaterialStackSave> stacks = new List<MaterialStackSave>();

        public int GetAmount(string materialId)
        {
            if (stacks == null || string.IsNullOrEmpty(materialId))
            {
                return 0;
            }

            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].materialId == materialId)
                {
                    return System.Math.Max(0, stacks[i].amount);
                }
            }

            return 0;
        }

        public void SetAmount(string materialId, int amount)
        {
            if (string.IsNullOrEmpty(materialId))
            {
                return;
            }

            if (stacks == null)
            {
                stacks = new List<MaterialStackSave>();
            }

            amount = System.Math.Max(0, amount);
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].materialId == materialId)
                {
                    if (amount == 0)
                    {
                        stacks.RemoveAt(i);
                    }
                    else
                    {
                        stacks[i].amount = amount;
                    }

                    return;
                }
            }

            if (amount > 0)
            {
                stacks.Add(new MaterialStackSave { materialId = materialId, amount = amount });
            }
        }

        public void Add(string materialId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetAmount(materialId, GetAmount(materialId) + amount);
        }

        public bool TryConsume(string materialId, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            int have = GetAmount(materialId);
            if (have < amount)
            {
                return false;
            }

            SetAmount(materialId, have - amount);
            return true;
        }
    }
}
