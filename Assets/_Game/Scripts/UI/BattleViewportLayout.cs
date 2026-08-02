using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// BattleDemo layout: 960×960 canvas, live battle locked to the bottom 320px band.
    /// </summary>
    [DisallowMultipleComponent]
    public class BattleViewportLayout : MonoBehaviour
    {
        public const float CanvasSize = 960f;
        public const float BattleHeight = 320f;

        [SerializeField] private Camera battleCamera;
        [SerializeField] private RectTransform battleMenu;
        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform levelUpPanel;
        [SerializeField] private RectTransform lootPopup;
        [SerializeField] private RectTransform battleResultPanel;

        public static float BattleHeightNormalized => BattleHeight / CanvasSize;

        public static BattleViewportLayout FindOrCreate()
        {
            BattleViewportLayout existing = FindFirstObjectByType<BattleViewportLayout>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureApplied();
                return existing;
            }

            Canvas canvas = FindScreenCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[BattleViewportLayout] ScreenCanvas not found.");
                return null;
            }

            BattleViewportLayout layout = canvas.gameObject.GetComponent<BattleViewportLayout>();
            if (layout == null)
            {
                layout = canvas.gameObject.AddComponent<BattleViewportLayout>();
            }

            layout.EnsureApplied();
            return layout;
        }

        private void Awake()
        {
            EnsureApplied();
        }

        private void OnEnable()
        {
            EnsureApplied();
        }

        private void LateUpdate()
        {
            // Keep camera rect stable if something else resets it.
            ApplyCameraRect();
        }

        public void EnsureApplied()
        {
            if (SceneManager.GetActiveScene().name != Game.Core.GameScenes.Battle)
            {
                return;
            }

            Game.Core.GameWindowBootstrap.ForceWindowed(
                Game.Core.GameWindowBootstrap.WindowWidth,
                Game.Core.GameWindowBootstrap.WindowHeight);

            ApplyCanvasScaler();
            ResolveRefs();
            ApplyCameraRect();
            LayoutBattleHudBand();
        }

        private void ApplyCanvasScaler()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = GetComponentInParent<CanvasScaler>();
            }

            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasSize, CanvasSize);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void ResolveRefs()
        {
            if (battleCamera == null)
            {
                battleCamera = Camera.main;
            }

            Transform canvas = transform;
            if (battleMenu == null)
            {
                battleMenu = FindRect(canvas, "BattleMenu");
            }

            if (topPanel == null)
            {
                topPanel = FindRect(canvas, "TopPanel");
            }

            if (levelUpPanel == null)
            {
                levelUpPanel = FindRect(canvas, "LevelUpPanel");
            }

            if (lootPopup == null)
            {
                lootPopup = FindRect(canvas, "LootPopup");
            }

            if (battleResultPanel == null)
            {
                battleResultPanel = FindRect(canvas, "BattleResultPanel");
            }
        }

        private void ApplyCameraRect()
        {
            if (battleCamera == null)
            {
                battleCamera = Camera.main;
            }

            if (battleCamera == null)
            {
                return;
            }

            // Bottom strip: height 320 / 960 of the screen.
            Rect target = new Rect(0f, 0f, 1f, BattleHeightNormalized);
            if (battleCamera.rect != target)
            {
                battleCamera.rect = target;
            }
        }

        private void LayoutBattleHudBand()
        {
            LayoutBottomBand(battleMenu, BattleHeight);
            LayoutBottomBand(topPanel, 44f, sitOnTopOfBand: true);
            LayoutCenteredInBand(lootPopup, 420f, 200f);
            LayoutCenteredInBand(levelUpPanel, 420f, 200f);
            LayoutCenteredInBand(battleResultPanel, 420f, 220f);
        }

        private static void LayoutBottomBand(RectTransform rt, float height, bool sitOnTopOfBand = false)
        {
            if (rt == null)
            {
                return;
            }

            if (sitOnTopOfBand)
            {
                // HUD bar along the top edge of the 320px battle band.
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, height);
                rt.anchoredPosition = new Vector2(0f, BattleHeight);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, height);
                rt.anchoredPosition = Vector2.zero;
            }

            rt.localScale = Vector3.one;
        }

        private static void LayoutCenteredInBand(RectTransform rt, float width, float height)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, BattleHeight * 0.5f);
            rt.localScale = Vector3.one;
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            Transform t = root.Find(name);
            if (t == null)
            {
                // May live under canvas root with different hierarchy depth.
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == name)
                    {
                        t = all[i];
                        break;
                    }
                }
            }

            return t as RectTransform;
        }

        private static Canvas FindScreenCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].name == "ScreenCanvas")
                {
                    return canvases[i];
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }
    }
}
