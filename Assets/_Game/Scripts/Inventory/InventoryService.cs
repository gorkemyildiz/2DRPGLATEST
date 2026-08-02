using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    public class InventoryService : MonoBehaviour
    {
        private List<Game.Save.ItemInstanceSave> instances;

        public event Action InventoryChanged;

        /// <summary>Legacy view of base item ids (one entry per instance).</summary>
        public List<string> ItemIds
        {
            get
            {
                List<string> ids = new List<string>();
                EnsureList();
                for (int i = 0; i < instances.Count; i++)
                {
                    if (instances[i] != null && !string.IsNullOrEmpty(instances[i].itemId))
                    {
                        ids.Add(instances[i].itemId);
                    }
                }

                return ids;
            }
        }

        public IReadOnlyList<Game.Save.ItemInstanceSave> Instances
        {
            get
            {
                EnsureList();
                return instances;
            }
        }

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Core.RuntimePlayerState.MigrateInventoryToInstancesIfNeeded();
            instances = Game.Core.RuntimePlayerState.InventoryInstances;
        }

        private void EnsureList()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (instances == null)
            {
                instances = Game.Core.RuntimePlayerState.InventoryInstances;
            }
        }

        public Dictionary<string, int> GetItemCounts()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            EnsureList();
            for (int i = 0; i < instances.Count; i++)
            {
                Game.Save.ItemInstanceSave inst = instances[i];
                if (inst == null || string.IsNullOrEmpty(inst.itemId))
                {
                    continue;
                }

                if (counts.ContainsKey(inst.itemId))
                {
                    counts[inst.itemId]++;
                }
                else
                {
                    counts[inst.itemId] = 1;
                }
            }

            return counts;
        }

        public Dictionary<string, List<Game.Save.ItemInstanceSave>> GetStacksByKey()
        {
            Dictionary<string, List<Game.Save.ItemInstanceSave>> stacks = new Dictionary<string, List<Game.Save.ItemInstanceSave>>();
            EnsureList();
            for (int i = 0; i < instances.Count; i++)
            {
                Game.Save.ItemInstanceSave inst = instances[i];
                if (inst == null || string.IsNullOrEmpty(inst.itemId))
                {
                    continue;
                }

                string key = inst.GetStackKey();
                if (!stacks.TryGetValue(key, out List<Game.Save.ItemInstanceSave> list))
                {
                    list = new List<Game.Save.ItemInstanceSave>();
                    stacks[key] = list;
                }

                list.Add(inst);
            }

            return stacks;
        }

        public Game.Save.ItemInstanceSave AddItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("[InventoryService] AddItem called with null or empty itemId.");
                return null;
            }

            EnsureList();
            Game.Save.ItemInstanceSave created = Game.Save.ItemInstanceSave.CreateNew(itemId);
            instances.Add(created);
            SyncLegacyIds();
            InventoryChanged?.Invoke();
            Debug.Log($"[InventoryService] Item added: {itemId} uid={created.uid}. Total: {instances.Count}");
            return created;
        }

        public bool RemoveItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            EnsureList();
            // Prefer plain 0★ / no-enchant copy for craft/disassemble
            int best = -1;
            for (int i = 0; i < instances.Count; i++)
            {
                Game.Save.ItemInstanceSave inst = instances[i];
                if (inst == null || inst.itemId != itemId)
                {
                    continue;
                }

                bool plain = inst.stars <= 0 && (inst.enchantIds == null || inst.enchantIds.Count == 0);
                if (plain)
                {
                    best = i;
                    break;
                }

                if (best < 0)
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                Debug.LogWarning($"[InventoryService] Item not found: {itemId}");
                return false;
            }

            instances.RemoveAt(best);
            SyncLegacyIds();
            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveInstance(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return false;
            }

            EnsureList();
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null && instances[i].uid == uid)
                {
                    instances.RemoveAt(i);
                    SyncLegacyIds();
                    InventoryChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public Game.Save.ItemInstanceSave GetInstance(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return null;
            }

            EnsureList();
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null && instances[i].uid == uid)
                {
                    return instances[i];
                }
            }

            return null;
        }

        public Game.Save.ItemInstanceSave FindFirstByItemId(string itemId, bool preferPlain = true)
        {
            EnsureList();
            Game.Save.ItemInstanceSave fallback = null;
            for (int i = 0; i < instances.Count; i++)
            {
                Game.Save.ItemInstanceSave inst = instances[i];
                if (inst == null || inst.itemId != itemId)
                {
                    continue;
                }

                if (!preferPlain)
                {
                    return inst;
                }

                bool plain = inst.stars <= 0 && (inst.enchantIds == null || inst.enchantIds.Count == 0);
                if (plain)
                {
                    return inst;
                }

                if (fallback == null)
                {
                    fallback = inst;
                }
            }

            return fallback;
        }

        public bool HasItem(string itemId)
        {
            return GetItemCount(itemId) > 0;
        }

        public int GetItemCount(string itemId)
        {
            int count = 0;
            EnsureList();
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null && instances[i].itemId == itemId)
                {
                    count++;
                }
            }

            return count;
        }

        public void NotifyChanged()
        {
            SyncLegacyIds();
            InventoryChanged?.Invoke();
        }

        public void ClearInventory()
        {
            EnsureList();
            instances.Clear();
            SyncLegacyIds();
            InventoryChanged?.Invoke();
        }

        private void SyncLegacyIds()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            List<string> legacy = Game.Core.RuntimePlayerState.InventoryItemIds;
            legacy.Clear();
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null && !string.IsNullOrEmpty(instances[i].itemId))
                {
                    legacy.Add(instances[i].itemId);
                }
            }
        }
    }
}
