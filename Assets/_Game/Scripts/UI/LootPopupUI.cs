using UnityEngine;

namespace Game.UI
{
    public class LootPopupUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text titleText;
        [SerializeField] private UnityEngine.UI.Text itemNameText;
        [SerializeField] private UnityEngine.UI.Text bonusText;
        [SerializeField] private UnityEngine.UI.Button equipButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;

        private Game.Data.ItemData currentItem;
        private Game.Inventory.ChestService chestService;
        private bool buttonsWired;

        private void OnEnable()
        {
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void WireButtons()
        {
            if (buttonsWired)
            {
                return;
            }

            if (equipButton != null)
            {
                equipButton.onClick.AddListener(OnEquipClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            buttonsWired = true;
        }

        private void UnwireButtons()
        {
            if (!buttonsWired)
            {
                return;
            }

            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(OnEquipClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }

            buttonsWired = false;
        }

        public void Show(Game.Data.ItemData item, Game.Inventory.ChestService service)
        {
            if (item == null)
            {
                return;
            }

            currentItem = item;
            chestService = service;
            gameObject.SetActive(true);
            WireButtons();

            if (titleText != null)
            {
                titleText.text = "CHEST LOOT!";
            }

            if (itemNameText != null)
            {
                itemNameText.text = $"{item.displayName}  [{item.rarity}]";
            }

            if (bonusText != null)
            {
                bonusText.text = $"Slot: {item.equipmentSlot}\nATK +{item.attackBonus}   HP +{item.healthBonus}";
            }

            Debug.Log($"[LootPopupUI] Showing loot: {item.displayName}");
        }

        public void Hide()
        {
            currentItem = null;
            gameObject.SetActive(false);
        }

        private void OnEquipClicked()
        {
            if (currentItem == null)
            {
                Hide();
                return;
            }

            if (chestService != null)
            {
                chestService.EquipLootedItem(currentItem);
            }
            else
            {
                Game.Inventory.EquipmentService equipment = FindFirstObjectByType<Game.Inventory.EquipmentService>();
                if (equipment != null)
                {
                    equipment.Equip(currentItem);
                }
            }

            Hide();
        }
    }
}
