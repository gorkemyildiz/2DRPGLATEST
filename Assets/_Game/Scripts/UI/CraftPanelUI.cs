using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CraftPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text materialsText;
        [SerializeField] private Text recipeListText;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button craftFirstButton;
        [SerializeField] private Button craftSecondButton;
        [SerializeField] private Button craftSummonButton;
        [SerializeField] private Button disassembleButton;
        [SerializeField] private Button mergeButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Game.Crafting.CraftService craftService;
        [SerializeField] private Game.Inventory.InventoryService inventoryService;
        [SerializeField] private Game.Data.ItemDatabase itemDatabase;

        private readonly List<Game.Data.CraftRecipeData> boundRecipes = new List<Game.Data.CraftRecipeData>();

        public void Bind(Game.Crafting.CraftService service)
        {
            craftService = service;
            WireButtons();
            Refresh();
        }

        public void Open()
        {
            ResolveRefs();

            Game.UI.VillagePanelUI village = FindFirstObjectByType<Game.UI.VillagePanelUI>(FindObjectsInactive.Include);
            if (village != null)
            {
                village.Close();
            }

            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(true);
            gameObject.SetActive(true);
            UIPanelSlider.OpenRoot(root);
            Refresh();
        }

        public void Close()
        {
            UIPanelSlider.CloseRoot(panelRoot, gameObject);
        }

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            WireButtons();
        }

        private void OnEnable()
        {
            ResolveRefs();
            if (craftService != null)
            {
                craftService.MaterialsChanged += Refresh;
                craftService.CraftActivity += Refresh;
            }
        }

        private void OnDisable()
        {
            if (craftService != null)
            {
                craftService.MaterialsChanged -= Refresh;
                craftService.CraftActivity -= Refresh;
            }
        }

        private void ResolveRefs()
        {
            if (craftService == null)
            {
                craftService = FindFirstObjectByType<Game.Crafting.CraftService>();
            }

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<Game.Inventory.InventoryService>();
            }
        }

        private void WireButtons()
        {
            WireRecipeButton(craftFirstButton, 0);
            WireRecipeButton(craftSecondButton, 1);
            WireRecipeButton(craftSummonButton, -1);

            if (disassembleButton != null)
            {
                disassembleButton.onClick.RemoveAllListeners();
                disassembleButton.onClick.AddListener(OnDisassemble);
            }

            if (mergeButton != null)
            {
                mergeButton.onClick.RemoveAllListeners();
                mergeButton.onClick.AddListener(OnMerge);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void WireRecipeButton(Button button, int recipeIndex)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCraftAt(recipeIndex));
        }

        private void OnCraftAt(int recipeIndex)
        {
            ResolveRefs();
            if (craftService == null)
            {
                SetStatus("Craft service missing");
                return;
            }

            Game.Data.CraftRecipeData recipe = null;
            if (recipeIndex < 0)
            {
                if (craftService.Catalog != null)
                {
                    recipe = craftService.Catalog.summonStoneRecipe;
                }
            }
            else if (recipeIndex < boundRecipes.Count)
            {
                recipe = boundRecipes[recipeIndex];
            }

            if (recipe == null)
            {
                SetStatus("No recipe");
                return;
            }

            if (craftService.TryCraft(recipe, out string message))
            {
                SetStatus(message);
            }
            else
            {
                SetStatus(message);
            }

            Refresh();
        }

        private void OnDisassemble()
        {
            ResolveRefs();
            if (craftService == null || inventoryService == null)
            {
                SetStatus("Missing services");
                return;
            }

            string target = FindFirstDisassembleTarget();
            if (string.IsNullOrEmpty(target))
            {
                SetStatus("No unequipped item to scrap");
                return;
            }

            craftService.TryDisassemble(target, out string message);
            SetStatus(message);
            Refresh();
        }

        private void OnMerge()
        {
            ResolveRefs();
            if (craftService == null || inventoryService == null)
            {
                SetStatus("Missing services");
                return;
            }

            int need = craftService.Catalog != null ? Mathf.Max(2, craftService.Catalog.mergeInputCount) : 9;
            string target = FindMergeTarget(need);
            if (string.IsNullOrEmpty(target))
            {
                SetStatus($"Need {need} of same unequipped item");
                return;
            }

            Game.Data.MaterialData booster = null;
            if (itemDatabase != null && craftService.Catalog != null)
            {
                Game.Data.ItemData item = itemDatabase.GetById(target);
                if (item != null && craftService.Catalog.materials != null)
                {
                    for (int i = 0; i < craftService.Catalog.materials.Count; i++)
                    {
                        Game.Data.MaterialData mat = craftService.Catalog.materials[i];
                        if (mat != null && mat.isMergeBooster && mat.boostsRarity == item.rarity
                            && craftService.GetMaterialAmount(mat.materialId) > 0)
                        {
                            booster = mat;
                            break;
                        }
                    }
                }
            }

            craftService.TryMerge(target, booster, out string message);
            SetStatus(message);
            Refresh();
        }

        private string FindFirstDisassembleTarget()
        {
            if (inventoryService == null)
            {
                return null;
            }

            List<string> ids = inventoryService.ItemIds;
            for (int i = 0; i < ids.Count; i++)
            {
                return ids[i];
            }

            return null;
        }

        private string FindMergeTarget(int need)
        {
            if (inventoryService == null)
            {
                return null;
            }

            Dictionary<string, int> counts = inventoryService.GetItemCounts();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value >= need)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public void Refresh()
        {
            ResolveRefs();

            if (titleText != null)
            {
                titleText.text = "BLACKSMITH CRAFT";
            }

            boundRecipes.Clear();
            if (craftService != null)
            {
                List<Game.Data.CraftRecipeData> recipes = craftService.GetRecipes();
                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i] == null)
                    {
                        continue;
                    }

                    if (craftService.Catalog != null && recipes[i] == craftService.Catalog.summonStoneRecipe)
                    {
                        continue;
                    }

                    boundRecipes.Add(recipes[i]);
                }
            }

            if (materialsText != null)
            {
                string summary = craftService != null
                    ? craftService.BuildMaterialsSummary()
                    : "No materials";
                materialsText.text = "MATERIALS\n" + summary;
            }

            if (recipeListText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("RECIPES");
                for (int i = 0; i < boundRecipes.Count && i < 4; i++)
                {
                    Game.Data.CraftRecipeData r = boundRecipes[i];
                    sb.Append("\n• ").Append(r.displayName);
                    if (r.outputItem != null)
                    {
                        sb.Append("  →  ").Append(r.outputItem.displayName);
                    }
                }

                if (craftService != null && craftService.Catalog != null && craftService.Catalog.summonStoneRecipe != null)
                {
                    sb.Append("\n• ").Append(craftService.Catalog.summonStoneRecipe.displayName);
                }

                if (boundRecipes.Count == 0
                    && (craftService == null || craftService.Catalog == null || craftService.Catalog.summonStoneRecipe == null))
                {
                    sb.Append("\nNo recipes");
                }

                recipeListText.text = sb.ToString();
            }

            UpdateCraftButton(craftFirstButton, 0);
            UpdateCraftButton(craftSecondButton, 1);
            UpdateSummonButton();

            if (disassembleButton != null)
            {
                Text t = disassembleButton.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.text = "Disassemble 1";
                }
            }

            if (mergeButton != null)
            {
                int need = craftService != null && craftService.Catalog != null
                    ? Mathf.Max(2, craftService.Catalog.mergeInputCount)
                    : 9;
                Text t = mergeButton.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.text = $"Merge {need}→1";
                }
            }
        }

        private void UpdateCraftButton(Button button, int index)
        {
            if (button == null)
            {
                return;
            }

            Game.Data.CraftRecipeData recipe = index < boundRecipes.Count ? boundRecipes[index] : null;
            button.gameObject.SetActive(recipe != null);
            if (recipe == null)
            {
                return;
            }

            string reason = string.Empty;
            bool can = craftService != null && craftService.CanCraft(recipe, out reason);
            button.interactable = can;
            Text t = button.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = can ? recipe.displayName : (string.IsNullOrEmpty(reason) ? "—" : reason);
            }
        }

        private void UpdateSummonButton()
        {
            if (craftSummonButton == null)
            {
                return;
            }

            Game.Data.CraftRecipeData recipe = craftService != null && craftService.Catalog != null
                ? craftService.Catalog.summonStoneRecipe
                : null;

            craftSummonButton.gameObject.SetActive(recipe != null);
            if (recipe == null)
            {
                return;
            }

            string reason = string.Empty;
            bool can = craftService.CanCraft(recipe, out reason);
            craftSummonButton.interactable = can;
            Text t = craftSummonButton.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = can ? "Summon Stone" : reason;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }
    }
}
