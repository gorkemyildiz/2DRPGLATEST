using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    public class EquipmentSlotData
    {
        public Game.Data.EquipmentSlot slot;
        public Game.Data.ItemData item;
        public string instanceUid;
    }

    public class EquipmentService : MonoBehaviour
    {
        [SerializeField] private List<EquipmentSlotData> equipment = new List<EquipmentSlotData>();
        [SerializeField] private Game.Data.EnhanceCatalog enhanceCatalog;

        public event Action EquipmentChanged;

        private void Awake()
        {
            EnsureAllSlotsExist();

            Game.Core.RuntimePlayerState.EnsureInitialized();
            RestoreFromRuntime();
        }

        private void Start()
        {
            RestoreFromRuntime();
            EquipmentChanged?.Invoke();
        }

        private void OnDisable()
        {
            CaptureToRuntime();
        }

        public void SetEnhanceCatalog(Game.Data.EnhanceCatalog catalog)
        {
            enhanceCatalog = catalog;
        }

        private void EnsureAllSlotsExist()
        {
            Game.Data.EquipmentSlot[] all =
            {
                Game.Data.EquipmentSlot.Weapon,
                Game.Data.EquipmentSlot.Helmet,
                Game.Data.EquipmentSlot.Armor,
                Game.Data.EquipmentSlot.Accessory,
                Game.Data.EquipmentSlot.Shoulders,
                Game.Data.EquipmentSlot.Legs,
                Game.Data.EquipmentSlot.Boots,
                Game.Data.EquipmentSlot.Ring2
            };

            for (int i = 0; i < all.Length; i++)
            {
                Game.Data.EquipmentSlot slot = all[i];
                if (equipment.Find(e => e.slot == slot) == null)
                {
                    equipment.Add(new EquipmentSlotData { slot = slot, item = null });
                }
            }
        }

        public void CaptureToRuntime()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            List<Game.Save.EquipmentSaveData> runtime = Game.Core.RuntimePlayerState.Equipment;
            runtime.Clear();

            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentSlotData slotData = equipment[i];
                runtime.Add(new Game.Save.EquipmentSaveData
                {
                    slot = slotData.slot,
                    itemId = slotData.item != null ? slotData.item.itemId : null,
                    instanceUid = slotData.instanceUid
                });
            }
        }

        public void RestoreFromRuntime()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            EnsureAllSlotsExist();
            Game.Data.ItemDatabase database = ResolveItemDatabase();
            InventoryService inventory = FindFirstObjectByType<InventoryService>();

            List<Game.Save.EquipmentSaveData> runtime = Game.Core.RuntimePlayerState.Equipment;
            for (int i = 0; i < runtime.Count; i++)
            {
                Game.Save.EquipmentSaveData save = runtime[i];
                EquipmentSlotData slotData = equipment.Find(e => e.slot == save.slot);
                if (slotData == null)
                {
                    slotData = new EquipmentSlotData { slot = save.slot, item = null };
                    equipment.Add(slotData);
                }

                slotData.instanceUid = save.instanceUid;
                if (!string.IsNullOrEmpty(save.instanceUid) && inventory != null)
                {
                    Game.Save.ItemInstanceSave inst = inventory.GetInstance(save.instanceUid);
                    if (inst != null && database != null)
                    {
                        slotData.item = database.GetById(inst.itemId);
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(save.itemId) || database == null)
                {
                    slotData.item = null;
                    slotData.instanceUid = null;
                }
                else
                {
                    slotData.item = database.GetById(save.itemId);
                }
            }
        }

        private Game.Data.ItemDatabase ResolveItemDatabase()
        {
            ChestService chestService = FindFirstObjectByType<ChestService>();
            if (chestService != null && chestService.ItemDatabase != null)
            {
                return chestService.ItemDatabase;
            }

            return null;
        }

        public void Equip(Game.Data.ItemData item)
        {
            if (item == null)
            {
                return;
            }

            InventoryService inventory = FindFirstObjectByType<InventoryService>();
            Game.Save.ItemInstanceSave inst = inventory != null ? inventory.FindFirstByItemId(item.itemId, preferPlain: false) : null;
            EquipInstance(inst, item);
        }

        public void EquipInstance(Game.Save.ItemInstanceSave instance, Game.Data.ItemData fallbackItem = null)
        {
            Game.Data.ItemData item = fallbackItem;
            if (instance != null)
            {
                Game.Data.ItemDatabase database = ResolveItemDatabase();
                if (database != null)
                {
                    item = database.GetById(instance.itemId);
                }
            }

            if (item == null)
            {
                Debug.LogWarning("[EquipmentService] Equip called with null item.");
                return;
            }

            EquipmentSlotData slotData = equipment.Find(e => e.slot == item.equipmentSlot);
            if (slotData == null)
            {
                slotData = new EquipmentSlotData { slot = item.equipmentSlot };
                equipment.Add(slotData);
            }

            slotData.item = item;
            slotData.instanceUid = instance != null ? instance.uid : null;

            CaptureToRuntime();
            EquipmentChanged?.Invoke();
            Debug.Log($"[EquipmentService] Equipped {item.displayName} ({slotData.instanceUid}).");
        }

        public void Unequip(Game.Data.EquipmentSlot slot)
        {
            EquipmentSlotData slotData = equipment.Find(e => e.slot == slot);
            if (slotData != null && (slotData.item != null || !string.IsNullOrEmpty(slotData.instanceUid)))
            {
                slotData.item = null;
                slotData.instanceUid = null;
                CaptureToRuntime();
                EquipmentChanged?.Invoke();
            }
        }

        public Game.Data.ItemData GetEquippedItem(Game.Data.EquipmentSlot slot)
        {
            EquipmentSlotData slotData = equipment.Find(e => e.slot == slot);
            return slotData?.item;
        }

        public Game.Save.ItemInstanceSave GetEquippedInstance(Game.Data.EquipmentSlot slot)
        {
            EquipmentSlotData slotData = equipment.Find(e => e.slot == slot);
            if (slotData == null || string.IsNullOrEmpty(slotData.instanceUid))
            {
                return null;
            }

            InventoryService inventory = FindFirstObjectByType<InventoryService>();
            return inventory != null ? inventory.GetInstance(slotData.instanceUid) : null;
        }

        public string GetEquippedInstanceUid(Game.Data.EquipmentSlot slot)
        {
            EquipmentSlotData slotData = equipment.Find(e => e.slot == slot);
            return slotData?.instanceUid;
        }

        public bool IsEquipped(Game.Data.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            Game.Data.ItemData equipped = GetEquippedItem(item.equipmentSlot);
            return equipped != null && equipped.itemId == item.itemId;
        }

        public bool IsEquipped(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            foreach (EquipmentSlotData slotData in equipment)
            {
                if (slotData.item != null && slotData.item.itemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsInstanceEquipped(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return false;
            }

            for (int i = 0; i < equipment.Count; i++)
            {
                if (equipment[i].instanceUid == uid)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<EquipmentSlotData> GetAllSlots()
        {
            return equipment;
        }

        public int GetTotalAttackBonus()
        {
            int total = 0;
            InventoryService inventory = FindFirstObjectByType<InventoryService>();
            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentSlotData slotData = equipment[i];
                if (slotData.item == null)
                {
                    continue;
                }

                Game.Save.ItemInstanceSave inst = null;
                if (inventory != null && !string.IsNullOrEmpty(slotData.instanceUid))
                {
                    inst = inventory.GetInstance(slotData.instanceUid);
                }

                total += Game.Data.ItemEnhanceUtil.GetAttackBonus(slotData.item, inst, enhanceCatalog);
            }

            return total;
        }

        public int GetTotalHealthBonus()
        {
            int total = 0;
            InventoryService inventory = FindFirstObjectByType<InventoryService>();
            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentSlotData slotData = equipment[i];
                if (slotData.item == null)
                {
                    continue;
                }

                Game.Save.ItemInstanceSave inst = null;
                if (inventory != null && !string.IsNullOrEmpty(slotData.instanceUid))
                {
                    inst = inventory.GetInstance(slotData.instanceUid);
                }

                total += Game.Data.ItemEnhanceUtil.GetHealthBonus(slotData.item, inst, enhanceCatalog);
            }

            return total;
        }
    }
}
