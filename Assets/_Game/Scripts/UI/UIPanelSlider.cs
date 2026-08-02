using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Slides a UI panel in/out instead of popping instantly.
    /// Attach to the panel root (or assign slideTarget).
    /// </summary>
    public class UIPanelSlider : MonoBehaviour
    {
        public enum SlideFrom
        {
            Left,
            Right
        }

        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private SlideFrom slideFrom = SlideFrom.Left;
        [SerializeField] private float duration = 0.38f;
        [SerializeField] private float slideDistance = 420f;
        [SerializeField] private bool deactivateOnHidden = true;

        private Vector2 shownPosition;
        private bool hasCachedShown;
        private Coroutine activeRoutine;
        private bool isOpen;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            EnsureRefs();
            CacheShownIfNeeded();
        }

        public void ShowImmediate()
        {
            EnsureRefs();
            CacheShownIfNeeded();
            StopActive();
            gameObject.SetActive(true);
            ApplyShown(1f);
            isOpen = true;
        }

        public void HideImmediate()
        {
            EnsureRefs();
            CacheShownIfNeeded();
            StopActive();
            ApplyShown(0f);
            isOpen = false;
            if (deactivateOnHidden)
            {
                gameObject.SetActive(false);
            }
        }

        public void Open()
        {
            EnsureRefs();
            CacheShownIfNeeded();
            StopActive();
            gameObject.SetActive(true);
            isOpen = true;
            ApplyShown(0f);
            if (!isActiveAndEnabled)
            {
                ApplyShown(1f);
                return;
            }

            activeRoutine = StartCoroutine(Animate(0f, 1f, keepActive: true));
        }

        public void Close()
        {
            if (!gameObject.activeInHierarchy)
            {
                HideImmediate();
                return;
            }

            EnsureRefs();
            CacheShownIfNeeded();
            StopActive();
            isOpen = false;
            activeRoutine = StartCoroutine(Animate(1f, 0f, keepActive: !deactivateOnHidden));
        }

        private IEnumerator Animate(float from, float to, bool keepActive)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                // Smooth ease-out
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                ApplyShown(Mathf.Lerp(from, to, eased));
                yield return null;
            }

            ApplyShown(to);
            activeRoutine = null;

            if (!keepActive && to <= 0.001f)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyShown(float shown01)
        {
            if (slideTarget != null)
            {
                Vector2 hidden = shownPosition + GetHiddenOffset();
                slideTarget.anchoredPosition = Vector2.Lerp(hidden, shownPosition, shown01);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = shown01;
                canvasGroup.interactable = shown01 > 0.95f;
                canvasGroup.blocksRaycasts = shown01 > 0.05f;
            }
        }

        private Vector2 GetHiddenOffset()
        {
            return slideFrom == SlideFrom.Left
                ? new Vector2(-Mathf.Abs(slideDistance), 0f)
                : new Vector2(Mathf.Abs(slideDistance), 0f);
        }

        private void EnsureRefs()
        {
            if (slideTarget == null)
            {
                slideTarget = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void CacheShownIfNeeded()
        {
            if (hasCachedShown || slideTarget == null)
            {
                return;
            }

            shownPosition = slideTarget.anchoredPosition;
            hasCachedShown = true;
        }

        private void StopActive()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
        }

        public static UIPanelSlider EnsureOn(GameObject root, RectTransform target = null, SlideFrom from = SlideFrom.Left)
        {
            if (root == null)
            {
                return null;
            }

            UIPanelSlider slider = root.GetComponent<UIPanelSlider>();
            if (slider == null)
            {
                slider = root.AddComponent<UIPanelSlider>();
            }

            if (target != null)
            {
                slider.slideTarget = target;
            }

            slider.slideFrom = from;
            if (root.GetComponent<CanvasGroup>() == null)
            {
                root.AddComponent<CanvasGroup>();
            }

            slider.canvasGroup = root.GetComponent<CanvasGroup>();
            return slider;
        }

        public static void OpenRoot(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            UIPanelSlider slider = root.GetComponent<UIPanelSlider>();
            if (slider != null)
            {
                slider.Open();
                return;
            }

            root.SetActive(true);
        }

        public static void CloseRoot(GameObject root, GameObject fallback = null)
        {
            GameObject target = root != null ? root : fallback;
            if (target == null)
            {
                return;
            }

            UIPanelSlider slider = target.GetComponent<UIPanelSlider>();
            if (slider != null)
            {
                slider.Close();
                return;
            }

            target.SetActive(false);
        }
    }
}
