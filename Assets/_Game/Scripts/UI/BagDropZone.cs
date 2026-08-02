using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Drop target on the bag area — unequip when dragging from paper-doll.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BagDropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Game.Inventory.EquipmentService equipmentService;

        public void Configure(Game.Inventory.EquipmentService equipment)
        {
            equipmentService = equipment;
        }

        private void Awake()
        {
            // Need a raycastable graphic for drop hits.
            Image img = GetComponent<Image>();
            if (img == null)
            {
                img = gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
            }

            img.raycastTarget = true;
        }

        public void OnDrop(PointerEventData eventData)
        {
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
