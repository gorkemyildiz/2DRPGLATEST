using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Shared drag state for bag ↔ paper-doll equip/unequip.
    /// </summary>
    public static class InventoryDragSession
    {
        public static bool IsDragging { get; private set; }
        public static Game.Save.ItemInstanceSave Instance { get; private set; }
        public static Game.Data.ItemData Item { get; private set; }
        public static bool FromEquipment { get; private set; }
        public static Game.Data.EquipmentSlot SourceEquipmentSlot { get; private set; }

        private static RectTransform ghostRoot;
        private static Image ghostIcon;
        private static Canvas rootCanvas;

        public static void Begin(
            Game.Data.ItemData item,
            Game.Save.ItemInstanceSave instance,
            Sprite icon,
            Canvas canvas,
            bool fromEquipment,
            Game.Data.EquipmentSlot sourceSlot)
        {
            if (item == null || instance == null || canvas == null)
            {
                return;
            }

            End();
            IsDragging = true;
            Item = item;
            Instance = instance;
            FromEquipment = fromEquipment;
            SourceEquipmentSlot = sourceSlot;
            rootCanvas = canvas;

            EnsureGhost(canvas);
            if (ghostIcon != null)
            {
                ghostIcon.sprite = icon;
                ghostIcon.enabled = icon != null;
                ghostIcon.color = Color.white;
            }

            if (ghostRoot != null)
            {
                ghostRoot.gameObject.SetActive(true);
            }
        }

        public static void Move(Vector2 screenPosition)
        {
            if (!IsDragging || ghostRoot == null || rootCanvas == null)
            {
                return;
            }

            RectTransform canvasRt = rootCanvas.transform as RectTransform;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, screenPosition, cam, out Vector2 local))
            {
                ghostRoot.anchoredPosition = local;
            }
        }

        public static void End()
        {
            IsDragging = false;
            Instance = null;
            Item = null;
            FromEquipment = false;
            SourceEquipmentSlot = default;
            if (ghostRoot != null)
            {
                ghostRoot.gameObject.SetActive(false);
            }
        }

        private static void EnsureGhost(Canvas canvas)
        {
            if (ghostRoot != null && ghostRoot)
            {
                ghostRoot.SetParent(canvas.transform, false);
                ghostRoot.SetAsLastSibling();
                return;
            }

            GameObject go = new GameObject("InventoryDragGhost");
            go.transform.SetParent(canvas.transform, false);
            ghostRoot = go.AddComponent<RectTransform>();
            ghostRoot.sizeDelta = new Vector2(48f, 48f);
            ghostRoot.anchorMin = new Vector2(0.5f, 0.5f);
            ghostRoot.anchorMax = new Vector2(0.5f, 0.5f);
            ghostRoot.pivot = new Vector2(0.5f, 0.5f);

            CanvasGroup cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            cg.alpha = 0.9f;

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            ghostIcon = iconGo.AddComponent<Image>();
            ghostIcon.raycastTarget = false;
            ghostIcon.preserveAspect = true;

            go.SetActive(false);
        }
    }
}
