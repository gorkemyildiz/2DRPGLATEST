using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class EnhancePanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text itemText;
        [SerializeField] private Text starsText;
        [SerializeField] private Text enchantsText;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button addStarButton;
        [SerializeField] private Button applyEnchantButton;
        [SerializeField] private Button clearEnchantButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Game.Inventory.ItemEnhanceService enhanceService;
        [SerializeField] private Game.Inventory.InventoryService inventoryService;

        private Game.Save.ItemInstanceSave selected;
        private int nextEnchantIndex;

        public void Bind(Game.Inventory.ItemEnhanceService service)
        {
            enhanceService = service;
            WireButtons();
            Refresh();
        }

        public void OpenFor(string instanceUid)
        {
            ResolveRefs();
            if (inventoryService != null)
            {
                selected = inventoryService.GetInstance(instanceUid);
            }

            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(true);
            gameObject.SetActive(true);
            UIPanelSlider.OpenRoot(root);
            Refresh();
        }

        public void Open()
        {
            ResolveRefs();
            if (selected == null && inventoryService != null && inventoryService.Instances.Count > 0)
            {
                selected = inventoryService.Instances[0];
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
            if (enhanceService != null)
            {
                enhanceService.Enhanced += Refresh;
            }
        }

        private void OnDisable()
        {
            if (enhanceService != null)
            {
                enhanceService.Enhanced -= Refresh;
            }
        }

        private void ResolveRefs()
        {
            if (enhanceService == null)
            {
                enhanceService = FindFirstObjectByType<Game.Inventory.ItemEnhanceService>();
            }

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<Game.Inventory.InventoryService>();
            }
        }

        private void WireButtons()
        {
            if (addStarButton != null)
            {
                addStarButton.onClick.RemoveAllListeners();
                addStarButton.onClick.AddListener(OnAddStar);
            }

            if (applyEnchantButton != null)
            {
                applyEnchantButton.onClick.RemoveAllListeners();
                applyEnchantButton.onClick.AddListener(OnApplyEnchant);
            }

            if (clearEnchantButton != null)
            {
                clearEnchantButton.onClick.RemoveAllListeners();
                clearEnchantButton.onClick.AddListener(OnClearEnchant);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnAddStar()
        {
            ResolveRefs();
            if (enhanceService == null || selected == null)
            {
                SetStatus("Select an item");
                return;
            }

            enhanceService.TryAddStar(selected, out string message);
            SetStatus(message);
            Refresh();
        }

        private void OnApplyEnchant()
        {
            ResolveRefs();
            if (enhanceService == null || selected == null || enhanceService.Catalog == null)
            {
                SetStatus("No enchant data");
                return;
            }

            List<Game.Data.EnchantData> list = enhanceService.Catalog.enchants;
            if (list == null || list.Count == 0)
            {
                SetStatus("No enchants");
                return;
            }

            Game.Data.ItemData item = enhanceService.GetItemData(selected);
            Game.Data.EnchantData chosen = null;
            for (int attempt = 0; attempt < list.Count; attempt++)
            {
                int index = (nextEnchantIndex + attempt) % list.Count;
                Game.Data.EnchantData candidate = list[index];
                if (candidate == null)
                {
                    continue;
                }

                if (item != null && (int)item.rarity < (int)candidate.requiredRarity)
                {
                    continue;
                }

                if (enhanceService.CanApplyEnchant(selected, candidate, out _))
                {
                    chosen = candidate;
                    nextEnchantIndex = (index + 1) % list.Count;
                    break;
                }
            }

            if (chosen == null)
            {
                SetStatus("No valid enchant");
                return;
            }

            enhanceService.TryApplyEnchant(selected, chosen, out string message);
            SetStatus(message);
            Refresh();
        }

        private void OnClearEnchant()
        {
            ResolveRefs();
            if (enhanceService == null || selected == null)
            {
                SetStatus("Select an item");
                return;
            }

            if (selected.enchantIds == null || selected.enchantIds.Count == 0)
            {
                SetStatus("No enchants");
                return;
            }

            enhanceService.TryClearEnchant(selected, selected.enchantIds.Count - 1, out string message);
            SetStatus(message);
            Refresh();
        }

        public void Refresh()
        {
            ResolveRefs();

            if (titleText != null)
            {
                titleText.text = "ENHANCE";
            }

            if (selected == null)
            {
                if (itemText != null)
                {
                    itemText.text = "No item selected\nOpen from Inventory → Enhance";
                }

                if (starsText != null)
                {
                    starsText.text = string.Empty;
                }

                if (enchantsText != null)
                {
                    enchantsText.text = string.Empty;
                }

                return;
            }

            // Refresh reference in case list changed
            if (inventoryService != null)
            {
                Game.Save.ItemInstanceSave live = inventoryService.GetInstance(selected.uid);
                if (live != null)
                {
                    selected = live;
                }
            }

            Game.Data.ItemData item = enhanceService != null ? enhanceService.GetItemData(selected) : null;
            Game.Data.EnhanceCatalog catalog = enhanceService != null ? enhanceService.Catalog : null;

            int atk = Game.Data.ItemEnhanceUtil.GetAttackBonus(item, selected, catalog);
            int hp = Game.Data.ItemEnhanceUtil.GetHealthBonus(item, selected, catalog);
            string rarity = item != null ? Game.Data.ItemRarityUtil.GetDisplayName(item.rarity) : "?";
            string name = item != null ? item.displayName : selected.itemId;

            if (itemText != null)
            {
                itemText.text = $"{name}\n[{rarity}]  +{atk} ATK / +{hp} HP";
            }

            string stars = Game.Data.ItemEnhanceUtil.FormatStars(selected.stars);
            if (starsText != null)
            {
                starsText.text = selected.stars > 0
                    ? $"Stars  {stars}  ({selected.stars}/5)"
                    : "Stars  (0/5)";
            }

            int slots = item != null ? Game.Data.ItemEnhanceUtil.GetEnchantSlotCount(item, selected) : 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append($"Enchants  {selected.enchantIds?.Count ?? 0}/{slots}");
            if (selected.enchantIds != null)
            {
                for (int i = 0; i < selected.enchantIds.Count; i++)
                {
                    Game.Data.EnchantData e = catalog != null ? catalog.GetEnchant(selected.enchantIds[i]) : null;
                    sb.Append("\n• ").Append(e != null ? e.displayName : selected.enchantIds[i]);
                }
            }

            if (slots <= 0)
            {
                sb.Append("\n(Need Nadir+ rarity for efsun)");
            }

            if (enchantsText != null)
            {
                enchantsText.text = sb.ToString();
            }

            if (addStarButton != null)
            {
                string reason = string.Empty;
                bool can = enhanceService != null && enhanceService.CanAddStar(selected, out reason);
                addStarButton.interactable = can || selected.stars < 5;
                Text t = addStarButton.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.text = can ? "+1 ★" : (selected.stars >= 5 ? "MAX ★" : (string.IsNullOrEmpty(reason) ? "—" : reason));
                }
            }

            if (applyEnchantButton != null)
            {
                applyEnchantButton.interactable = slots > 0;
                Text t = applyEnchantButton.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.text = "Apply Efsun";
                }
            }

            if (clearEnchantButton != null)
            {
                bool has = selected.enchantIds != null && selected.enchantIds.Count > 0;
                clearEnchantButton.interactable = has;
                Text t = clearEnchantButton.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.text = "Clear Last";
                }
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
