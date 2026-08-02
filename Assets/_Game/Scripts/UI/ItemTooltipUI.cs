using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Hover popup for inventory items. Art: Item_Tooltip_Frame.png
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image frameImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Vector2 screenOffset = new Vector2(18f, -18f);

        private RectTransform rootRect;
        private Canvas rootCanvas;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            rootRect = root.GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void Show(
            Game.Data.ItemData item,
            Game.Save.ItemInstanceSave instance,
            int stackCount,
            RectTransform anchor)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            if (nameText != null)
            {
                nameText.text = item.displayName;
                nameText.color = InventoryPanelUI.GetRarityColorPublic(item.rarity);
            }

            if (bodyText != null)
            {
                string rarity = Game.Data.ItemRarityUtil.GetDisplayName(item.rarity);
                string stars = Game.Data.ItemEnhanceUtil.FormatStars(instance != null ? instance.stars : 0);
                string starPart = string.IsNullOrEmpty(stars) ? "" : $"\n{stars}";
                int atk = Game.Data.ItemEnhanceUtil.GetAttackBonus(item, instance, null);
                int hp = Game.Data.ItemEnhanceUtil.GetHealthBonus(item, instance, null);
                string stackPart = string.Empty;
                bodyText.text =
                    $"{rarity}  ·  {item.equipmentSlot}{starPart}\n" +
                    $"+{atk} ATK    +{hp} HP{stackPart}";
                bodyText.color = new Color(0.82f, 0.9f, 0.88f, 1f);
            }

            if (root != null)
            {
                root.SetActive(true);
            }

            PositionNear(anchor);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void PositionNear(RectTransform anchor)
        {
            if (rootRect == null)
            {
                return;
            }

            Vector2 screenPoint;
            if (anchor != null)
            {
                Vector3[] corners = new Vector3[4];
                anchor.GetWorldCorners(corners);
                // Top-right of slot in screen space
                Camera cam = null;
                if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    cam = rootCanvas.worldCamera;
                }

                screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            }
            else
            {
                screenPoint = Input.mousePosition;
            }

            screenPoint += screenOffset;

            RectTransform canvasRect = rootCanvas != null
                ? rootCanvas.transform as RectTransform
                : rootRect.parent as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera eventCam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, eventCam, out Vector2 local))
            {
                rootRect.SetParent(canvasRect, false);
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0f, 1f);
                rootRect.anchoredPosition = local;

                // Keep on screen roughly
                Vector2 size = rootRect.sizeDelta;
                Vector2 canvasSize = canvasRect.rect.size;
                float maxX = canvasSize.x * 0.5f - size.x - 8f;
                float minY = -canvasSize.y * 0.5f + 8f;
                Vector2 pos = rootRect.anchoredPosition;
                if (pos.x > maxX) pos.x = maxX;
                if (pos.y < minY + size.y) pos.y = minY + size.y;
                rootRect.anchoredPosition = pos;
            }
        }
    }
}
