using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Scene-authored chapter dropdown row. Layout is edited in Edit Mode;
    /// runtime only refreshes label / sprite / interactable.
    /// </summary>
    public class AdventureChapterOptionUI : MonoBehaviour
    {
        [SerializeField] private int chapterIndex = 1;
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text labelText;

        public int ChapterIndex => chapterIndex;
        public Button Button => button;

        public void Configure(int index, Button btn = null, Image bg = null, Text label = null)
        {
            chapterIndex = index;
            if (btn != null) button = btn;
            if (bg != null) backgroundImage = bg;
            if (label != null) labelText = label;
        }

        public void SetState(
            string label,
            bool unlocked,
            bool selected,
            Sprite normalSprite,
            Sprite selectedSprite)
        {
            if (labelText != null)
            {
                labelText.text = unlocked ? label : $"{label}  (Locked)";
                labelText.color = unlocked
                    ? (selected ? new Color(0.05f, 0.12f, 0.12f, 1f) : new Color(0.78f, 0.98f, 0.94f, 1f))
                    : new Color(0.45f, 0.5f, 0.48f, 1f);
                ForceOpaque(labelText);
            }

            if (backgroundImage != null)
            {
                Sprite use = selected && selectedSprite != null ? selectedSprite : normalSprite;
                if (use != null)
                {
                    backgroundImage.sprite = use;
                    backgroundImage.color = Color.white;
                }

                ForceOpaque(backgroundImage);
            }

            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                button.interactable = unlocked;
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
