using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    public class ItemEnhanceService : MonoBehaviour
    {
        [SerializeField] private Game.Data.EnhanceCatalog enhanceCatalog;
        [SerializeField] private Game.Data.ItemDatabase itemDatabase;
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private EquipmentService equipmentService;
        [SerializeField] private Game.Rewards.RewardService rewardService;
        [SerializeField] private Game.Crafting.CraftService craftService;

        public event Action Enhanced;

        public Game.Data.EnhanceCatalog Catalog => enhanceCatalog;

        public void SetCatalog(Game.Data.EnhanceCatalog catalog)
        {
            enhanceCatalog = catalog;
            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<EquipmentService>();
            }

            equipmentService?.SetEnhanceCatalog(catalog);
        }

        private void Awake()
        {
            ResolveRefs();
            if (enhanceCatalog != null)
            {
                equipmentService?.SetEnhanceCatalog(enhanceCatalog);
            }
        }

        private void ResolveRefs()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }

            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<EquipmentService>();
            }

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (craftService == null)
            {
                craftService = FindFirstObjectByType<Game.Crafting.CraftService>();
            }
        }

        public Game.Data.ItemData GetItemData(Game.Save.ItemInstanceSave instance)
        {
            if (instance == null)
            {
                return null;
            }

            if (itemDatabase == null)
            {
                Game.Inventory.ChestService chest = FindFirstObjectByType<ChestService>();
                if (chest != null)
                {
                    itemDatabase = chest.ItemDatabase;
                }
            }

            return itemDatabase != null ? itemDatabase.GetById(instance.itemId) : null;
        }

        public bool CanAddStar(Game.Save.ItemInstanceSave instance, out string reason)
        {
            reason = string.Empty;
            ResolveRefs();

            if (instance == null || enhanceCatalog == null)
            {
                reason = "Invalid";
                return false;
            }

            if (instance.stars >= 5)
            {
                reason = "MAX ★";
                return false;
            }

            Game.Data.ItemData item = GetItemData(instance);
            if (item == null)
            {
                reason = "Unknown item";
                return false;
            }

            int goldCost = enhanceCatalog.GetStarGoldCost(instance.stars);
            int scrapCost = enhanceCatalog.GetStarScrapCost(instance.stars);
            int gold = Game.Core.RuntimePlayerState.Progress != null
                ? Game.Core.RuntimePlayerState.Progress.gold
                : 0;

            if (gold < goldCost)
            {
                reason = $"Need {goldCost}g";
                return false;
            }

            string scrapId = enhanceCatalog.scrapMaterialId;
            int scrapHave = craftService != null
                ? craftService.GetMaterialAmount(scrapId)
                : Game.Core.RuntimePlayerState.Materials.GetAmount(scrapId);

            if (scrapHave < scrapCost)
            {
                reason = $"Need scrap x{scrapCost}";
                return false;
            }

            int nextStar = instance.stars + 1;
            if (nextStar >= enhanceCatalog.duplicateRequiredFromStar)
            {
                int copies = CountOtherCopies(instance);
                if (copies < 1)
                {
                    reason = "Need duplicate";
                    return false;
                }
            }

            return true;
        }

        public bool TryAddStar(Game.Save.ItemInstanceSave instance, out string message)
        {
            if (!CanAddStar(instance, out string reason))
            {
                message = reason;
                return false;
            }

            ResolveRefs();
            int goldCost = enhanceCatalog.GetStarGoldCost(instance.stars);
            int scrapCost = enhanceCatalog.GetStarScrapCost(instance.stars);

            if (rewardService != null)
            {
                if (!rewardService.TrySpendGold(goldCost))
                {
                    message = "Not enough gold";
                    return false;
                }
            }
            else
            {
                Game.Core.RuntimePlayerState.Progress.gold -= goldCost;
            }

            string scrapId = enhanceCatalog.scrapMaterialId;
            if (craftService != null)
            {
                Game.Core.RuntimePlayerState.Materials.TryConsume(scrapId, scrapCost);
            }
            else
            {
                Game.Core.RuntimePlayerState.Materials.TryConsume(scrapId, scrapCost);
            }

            int nextStar = instance.stars + 1;
            if (nextStar >= enhanceCatalog.duplicateRequiredFromStar)
            {
                if (!ConsumeDuplicate(instance))
                {
                    message = "Need duplicate";
                    return false;
                }
            }

            instance.stars = nextStar;
            inventoryService?.NotifyChanged();
            equipmentService?.CaptureToRuntime();
            message = $"★{instance.stars}";
            Enhanced?.Invoke();
            Game.Save.GameSaveController.SaveGame();
            return true;
        }

        public bool CanApplyEnchant(Game.Save.ItemInstanceSave instance, Game.Data.EnchantData enchant, out string reason)
        {
            reason = string.Empty;
            if (instance == null || enchant == null)
            {
                reason = "Invalid";
                return false;
            }

            Game.Data.ItemData item = GetItemData(instance);
            if (item == null)
            {
                reason = "Unknown item";
                return false;
            }

            if ((int)item.rarity < (int)enchant.requiredRarity)
            {
                reason = "Rarity low";
                return false;
            }

            int maxSlots = Game.Data.ItemEnhanceUtil.GetEnchantSlotCount(item, instance);
            if (maxSlots <= 0)
            {
                reason = "No slots";
                return false;
            }

            if (instance.enchantIds == null)
            {
                instance.enchantIds = new List<string>();
            }

            if (instance.enchantIds.Count >= maxSlots)
            {
                reason = "Slots full";
                return false;
            }

            if (instance.enchantIds.Contains(enchant.enchantId))
            {
                reason = "Already has";
                return false;
            }

            // Legendary 3-slot: max 2 of same category (2+1 rule)
            if (maxSlots >= 3)
            {
                int same = CountCategory(instance, enchant.category);
                if (same >= 2)
                {
                    reason = "2+1 category";
                    return false;
                }
            }

            return true;
        }

        public bool TryApplyEnchant(Game.Save.ItemInstanceSave instance, Game.Data.EnchantData enchant, out string message)
        {
            if (!CanApplyEnchant(instance, enchant, out string reason))
            {
                message = reason;
                return false;
            }

            instance.enchantIds.Add(enchant.enchantId);
            inventoryService?.NotifyChanged();
            equipmentService?.CaptureToRuntime();
            message = $"Applied {enchant.displayName}";
            Enhanced?.Invoke();
            Game.Save.GameSaveController.SaveGame();
            return true;
        }

        public bool TryClearEnchant(Game.Save.ItemInstanceSave instance, int index, out string message)
        {
            message = string.Empty;
            ResolveRefs();

            if (instance == null || instance.enchantIds == null || index < 0 || index >= instance.enchantIds.Count)
            {
                message = "No enchant";
                return false;
            }

            Game.Data.EnchantData enchant = enhanceCatalog != null
                ? enhanceCatalog.GetEnchant(instance.enchantIds[index])
                : null;

            Game.Data.ItemData item = GetItemData(instance);
            int clearCost = enchant != null ? enchant.clearGoldCost : 50;
            if (item != null)
            {
                clearCost += (int)item.rarity * 25;
            }

            int gold = Game.Core.RuntimePlayerState.Progress != null
                ? Game.Core.RuntimePlayerState.Progress.gold
                : 0;
            if (gold < clearCost)
            {
                message = $"Need {clearCost}g";
                return false;
            }

            if (rewardService != null)
            {
                if (!rewardService.TrySpendGold(clearCost))
                {
                    message = $"Need {clearCost}g";
                    return false;
                }
            }
            else
            {
                Game.Core.RuntimePlayerState.Progress.gold -= clearCost;
            }

            string removed = instance.enchantIds[index];
            instance.enchantIds.RemoveAt(index);
            inventoryService?.NotifyChanged();
            equipmentService?.CaptureToRuntime();
            message = $"Cleared {removed} (-{clearCost}g)";
            Enhanced?.Invoke();
            Game.Save.GameSaveController.SaveGame();
            return true;
        }

        private int CountCategory(Game.Save.ItemInstanceSave instance, Game.Data.EnchantCategory category)
        {
            int count = 0;
            if (instance.enchantIds == null || enhanceCatalog == null)
            {
                return 0;
            }

            for (int i = 0; i < instance.enchantIds.Count; i++)
            {
                Game.Data.EnchantData e = enhanceCatalog.GetEnchant(instance.enchantIds[i]);
                if (e != null && e.category == category)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountOtherCopies(Game.Save.ItemInstanceSave target)
        {
            if (inventoryService == null || target == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<Game.Save.ItemInstanceSave> list = inventoryService.Instances;
            for (int i = 0; i < list.Count; i++)
            {
                Game.Save.ItemInstanceSave inst = list[i];
                if (inst == null || inst.uid == target.uid)
                {
                    continue;
                }

                if (inst.itemId == target.itemId)
                {
                    count++;
                }
            }

            return count;
        }

        private bool ConsumeDuplicate(Game.Save.ItemInstanceSave target)
        {
            if (inventoryService == null)
            {
                return false;
            }

            // Prefer plain unequipped duplicate
            Game.Save.ItemInstanceSave best = null;
            IReadOnlyList<Game.Save.ItemInstanceSave> list = inventoryService.Instances;
            for (int i = 0; i < list.Count; i++)
            {
                Game.Save.ItemInstanceSave inst = list[i];
                if (inst == null || inst.uid == target.uid || inst.itemId != target.itemId)
                {
                    continue;
                }

                if (equipmentService != null && equipmentService.IsInstanceEquipped(inst.uid))
                {
                    continue;
                }

                bool plain = inst.stars <= 0 && (inst.enchantIds == null || inst.enchantIds.Count == 0);
                if (plain)
                {
                    best = inst;
                    break;
                }

                if (best == null)
                {
                    best = inst;
                }
            }

            if (best == null)
            {
                return false;
            }

            return inventoryService.RemoveInstance(best.uid);
        }
    }
}
