using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Shared 960×960 layout for BattleDemo and VillageHub:
    /// live world/HUD in the bottom 320px band, magenta-cleared upper band for desktop see-through,
    /// overlay cards (inventory, map, craft, …) centered in the upper free area.
    /// </summary>
    [DisallowMultipleComponent]
    public class BattleViewportLayout : MonoBehaviour
    {
        public const float CanvasSize = 960f;
        public const float BandHeight = 320f;

        /// <summary>Legacy alias used by battle HUD helpers.</summary>
        public const float BattleHeight = BandHeight;

        [Header("World camera")]
        [SerializeField] private Camera worldCamera;

        [Header("Upper band")]
        [SerializeField] private Image upperBandImage;

        private const string UpperClearCameraName = "GameUpperClearCamera";
        private Camera upperClearCamera;

        [Header("Bottom-band roots")]
        [SerializeField] private RectTransform battleMenu;
        [SerializeField] private RectTransform hubMenu;
        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform levelUpPanel;
        [SerializeField] private RectTransform lootPopup;
        [SerializeField] private RectTransform battleResultPanel;

        public static float BandHeightNormalized => BandHeight / CanvasSize;
        public static float BattleHeightNormalized => BandHeightNormalized;
        public static float UpperBandHeightNormalized => 1f - BandHeightNormalized;

        /// <summary>
        /// Card Y on a full-stretch centered canvas (Y ∈ [-480,480]).
        /// Upper band midpoint above the 320px world band.
        /// </summary>
        public static float UpperBandCardAnchoredY => 160f;

        public static bool IsBandLayoutScene()
        {
            string name = SceneManager.GetActiveScene().name;
            return name == Game.Core.GameScenes.Battle || name == Game.Core.GameScenes.Hub;
        }

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

        /// <summary>
        /// Moves Card / AdventureCard into the upper free band (same as battle inventory).
        /// </summary>
        public static void ApplyUpperBandCard(Transform panelRoot)
        {
            if (!IsBandLayoutScene() || panelRoot == null)
            {
                return;
            }

            RectTransform cardRt = FindOverlayCard(panelRoot);
            if (cardRt == null)
            {
                return;
            }

            Vector2 pos = cardRt.anchoredPosition;
            pos.y = UpperBandCardAnchoredY;
            cardRt.anchoredPosition = pos;
            cardRt.localScale = Vector3.one;
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
            if (!IsBandLayoutScene())
            {
                return;
            }

            ApplyCameraRect();
            EnsureUpperClearCamera();
            EnsureUpperBandBackground();
            Game.Core.DesktopTransparency.Pulse();
        }

        private void OnDestroy()
        {
            Game.Core.DesktopTransparency.Disable();
            if (upperClearCamera != null)
            {
                Destroy(upperClearCamera.gameObject);
                upperClearCamera = null;
            }
        }

        public void EnsureApplied()
        {
            if (!IsBandLayoutScene())
            {
                return;
            }

            Game.Core.GameWindowBootstrap.ForceWindowed(
                Game.Core.GameWindowBootstrap.WindowWidth,
                Game.Core.GameWindowBootstrap.WindowHeight);

            ApplyCanvasScaler();
            ResolveRefs();
            ApplyCameraRect();
            EnsureUpperClearCamera();
            EnsureUpperBandBackground();
            LayoutBottomHudBand();

            Game.Core.DesktopTransparency.Enable();
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
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            Transform canvas = transform;
            if (battleMenu == null)
            {
                battleMenu = FindRect(canvas, "BattleMenu");
            }

            if (hubMenu == null)
            {
                hubMenu = FindRect(canvas, "HubMenu");
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
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            worldCamera.rect = new Rect(0f, 0f, 1f, BandHeightNormalized);
        }

        private void EnsureUpperClearCamera()
        {
            if (upperClearCamera == null)
            {
                GameObject existing = GameObject.Find(UpperClearCameraName);
                if (existing == null)
                {
                    // Migrate old battle-only name if present.
                    existing = GameObject.Find("BattleUpperClearCamera");
                    if (existing != null)
                    {
                        existing.name = UpperClearCameraName;
                    }
                }

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

            float depth = worldCamera != null ? worldCamera.depth : 0f;
            upperClearCamera.depth = depth - 1f;
            upperClearCamera.clearFlags = CameraClearFlags.SolidColor;
            upperClearCamera.backgroundColor = Game.Core.DesktopTransparency.KeyColorUnity;
            upperClearCamera.cullingMask = 0;
            upperClearCamera.orthographic = true;
            upperClearCamera.orthographicSize = 1f;
            upperClearCamera.nearClipPlane = 0.1f;
            upperClearCamera.farClipPlane = 10f;
            upperClearCamera.allowHDR = false;
            upperClearCamera.allowMSAA = false;
            upperClearCamera.rect = new Rect(0f, BandHeightNormalized, 1f, UpperBandHeightNormalized);
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

            if (upperBandImage != null)
            {
                upperBandImage.enabled = false;
            }
        }

        private void LayoutBottomHudBand()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene == Game.Core.GameScenes.Battle)
            {
                LayoutBottomBand(battleMenu, BandHeight);
                LayoutBottomBand(topPanel, 44f, sitOnTopOfBand: true);
                LayoutCenteredInBand(lootPopup, 420f, 200f);
                LayoutCenteredInBand(levelUpPanel, 420f, 200f);
                LayoutCenteredInBand(battleResultPanel, 420f, 220f);
            }
            else if (scene == Game.Core.GameScenes.Hub)
            {
                LayoutBottomBand(hubMenu, BandHeight);
            }
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
                rt.anchoredPosition = new Vector2(0f, BandHeight);
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
            rt.anchoredPosition = new Vector2(0f, BandHeight * 0.5f);
            rt.localScale = Vector3.one;
        }

        private static RectTransform FindOverlayCard(Transform panelRoot)
        {
            Transform card = panelRoot.Find("Card");
            if (card == null)
            {
                card = panelRoot.Find("AdventureCard");
            }

            if (card == null)
            {
                Transform[] all = panelRoot.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null || all[i] == panelRoot)
                    {
                        continue;
                    }

                    if (all[i].name == "Card" || all[i].name == "AdventureCard")
                    {
                        card = all[i];
                        break;
                    }
                }
            }

            return card as RectTransform;
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
