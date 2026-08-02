using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Battle HUD button: leave stage and return to village hub.
    /// </summary>
    public class ReturnToVillageButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private string hubSceneName = Game.Core.GameScenes.Hub;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            Debug.Log($"[ReturnToVillageButton] Returning to hub: {hubSceneName}");
            Game.Core.BattleSession.Clear();
            Game.Save.GameSaveController.SaveGame();
            SceneManager.LoadScene(hubSceneName);
        }
    }
}
