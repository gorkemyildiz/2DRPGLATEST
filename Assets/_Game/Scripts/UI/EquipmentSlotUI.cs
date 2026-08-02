using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Paper-doll slot. Accepts drag-equip; drag out to bag to unequip. No click equip/unequip.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IDropHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private Game.Data.EquipmentSlot slot;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text placeholderText;

        private ItemTooltipUI tooltip;
        private Game.Inventory.EquipmentService equipmentService;
        private Game.Data.EnhanceCatalog enhanceCatalog;
        private Canvas canvas;
        private bool dragging;

        public Game.Data.EquipmentSlot Slot => slot;

        public void Configure(
            Game.Data.EquipmentSlot equipmentSlot,
            Image icon,
            Image background,
            Text placeholder,
            ItemTooltipUI tooltipUi,
            Game.Inventory.EquipmentService equipment,
            Game.Data.EnhanceCatalog catalog)
        {
            slot = equipmentSlot;
            if (icon != null) iconImage = icon;
            if (background != null) backgroundImage = background;
            if (placeholder != null) placeholderText = placeholder;
            tooltip = tooltipUi;
            equipmentService = equipment;
            enhanceCatalog = catalog;
            canvas = GetComponentInParent<Canvas>();
        }

        public void Refresh()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            Game.Data.ItemData item = equipmentService != null
                ? equipmentService.GetEquippedItem(slot)
                : null;

            if (iconImage != null)
            {
                if (item != null && item.icon != null)
                {
                    iconImage.sprite = item.icon;
                    iconImage.enabled = true;
                    iconImage.preserveAspect = true;
                    iconImage.color = Color.white;
                    iconImage.raycastTarget = false;
                    iconImage.type = Image.Type.Simple;
                    iconImage.useSpriteMesh = false;

                    RectTransform iconRt = iconImage.rectTransform;
                    if (iconRt != null)
                    {
                        iconRt.anchorMin = Vector2.zero;
                        iconRt.anchorMax = Vector2.one;
                        iconRt.offsetMin = new Vector2(4f, 4f);
                        iconRt.offsetMax = new Vector2(-4f, -4f);
                        iconRt.anchoredPosition = Vector2.zero;
                    }
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
            }

            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(item == null);
            }

            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = true;
                if (backgroundImage.sprite == null && backgroundImage.color.a < 0.01f)
                {
                    backgroundImage.color = new Color(1f, 1f, 1f, 0.001f);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (dragging || tooltip == null || equipmentService == null)
            {
                return;
            }

            Game.Data.ItemData item = equipmentService.GetEquippedItem(slot);
            if (item == null)
            {
                return;
            }

            tooltip.Show(item, equipmentService.GetEquippedInstance(slot), 1, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!InventoryDragSession.IsDragging || InventoryDragSession.Item == null || equipmentService == null)
            {
                return;
            }

            Game.Data.ItemData dragged = InventoryDragSession.Item;
            if (dragged.equipmentSlot != slot)
            {
                return;
            }

            if (tooltip != null)
            {
                tooltip.Hide();
            }

            equipmentService.EquipInstance(InventoryDragSession.Instance, dragged);
            InventoryDragSession.End();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (equipmentService == null || canvas == null)
            {
                return;
            }

            Game.Data.ItemData equipped = equipmentService.GetEquippedItem(slot);
            Game.Save.ItemInstanceSave inst = equipmentService.GetEquippedInstance(slot);
            if (equipped == null || inst == null)
            {
                return;
            }

            dragging = true;
            if (tooltip != null)
            {
                tooltip.Hide();
            }

            Sprite icon = iconImage != null && iconImage.sprite != null ? iconImage.sprite : equipped.icon;
            InventoryDragSession.Begin(equipped, inst, icon, canvas, true, slot);

            if (iconImage != null)
            {
                iconImage.color = new Color(1f, 1f, 1f, 0.35f);
            }

            InventoryDragSession.Move(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            InventoryDragSession.Move(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            if (iconImage != null)
            {
                iconImage.color = Color.white;
            }

            // If dropped on bag, BagDropZone unequips and ends session.
            if (InventoryDragSession.IsDragging && InventoryDragSession.FromEquipment)
            {
                InventoryDragSession.End();
                Refresh();
            }
        }
    }
}
