using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Adventure map picker with framed path stages + chapter dropdown.
    /// Art: Assets/_Game/Art/UI/Adventure/
    /// </summary>
    public class MapSelectUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Game.Data.MapCatalog mapCatalog;

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button layerBossButton;
        [SerializeField] private Button weeklyDungeonButton;
        [SerializeField] private Button closeButton;

        [Header("Layer Boss Node")]
        [SerializeField] private AdventureStageNodeUI layerBossNode;

        [Header("Chapter Dropdown")]
        [SerializeField] private Button chapterDropdownButton;
        [SerializeField] private Text chapterDropdownLabel;
        [SerializeField] private GameObject chapterListRoot;
        [SerializeField] private Transform chapterListContent;
        [SerializeField] private List<AdventureChapterOptionUI> chapterOptions = new List<AdventureChapterOptionUI>();
        [SerializeField] private Sprite dropdownItemNormalSprite;
        [SerializeField] private Sprite dropdownItemSelectedSprite;

        [Header("Adventure Stages")]
        [SerializeField] private List<AdventureStageNodeUI> stageNodes = new List<AdventureStageNodeUI>();
        [SerializeField] private Sprite stageNormalSprite;
        [SerializeField] private Sprite stageSelectedSprite;
        [SerializeField] private Sprite stageLockedSprite;

        private Game.Hub.HubController hubController;
        private int viewLayer = 1;
        private int viewChapter = 1;
        private Game.Data.LevelData selectedLevel;
        private bool selectedLayerBoss;
        private bool chapterListOpen;

        public void Bind(Game.Hub.HubController controller, Game.Data.MapCatalog catalog)
        {
            hubController = controller;
            if (catalog != null)
            {
                mapCatalog = catalog;
            }

            WireButtons();
        }

        public void Open()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;
            viewLayer = Mathf.Max(1, progress.unlockedLayer);
            viewChapter = Mathf.Max(1, progress.unlockedChapter);

            SetChapterListOpen(false);
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(true);
            gameObject.SetActive(true);
            UIPanelSlider.OpenRoot(root);
            AutoSelectFirstUnlocked();
            Refresh();
        }

        public void Close()
        {
            SetChapterListOpen(false);
            UIPanelSlider.CloseRoot(panelRoot, gameObject);
        }

        public void CloseFromUser()
        {
            Close();
            Game.UI.HubMenuUI hubMenu = FindFirstObjectByType<Game.UI.HubMenuUI>(FindObjectsInactive.Include);
            if (hubMenu != null)
            {
                hubMenu.NotifyPanelClosed();
            }
        }

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            WireButtons();
            SetChapterListOpen(false);
        }

        private void WireButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartClicked);
                Text enterLabel = startButton.GetComponentInChildren<Text>();
                if (enterLabel != null && string.IsNullOrEmpty(enterLabel.text))
                {
                    enterLabel.text = "ENTER";
                }
            }

            if (layerBossButton != null)
            {
                // Legacy text button — replaced by stage-like LayerBoss node.
                layerBossButton.onClick.RemoveAllListeners();
                layerBossButton.gameObject.SetActive(false);
            }

            if (weeklyDungeonButton == null)
            {
                weeklyDungeonButton = Faz6RuntimeBootstrap.EnsureWeeklyButton(transform);
            }

            if (weeklyDungeonButton != null)
            {
                weeklyDungeonButton.onClick.RemoveAllListeners();
                weeklyDungeonButton.onClick.AddListener(OnWeeklyDungeonClicked);
                weeklyDungeonButton.gameObject.SetActive(true);
                Text weeklyLabel = weeklyDungeonButton.GetComponentInChildren<Text>();
                if (weeklyLabel != null)
                {
                    weeklyLabel.text = "WEEKLY";
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseFromUser);
            }

            if (chapterDropdownButton != null)
            {
                chapterDropdownButton.onClick.RemoveAllListeners();
                chapterDropdownButton.onClick.AddListener(ToggleChapterList);
            }

            WireStageNodes();
            WireLayerBossNode();
            WireChapterOptions();
        }

        private void ToggleChapterList()
        {
            SetChapterListOpen(!chapterListOpen);
            if (chapterListOpen)
            {
                RefreshChapterOptions();
            }
        }

        private void SetChapterListOpen(bool open)
        {
            chapterListOpen = open;
            if (chapterListRoot != null)
            {
                chapterListRoot.SetActive(open);
            }
        }

        private void WireChapterOptions()
        {
            EnsureChapterOptionsFromContent();
            if (chapterOptions == null)
            {
                return;
            }

            for (int i = 0; i < chapterOptions.Count; i++)
            {
                AdventureChapterOptionUI option = chapterOptions[i];
                if (option == null || option.Button == null)
                {
                    continue;
                }

                int captured = option.ChapterIndex > 0 ? option.ChapterIndex : i + 1;
                option.Button.onClick.RemoveAllListeners();
                option.Button.onClick.AddListener(() => SelectChapter(captured));
            }
        }

        private void EnsureChapterOptionsFromContent()
        {
            if (chapterOptions == null)
            {
                chapterOptions = new List<AdventureChapterOptionUI>();
            }

            if (chapterOptions.Count > 0 || chapterListContent == null)
            {
                return;
            }

            AdventureChapterOptionUI[] found =
                chapterListContent.GetComponentsInChildren<AdventureChapterOptionUI>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    chapterOptions.Add(found[i]);
                }
            }
        }

        private void RefreshChapterOptions()
        {
            EnsureChapterOptionsFromContent();
            if (chapterOptions == null || mapCatalog == null)
            {
                return;
            }

            Game.Data.LayerData layer = mapCatalog.GetLayer(viewLayer);
            int chapterCount = layer != null ? Mathf.Max(1, layer.ChapterCount) : 0;
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;

            for (int i = 0; i < chapterOptions.Count; i++)
            {
                AdventureChapterOptionUI option = chapterOptions[i];
                if (option == null)
                {
                    continue;
                }

                int chapterIndex = option.ChapterIndex > 0 ? option.ChapterIndex : i + 1;
                bool inRange = chapterIndex >= 1 && chapterIndex <= chapterCount;
                option.gameObject.SetActive(inRange);
                if (!inRange)
                {
                    continue;
                }

                Game.Data.ChapterData chapter = layer != null ? layer.GetChapter(chapterIndex) : null;
                string label = chapter != null && !string.IsNullOrEmpty(chapter.displayName)
                    ? chapter.displayName
                    : $"Chapter {chapterIndex}";
                bool unlocked = IsChapterUnlocked(progress, viewLayer, chapterIndex);
                bool selected = chapterIndex == viewChapter;
                option.SetState(
                    label,
                    unlocked,
                    selected,
                    dropdownItemNormalSprite,
                    dropdownItemSelectedSprite);
            }
        }

        private static bool IsChapterUnlocked(Game.Save.MapProgress progress, int layer, int chapter)
        {
            if (progress == null)
            {
                return chapter <= 1;
            }

            if (layer < progress.unlockedLayer)
            {
                return true;
            }

            if (layer > progress.unlockedLayer)
            {
                return false;
            }

            return chapter <= progress.unlockedChapter;
        }

        private void SelectChapter(int chapterIndex)
        {
            viewChapter = chapterIndex;
            selectedLevel = null;
            selectedLayerBoss = false;
            SetChapterListOpen(false);
            AutoSelectFirstUnlocked();
            Refresh();
        }

        private void WireStageNodes()
        {
            if (stageNodes == null)
            {
                return;
            }

            for (int i = 0; i < stageNodes.Count; i++)
            {
                AdventureStageNodeUI node = stageNodes[i];
                if (node == null || node.Button == null)
                {
                    continue;
                }

                if (node == layerBossNode || node.name == "Stage_LayerBoss")
                {
                    continue;
                }

                int capturedIndex = i + 1;
                node.Button.onClick.RemoveAllListeners();
                node.Button.onClick.AddListener(() => OnStageClicked(capturedIndex));
            }
        }

        private void WireLayerBossNode()
        {
            if (layerBossNode == null && stageNodes != null)
            {
                // Fallback: find by name under StagesRoot
                for (int i = 0; i < stageNodes.Count; i++)
                {
                    if (stageNodes[i] != null && stageNodes[i].name == "Stage_LayerBoss")
                    {
                        layerBossNode = stageNodes[i];
                        break;
                    }
                }
            }

            if (layerBossNode == null)
            {
                Transform stagesRoot = transform.Find("AdventureCard/StagesRoot");
                if (stagesRoot != null)
                {
                    Transform bossTf = stagesRoot.Find("Stage_LayerBoss");
                    if (bossTf != null)
                    {
                        layerBossNode = bossTf.GetComponent<AdventureStageNodeUI>();
                    }
                }
            }

            if (layerBossNode == null || layerBossNode.Button == null)
            {
                return;
            }

            layerBossNode.Button.onClick.RemoveAllListeners();
            layerBossNode.Button.onClick.AddListener(OnLayerBossNodeClicked);
        }

        private void OnStageClicked(int levelIndex)
        {
            if (mapCatalog == null)
            {
                return;
            }

            Game.Data.ChapterData chapter = GetViewChapter();
            Game.Data.LevelData level = chapter != null ? chapter.GetLevel(levelIndex) : null;
            if (level == null)
            {
                return;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (!Game.Core.RuntimePlayerState.MapProgress.IsLevelUnlocked(
                    level.layerIndex, level.chapterIndex, level.levelIndex))
            {
                return;
            }

            selectedLevel = level;
            selectedLayerBoss = false;
            Refresh();
        }

        private void OnLayerBossNodeClicked()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;
            if (!progress.IsLayerBossUnlocked(viewLayer)
                || progress.pendingLayerBossChapter != viewChapter)
            {
                return;
            }

            selectedLevel = null;
            selectedLayerBoss = true;
            Refresh();
        }

        private void AutoSelectFirstUnlocked()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;

            // Prefer layer boss when it is the next required fight.
            if (progress.IsLayerBossUnlocked(viewLayer) && progress.pendingLayerBossChapter > 0)
            {
                viewChapter = progress.pendingLayerBossChapter;
                selectedLevel = null;
                selectedLayerBoss = true;
                return;
            }

            Game.Data.ChapterData chapter = GetViewChapter();
            if (chapter == null || chapter.levels == null)
            {
                return;
            }

            if (selectedLevel != null
                && !selectedLayerBoss
                && progress.IsLevelUnlocked(selectedLevel.layerIndex, selectedLevel.chapterIndex, selectedLevel.levelIndex)
                && selectedLevel.chapterIndex == viewChapter
                && selectedLevel.layerIndex == viewLayer)
            {
                return;
            }

            selectedLevel = null;
            selectedLayerBoss = false;
            for (int i = 0; i < chapter.levels.Count; i++)
            {
                Game.Data.LevelData level = chapter.levels[i];
                if (level == null)
                {
                    continue;
                }

                if (progress.IsLevelUnlocked(level.layerIndex, level.chapterIndex, level.levelIndex))
                {
                    selectedLevel = level;
                }
            }
        }

        private Game.Data.ChapterData GetViewChapter()
        {
            if (mapCatalog == null)
            {
                return null;
            }

            Game.Data.LayerData layer = mapCatalog.GetLayer(viewLayer);
            if (layer == null)
            {
                return null;
            }

            int chapterCount = Mathf.Max(1, layer.ChapterCount);
            viewChapter = Mathf.Clamp(viewChapter, 1, chapterCount);
            return layer.GetChapter(viewChapter);
        }

        private void Refresh()
        {
            if (mapCatalog == null)
            {
                return;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            Game.Save.MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;
            Game.Data.LayerData layer = mapCatalog.GetLayer(viewLayer);
            Game.Data.ChapterData chapter = GetViewChapter();

            if (titleText != null)
            {
                titleText.text = "ADVENTURE";
            }

            if (chapterDropdownLabel != null)
            {
                chapterDropdownLabel.text = chapter != null && !string.IsNullOrEmpty(chapter.displayName)
                    ? chapter.displayName
                    : $"Chapter {viewChapter}";
            }

            RefreshDropdownButtonArt();
            RefreshStageNodes(chapter, progress);
            RefreshLayerBossNode(layer, progress);

            if (layerBossButton != null)
            {
                layerBossButton.gameObject.SetActive(false);
            }

            if (detailText != null)
            {
                detailText.gameObject.SetActive(false);
            }

            if (startButton != null)
            {
                bool canStartLevel = selectedLevel != null
                    && progress.IsLevelUnlocked(
                        selectedLevel.layerIndex, selectedLevel.chapterIndex, selectedLevel.levelIndex);
                bool canStartBoss = selectedLayerBoss && progress.IsLayerBossUnlocked(viewLayer);
                startButton.interactable = canStartLevel || canStartBoss;
                Text enterLabel = startButton.GetComponentInChildren<Text>();
                if (enterLabel != null)
                {
                    enterLabel.text = "ENTER";
                }
            }

            if (chapterListOpen)
            {
                RefreshChapterOptions();
            }
        }

        private void RefreshDropdownButtonArt()
        {
            if (chapterDropdownButton == null)
            {
                return;
            }

            Image img = chapterDropdownButton.GetComponent<Image>();
            if (img == null)
            {
                return;
            }

            // Closed bar uses selected art so current chapter reads clearly.
            Sprite use = dropdownItemSelectedSprite != null
                ? dropdownItemSelectedSprite
                : dropdownItemNormalSprite;
            if (use != null)
            {
                img.sprite = use;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
                img.CrossFadeAlpha(1f, 0f, true);
                img.canvasRenderer.SetAlpha(1f);
            }
        }

        private void RefreshStageNodes(Game.Data.ChapterData chapter, Game.Save.MapProgress progress)
        {
            if (stageNodes == null)
            {
                return;
            }

            for (int i = 0; i < stageNodes.Count; i++)
            {
                AdventureStageNodeUI node = stageNodes[i];
                if (node == null)
                {
                    continue;
                }

                int levelIndex = i + 1;
                Game.Data.LevelData level = chapter != null ? chapter.GetLevel(levelIndex) : null;
                bool hasLevel = level != null;
                bool unlocked = hasLevel
                    && progress.IsLevelUnlocked(level.layerIndex, level.chapterIndex, level.levelIndex);
                bool isSelected = hasLevel && !selectedLayerBoss && selectedLevel == level;

                string label = hasLevel
                    ? (level.isChapterBoss ? "B" : level.levelIndex.ToString())
                    : levelIndex.ToString();

                // Skip dedicated layer-boss node if it was accidentally listed in stageNodes.
                if (node == layerBossNode || node.name == "Stage_LayerBoss")
                {
                    continue;
                }

                if (!hasLevel && i >= (chapter != null && chapter.levels != null ? chapter.levels.Count : 0))
                {
                    node.gameObject.SetActive(false);
                    continue;
                }

                node.gameObject.SetActive(true);

                if (stageNormalSprite != null || stageSelectedSprite != null || stageLockedSprite != null)
                {
                    node.Configure(
                        levelIndex,
                        stageNormalSprite,
                        stageSelectedSprite,
                        stageLockedSprite);
                }

                node.SetState(unlocked, isSelected, label);
            }
        }

        private void RefreshLayerBossNode(Game.Data.LayerData layer, Game.Save.MapProgress progress)
        {
            if (layerBossNode == null)
            {
                WireLayerBossNode();
            }

            if (layerBossNode == null)
            {
                return;
            }

            layerBossNode.gameObject.SetActive(true);

            // Per-chapter: only the chapter whose LB was beaten stays open; others stay locked
            // until that chapter's stage 10 is cleared (pending).
            bool pendingForThisChapter = progress.IsLayerBossUnlocked(viewLayer)
                                         && progress.pendingLayerBossChapter == viewChapter;
            bool beatenForThisChapter = progress.layerBossClearedChapterUpTo >= viewChapter
                                        || progress.IsLayerBossCleared(viewLayer);
            bool showLocked = !pendingForThisChapter && !beatenForThisChapter;
            bool isSelected = selectedLayerBoss && pendingForThisChapter;
            string label = pendingForThisChapter ? "LB" : (beatenForThisChapter ? "OK" : "LB");

            if (stageNormalSprite != null || stageSelectedSprite != null || stageLockedSprite != null)
            {
                layerBossNode.Configure(
                    100,
                    stageNormalSprite,
                    stageSelectedSprite,
                    stageLockedSprite);
            }

            layerBossNode.SetState(pendingForThisChapter, isSelected, label, showLocked);
        }

        private void OnWeeklyDungeonClicked()
        {
            if (hubController != null)
            {
                hubController.OpenWeeklyDungeon();
                return;
            }

            Game.UI.WeeklyDungeonPanelUI panel =
                FindFirstObjectByType<Game.UI.WeeklyDungeonPanelUI>(FindObjectsInactive.Include);
            if (panel != null)
            {
                Game.Dungeon.WeeklyDungeonService weekly =
                    FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
                panel.Bind(weekly);
                panel.Open();
            }
        }

        private void OnStartClicked()
        {
            if (hubController == null || mapCatalog == null)
            {
                return;
            }

            if (selectedLayerBoss)
            {
                Game.Data.LayerData layer = mapCatalog.GetLayer(viewLayer);
                if (layer == null)
                {
                    return;
                }

                Game.Core.RuntimePlayerState.EnsureInitialized();
                Game.Save.MapProgress progress = Game.Core.RuntimePlayerState.MapProgress;
                if (!progress.IsLayerBossUnlocked(viewLayer)
                    || progress.pendingLayerBossChapter != viewChapter)
                {
                    return;
                }

                hubController.StartLayerBoss(layer);
                return;
            }

            if (selectedLevel == null)
            {
                return;
            }

            hubController.StartLevel(selectedLevel);
        }
    }
}
