using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Battle HUD left rail — same SideNav icons as VillageHub.
    /// Adventure / Village leave the stage; Inventory toggles the in-battle bag.
    /// </summary>
    public class BattleMenuUI : MonoBehaviour
    {
        [Header("Side Nav")]
        [SerializeField] private HubSideNavUI sideNav;
        [SerializeField] private Button adventureButton;
        [SerializeField] private Button villageButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private InventoryPanelUI inventoryPanel;

        [Header("Scene")]
        [SerializeField] private string hubSceneName = Game.Core.GameScenes.Hub;

        private void Awake()
        {
            BattleViewportLayout.FindOrCreate();

            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryPanelUI>(FindObjectsInactive.Include);
            }

            WireButtons();
        }

        private void OnEnable()
        {
            WireButtons();
            if (sideNav != null)
            {
                sideNav.ClearSelection(false);
            }
        }

        public void NotifyPanelClosed()
        {
            if (sideNav != null)
            {
                sideNav.ClearSelection(true);
            }
        }

        private void WireButtons()
        {
            Wire(adventureButton, OnAdventureClicked);
            Wire(villageButton, OnVillageClicked);
            Wire(inventoryButton, OnInventoryClicked);
            Wire(quitButton, OnQuitClicked);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void SelectTab(HubNavTab tab)
        {
            if (sideNav != null)
            {
                sideNav.Select(tab, true);
            }
        }

        private void OnAdventureClicked()
        {
            SelectTab(HubNavTab.Adventure);
            ReturnToHub(Game.Core.HubLaunchIntent.PendingAction.OpenAdventure);
        }

        private void OnVillageClicked()
        {
            SelectTab(HubNavTab.Village);
            ReturnToHub(Game.Core.HubLaunchIntent.PendingAction.None);
        }

        private void OnInventoryClicked()
        {
            SelectTab(HubNavTab.Inventory);
            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryPanelUI>(FindObjectsInactive.Include);
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.Toggle();
                if (!inventoryPanel.IsOpen)
                {
                    NotifyPanelClosed();
                }
            }
        }

        private void OnQuitClicked()
        {
            if (sideNav != null)
            {
                sideNav.ClearSelection(true);
            }

            Game.Save.GameSaveController.SaveGame();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ReturnToHub(Game.Core.HubLaunchIntent.PendingAction intent)
        {
            Debug.Log($"[BattleMenuUI] Returning to hub ({hubSceneName}), intent={intent}");
            Game.Core.BattleSession.Clear();
            Game.Core.HubLaunchIntent.Request(intent);
            Game.Save.GameSaveController.SaveGame();
            SceneManager.LoadScene(hubSceneName);
        }
    }
}
