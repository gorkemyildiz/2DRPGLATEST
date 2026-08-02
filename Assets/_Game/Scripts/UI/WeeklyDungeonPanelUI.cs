using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class WeeklyDungeonPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button claimStoneButton;

        [Header("Services")]
        [SerializeField] private Game.Dungeon.WeeklyDungeonService weeklyService;
        [SerializeField] private Game.Hub.HubController hubController;

        public void Bind(Game.Dungeon.WeeklyDungeonService weekly, Game.Hub.HubController hub = null)
        {
            weeklyService = weekly;
            if (hub != null)
            {
                hubController = hub;
            }

            WireButtons();
            Refresh();
        }

        public void Configure(
            GameObject root,
            Text title,
            Text body,
            Text status,
            Button close,
            Button enter,
            Button claim)
        {
            panelRoot = root != null ? root : gameObject;
            titleText = title;
            bodyText = body;
            statusText = status;
            closeButton = close;
            enterButton = enter;
            claimStoneButton = claim;
            WireButtons();
        }

        public void Open()
        {
            ResolveRefs();
            if (weeklyService != null)
            {
                weeklyService.EnsureCurrentWeek();
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
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveRefs()
        {
            if (weeklyService == null)
            {
                weeklyService = FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
            }

            if (hubController == null)
            {
                hubController = FindFirstObjectByType<Game.Hub.HubController>();
            }
        }

        private void Subscribe()
        {
            if (weeklyService != null)
            {
                weeklyService.Changed -= Refresh;
                weeklyService.Changed += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (weeklyService != null)
            {
                weeklyService.Changed -= Refresh;
            }
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (enterButton != null)
            {
                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(OnEnter);
            }

            if (claimStoneButton != null)
            {
                claimStoneButton.onClick.RemoveAllListeners();
                claimStoneButton.onClick.AddListener(OnClaim);
            }
        }

        private void OnEnter()
        {
            ResolveRefs();
            if (hubController != null)
            {
                hubController.StartWeeklyDungeon();
                return;
            }

            if (weeklyService == null)
            {
                SetStatus("Weekly service missing");
                return;
            }

            if (!weeklyService.TryEnter())
            {
                weeklyService.CanEnter(out string reason);
                SetStatus(reason ?? "Cannot enter");
                Refresh();
                return;
            }

            Game.Save.GameSaveController.SaveGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.GameScenes.Battle);
        }

        private void OnClaim()
        {
            ResolveRefs();
            if (weeklyService == null)
            {
                SetStatus("Weekly service missing");
                return;
            }

            if (weeklyService.TryClaimWeeklyStone())
            {
                SetStatus("Claimed Courage Stone");
                Game.Save.GameSaveController.SaveGame();
            }
            else
            {
                weeklyService.CanClaimWeeklyStone(out string reason);
                SetStatus(reason ?? "Cannot claim");
            }

            Refresh();
        }

        private void Refresh()
        {
            ResolveRefs();
            if (titleText != null)
            {
                titleText.text = "WEEKLY DUNGEON";
            }

            if (bodyText != null)
            {
                if (weeklyService == null)
                {
                    bodyText.text = "Weekly dungeon service missing.";
                }
                else
                {
                    weeklyService.EnsureCurrentWeek();
                    Game.Data.EnemyData boss = weeklyService.GetCurrentBoss();
                    Game.Data.WeeklyDungeonCatalog catalog = weeklyService.Catalog;
                    StringBuilder sb = new StringBuilder(256);
                    sb.AppendLine(catalog != null ? catalog.displayName : "Weekly Dungeon");
                    sb.AppendLine($"Week: {Game.Dungeon.WeeklyDungeonService.GetCurrentWeekKey()}");
                    sb.AppendLine($"Boss: {(boss != null ? boss.displayName : "—")}");
                    sb.AppendLine($"Entries: {weeklyService.GetEntriesRemaining()} / {weeklyService.GetMaxEntries()} left");
                    int cost = catalog != null ? Mathf.Max(1, catalog.courageStoneCost) : 1;
                    sb.AppendLine($"Courage Stones: {weeklyService.GetCourageStoneCount()} (cost {cost})");
                    bodyText.text = sb.ToString();
                }
            }

            if (enterButton != null)
            {
                enterButton.interactable = weeklyService != null && weeklyService.CanEnter(out _);
            }

            if (claimStoneButton != null)
            {
                claimStoneButton.interactable = weeklyService != null && weeklyService.CanClaimWeeklyStone(out _);
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
