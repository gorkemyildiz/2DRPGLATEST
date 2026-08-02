using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class InventoryPanelUI : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private Game.Inventory.InventoryService inventoryService;
        [SerializeField] private Game.Inventory.EquipmentService equipmentService;
        [SerializeField] private Game.Data.ItemDatabase itemDatabase;

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image cardBackground;

        [Header("Paper Doll (8 fixed boxes)")]
        [SerializeField] private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
        [SerializeField] private Text bonusSummaryText;

        [Header("Legacy text slots (hidden when paper doll exists)")]
        [SerializeField] private Text weaponText;
        [SerializeField] private Text helmetText;
        [SerializeField] private Text armorText;
        [SerializeField] private Text accessoryText;
        [SerializeField] private Button unequipWeaponButton;
        [SerializeField] private Button unequipHelmetButton;
        [SerializeField] private Button unequipArmorButton;
        [SerializeField] private Button unequipAccessoryButton;

        [Header("Bag Grid")]
        [SerializeField] private Transform bagContentRoot;
        [Tooltip("Edit Mode: set Width/Height = cell size, edit Icon Rect + Background Image sprite/color. Runtime clones this.")]
        [SerializeField] private GameObject bagSlotTemplate;
        [SerializeField] private Text emptyBagText;
        [SerializeField] private Button closeButton;
        [SerializeField] private ItemTooltipUI itemTooltip;
        [SerializeField] private Game.UI.EnhancePanelUI enhancePanelUI;
        [SerializeField] private Game.UI.BuildPanelUI buildPanelUI;
        [SerializeField] private Button buildButton;
        [SerializeField] private Game.UI.RosterPanelUI rosterPanelUI;
        [SerializeField] private Button classButton;
        [SerializeField] private Game.Data.EnhanceCatalog enhanceCatalog;
        [SerializeField] private bool seedDemoItemsIfEmpty = true;

        private readonly List<GameObject> spawnedSlots = new List<GameObject>();
        private bool isOpen;
        private bool seededDemo;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            ApplyFramePresentation();
            HideEmptyBagLabel();
            ClearBagScrollBackground();
            ResolveBagSlotTemplate();
            // Template stays visible in Edit Mode; hide only while playing.
            if (bagSlotTemplate != null)
            {
                bagSlotTemplate.SetActive(false);
            }

            WireStaticButtons();
            EnsureBagDropZone();
            ApplyFramePresentation();
        }

        private void OnEnable()
        {
            ResolveServices();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (itemTooltip != null)
            {
                itemTooltip.Hide();
            }
        }

        private void ResolveServices()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<Game.Inventory.InventoryService>();
            }

            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<Game.Inventory.EquipmentService>();
            }

            if (itemDatabase == null)
            {
                Game.Inventory.ChestService chestService = FindFirstObjectByType<Game.Inventory.ChestService>();
                if (chestService != null)
                {
                    itemDatabase = chestService.ItemDatabase;
                }
            }

            if (itemTooltip == null)
            {
                itemTooltip = GetComponentInChildren<ItemTooltipUI>(true);
            }

            EnsureEquipmentSlotsFromHierarchy();
            WirePaperDollSlots();
        }

        private void Subscribe()
        {
            if (inventoryService != null)
            {
                inventoryService.InventoryChanged -= Refresh;
                inventoryService.InventoryChanged += Refresh;
            }

            if (equipmentService != null)
            {
                equipmentService.EquipmentChanged -= Refresh;
                equipmentService.EquipmentChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (inventoryService != null)
            {
                inventoryService.InventoryChanged -= Refresh;
            }

            if (equipmentService != null)
            {
                equipmentService.EquipmentChanged -= Refresh;
            }
        }

        private void WireStaticButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseFromUser);
            }

            ResolveBuildButton();
            if (buildButton != null)
            {
                buildButton.onClick.RemoveAllListeners();
                buildButton.onClick.AddListener(OpenBuildPanel);
                buildButton.gameObject.SetActive(true);
                buildButton.transform.SetAsLastSibling();
            }

            ResolveClassButton();
            if (classButton != null)
            {
                classButton.onClick.RemoveAllListeners();
                classButton.onClick.AddListener(OpenRosterPanel);
                classButton.gameObject.SetActive(true);
                classButton.transform.SetAsLastSibling();
            }

            EnsureEquipmentSlotsFromHierarchy();
            WirePaperDollSlots();
        }

        private void ResolveBuildButton()
        {
            if (buildButton == null)
            {
                Transform card = transform.Find("Card");
                Transform btnTf = card != null ? card.Find("BuildButton") : transform.Find("BuildButton");
                if (btnTf != null)
                {
                    buildButton = btnTf.GetComponent<Button>();
                }
            }

            if (buildPanelUI == null)
            {
                buildPanelUI = FindFirstObjectByType<BuildPanelUI>(FindObjectsInactive.Include);
            }

            if (buildButton == null)
            {
                return;
            }

            // Size/position are owned by ApplyFramePresentation → PlaceHeaderButtons.
            Image img = buildButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.12f, 0.55f, 0.48f, 1f);
                img.raycastTarget = true;
            }

            Text label = buildButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "BUILD";
                label.fontSize = 16;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.raycastTarget = false;
                if (label.rectTransform != null)
                {
                    label.rectTransform.localScale = Vector3.one;
                }
            }
        }

        public void OpenBuildPanel()
        {
            ResolveBuildButton();
            if (buildPanelUI == null)
            {
                buildPanelUI = FindFirstObjectByType<BuildPanelUI>(FindObjectsInactive.Include);
            }

            if (buildPanelUI != null)
            {
                Game.Build.RuneService runes = FindFirstObjectByType<Game.Build.RuneService>();
                Game.Build.SkillService skills = FindFirstObjectByType<Game.Build.SkillService>();
                buildPanelUI.Bind(runes, skills);
                buildPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[InventoryPanelUI] BuildPanelUI missing.");
            }
        }

        private void ResolveClassButton()
        {
            if (classButton == null)
            {
                Transform card = transform.Find("Card");
                Transform btnTf = card != null ? card.Find("ClassButton") : transform.Find("ClassButton");
                if (btnTf != null)
                {
                    classButton = btnTf.GetComponent<Button>();
                }
            }

            if (classButton == null)
            {
                classButton = Faz6RuntimeBootstrap.EnsureClassButton(transform);
            }

            if (rosterPanelUI == null)
            {
                rosterPanelUI = FindFirstObjectByType<RosterPanelUI>(FindObjectsInactive.Include);
            }

            if (rosterPanelUI == null)
            {
                rosterPanelUI = Faz6RuntimeBootstrap.EnsureRosterPanel();
            }

            if (classButton == null)
            {
                return;
            }

            // Size/position are owned by ApplyFramePresentation → PlaceHeaderButtons.
            Image img = classButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.35f, 0.28f, 0.55f, 1f);
                img.raycastTarget = true;
            }

            Text label = classButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "CLASS";
                label.fontSize = 16;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.raycastTarget = false;
                if (label.rectTransform != null)
                {
                    label.rectTransform.localScale = Vector3.one;
                }
            }
        }

        public void OpenRosterPanel()
        {
            ResolveClassButton();
            Faz6RuntimeBootstrap.EnsureRosterService();
            if (rosterPanelUI == null)
            {
                rosterPanelUI = Faz6RuntimeBootstrap.EnsureRosterPanel();
            }

            if (rosterPanelUI != null)
            {
                Game.Roster.RosterService roster = FindFirstObjectByType<Game.Roster.RosterService>();
                rosterPanelUI.Bind(roster);
                rosterPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[InventoryPanelUI] RosterPanelUI missing.");
            }
        }

        private void EnsureEquipmentSlotsFromHierarchy()
        {
            if (equipmentSlots == null)
            {
                equipmentSlots = new List<EquipmentSlotUI>();
            }

            if (equipmentSlots.Count > 0)
            {
                return;
            }

            Transform doll = transform.Find("Card/PaperDoll");
            if (doll == null)
            {
                return;
            }

            EquipmentSlotUI[] found = doll.GetComponentsInChildren<EquipmentSlotUI>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    equipmentSlots.Add(found[i]);
                }
            }
        }

        private void WirePaperDollSlots()
        {
            if (equipmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                EquipmentSlotUI slotUi = equipmentSlots[i];
                if (slotUi == null)
                {
                    continue;
                }

                slotUi.Configure(
                    slotUi.Slot,
                    null,
                    null,
                    null,
                    itemTooltip,
                    equipmentService,
                    enhanceCatalog);
            }
        }

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            ResolveServices();
            ResolveBuildButton();
            ResolveClassButton();
            ApplyFramePresentation();
            EnsureBagDropZone();
            Subscribe();
            TrySeedDemoItems();
            isOpen = true;
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            // Skip fade in battle: CanvasGroup alpha over magenta key flashes pink.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Game.Core.GameScenes.Battle)
            {
                UIPanelSlider slider = UIPanelSlider.EnsureOn(root);
                if (slider != null)
                {
                    slider.ShowImmediate();
                }
                else
                {
                    root.SetActive(true);
                }
            }
            else
            {
                UIPanelSlider.OpenRoot(root);
            }

            ApplyBattleInventoryPosition();
            Refresh();
        }

        /// <summary>
        /// In BattleDemo, keep the same inventory panel — only move it into the upper free area
        /// above the 320px battle band (960×960 canvas).
        /// </summary>
        private void ApplyBattleInventoryPosition()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != Game.Core.GameScenes.Battle)
            {
                return;
            }

            Transform card = transform.Find("Card");
            RectTransform cardRt = card != null ? card as RectTransform : null;
            if (cardRt == null)
            {
                return;
            }

            // Full-screen parent, center pivot: screen Y ∈ [-480, 480].
            // Battle band = bottom 320px → [-480, -160]. Upper band midpoint = 160.
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, 160f);
            cardRt.localScale = Vector3.one;
        }

        public void Close()
        {
            isOpen = false;
            if (itemTooltip != null)
            {
                itemTooltip.Hide();
            }

            GameObject root = panelRoot != null ? panelRoot : gameObject;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Game.Core.GameScenes.Battle)
            {
                UIPanelSlider slider = root != null ? root.GetComponent<UIPanelSlider>() : null;
                if (slider != null)
                {
                    slider.HideImmediate();
                }
                else if (root != null)
                {
                    root.SetActive(false);
                }
            }
            else
            {
                UIPanelSlider.CloseRoot(panelRoot, gameObject);
            }
        }

        public void CloseFromUser()
        {
            Close();
            Game.UI.HubMenuUI hubMenu = FindFirstObjectByType<Game.UI.HubMenuUI>(FindObjectsInactive.Include);
            if (hubMenu != null)
            {
                hubMenu.NotifyPanelClosed();
            }

            Game.UI.BattleMenuUI battleMenu = FindFirstObjectByType<Game.UI.BattleMenuUI>(FindObjectsInactive.Include);
            if (battleMenu != null)
            {
                battleMenu.NotifyPanelClosed();
            }
        }

        public void Refresh()
        {
            RefreshEquipped();
            RefreshBag();
        }

        private void TrySeedDemoItems()
        {
            if (!seedDemoItemsIfEmpty || seededDemo || inventoryService == null || itemDatabase == null)
            {
                return;
            }

            if (inventoryService.Instances != null && inventoryService.Instances.Count > 0)
            {
                return;
            }

            string[] demoIds = { "rusty_sword", "teal_ring", "moss_helm" };
            for (int i = 0; i < demoIds.Length; i++)
            {
                Game.Data.ItemData item = itemDatabase.GetById(demoIds[i]);
                if (item != null)
                {
                    inventoryService.AddItem(item.itemId);
                }
            }

            seededDemo = true;
        }

        private void RefreshEquipped()
        {
            EnsureEquipmentSlotsFromHierarchy();
            WirePaperDollSlots();

            bool hasPaperDoll = equipmentSlots != null && equipmentSlots.Count > 0;
            Transform legacyEq = transform.Find("Card/EquippedSection");
            if (legacyEq != null)
            {
                legacyEq.gameObject.SetActive(!hasPaperDoll);
            }

            if (hasPaperDoll)
            {
                for (int i = 0; i < equipmentSlots.Count; i++)
                {
                    if (equipmentSlots[i] != null)
                    {
                        equipmentSlots[i].Refresh();
                    }
                }
            }

            if (bonusSummaryText != null && equipmentService != null)
            {
                bonusSummaryText.text =
                    $"ATK +{equipmentService.GetTotalAttackBonus()}   HP +{equipmentService.GetTotalHealthBonus()}";
            }
            else if (bonusSummaryText != null)
            {
                bonusSummaryText.text = "ATK +0   HP +0";
            }
        }

        private void RefreshBag()
        {
            ClearSlots();
            HideEmptyBagLabel();
            ClearBagScrollBackground();
            EnsureBagGridLayout();

            if (bagContentRoot == null || inventoryService == null || itemDatabase == null)
            {
                return;
            }

            List<Game.Save.ItemInstanceSave> items = new List<Game.Save.ItemInstanceSave>();
            IReadOnlyList<Game.Save.ItemInstanceSave> source = inventoryService.Instances;
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    Game.Save.ItemInstanceSave inst = source[i];
                    if (inst == null || string.IsNullOrEmpty(inst.itemId))
                    {
                        continue;
                    }

                    // Equipped items only show on the paper doll, not in the bag.
                    if (equipmentService != null && equipmentService.IsInstanceEquipped(inst.uid))
                    {
                        continue;
                    }

                    items.Add(inst);
                }
            }

            items.Sort((a, b) =>
            {
                Game.Data.ItemData ia = itemDatabase.GetById(a.itemId);
                Game.Data.ItemData ib = itemDatabase.GetById(b.itemId);
                if (ia == null || ib == null)
                {
                    return string.CompareOrdinal(a.itemId, b.itemId);
                }

                int slot = ia.equipmentSlot.CompareTo(ib.equipmentSlot);
                if (slot != 0)
                {
                    return slot;
                }

                int rarity = ib.rarity.CompareTo(ia.rarity);
                if (rarity != 0)
                {
                    return rarity;
                }

                int stars = b.stars.CompareTo(a.stars);
                if (stars != 0)
                {
                    return stars;
                }

                int name = string.CompareOrdinal(ia.displayName, ib.displayName);
                return name != 0 ? name : string.CompareOrdinal(a.uid, b.uid);
            });

            for (int i = 0; i < items.Count; i++)
            {
                CreateBagSlot(items[i]);
            }
        }

        private void HideEmptyBagLabel()
        {
            if (emptyBagText != null)
            {
                emptyBagText.gameObject.SetActive(false);
            }

            Transform emptyTf = transform.Find("Card/BagSection/EmptyBagText");
            if (emptyTf != null)
            {
                emptyTf.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Make Inventory_Frame.png fill the card at true 3:2 and restore backdrop dim.
        /// Size is fitted to the canvas (ref 1080x720) so the frame never clips.
        /// </summary>
        private void ApplyFramePresentation()
        {
            Transform card = transform.Find("Card");
            if (card == null)
            {
                return;
            }

            RectTransform panelRt = (panelRoot != null ? panelRoot : gameObject).GetComponent<RectTransform>();
            if (panelRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
            }

            // Fit 3:2 inside the live canvas, leaving room for SideNav + margins.
            float availW = panelRt != null ? Mathf.Max(320f, panelRt.rect.width) : 1080f;
            float availH = panelRt != null ? Mathf.Max(240f, panelRt.rect.height) : 720f;
            const float sideNavPad = 110f;
            const float edgePad = 16f;
            float maxW = Mathf.Max(280f, availW - sideNavPad - edgePad);
            float maxH = Mathf.Max(200f, availH - edgePad * 2f);

            // Prefer filling height (short Game views clip first), keep 3:2.
            float frameH = maxH;
            float frameW = frameH * 1.5f;
            if (frameW > maxW)
            {
                frameW = maxW;
                frameH = frameW / 1.5f;
            }

            // Slight right bias so SideNav doesn't cover the left lanterns.
            float centerX = (sideNavPad - edgePad) * 0.5f;

            RectTransform cardRt = card.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = new Vector2(centerX, 0f);
                cardRt.sizeDelta = new Vector2(frameW, frameH);
                cardRt.localScale = Vector3.one;
            }

            Image bg = cardBackground != null ? cardBackground : card.GetComponent<Image>();
            if (bg != null)
            {
                cardBackground = bg;
                bg.color = Color.white;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = true;
                bg.raycastTarget = true;
            }

            Image dim = GetComponent<Image>();
            if (dim != null && dim != bg)
            {
                dim.enabled = true;
                dim.sprite = null;
                dim.raycastTarget = true;
                // Battle uses magenta color-key for desktop see-through. A semi-transparent
                // black dim over that key color becomes pink and no longer keys out.
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Game.Core.GameScenes.Battle)
                {
                    dim.color = new Color(0f, 0f, 0f, 0f);
                }
                else
                {
                    dim.color = new Color(0f, 0f, 0f, 0.62f);
                }
            }

            transform.localScale = Vector3.one;

            SetChildActive(card, "InventoryTitle", false);
            SetChildActive(card, "Divider", false);

            ClearBagScrollBackground();
            Transform bagSection = card.Find("BagSection");
            if (bagSection != null)
            {
                Image bagImg = bagSection.GetComponent<Image>();
                if (bagImg != null)
                {
                    bagImg.color = new Color(1f, 1f, 1f, 0f);
                }

                // Painted bag wells on Inventory_Frame.png (7 cols × 5 rows).
                RectTransform bagRt = bagSection.GetComponent<RectTransform>();
                if (bagRt != null)
                {
                    bagRt.anchorMin = new Vector2(0.435f, 0.241f);
                    bagRt.anchorMax = new Vector2(0.893f, 0.697f);
                    bagRt.offsetMin = Vector2.zero;
                    bagRt.offsetMax = Vector2.zero;
                    bagRt.anchoredPosition = Vector2.zero;
                    bagRt.sizeDelta = Vector2.zero;
                }

                Transform bagScroll = bagSection.Find("BagScroll");
                if (bagScroll != null)
                {
                    RectTransform scrollRt = bagScroll.GetComponent<RectTransform>();
                    if (scrollRt != null)
                    {
                        scrollRt.anchorMin = Vector2.zero;
                        scrollRt.anchorMax = Vector2.one;
                        scrollRt.offsetMin = Vector2.zero;
                        scrollRt.offsetMax = Vector2.zero;
                        scrollRt.anchoredPosition = Vector2.zero;
                        scrollRt.sizeDelta = Vector2.zero;
                    }

                    Transform viewport = bagScroll.Find("Viewport");
                    if (viewport != null)
                    {
                        RectTransform vpRt = viewport.GetComponent<RectTransform>();
                        if (vpRt != null)
                        {
                            vpRt.anchorMin = Vector2.zero;
                            vpRt.anchorMax = Vector2.one;
                            vpRt.offsetMin = Vector2.zero;
                            vpRt.offsetMax = Vector2.zero;
                            vpRt.anchoredPosition = Vector2.zero;
                            vpRt.sizeDelta = Vector2.zero;
                        }
                    }
                }
            }

            Transform paperDoll = card.Find("PaperDoll");
            if (paperDoll != null)
            {
                RectTransform dollRt = paperDoll.GetComponent<RectTransform>();
                if (dollRt != null)
                {
                    dollRt.anchorMin = new Vector2(0.07f, 0.16f);
                    dollRt.anchorMax = new Vector2(0.40f, 0.78f);
                    dollRt.offsetMin = Vector2.zero;
                    dollRt.offsetMax = Vector2.zero;
                    dollRt.anchoredPosition = Vector2.zero;
                    dollRt.sizeDelta = Vector2.zero;
                }

                PlacePaperDollSlots(paperDoll, frameW, frameH);
            }

            PlaceHeaderButtons(card, frameW, frameH);
            PlaceCloseButton(card, frameW, frameH);

            // Refit bag cells now that BagSection has its final rect.
            Canvas.ForceUpdateCanvases();
            EnsureBagGridLayout();
        }

        private void PlacePaperDollSlots(Transform paperDoll, float frameW, float frameH)
        {
            // Scale equipment wells with the frame so icons stay readable at 1080p.
            float slot = Mathf.Clamp(frameH * 0.145f, 88f, 120f);
            float col = slot * 1.05f;
            float row = slot * 1.12f;
            float leftX = -col;
            float rightX = col;
            float topY = row * 1.55f;

            // Left column: Helmet, Armor, Legs, Accessory
            // Right column: Shoulders, Weapon, Boots, Ring2
            ApplySlotSize(paperDoll, "Eq_Helmet", leftX, topY, slot);
            ApplySlotSize(paperDoll, "Eq_Armor", leftX, topY - row, slot);
            ApplySlotSize(paperDoll, "Eq_Legs", leftX, topY - row * 2f, slot);
            ApplySlotSize(paperDoll, "Eq_Accessory", leftX, topY - row * 3f, slot);
            ApplySlotSize(paperDoll, "Eq_Shoulders", rightX, topY, slot);
            ApplySlotSize(paperDoll, "Eq_Weapon", rightX, topY - row, slot);
            ApplySlotSize(paperDoll, "Eq_Boots", rightX, topY - row * 2f, slot);
            ApplySlotSize(paperDoll, "Eq_Ring2", rightX, topY - row * 3f, slot);
        }

        private static void ApplySlotSize(Transform paperDoll, string name, float x, float y, float size)
        {
            Transform tf = paperDoll.Find(name);
            if (tf == null)
            {
                return;
            }

            RectTransform rt = tf.GetComponent<RectTransform>();
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one;
        }

        private void PlaceHeaderButtons(Transform card, float frameW, float frameH)
        {
            float halfW = frameW * 0.5f;
            float halfH = frameH * 0.5f;
            // Sit just under the ornate title bar inside the frame.
            float y = halfH - Mathf.Clamp(frameH * 0.11f, 36f, 58f);
            float btnH = Mathf.Clamp(frameH * 0.065f, 28f, 40f);
            float btnW = Mathf.Clamp(frameW * 0.12f, 90f, 120f);

            if (buildButton != null)
            {
                RectTransform rect = buildButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(-halfW + btnW * 0.7f + 18f, y);
                    rect.sizeDelta = new Vector2(btnW, btnH);
                    rect.localScale = Vector3.one;
                }
            }

            if (classButton != null)
            {
                RectTransform rect = classButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(-halfW + btnW * 1.85f + 28f, y);
                    rect.sizeDelta = new Vector2(btnW, btnH);
                    rect.localScale = Vector3.one;
                }
            }
        }

        private void PlaceCloseButton(Transform card, float frameW, float frameH)
        {
            Transform closeTf = card.Find("CloseInventoryButton");
            if (closeTf == null && closeButton != null)
            {
                closeTf = closeButton.transform;
            }

            if (closeTf == null)
            {
                return;
            }

            float halfW = frameW * 0.5f;
            float halfH = frameH * 0.5f;
            RectTransform closeRt = closeTf.GetComponent<RectTransform>();
            if (closeRt != null)
            {
                closeRt.anchorMin = new Vector2(0.5f, 0.5f);
                closeRt.anchorMax = new Vector2(0.5f, 0.5f);
                closeRt.pivot = new Vector2(0.5f, 0.5f);
                closeRt.anchoredPosition = new Vector2(halfW - 28f, halfH - 28f);
                closeRt.sizeDelta = new Vector2(40f, 40f);
            }
        }

        private static void SetChildActive(Transform parent, string childName, bool active)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private void ClearBagScrollBackground()
        {
            Transform scrollTf = transform.Find("Card/BagSection/BagScroll");
            if (scrollTf == null)
            {
                return;
            }

            Image scrollImg = scrollTf.GetComponent<Image>();
            if (scrollImg != null)
            {
                scrollImg.sprite = null;
                scrollImg.color = new Color(1f, 1f, 1f, 0f);
            }

            Transform vp = scrollTf.Find("Viewport");
            if (vp != null)
            {
                Image vpImg = vp.GetComponent<Image>();
                if (vpImg != null)
                {
                    // Keep tiny alpha so RectMask2D / scroll raycasts still work.
                    vpImg.color = new Color(1f, 1f, 1f, 0.001f);
                }
            }
        }

        private void EnsureBagGridLayout()
        {
            if (bagContentRoot == null)
            {
                return;
            }

            VerticalLayoutGroup oldVlg = bagContentRoot.GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null)
            {
                Destroy(oldVlg);
            }

            GridLayoutGroup grid = bagContentRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = bagContentRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            // Match painted wells: 7 columns × 5 rows, thin gutters (~8% of pitch).
            const int columns = 7;
            const int rows = 5;
            const float spacingRatio = 0.08f;
            float bagW = ResolveBagLayoutWidth();
            float bagH = ResolveBagLayoutHeight();
            float pitchX = bagW > 1f ? bagW / columns : 40f;
            float pitchY = bagH > 1f ? bagH / rows : pitchX;
            float spacingX = Mathf.Max(2f, pitchX * spacingRatio);
            float spacingY = Mathf.Max(2f, pitchY * spacingRatio);
            float cellX = Mathf.Max(16f, pitchX - spacingX);
            float cellY = Mathf.Max(16f, pitchY - spacingY);

            grid.cellSize = new Vector2(cellX, cellY);
            grid.spacing = new Vector2(spacingX, spacingY);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = bagContentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = bagContentRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform contentRt = bagContentRoot as RectTransform;
            if (contentRt != null)
            {
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.offsetMin = new Vector2(0f, contentRt.offsetMin.y);
                contentRt.offsetMax = new Vector2(0f, 0f);
            }
        }

        private float ResolveBagLayoutWidth()
        {
            RectTransform rt = bagContentRoot as RectTransform;
            if (rt != null && rt.rect.width > 1f)
            {
                return rt.rect.width;
            }

            Transform scroll = transform.Find("Card/BagSection/BagScroll/Viewport");
            if (scroll == null)
            {
                scroll = transform.Find("Card/BagSection");
            }

            RectTransform parentRt = scroll != null ? scroll.GetComponent<RectTransform>() : null;
            return parentRt != null && parentRt.rect.width > 1f ? parentRt.rect.width : 280f;
        }

        private float ResolveBagLayoutHeight()
        {
            Transform section = transform.Find("Card/BagSection");
            RectTransform sectionRt = section != null ? section.GetComponent<RectTransform>() : null;
            if (sectionRt != null && sectionRt.rect.height > 1f)
            {
                return sectionRt.rect.height;
            }

            RectTransform rt = bagContentRoot as RectTransform;
            return rt != null && rt.rect.height > 1f ? rt.rect.height : 200f;
        }

        private void CreateBagSlot(Game.Save.ItemInstanceSave inst)
        {
            if (inst == null || itemDatabase == null)
            {
                return;
            }

            Game.Data.ItemData item = itemDatabase.GetById(inst.itemId);
            if (item == null)
            {
                return;
            }

            ResolveBagSlotTemplate();
            GameObject cell;
            if (bagSlotTemplate != null)
            {
                cell = Instantiate(bagSlotTemplate, bagContentRoot);
                cell.name = $"Slot_{inst.uid}";
                cell.SetActive(true);
            }
            else
            {
                cell = BuildFallbackSlot(inst.uid);
            }

            InventorySlotUI slot = cell.GetComponent<InventorySlotUI>();
            if (slot == null)
            {
                slot = cell.AddComponent<InventorySlotUI>();
            }

            slot.AutoWireChildren();
            slot.Bind(item, inst, itemTooltip, equipmentService);
            spawnedSlots.Add(cell);
        }

        private void EnsureBagDropZone()
        {
            // Prefer BagScroll (fills the bag area and already has an Image for raycasts).
            Transform target = transform.Find("Card/BagSection/BagScroll");
            if (target == null)
            {
                target = transform.Find("Card/BagSection");
            }

            if (target == null)
            {
                return;
            }

            BagDropZone zone = target.GetComponent<BagDropZone>();
            if (zone == null)
            {
                zone = target.gameObject.AddComponent<BagDropZone>();
            }

            zone.Configure(equipmentService);
        }

        private void ResolveBagSlotTemplate()
        {
            if (bagSlotTemplate != null)
            {
                return;
            }

            Transform card = transform.Find("Card");
            Transform templateTf = card != null
                ? card.Find("BagSection/BagSlotTemplate")
                : transform.Find("BagSlotTemplate");
            if (templateTf != null)
            {
                bagSlotTemplate = templateTf.gameObject;
            }
        }

        private GameObject BuildFallbackSlot(string uid)
        {
            GameObject cell = new GameObject($"Slot_{uid}");
            cell.transform.SetParent(bagContentRoot, false);

            Image bg = cell.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.001f);
            bg.raycastTarget = true;

            Button btn = cell.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cell.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.06f, 0.06f);
            iconRt.anchorMax = new Vector2(0.94f, 0.94f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            InventorySlotUI slot = cell.AddComponent<InventorySlotUI>();
            slot.ConfigureVisuals(iconImg, bg, null, btn);
            return cell;
        }

        public static Color GetRarityColorPublic(Game.Data.ItemRarity rarity)
        {
            switch (rarity)
            {
                case Game.Data.ItemRarity.Common: return new Color(0.78f, 0.82f, 0.8f);
                case Game.Data.ItemRarity.Uncommon: return new Color(0.45f, 0.85f, 0.55f);
                case Game.Data.ItemRarity.Rare: return new Color(0.4f, 0.7f, 1f);
                case Game.Data.ItemRarity.Epic: return new Color(0.78f, 0.45f, 1f);
                case Game.Data.ItemRarity.Legendary: return new Color(1f, 0.75f, 0.3f);
                default: return Color.white;
            }
        }

        private void ClearSlots()
        {
            if (itemTooltip != null)
            {
                itemTooltip.Hide();
            }

            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                {
                    Destroy(spawnedSlots[i]);
                }
            }

            spawnedSlots.Clear();
        }
    }
}
