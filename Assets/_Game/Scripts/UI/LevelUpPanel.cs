using System.Collections;
using UnityEngine;

namespace Game.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text titleText;
        [SerializeField] private UnityEngine.UI.Text detailText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float visibleDuration = 2.2f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            HideImmediate();
        }

        public void Show(int level, int attackDamage, int maxHealth)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = $"LEVEL UP!  Lv.{level}";
            }

            if (detailText != null)
            {
                detailText.text = $"ATK {attackDamage}   HP {maxHealth}";
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            hideCoroutine = StartCoroutine(HideAfterDelay());
            Debug.Log($"[LevelUpPanel] Showing level {level}");
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(visibleDuration);

            if (canvasGroup != null && fadeOutDuration > 0f)
            {
                float elapsed = 0f;
                float startAlpha = canvasGroup.alpha;

                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                    yield return null;
                }

                canvasGroup.alpha = 0f;
            }

            HideImmediate();
            hideCoroutine = null;
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }
    }
}
