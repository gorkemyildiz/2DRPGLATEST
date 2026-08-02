using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Village plaza hub menu: left icon rail + gold.
    /// Swap side-nav sprites under Art/UI/Hub/SideNav/.
    /// </summary>
    public class HubMenuUI : MonoBehaviour
    {
        [Header("Side Nav")]
        [SerializeField] private HubSideNavUI sideNav;
        [SerializeField] private Button adventureButton;
        [SerializeField] private Button villageButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button quitButton;

        [Header("Labels")]
        [SerializeField] private Text goldText;

        private Game.Hub.HubController hubController;
        private Game.Rewards.RewardService boundRewards;

        public void Bind(Game.Hub.HubController controller)
        {
            hubController = controller;
            WireButtons();
            RefreshStatus();
            if (sideNav != null)
            {
                sideNav.Select(HubNavTab.Village, false);
            }
        }

        public void RefreshStatus()
        {
            Game.Rewards.RewardService rewards = FindFirstObjectByType<Game.Rewards.RewardService>();
            if (rewards == null || rewards.Progress == null)
            {
                return;
            }

            if (goldText != null)
            {
                goldText.text = rewards.Progress.gold.ToString();
            }
        }

        public void NotifyPanelClosed()
        {
            if (sideNav != null)
            {
                sideNav.Select(HubNavTab.Village, true);
            }
        }

        private void Awake()
        {
            BattleViewportLayout.FindOrCreate();
            WireButtons();
        }

        private void OnEnable()
        {
            SubscribeRewards();
            RefreshStatus();
            if (sideNav != null && sideNav.Selected == HubNavTab.Quit)
            {
                sideNav.Select(HubNavTab.Village, false);
            }
        }

        private void OnDisable()
        {
            UnsubscribeRewards();
        }

        private void SubscribeRewards()
        {
            UnsubscribeRewards();
            boundRewards = FindFirstObjectByType<Game.Rewards.RewardService>();
            if (boundRewards == null)
            {
                return;
            }

            boundRewards.GoldChanged += OnGoldChanged;
            boundRewards.LevelUp += OnLevelUp;
            boundRewards.BindUI(goldText, null, null);
        }

        private void UnsubscribeRewards()
        {
            if (boundRewards == null)
            {
                return;
            }

            boundRewards.GoldChanged -= OnGoldChanged;
            boundRewards.LevelUp -= OnLevelUp;
            boundRewards = null;
        }

        private void OnGoldChanged(int _)
        {
            RefreshStatus();
        }

        private void OnLevelUp(int _)
        {
            RefreshStatus();
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

        private Game.Hub.HubController ResolveHub()
        {
            if (hubController == null)
            {
                hubController = FindFirstObjectByType<Game.Hub.HubController>();
            }

            return hubController;
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
            Game.Hub.HubController hub = ResolveHub();
            if (hub != null)
            {
                hub.StartAdventure();
            }
        }

        private void OnVillageClicked()
        {
            SelectTab(HubNavTab.Village);
            Game.Hub.HubController hub = ResolveHub();
            if (hub != null)
            {
                hub.ShowVillagePlaza();
            }
        }

        private void OnInventoryClicked()
        {
            SelectTab(HubNavTab.Inventory);
            Game.Hub.HubController hub = ResolveHub();
            if (hub != null)
            {
                hub.OpenInventory();
            }
        }

        private void OnQuitClicked()
        {
            if (sideNav != null)
            {
                sideNav.Select(HubNavTab.Village, true);
            }

            Game.Hub.HubController hub = ResolveHub();
            if (hub != null)
            {
                hub.QuitGame();
            }
            else
            {
                Application.Quit();
            }
        }
    }
}
