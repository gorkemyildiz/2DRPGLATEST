using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class VillagePanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text libraryText;
        [SerializeField] private Text mineText;
        [SerializeField] private Text blacksmithText;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button upgradeLibraryButton;
        [SerializeField] private Button upgradeMineButton;
        [SerializeField] private Button upgradeBlacksmithButton;
        [SerializeField] private Button openCraftButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Game.Village.VillageService villageService;
        [SerializeField] private Game.UI.CraftPanelUI craftPanelUI;

        public void Bind(Game.Village.VillageService service)
        {
            villageService = service;
            WireButtons();
            Refresh();
        }

        public void Open()
        {
            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (craftPanelUI == null)
            {
                craftPanelUI = FindFirstObjectByType<Game.UI.CraftPanelUI>(FindObjectsInactive.Include);
            }

            if (craftPanelUI != null)
            {
                craftPanelUI.Close();
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

        public void CloseFromUser()
        {
            Close();
            Game.UI.HubMenuUI hubMenu = FindFirstObjectByType<Game.UI.HubMenuUI>(FindObjectsInactive.Include);
            if (hubMenu != null)
            {
                hubMenu.NotifyPanelClosed();
            }
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
            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (villageService != null)
            {
                villageService.BuildingsChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (villageService != null)
            {
                villageService.BuildingsChanged -= Refresh;
            }
        }

        private void WireButtons()
        {
            WireUpgrade(upgradeLibraryButton, Game.Data.VillageBuildingType.Library);
            WireUpgrade(upgradeMineButton, Game.Data.VillageBuildingType.Mine);
            WireUpgrade(upgradeBlacksmithButton, Game.Data.VillageBuildingType.Blacksmith);

            if (openCraftButton != null)
            {
                openCraftButton.onClick.RemoveAllListeners();
                openCraftButton.onClick.AddListener(OpenCraft);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseFromUser);
            }
        }

        private void OpenCraft()
        {
            if (craftPanelUI == null)
            {
                craftPanelUI = FindFirstObjectByType<Game.UI.CraftPanelUI>(FindObjectsInactive.Include);
            }

            if (craftPanelUI != null)
            {
                Close();
                craftPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[VillagePanelUI] CraftPanelUI missing.");
            }
        }

        private void WireUpgrade(Button button, Game.Data.VillageBuildingType type)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (villageService == null)
                {
                    villageService = FindFirstObjectByType<Game.Village.VillageService>();
                }

                if (villageService != null)
                {
                    villageService.TryUpgrade(type);
                    Refresh();
                }
            });
        }

        public void Refresh()
        {
            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (titleText != null)
            {
                titleText.text = "VILLAGE";
            }

            SetBuildingLine(libraryText, upgradeLibraryButton, Game.Data.VillageBuildingType.Library, "Library", "EXP/h");
            SetBuildingLine(mineText, upgradeMineButton, Game.Data.VillageBuildingType.Mine, "Mine", "Gold/h");
            SetBuildingLine(blacksmithText, upgradeBlacksmithButton, Game.Data.VillageBuildingType.Blacksmith, "Blacksmith", "Craft");

            if (statusText != null && villageService != null)
            {
                statusText.text =
                    $"AFK passive\nGold {villageService.GetTotalGoldPerHour()}/h   ·   EXP {villageService.GetTotalExperiencePerHour()}/h";
            }
        }

        private void SetBuildingLine(
            Text label,
            Button upgradeButton,
            Game.Data.VillageBuildingType type,
            string name,
            string rateLabel)
        {
            if (villageService == null)
            {
                return;
            }

            Game.Data.VillageBuildingData data = villageService.Catalog != null
                ? villageService.Catalog.GetByType(type)
                : null;
            int level = villageService.GetBuildingLevel(type);

            string detail;
            if (type == Game.Data.VillageBuildingType.Blacksmith)
            {
                float bonus = villageService.GetCraftSpeedBonus();
                detail = $"Lv.{level}  Craft +{bonus:P0}";
            }
            else if (type == Game.Data.VillageBuildingType.Mine)
            {
                detail = $"Lv.{level}  {villageService.GetRateGold(type)} {rateLabel}";
            }
            else
            {
                detail = $"Lv.{level}  {villageService.GetRateExp(type)} {rateLabel}";
            }

            if (label != null)
            {
                label.text = $"{name}\n{detail}";
            }

            if (upgradeButton != null)
            {
                bool can = villageService.CanUpgrade(type, out string reason);
                upgradeButton.interactable = can;
                Text btnText = upgradeButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    if (data != null && level < data.maxLevel)
                    {
                        int cost = data.GetUpgradeGoldCost(level);
                        btnText.text = can ? $"{cost}g" : "Locked";
                        if (!can && !string.IsNullOrEmpty(reason) && reason.Length <= 14)
                        {
                            btnText.text = reason;
                        }
                    }
                    else
                    {
                        btnText.text = "MAX";
                    }
                }
            }
        }
    }
}
