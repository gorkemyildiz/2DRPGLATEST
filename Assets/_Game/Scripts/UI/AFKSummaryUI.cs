using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class AFKSummaryUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text rewardsText;

        [Header("Buttons")]
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;

        private Game.AFK.AFKReward pendingReward;
        private Game.AFK.AFKRewardService afkService;
        private Game.Rewards.RewardService rewardService;

        public void Bind(Game.AFK.AFKRewardService afk, Game.Rewards.RewardService rewards)
        {
            afkService = afk;
            rewardService = rewards;
            WireButtons();
        }

        public void Show(Game.AFK.AFKReward reward)
        {
            pendingReward = reward;
            if (reward == null || (reward.goldEarned <= 0 && reward.experienceEarned <= 0 && reward.scrapEarned <= 0 && !reward.chestDropped))
            {
                Close();
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "AFK REWARDS";
            }

            if (detailText != null)
            {
                float hours = reward.afkDurationSeconds / 3600f;
                detailText.text = $"Away for {hours:F1} hours  (30% efficiency)";
            }

            if (rewardsText != null)
            {
                string chest = reward.chestDropped ? "\n+ AFK Chest" : "";
                string scrap = reward.scrapEarned > 0 ? $"\n+{reward.scrapEarned} Scrap" : "";
                rewardsText.text = $"+{reward.goldEarned} Gold\n+{reward.experienceEarned} EXP{scrap}{chest}";
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
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

        private void WireButtons()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(OnClaimClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnClaimClicked()
        {
            if (afkService == null)
            {
                afkService = FindFirstObjectByType<Game.AFK.AFKRewardService>();
            }

            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (afkService != null && pendingReward != null)
            {
                afkService.ClaimReward(pendingReward, rewardService);
            }

            Close();
        }

        private void OnCloseClicked()
        {
            // Closing without claim still grants (idle games usually auto-apply)
            OnClaimClicked();
        }
    }
}
