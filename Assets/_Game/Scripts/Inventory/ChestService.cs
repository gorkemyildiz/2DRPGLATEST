using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    public class ChestService : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Game.Data.ChestData defaultChest;
        [SerializeField] private Game.Data.ItemDatabase itemDatabase;
        [SerializeField] private Game.Data.MapCatalog mapCatalog;

        [Header("Services")]
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private EquipmentService equipmentService;

        [Header("UI")]
        [SerializeField] private Game.UI.LootPopupUI lootPopup;

        public event Action<Game.Data.ItemData> ItemDropped;
        public event Action<Game.Data.ItemData> ItemEquippedFromLoot;

        public Game.Data.ItemDatabase ItemDatabase => itemDatabase;
        public Game.Data.ChestData DefaultChest => defaultChest;
        public Game.Data.MapCatalog MapCatalog => mapCatalog;

        private void Awake()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }

            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<EquipmentService>();
            }

            if (lootPopup == null)
            {
                lootPopup = FindFirstObjectByType<Game.UI.LootPopupUI>(FindObjectsInactive.Include);
            }
        }

        public bool TryOpenTierChest(Game.Data.ChestTier tier, Game.Data.ChestData chestOverride = null)
        {
            Game.Data.ChestData chest = chestOverride;
            if (chest == null && mapCatalog != null)
            {
                chest = mapCatalog.GetChestForTier(tier);
            }

            if (chest == null)
            {
                chest = defaultChest;
            }

            Debug.Log($"[ChestService] Opening {tier} chest: {(chest != null ? chest.displayName : "null")}");
            return OpenChest(chest);
        }

        public bool TryDropFromChance(float chance, Game.Data.ChestData chestOverride = null)
        {
            if (chance <= 0f)
            {
                return false;
            }

            if (UnityEngine.Random.value > chance)
            {
                return false;
            }

            return OpenChest(chestOverride != null ? chestOverride : defaultChest);
        }

        public bool OpenChest(Game.Data.ChestData chest)
        {
            if (chest == null)
            {
                Debug.LogWarning("[ChestService] OpenChest called with null chest.");
                return false;
            }

            if (chest.possibleItems == null || chest.possibleItems.Count == 0)
            {
                Debug.LogWarning($"[ChestService] Chest '{chest.displayName}' has no possible items.");
                return false;
            }

            int min = Mathf.Max(1, chest.minimumItems);
            int max = Mathf.Max(min, chest.maximumItems);
            int dropCount = UnityEngine.Random.Range(min, max + 1);

            List<Game.Data.ItemData> dropped = new List<Game.Data.ItemData>();

            for (int i = 0; i < dropCount; i++)
            {
                Game.Data.ItemData item = PickRandomItem(chest);
                if (item == null)
                {
                    continue;
                }

                if (inventoryService != null)
                {
                    inventoryService.AddItem(item.itemId);
                }

                dropped.Add(item);
                ItemDropped?.Invoke(item);
                Debug.Log($"[ChestService] Dropped item: {item.displayName} ({item.rarity})");
            }

            if (dropped.Count == 0)
            {
                return false;
            }

            // Show first dropped item in popup for demo simplicity
            if (lootPopup != null)
            {
                lootPopup.Show(dropped[0], this);
            }

            return true;
        }

        public void EquipLootedItem(Game.Data.ItemData item)
        {
            if (item == null)
            {
                return;
            }

            if (equipmentService == null)
            {
                Debug.LogWarning("[ChestService] EquipmentService missing. Cannot equip.");
                return;
            }

            equipmentService.Equip(item);
            ItemEquippedFromLoot?.Invoke(item);
            Debug.Log($"[ChestService] Equipped looted item: {item.displayName}");
        }

        public Game.Data.ItemData GetItemById(string itemId)
        {
            if (itemDatabase != null)
            {
                return itemDatabase.GetById(itemId);
            }

            return null;
        }

        private static Game.Data.ItemData PickRandomItem(Game.Data.ChestData chest)
        {
            List<Game.Data.ItemData> valid = new List<Game.Data.ItemData>();
            for (int i = 0; i < chest.possibleItems.Count; i++)
            {
                if (chest.possibleItems[i] != null)
                {
                    valid.Add(chest.possibleItems[i]);
                }
            }

            if (valid.Count == 0)
            {
                return null;
            }

            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
    }
}
