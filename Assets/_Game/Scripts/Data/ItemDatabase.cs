using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game Data/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;

        public ItemData GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || items == null)
            {
                return null;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                if (item != null && item.itemId == itemId)
                {
                    return item;
                }
            }

            return null;
        }

        public void SetItems(List<ItemData> newItems)
        {
            items = newItems ?? new List<ItemData>();
        }
    }
}
