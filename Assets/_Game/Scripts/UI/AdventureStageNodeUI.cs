using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One stage node on the Adventure map path.
    /// Does not move/scale the RectTransform — editor layout stays intact.
    /// </summary>
    public class AdventureStageNodeUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image stageImage;
        [SerializeField] private Image lockImage;
        [SerializeField] private Text numberText;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private int levelIndex = 1;

        public int LevelIndex => levelIndex;
        public Button Button => button;

        public void Configure(
            int index,
            Sprite normal,
            Sprite selectedSpriteAsset,
            Sprite lockedSpriteAsset = null,
            Button btn = null,
            Image stage = null,
            Image lockImg = null,
            Text number = null)
        {
            levelIndex = index;
            if (normal != null)
            {
                normalSprite = normal;
            }

            if (selectedSpriteAsset != null)
            {
                selectedSprite = selectedSpriteAsset;
            }

            if (lockedSpriteAsset != null)
            {
                lockedSprite = lockedSpriteAsset;
            }

            if (btn != null) button = btn;
            if (stage != null) stageImage = stage;
            if (lockImg != null) lockImage = lockImg;
            if (number != null) numberText = number;
        }

        /// <param name="interactable">Can the player click this node.</param>
        /// <param name="showLocked">
        /// When null, locked art follows !interactable.
        /// Pass false to keep open art on a cleared-but-not-selectable node (layer boss).
        /// </param>
        public void SetState(bool interactable, bool isSelected, string label, bool? showLocked = null)
        {
            bool lockedVisual = showLocked ?? !interactable;

            if (button != null)
            {
                // Must be None before interactable changes, otherwise ColorTint
                // leaves CanvasRenderer alpha stuck low (ghosted stages).
                button.transition = Selectable.Transition.None;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.disabledColor = Color.white;
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0f;
                button.colors = colors;
                button.interactable = interactable;
            }

            if (numberText != null)
            {
                numberText.text = label;
                numberText.color = new Color(0.85f, 1f, 0.96f, 1f);
                ForceOpaque(numberText);
            }

            if (stageImage != null)
            {
                Sprite use;
                if (lockedVisual && lockedSprite != null)
                {
                    use = lockedSprite;
                }
                else if (!lockedVisual && isSelected && selectedSprite != null)
                {
                    use = selectedSprite;
                }
                else
                {
                    use = normalSprite;
                }

                if (use != null)
                {
                    stageImage.sprite = use;
                    stageImage.enabled = true;
                }

                stageImage.preserveAspect = true;
                stageImage.color = Color.white;
                ForceOpaque(stageImage);
            }

            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(false);
            }
        }

        private static void ForceOpaque(Graphic graphic)
        {
            if (graphic == null)
            {
                return;
            }

            Color c = graphic.color;
            c.a = 1f;
            graphic.color = c;
            graphic.CrossFadeAlpha(1f, 0f, true);
            graphic.canvasRenderer.SetAlpha(1f);
        }
    }
}
