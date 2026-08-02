using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Per-building village panel (Library / Mine). Blacksmith uses CraftPanelUI.
    /// </summary>
    public class BuildingPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Game.Data.VillageBuildingType buildingType = Game.Data.VillageBuildingType.Library;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Game.Village.VillageService villageService;

        public Game.Data.VillageBuildingType BuildingType => buildingType;

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
            // Do NOT Close() here — SetActive(true) during Open() would re-enter Awake and
            // immediately deactivate the panel again (common Unity gotcha).
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
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgrade);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnUpgrade()
        {
            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (villageService != null)
            {
                villageService.TryUpgrade(buildingType);
                Refresh();
            }
        }

        public void Refresh()
        {
            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            string name = buildingType.ToString();
            if (titleText != null)
            {
                titleText.text = name.ToUpperInvariant();
            }

            if (villageService == null)
            {
                return;
            }

            Game.Data.VillageBuildingData data = villageService.Catalog != null
                ? villageService.Catalog.GetByType(buildingType)
                : null;
            int level = villageService.GetBuildingLevel(buildingType);

            string detail;
            if (buildingType == Game.Data.VillageBuildingType.Library)
            {
                detail = $"Lv.{level}\nEXP {villageService.GetRateExp(buildingType)}/h";
            }
            else if (buildingType == Game.Data.VillageBuildingType.Mine)
            {
                detail = $"Lv.{level}\nGold {villageService.GetRateGold(buildingType)}/h";
            }
            else
            {
                detail = $"Lv.{level}\nCraft +{villageService.GetCraftSpeedBonus():P0}";
            }

            if (detailText != null)
            {
                detailText.text = detail;
            }

            if (statusText != null)
            {
                statusText.text = data != null ? data.displayName : name;
            }

            if (upgradeButton != null)
            {
                bool can = villageService.CanUpgrade(buildingType, out string reason);
                upgradeButton.interactable = can || (data != null && level >= data.maxLevel);
                Text btnText = upgradeButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    if (data != null && level >= data.maxLevel)
                    {
                        btnText.text = "MAX";
                        upgradeButton.interactable = false;
                    }
                    else if (data != null)
                    {
                        int cost = data.GetUpgradeGoldCost(level);
                        btnText.text = can ? $"Upgrade {cost}g" : (string.IsNullOrEmpty(reason) ? "Locked" : reason);
                    }
                    else
                    {
                        btnText.text = "Upgrade";
                    }
                }
            }
        }
    }
}
