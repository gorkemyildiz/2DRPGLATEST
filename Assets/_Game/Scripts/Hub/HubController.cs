using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Hub
{
    public class HubController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Game.UI.HubMenuUI hubMenuUI;
        [SerializeField] private Game.UI.InventoryPanelUI inventoryPanel;
        [SerializeField] private Game.UI.MapSelectUI mapSelectUI;
        [SerializeField] private Game.UI.VillagePanelUI villagePanelUI;
        [SerializeField] private Game.UI.BuildingPanelUI libraryPanelUI;
        [SerializeField] private Game.UI.BuildingPanelUI minePanelUI;
        [SerializeField] private Game.UI.CraftPanelUI craftPanelUI;
        [SerializeField] private Game.UI.BuildPanelUI buildPanelUI;
        [SerializeField] private Game.UI.RosterPanelUI rosterPanelUI;
        [SerializeField] private Game.UI.WeeklyDungeonPanelUI weeklyDungeonPanelUI;
        [SerializeField] private Game.UI.AFKSummaryUI afkSummaryUI;

        [Header("Data")]
        [SerializeField] private Game.Data.MapCatalog mapCatalog;
        [SerializeField] private Game.Data.VillageCatalog villageCatalog;
        [SerializeField] private Game.Data.CraftCatalog craftCatalog;

        [Header("Systems")]
        [SerializeField] private Game.Village.VillageService villageService;
        [SerializeField] private Game.Crafting.CraftService craftService;
        [SerializeField] private Game.AFK.AFKRewardService afkRewardService;
        [SerializeField] private Game.Rewards.RewardService rewardService;
        [SerializeField] private Game.Save.SaveService saveService;

        [Header("Scene")]
        [SerializeField] private string battleSceneName = Game.Core.GameScenes.Battle;

        private void Start()
        {
            Game.UI.BattleViewportLayout.FindOrCreate();

            Game.Save.GameSaveController.LoadGameIfNeeded();
            ResolveRefs();

            if (hubMenuUI != null)
            {
                hubMenuUI.Bind(this);
            }

            if (mapSelectUI != null)
            {
                mapSelectUI.Bind(this, mapCatalog);
                mapSelectUI.Close();
            }

            if (villagePanelUI != null)
            {
                villagePanelUI.Bind(villageService);
                villagePanelUI.Close();
            }

            if (libraryPanelUI != null)
            {
                libraryPanelUI.Bind(villageService);
                libraryPanelUI.Close();
            }

            if (minePanelUI != null)
            {
                minePanelUI.Bind(villageService);
                minePanelUI.Close();
            }

            if (craftPanelUI != null)
            {
                craftPanelUI.Bind(craftService);
                craftPanelUI.Close();
            }

            if (craftService != null)
            {
                craftService.EnsureStarterMaterialsIfEmpty();
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.Close();
            }

            Game.UI.Faz6RuntimeBootstrap.EnsureRosterService();
            Game.UI.Faz6RuntimeBootstrap.EnsureWeeklyService();

            TryShowAfkRewards();
            Game.Save.GameSaveController.SaveGame();
            ApplyHubLaunchIntent();
            Debug.Log("[HubController] Village hub ready.");
        }

        private void ApplyHubLaunchIntent()
        {
            Game.Core.HubLaunchIntent.PendingAction intent = Game.Core.HubLaunchIntent.Consume();
            switch (intent)
            {
                case Game.Core.HubLaunchIntent.PendingAction.OpenAdventure:
                    StartAdventure();
                    break;
                case Game.Core.HubLaunchIntent.PendingAction.OpenInventory:
                    OpenInventory();
                    break;
            }
        }

        private void ResolveRefs()
        {
            if (hubMenuUI == null)
            {
                hubMenuUI = FindFirstObjectByType<Game.UI.HubMenuUI>(FindObjectsInactive.Include);
            }

            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<Game.UI.InventoryPanelUI>(FindObjectsInactive.Include);
            }

            if (mapSelectUI == null)
            {
                mapSelectUI = FindFirstObjectByType<Game.UI.MapSelectUI>(FindObjectsInactive.Include);
            }

            if (villagePanelUI == null)
            {
                villagePanelUI = FindFirstObjectByType<Game.UI.VillagePanelUI>(FindObjectsInactive.Include);
            }

            if (libraryPanelUI == null || minePanelUI == null)
            {
                Game.UI.BuildingPanelUI[] buildingPanels =
                    FindObjectsByType<Game.UI.BuildingPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < buildingPanels.Length; i++)
                {
                    Game.UI.BuildingPanelUI panel = buildingPanels[i];
                    if (panel == null)
                    {
                        continue;
                    }

                    if (panel.BuildingType == Game.Data.VillageBuildingType.Library && libraryPanelUI == null)
                    {
                        libraryPanelUI = panel;
                    }
                    else if (panel.BuildingType == Game.Data.VillageBuildingType.Mine && minePanelUI == null)
                    {
                        minePanelUI = panel;
                    }
                }
            }

            if (craftPanelUI == null)
            {
                craftPanelUI = FindFirstObjectByType<Game.UI.CraftPanelUI>(FindObjectsInactive.Include);
            }

            if (buildPanelUI == null)
            {
                buildPanelUI = FindFirstObjectByType<Game.UI.BuildPanelUI>(FindObjectsInactive.Include);
            }

            if (rosterPanelUI == null)
            {
                rosterPanelUI = FindFirstObjectByType<Game.UI.RosterPanelUI>(FindObjectsInactive.Include);
            }

            if (weeklyDungeonPanelUI == null)
            {
                weeklyDungeonPanelUI = FindFirstObjectByType<Game.UI.WeeklyDungeonPanelUI>(FindObjectsInactive.Include);
            }

            if (afkSummaryUI == null)
            {
                afkSummaryUI = FindFirstObjectByType<Game.UI.AFKSummaryUI>(FindObjectsInactive.Include);
            }

            if (villageService == null)
            {
                villageService = FindFirstObjectByType<Game.Village.VillageService>();
            }

            if (villageService != null && villageCatalog != null)
            {
                villageService.SetCatalog(villageCatalog);
            }

            if (craftService == null)
            {
                craftService = FindFirstObjectByType<Game.Crafting.CraftService>();
            }

            if (craftService != null && craftCatalog != null)
            {
                craftService.SetCatalog(craftCatalog);
            }

            if (afkRewardService == null)
            {
                afkRewardService = FindFirstObjectByType<Game.AFK.AFKRewardService>();
            }

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (saveService == null)
            {
                saveService = FindFirstObjectByType<Game.Save.SaveService>();
            }
        }

        private void TryShowAfkRewards()
        {
            if (afkRewardService == null || afkSummaryUI == null)
            {
                return;
            }

            System.DateTime lastSave = System.DateTime.MinValue;
            if (saveService != null)
            {
                lastSave = saveService.GetLastSaveTime();
            }

            Game.AFK.AFKReward reward = afkRewardService.CalculateAFKReward(lastSave);
            if (reward.goldEarned > 0 || reward.experienceEarned > 0 || reward.scrapEarned > 0 || reward.chestDropped)
            {
                afkSummaryUI.Bind(afkRewardService, rewardService);
                afkSummaryUI.Show(reward);
            }
        }

        /// <summary>
        /// Side-nav Village: close overlays and stay on plaza (no upgrade list popup).
        /// </summary>
        public void ShowVillagePlaza()
        {
            CloseOverlayPanels();
        }

        public void OpenBuilding(Game.Data.VillageBuildingType type)
        {
            switch (type)
            {
                case Game.Data.VillageBuildingType.Library:
                    OpenLibrary();
                    break;
                case Game.Data.VillageBuildingType.Mine:
                    OpenMine();
                    break;
                case Game.Data.VillageBuildingType.Blacksmith:
                    OpenCraft();
                    break;
                default:
                    Debug.LogWarning($"[HubController] Unknown building {type}");
                    break;
            }
        }

        public void OpenLibrary()
        {
            ResolveRefs();
            CloseOverlayPanels(keepLibrary: true);
            if (libraryPanelUI != null)
            {
                libraryPanelUI.Bind(villageService);
                libraryPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] LibraryPanel missing.");
            }
        }

        public void OpenMine()
        {
            ResolveRefs();
            CloseOverlayPanels(keepMine: true);
            if (minePanelUI != null)
            {
                minePanelUI.Bind(villageService);
                minePanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] MinePanel missing.");
            }
        }

        public void OpenMapSelect()
        {
            ResolveRefs();
            CloseOverlayPanels(keepMap: true);
            if (mapSelectUI != null)
            {
                mapSelectUI.Bind(this, mapCatalog);
                mapSelectUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] MapSelectUI missing.");
            }
        }

        public void OpenVillage()
        {
            // Legacy combined panel — prefer per-building panels via OpenBuilding.
            ShowVillagePlaza();
        }

        public void StartAdventure()
        {
            OpenMapSelect();
        }

        public void StartLevel(Game.Data.LevelData level)
        {
            if (level == null)
            {
                Debug.LogWarning("[HubController] StartLevel called with null level.");
                return;
            }

            Game.Core.BattleSession.SelectLevel(level, mapCatalog);
            Game.Save.GameSaveController.SaveGame();
            Debug.Log($"[HubController] Starting level {level.displayName} -> {battleSceneName}");
            SceneManager.LoadScene(battleSceneName);
        }

        public void StartLayerBoss(Game.Data.LayerData layer)
        {
            if (layer == null)
            {
                Debug.LogWarning("[HubController] StartLayerBoss called with null layer.");
                return;
            }

            // Summon stone cost disabled for now — layer boss is free to enter.
            // Re-enable with layer.requireSummonStone when materials are ready.

            Game.Core.BattleSession.SelectLayerBoss(layer, mapCatalog);
            Game.Save.GameSaveController.SaveGame();
            Debug.Log($"[HubController] Starting layer boss {layer.displayName} -> {battleSceneName}");
            SceneManager.LoadScene(battleSceneName);
        }

        public void OpenCraft()
        {
            ResolveRefs();
            CloseOverlayPanels(keepCraft: true);
            if (craftPanelUI != null)
            {
                craftPanelUI.Bind(craftService);
                craftPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] CraftPanelUI missing.");
            }
        }

        public void OpenInventory()
        {
            ResolveRefs();
            CloseOverlayPanels(keepInventory: true);
            if (inventoryPanel != null)
            {
                inventoryPanel.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] Inventory panel not found in hub scene.");
            }
        }

        public void OpenBuild()
        {
            ResolveRefs();
            CloseOverlayPanels(keepBuild: true, keepInventory: true);
            if (buildPanelUI != null)
            {
                Game.Build.RuneService runes = FindFirstObjectByType<Game.Build.RuneService>();
                Game.Build.SkillService skills = FindFirstObjectByType<Game.Build.SkillService>();
                buildPanelUI.Bind(runes, skills);
                buildPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] BuildPanelUI missing.");
            }
        }

        public void OpenRoster()
        {
            ResolveRefs();
            CloseOverlayPanels(keepRoster: true, keepInventory: true);
            Game.UI.Faz6RuntimeBootstrap.EnsureRosterService();
            if (rosterPanelUI == null)
            {
                rosterPanelUI = Game.UI.Faz6RuntimeBootstrap.EnsureRosterPanel();
            }

            if (rosterPanelUI != null)
            {
                Game.Roster.RosterService roster = FindFirstObjectByType<Game.Roster.RosterService>();
                rosterPanelUI.Bind(roster);
                rosterPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] RosterPanelUI missing.");
            }
        }

        public void OpenWeeklyDungeon()
        {
            ResolveRefs();
            CloseOverlayPanels(keepWeekly: true, keepMap: true);
            Game.UI.Faz6RuntimeBootstrap.EnsureWeeklyService();
            if (weeklyDungeonPanelUI == null)
            {
                weeklyDungeonPanelUI = Game.UI.Faz6RuntimeBootstrap.EnsureWeeklyPanel();
            }

            if (weeklyDungeonPanelUI != null)
            {
                Game.Dungeon.WeeklyDungeonService weekly =
                    FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
                weeklyDungeonPanelUI.Bind(weekly, this);
                weeklyDungeonPanelUI.Open();
            }
            else
            {
                Debug.LogWarning("[HubController] WeeklyDungeonPanelUI missing.");
            }
        }

        public void StartWeeklyDungeon()
        {
            ResolveRefs();
            Game.UI.Faz6RuntimeBootstrap.EnsureWeeklyService();
            Game.Dungeon.WeeklyDungeonService weekly =
                FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
            if (weekly == null)
            {
                Debug.LogWarning("[HubController] WeeklyDungeonService missing.");
                return;
            }

            if (!weekly.TryEnter())
            {
                weekly.CanEnter(out string reason);
                Debug.LogWarning($"[HubController] Weekly enter blocked: {reason}");
                return;
            }

            Game.Save.GameSaveController.SaveGame();
            Debug.Log($"[HubController] Starting weekly dungeon -> {battleSceneName}");
            SceneManager.LoadScene(battleSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private void CloseOverlayPanels(
            bool keepMap = false,
            bool keepInventory = false,
            bool keepCraft = false,
            bool keepLibrary = false,
            bool keepMine = false,
            bool keepBuild = false,
            bool keepRoster = false,
            bool keepWeekly = false)
        {
            if (!keepMap && mapSelectUI != null)
            {
                mapSelectUI.Close();
            }

            if (!keepInventory && inventoryPanel != null)
            {
                inventoryPanel.Close();
            }

            if (!keepCraft && craftPanelUI != null)
            {
                craftPanelUI.Close();
            }

            if (!keepLibrary && libraryPanelUI != null)
            {
                libraryPanelUI.Close();
            }

            if (!keepMine && minePanelUI != null)
            {
                minePanelUI.Close();
            }

            if (!keepBuild && buildPanelUI != null)
            {
                buildPanelUI.Close();
            }

            if (!keepRoster && rosterPanelUI != null)
            {
                rosterPanelUI.Close();
            }

            if (!keepWeekly && weeklyDungeonPanelUI != null)
            {
                weeklyDungeonPanelUI.Close();
            }

            if (villagePanelUI != null)
            {
                villagePanelUI.Close();
            }
        }
    }
}
