using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    public class BattleResultPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text goldRewardText;
        [SerializeField] private Text expRewardText;
        [SerializeField] private Text stageInfoText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject victoryContent;
        [SerializeField] private GameObject defeatContent;

        [Header("Scene Flow")]
        [SerializeField] private string hubSceneName = Game.Core.GameScenes.Hub;

        [Header("Colors")]
        [SerializeField] private Color victoryColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color defeatColor = new Color(0.8f, 0.2f, 0.2f);

        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        /// <summary>
        /// Victory banner only — no Restart/Village buttons (auto-continue handled by battle controller).
        /// </summary>
        public void ShowVictory(string stageName, int goldEarned, int expEarned)
        {
            gameObject.SetActive(true);

            if (victoryContent != null) victoryContent.SetActive(true);
            if (defeatContent != null) defeatContent.SetActive(false);

            if (titleText != null)
            {
                titleText.text = "VICTORY!";
                titleText.color = victoryColor;
            }

            if (stageInfoText != null)
            {
                stageInfoText.text = $"{stageName}  cleared";
            }

            if (goldRewardText != null)
            {
                goldRewardText.text = $"+{goldEarned} Gold";
            }

            if (expRewardText != null)
            {
                expRewardText.text = $"+{expEarned} EXP";
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            Debug.Log($"[BattleResultPanel] Victory shown (auto-continue): {goldEarned} gold, {expEarned} exp");
        }

        public void ShowDefeat(string stageName)
        {
            gameObject.SetActive(true);

            if (victoryContent != null) victoryContent.SetActive(false);
            if (defeatContent != null) defeatContent.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "DEFEAT";
                titleText.color = defeatColor;
            }

            if (stageInfoText != null)
            {
                stageInfoText.text = $"Failed at: {stageName}";
            }

            if (goldRewardText != null)
            {
                goldRewardText.text = "";
            }

            if (expRewardText != null)
            {
                expRewardText.text = "";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                SetButtonLabel(continueButton, "Village");
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
                SetButtonLabel(restartButton, "Retry");
            }

            Debug.Log("[BattleResultPanel] Defeat shown");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRestartClicked()
        {
            Debug.Log("[BattleResultPanel] Restart clicked - Reloading battle");
            Game.Save.GameSaveController.SaveGame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnContinueClicked()
        {
            Debug.Log($"[BattleResultPanel] Returning to village hub: {hubSceneName}");
            Game.Save.GameSaveController.SaveGame();
            SceneManager.LoadScene(hubSceneName);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }
    }
}
