using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// BattleDemo layout: 960×960 canvas, live battle locked to the bottom 320px band.
    /// Upper band is cleared each frame (clear camera + opaque UI fill) so closed panels leave no ghosts.
    /// </summary>
    [DisallowMultipleComponent]
    public class BattleViewportLayout : MonoBehaviour
    {
        public const float CanvasSize = 960f;
        public const float BattleHeight = 320f;

        [Header("Battle camera")]
        [SerializeField] private Camera battleCamera;

        [Header("Upper band background")]
        [SerializeField] private Color upperBandTint = Color.black;
        [SerializeField] private Image upperBandImage;

        private const string UpperClearCameraName = "BattleUpperClearCamera";
        private Camera upperClearCamera;

        [Header("Battle HUD")]
        [SerializeField] private RectTransform battleMenu;
        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform levelUpPanel;
        [SerializeField] private RectTransform lootPopup;
        [SerializeField] private RectTransform battleResultPanel;

        public static float BattleHeightNormalized => BattleHeight / CanvasSize;
        public static float UpperBandHeightNormalized => 1f - BattleHeightNormalized;

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
            ApplyCameraRect();
            EnsureUpperClearCamera();
            EnsureUpperBandBackground();
        }

        private void OnDestroy()
        {
            // Make sure leftover desktop-key mode is off if an older build enabled it.
            Game.Core.DesktopTransparency.Disable();
            if (upperClearCamera != null)
            {
                Destroy(upperClearCamera.gameObject);
                upperClearCamera = null;
            }
        }

        public void EnsureApplied()
        {
            if (SceneManager.GetActiveScene().name != Game.Core.GameScenes.Battle)
            {
                return;
            }

            Game.Core.DesktopTransparency.Disable();

            Game.Core.GameWindowBootstrap.ForceWindowed(
                Game.Core.GameWindowBootstrap.WindowWidth,
                Game.Core.GameWindowBootstrap.WindowHeight);

            ApplyCanvasScaler();
            ResolveRefs();
            ApplyCameraRect();
            EnsureUpperClearCamera();
            EnsureUpperBandBackground();
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

            if (upperBandImage == null)
            {
                Transform existing = canvas.Find("UpperBandBackground");
                if (existing != null)
                {
                    upperBandImage = existing.GetComponent<Image>();
                }
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

            // Bottom 320px only. Clear Flags left alone (your Inspector setting).
            battleCamera.rect = new Rect(0f, 0f, 1f, BattleHeightNormalized);
        }

        /// <summary>
        /// Battle camera only clears its bottom rect. Without this, closing Overlay UI leaves
        /// smeared pixels in the uncleared upper framebuffer (inventory ghost).
        /// </summary>
        private void EnsureUpperClearCamera()
        {
            if (upperClearCamera == null)
            {
                GameObject existing = GameObject.Find(UpperClearCameraName);
                if (existing != null)
                {
                    upperClearCamera = existing.GetComponent<Camera>();
                }
            }

            if (upperClearCamera == null)
            {
                GameObject go = new GameObject(UpperClearCameraName);
                upperClearCamera = go.AddComponent<Camera>();
            }

            float battleDepth = battleCamera != null ? battleCamera.depth : 0f;
            upperClearCamera.depth = battleDepth - 1f;
            upperClearCamera.clearFlags = CameraClearFlags.SolidColor;
            upperClearCamera.backgroundColor = new Color(upperBandTint.r, upperBandTint.g, upperBandTint.b, 1f);
            upperClearCamera.cullingMask = 0;
            upperClearCamera.orthographic = true;
            upperClearCamera.orthographicSize = 1f;
            upperClearCamera.nearClipPlane = 0.1f;
            upperClearCamera.farClipPlane = 10f;
            upperClearCamera.allowHDR = false;
            upperClearCamera.allowMSAA = false;
            upperClearCamera.rect = new Rect(0f, BattleHeightNormalized, 1f, UpperBandHeightNormalized);
            upperClearCamera.enabled = true;
        }

        private void EnsureUpperBandBackground()
        {
            Transform canvas = transform;
            if (upperBandImage == null)
            {
                Transform existing = canvas.Find("UpperBandBackground");
                if (existing != null)
                {
                    upperBandImage = existing.GetComponent<Image>();
                }
            }

            if (upperBandImage == null)
            {
                GameObject go = new GameObject("UpperBandBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(canvas, false);
                upperBandImage = go.GetComponent<Image>();
            }

            RectTransform rt = upperBandImage.rectTransform;
            // Top 640px of the 960 canvas (everything above the battle band).
            rt.anchorMin = new Vector2(0f, BattleHeight / CanvasSize);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsFirstSibling(); // Behind inventory / HUD

            upperBandImage.raycastTarget = false;
            upperBandImage.type = Image.Type.Simple;
            upperBandImage.preserveAspect = false;
            // Opaque fill — transparent Empty.png cannot cover framebuffer ghosts.
            upperBandImage.sprite = null;
            upperBandImage.color = new Color(upperBandTint.r, upperBandTint.g, upperBandTint.b, 1f);
            upperBandImage.enabled = true;
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
