using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Crafting
{
    public class CraftService : MonoBehaviour
    {
        [SerializeField] private Game.Data.CraftCatalog craftCatalog;
        [SerializeField] private Game.Data.ItemDatabase itemDatabase;
        [SerializeField] private Game.Inventory.InventoryService inventoryService;
        [SerializeField] private Game.Inventory.EquipmentService equipmentService;
        [SerializeField] private Game.Village.VillageService villageService;
        [SerializeField] private Game.Rewards.RewardService rewardService;

        public event Action MaterialsChanged;
        public event Action CraftActivity;

        public Game.Data.CraftCatalog Catalog => craftCatalog;

        public void SetCatalog(Game.Data.CraftCatalog catalog)
        {
            craftCatalog = catalog;
        }

        public void SetItemDatabase(Game.Data.ItemDatabase database)
        {
            itemDatabase = database;
        }

        private void Awake()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            ResolveRefs();
        }

        private void ResolveRefs()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<Game.Inventory.InventoryService>();
            }

            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<Game.Inventory.EquipmentService>();
            }

            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }
        }

        public int GetMaterialAmount(string materialId)
        {
            return Game.Core.RuntimePlayerState.Materials.GetAmount(materialId);
        }

        public void AddMaterial(string materialId, int amount)
        {
            if (string.IsNullOrEmpty(materialId) || amount <= 0)
            {
                return;
            }

            Game.Core.RuntimePlayerState.Materials.Add(materialId, amount);
            MaterialsChanged?.Invoke();
        }

        public bool HasSummonStone()
        {
            if (craftCatalog == null || craftCatalog.summonStone == null)
            {
                return false;
            }

            return GetMaterialAmount(craftCatalog.summonStone.materialId) > 0;
        }

        public bool TryConsumeSummonStone()
        {
            if (craftCatalog == null || craftCatalog.summonStone == null)
            {
                return false;
            }

            bool ok = Game.Core.RuntimePlayerState.Materials.TryConsume(craftCatalog.summonStone.materialId, 1);
            if (ok)
            {
                MaterialsChanged?.Invoke();
            }

            return ok;
        }

        public int GetCourageStoneCount()
        {
            string id = ResolveCourageStoneId();
            return string.IsNullOrEmpty(id) ? 0 : GetMaterialAmount(id);
        }

        public bool HasCourageStone(int amount = 1)
        {
            return GetCourageStoneCount() >= Mathf.Max(1, amount);
        }

        public bool TryConsumeCourageStone(int amount = 1)
        {
            string id = ResolveCourageStoneId();
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            bool ok = Game.Core.RuntimePlayerState.Materials.TryConsume(id, Mathf.Max(1, amount));
            if (ok)
            {
                MaterialsChanged?.Invoke();
            }

            return ok;
        }

        private string ResolveCourageStoneId()
        {
            if (craftCatalog != null && craftCatalog.courageStone != null)
            {
                return craftCatalog.courageStone.materialId;
            }

            return "mat_courage_stone";
        }

        public bool CanCraft(Game.Data.CraftRecipeData recipe, out string reason)
        {
            reason = string.Empty;
            ResolveRefs();

            if (recipe == null)
            {
                reason = "Invalid recipe";
                return false;
            }

            bool isSummonRecipe = craftCatalog != null && recipe == craftCatalog.summonStoneRecipe;
            if (!isSummonRecipe && recipe.outputItem == null)
            {
                reason = "Invalid recipe";
                return false;
            }

            if (isSummonRecipe && (craftCatalog.summonStone == null))
            {
                reason = "Summon stone missing";
                return false;
            }

            int smithLevel = villageService != null
                ? villageService.GetBuildingLevel(Game.Data.VillageBuildingType.Blacksmith)
                : 1;

            if (smithLevel < recipe.requiredBlacksmithLevel)
            {
                reason = $"Need Blacksmith Lv.{recipe.requiredBlacksmithLevel}";
                return false;
            }

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (recipe.goldCost > 0)
            {
                int gold = Game.Core.RuntimePlayerState.Progress != null
                    ? Game.Core.RuntimePlayerState.Progress.gold
                    : 0;
                if (gold < recipe.goldCost)
                {
                    reason = $"Need {recipe.goldCost}g";
                    return false;
                }
            }

            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    Game.Data.CraftIngredient ingredient = recipe.ingredients[i];
                    if (ingredient == null || ingredient.material == null || ingredient.amount <= 0)
                    {
                        continue;
                    }

                    int have = GetMaterialAmount(ingredient.material.materialId);
                    if (have < ingredient.amount)
                    {
                        reason = $"Need {ingredient.material.displayName} x{ingredient.amount}";
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryCraft(Game.Data.CraftRecipeData recipe, out string message)
        {
            if (!CanCraft(recipe, out string reason))
            {
                message = reason;
                return false;
            }

            ResolveRefs();

            if (recipe.goldCost > 0 && rewardService != null)
            {
                if (!rewardService.TrySpendGold(recipe.goldCost))
                {
                    message = "Not enough gold";
                    return false;
                }
            }
            else if (recipe.goldCost > 0)
            {
                Game.Core.RuntimePlayerState.Progress.gold -= recipe.goldCost;
            }

            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    Game.Data.CraftIngredient ingredient = recipe.ingredients[i];
                    if (ingredient == null || ingredient.material == null || ingredient.amount <= 0)
                    {
                        continue;
                    }

                    Game.Core.RuntimePlayerState.Materials.TryConsume(ingredient.material.materialId, ingredient.amount);
                }
            }

            bool isSummonRecipe = craftCatalog != null && recipe == craftCatalog.summonStoneRecipe;
            if (isSummonRecipe && craftCatalog.summonStone != null)
            {
                AddMaterial(craftCatalog.summonStone.materialId, Mathf.Max(1, recipe.outputCount));
                message = $"Crafted {craftCatalog.summonStone.displayName}";
                CraftActivity?.Invoke();
                Game.Save.GameSaveController.SaveGame();
                return true;
            }

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<Game.Inventory.InventoryService>();
            }

            int count = Mathf.Max(1, recipe.outputCount);
            for (int i = 0; i < count; i++)
            {
                inventoryService?.AddItem(recipe.outputItem.itemId);
            }

            message = $"Crafted {recipe.outputItem.displayName} x{count}";
            MaterialsChanged?.Invoke();
            CraftActivity?.Invoke();
            Game.Save.GameSaveController.SaveGame();
            return true;
        }

        public bool TryDisassemble(string itemId, out string message)
        {
            ResolveRefs();
            message = string.Empty;

            if (string.IsNullOrEmpty(itemId) || craftCatalog == null || craftCatalog.scrapMaterial == null)
            {
                message = "Cannot disassemble";
                return false;
            }

            if (equipmentService != null)
            {
                Game.Save.ItemInstanceSave target = inventoryService.FindFirstByItemId(itemId, preferPlain: true);
                if (target != null && equipmentService.IsInstanceEquipped(target.uid))
                {
                    message = "Unequip first";
                    return false;
                }
            }

            if (inventoryService == null || !inventoryService.HasItem(itemId))
            {
                message = "Item not in bag";
                return false;
            }

            Game.Data.ItemData item = itemDatabase != null ? itemDatabase.GetById(itemId) : null;
            if (item == null)
            {
                message = "Unknown item";
                return false;
            }

            inventoryService.RemoveItem(itemId);

            int rarityIndex = Mathf.Clamp((int)item.rarity, 0, craftCatalog.scrapByRarity.Length - 1);
            int scrap = craftCatalog.scrapByRarity.Length > 0
                ? Mathf.Max(1, craftCatalog.scrapByRarity[rarityIndex])
                : 1;

            AddMaterial(craftCatalog.scrapMaterial.materialId, scrap);
            message = $"Disassembled {item.displayName} → {scrap} {craftCatalog.scrapMaterial.displayName}";
            CraftActivity?.Invoke();
            Game.Save.GameSaveController.SaveGame();
            return true;
        }

        public bool TryMerge(string itemId, Game.Data.MaterialData booster, out string message)
        {
            ResolveRefs();
            message = string.Empty;

            if (string.IsNullOrEmpty(itemId) || craftCatalog == null)
            {
                message = "Cannot merge";
                return false;
            }

            Game.Data.ItemData template = itemDatabase != null ? itemDatabase.GetById(itemId) : null;
            if (template == null)
            {
                message = "Unknown item";
                return false;
            }

            int need = Mathf.Max(2, craftCatalog.mergeInputCount);
            int available = CountUnequipped(itemId);
            if (available < need)
            {
                message = $"Need {need} unequipped {template.displayName}";
                return false;
            }

            float plusOne = craftCatalog.chancePlusOneRarity;
            float plusTwo = craftCatalog.chancePlusTwoRarity;

            if (booster != null && booster.isMergeBooster && booster.boostsRarity == template.rarity)
            {
                if (!Game.Core.RuntimePlayerState.Materials.TryConsume(booster.materialId, 1))
                {
                    message = $"Need {booster.displayName}";
                    return false;
                }

                plusOne = Mathf.Clamp01(plusOne + booster.successBonus);
                plusTwo = Mathf.Clamp01(plusTwo + booster.successBonus * 0.5f);
            }

            for (int i = 0; i < need; i++)
            {
                inventoryService.RemoveItem(itemId);
            }

            int raise = 0;
            float roll = UnityEngine.Random.value;
            if (roll <= plusTwo)
            {
                raise = 2;
            }
            else if (roll <= plusTwo + plusOne)
            {
                raise = 1;
            }

            Game.Data.ItemRarity resultRarity = Game.Data.ItemRarityUtil.Raise(template.rarity, raise);
            Game.Data.ItemData result = craftCatalog.FindMergeResult(template.equipmentSlot, resultRarity, template.levelBand);
            if (result == null)
            {
                result = craftCatalog.FindMergeResult(template.equipmentSlot, template.rarity, template.levelBand);
            }

            if (result == null)
            {
                result = template;
            }

            inventoryService.AddItem(result.itemId);

            string rarityName = Game.Data.ItemRarityUtil.GetDisplayName(result.rarity);
            message = raise > 0
                ? $"Merged → {result.displayName} ({rarityName}, +{raise})"
                : $"Merged → {result.displayName} ({rarityName})";

            MaterialsChanged?.Invoke();
            CraftActivity?.Invoke();
            Game.Save.GameSaveController.SaveGame();
            return true;
        }

        public int CountUnequipped(string itemId)
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<Game.Inventory.InventoryService>();
            }

            if (inventoryService == null)
            {
                return 0;
            }

            int count = inventoryService.GetItemCount(itemId);
            if (equipmentService != null && equipmentService.IsEquipped(itemId) && count > 0)
            {
                // One of the stack may be equipped (inventory still holds id when equipped depending on design).
                // EquipmentService typically keeps item in equipment and removes from bag — check inventory only.
            }

            return count;
        }

        public List<Game.Data.CraftRecipeData> GetRecipes()
        {
            if (craftCatalog == null || craftCatalog.recipes == null)
            {
                return new List<Game.Data.CraftRecipeData>();
            }

            return craftCatalog.recipes;
        }

        public string BuildMaterialsSummary()
        {
            if (craftCatalog == null || craftCatalog.materials == null)
            {
                return "No materials";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int shown = 0;
            for (int i = 0; i < craftCatalog.materials.Count; i++)
            {
                Game.Data.MaterialData mat = craftCatalog.materials[i];
                if (mat == null)
                {
                    continue;
                }

                int amount = GetMaterialAmount(mat.materialId);
                if (amount <= 0 && !mat.isSummonStone && !mat.isCourageStone)
                {
                    continue;
                }

                if (shown > 0)
                {
                    sb.Append(shown % 3 == 0 ? "\n" : "   ");
                }

                sb.Append(mat.displayName).Append(" ×").Append(amount);
                shown++;
            }

            return shown > 0 ? sb.ToString() : "No materials yet";
        }

        /// <summary>
        /// One-time starter kit for empty material bags (new saves / first craft session).
        /// </summary>
        public void EnsureStarterMaterialsIfEmpty()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (Game.Core.RuntimePlayerState.Materials.stacks != null
                && Game.Core.RuntimePlayerState.Materials.stacks.Count > 0)
            {
                return;
            }

            if (craftCatalog == null)
            {
                return;
            }

            if (craftCatalog.scrapMaterial != null)
            {
                AddMaterial(craftCatalog.scrapMaterial.materialId, 12);
            }

            AddNamed("mat_iron_ore", 6);
            AddNamed("mat_leather", 5);
            AddNamed("mat_gem_dust", 5);
            AddNamed("mat_boost_common", 2);
            Debug.Log("[CraftService] Starter materials granted.");
        }

        private void AddNamed(string materialId, int amount)
        {
            if (craftCatalog != null && craftCatalog.GetMaterial(materialId) != null)
            {
                AddMaterial(materialId, amount);
            }
        }
    }
}
