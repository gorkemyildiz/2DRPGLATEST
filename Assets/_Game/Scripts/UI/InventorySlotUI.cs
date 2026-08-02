using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Bag cell. Equip via drag onto paper-doll; click does not equip.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text stackText;
        [SerializeField] private Button button;

        private Game.Data.ItemData item;
        private Game.Save.ItemInstanceSave instance;
        private ItemTooltipUI tooltip;
        private Game.Inventory.EquipmentService equipmentService;
        private ScrollRect parentScroll;
        private bool dragging;
        private Canvas canvas;

        public Game.Data.ItemData Item => item;
        public Game.Save.ItemInstanceSave Instance => instance;
        public Image IconImage => iconImage;

        public void ConfigureVisuals(Image icon, Image background, Text stack, Button btn)
        {
            if (icon != null) iconImage = icon;
            if (background != null) backgroundImage = background;
            if (stack != null) stackText = stack;
            if (btn != null) button = btn;
        }

        public void AutoWireChildren()
        {
            if (iconImage == null)
            {
                Transform iconTf = transform.Find("Icon");
                if (iconTf != null) iconImage = iconTf.GetComponent<Image>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (stackText == null)
            {
                Transform stackTf = transform.Find("Stack");
                if (stackTf != null) stackText = stackTf.GetComponent<Text>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        public void Bind(
            Game.Data.ItemData itemData,
            Game.Save.ItemInstanceSave inst,
            ItemTooltipUI tooltipUi,
            Game.Inventory.EquipmentService equipment = null,
            Sprite fallbackIcon = null)
        {
            AutoWireChildren();
            item = itemData;
            instance = inst;
            tooltip = tooltipUi;
            equipmentService = equipment;
            parentScroll = GetComponentInParent<ScrollRect>();
            canvas = GetComponentInParent<Canvas>();

            if (iconImage != null)
            {
                Sprite icon = itemData != null && itemData.icon != null ? itemData.icon : fallbackIcon;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
                iconImage.type = Image.Type.Simple;
                iconImage.useSpriteMesh = false;

                RectTransform iconRt = iconImage.rectTransform;
                if (iconRt != null)
                {
                    // Fill the painted well; tiny inset so border art stays visible.
                    iconRt.anchorMin = Vector2.zero;
                    iconRt.anchorMax = Vector2.one;
                    iconRt.offsetMin = new Vector2(3f, 3f);
                    iconRt.offsetMax = new Vector2(-3f, -3f);
                    iconRt.anchoredPosition = Vector2.zero;
                }
            }

            if (stackText != null)
            {
                stackText.text = string.Empty;
                stackText.gameObject.SetActive(false);
            }

            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = true;
                if (backgroundImage.sprite == null && backgroundImage.color.a < 0.01f)
                {
                    backgroundImage.color = new Color(1f, 1f, 1f, 0.001f);
                }
            }

            // Button would steal clicks; equip is drag-only.
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                button.onClick.RemoveAllListeners();
                button.enabled = false;
                if (backgroundImage != null)
                {
                    button.targetGraphic = backgroundImage;
                }
            }
        }

        public void ClearSlot()
        {
            item = null;
            instance = null;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (stackText != null)
            {
                stackText.text = string.Empty;
                stackText.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (dragging || tooltip == null || item == null)
            {
                return;
            }

            tooltip.Show(item, instance, 1, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (item == null || instance == null || canvas == null)
            {
                return;
            }

            dragging = true;
            if (tooltip != null)
            {
                tooltip.Hide();
            }

            if (parentScroll != null)
            {
                parentScroll.enabled = false;
            }

            Sprite icon = iconImage != null ? iconImage.sprite : item.icon;
            InventoryDragSession.Begin(item, instance, icon, canvas, false, item.equipmentSlot);

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
            if (parentScroll != null)
            {
                parentScroll.enabled = true;
            }

            if (iconImage != null)
            {
                iconImage.color = Color.white;
            }

            // Drop handled by EquipmentSlotUI / BagDropZone via OnDrop.
            // If nothing accepted the drop, just clear the ghost.
            if (InventoryDragSession.IsDragging)
            {
                InventoryDragSession.End();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Dragging equipped gear onto bag unequips it.
            if (!InventoryDragSession.IsDragging || !InventoryDragSession.FromEquipment)
            {
                return;
            }

            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<Game.Inventory.EquipmentService>();
            }

            if (equipmentService == null)
            {
                return;
            }

            equipmentService.Unequip(InventoryDragSession.SourceEquipmentSlot);
            InventoryDragSession.End();
        }
    }
}
