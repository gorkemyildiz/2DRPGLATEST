using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Hub
{
    /// <summary>
    /// Reliable plaza building clicks (works with New Input System; OnMouse* does not).
    /// </summary>
    public class HubPlazaClickRouter : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private HubController hubController;
        [SerializeField] private LayerMask buildingMask = ~0;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (hubController == null)
            {
                hubController = FindFirstObjectByType<HubController>();
            }
        }

        private void Update()
        {
            if (!WasPrimaryPressedThisFrame())
            {
                return;
            }

            if (ShouldBlockForUi())
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
                if (worldCamera == null)
                {
                    return;
                }
            }

            Vector3 screen = GetPointerScreenPosition();

            // Ignore clicks outside the bottom world band (transparent upper area).
            Rect pixelRect = worldCamera.pixelRect;
            if (screen.x < pixelRect.xMin || screen.x > pixelRect.xMax ||
                screen.y < pixelRect.yMin || screen.y > pixelRect.yMax)
            {
                return;
            }

            Vector3 world = worldCamera.ScreenToWorldPoint(screen);
            Vector2 point = new Vector2(world.x, world.y);

            Collider2D hit = Physics2D.OverlapPoint(point, buildingMask);
            if (hit == null)
            {
                return;
            }

            HubBuildingHotspot hotspot = hit.GetComponent<HubBuildingHotspot>();
            if (hotspot == null)
            {
                hotspot = hit.GetComponentInParent<HubBuildingHotspot>();
            }

            if (hotspot == null)
            {
                return;
            }

            if (hubController == null)
            {
                hubController = FindFirstObjectByType<HubController>();
            }

            hotspot.Activate(hubController);
        }

        private bool ShouldBlockForUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData ped = new PointerEventData(EventSystem.current)
            {
                position = GetPointerScreenPosition()
            };

            uiHits.Clear();
            EventSystem.current.RaycastAll(ped, uiHits);
            for (int i = 0; i < uiHits.Count; i++)
            {
                GameObject go = uiHits[i].gameObject;
                if (go == null)
                {
                    continue;
                }

                // Only block when clicking real controls / open overlay cards.
                if (go.GetComponentInParent<Button>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<ScrollRect>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<Game.UI.InventoryPanelUI>() != null ||
                    go.GetComponentInParent<Game.UI.MapSelectUI>() != null ||
                    go.GetComponentInParent<Game.UI.CraftPanelUI>() != null ||
                    go.GetComponentInParent<Game.UI.BuildingPanelUI>() != null ||
                    go.GetComponentInParent<Game.UI.VillagePanelUI>() != null ||
                    go.GetComponentInParent<Game.UI.EnhancePanelUI>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WasPrimaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }
#endif
            // Fallback when define symbols differ between editor/player.
            try
            {
                return Input.GetMouseButtonDown(0);
            }
            catch
            {
                return false;
            }
        }

        private static Vector3 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                Vector2 t = touch.primaryTouch.position.ReadValue();
                return new Vector3(t.x, t.y, 0f);
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                Vector2 m = mouse.position.ReadValue();
                return new Vector3(m.x, m.y, 0f);
            }
#endif
            return Input.mousePosition;
        }
    }
}
