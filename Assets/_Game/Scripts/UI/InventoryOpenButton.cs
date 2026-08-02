using UnityEngine;

namespace Game.UI
{
    public class InventoryOpenButton : MonoBehaviour
    {
        [SerializeField] private InventoryPanelUI inventoryPanel;
        [SerializeField] private UnityEngine.UI.Button button;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<UnityEngine.UI.Button>();
            }

            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryPanelUI>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<UnityEngine.UI.Button>();
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
            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryPanelUI>(FindObjectsInactive.Include);
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.Toggle();
            }
        }
    }
}
