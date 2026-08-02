using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Save
{
    [Serializable]
    public class ItemInstanceSave
    {
        public string uid;
        public string itemId;
        public int stars;
        public List<string> enchantIds = new List<string>();

        // Timed buff stub (Phase 4 placeholder — combat apply later)
        public string timedBuffId;
        public string timedBuffExpiresUtc;

        public ItemInstanceSave()
        {
            enchantIds = new List<string>();
        }

        public ItemInstanceSave Clone()
        {
            ItemInstanceSave copy = new ItemInstanceSave
            {
                uid = uid,
                itemId = itemId,
                stars = stars,
                timedBuffId = timedBuffId,
                timedBuffExpiresUtc = timedBuffExpiresUtc,
                enchantIds = new List<string>()
            };

            if (enchantIds != null)
            {
                copy.enchantIds.AddRange(enchantIds);
            }

            return copy;
        }

        public string GetStackKey()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(itemId ?? "").Append('|').Append(stars).Append('|');
            if (enchantIds != null)
            {
                for (int i = 0; i < enchantIds.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(enchantIds[i] ?? "");
                }
            }

            if (!string.IsNullOrEmpty(timedBuffId))
            {
                sb.Append('|').Append(timedBuffId);
            }

            return sb.ToString();
        }

        public static ItemInstanceSave CreateNew(string itemId)
        {
            return new ItemInstanceSave
            {
                uid = Guid.NewGuid().ToString("N"),
                itemId = itemId,
                stars = 0,
                enchantIds = new List<string>()
            };
        }
    }
}
