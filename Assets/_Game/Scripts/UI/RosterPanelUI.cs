using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class RosterPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Labels")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button prevSlotButton;
        [SerializeField] private Button nextSlotButton;
        [SerializeField] private Button unlockSlotButton;
        [SerializeField] private Button clearSlotButton;

        [Header("Services")]
        [SerializeField] private Game.Roster.RosterService rosterService;

        private readonly List<Game.Data.HeroClassData> classes = new List<Game.Data.HeroClassData>();
        private int classIndex;
        private int partySlotIndex;

        public void Bind(Game.Roster.RosterService roster)
        {
            rosterService = roster;
            WireButtons();
            RebuildCache();
            Refresh();
        }

        public void Configure(
            GameObject root,
            Text title,
            Text body,
            Text status,
            Button close,
            Button prev,
            Button next,
            Button select,
            Button unlock)
        {
            panelRoot = root != null ? root : gameObject;
            titleText = title;
            bodyText = body;
            statusText = status;
            closeButton = close;
            prevButton = prev;
            nextButton = next;
            selectButton = select;
            unlockButton = unlock;
            WireButtons();
        }

        public void Open()
        {
            ResolveRefs();
            RebuildCache();
            FocusSelected();
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(true);
            gameObject.SetActive(true);
            UIPanelSlider.OpenRoot(root);
            Refresh();
        }

        public void Close()
        {
            UIPanelSlider.CloseRoot(panelRoot, gameObject);
        }

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            WireButtons();
        }

        private void OnEnable()
        {
            ResolveRefs();
            Subscribe();
            RebuildCache();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveRefs()
        {
            if (rosterService == null)
            {
                rosterService = FindFirstObjectByType<Game.Roster.RosterService>();
            }
        }

        private void Subscribe()
        {
            if (rosterService != null)
            {
                rosterService.ClassChanged -= Refresh;
                rosterService.RosterChanged -= Refresh;
                rosterService.ClassChanged += Refresh;
                rosterService.RosterChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (rosterService != null)
            {
                rosterService.ClassChanged -= Refresh;
                rosterService.RosterChanged -= Refresh;
            }
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (prevButton != null)
            {
                prevButton.onClick.RemoveAllListeners();
                prevButton.onClick.AddListener(() => StepClass(-1));
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => StepClass(1));
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnAssignToSlot);
                Text label = selectButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "Assign";
                }
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(OnUnlockClass);
            }

            EnsurePartyButtons();

            if (prevSlotButton != null)
            {
                prevSlotButton.onClick.RemoveAllListeners();
                prevSlotButton.onClick.AddListener(() => StepSlot(-1));
            }

            if (nextSlotButton != null)
            {
                nextSlotButton.onClick.RemoveAllListeners();
                nextSlotButton.onClick.AddListener(() => StepSlot(1));
            }

            if (unlockSlotButton != null)
            {
                unlockSlotButton.onClick.RemoveAllListeners();
                unlockSlotButton.onClick.AddListener(OnUnlockSlot);
            }

            if (clearSlotButton != null)
            {
                clearSlotButton.onClick.RemoveAllListeners();
                clearSlotButton.onClick.AddListener(OnClearSlot);
            }
        }

        private void EnsurePartyButtons()
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform card = panelRoot.transform.Find("Card");
            if (card == null)
            {
                card = panelRoot.transform;
            }

            if (prevSlotButton == null)
            {
                prevSlotButton = FindOrCreateButton(card, "PrevSlotButton", "<S", new Vector2(-200f, -175f), new Vector2(60f, 30f));
            }

            if (nextSlotButton == null)
            {
                nextSlotButton = FindOrCreateButton(card, "NextSlotButton", "S>", new Vector2(-130f, -175f), new Vector2(60f, 30f));
            }

            if (unlockSlotButton == null)
            {
                unlockSlotButton = FindOrCreateButton(card, "UnlockSlotButton", "Unlock Slot", new Vector2(10f, -175f), new Vector2(130f, 30f));
            }

            if (clearSlotButton == null)
            {
                clearSlotButton = FindOrCreateButton(card, "ClearSlotButton", "Clear", new Vector2(150f, -175f), new Vector2(80f, 30f));
            }
        }

        private static Button FindOrCreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            return CreateNamedButton(parent, name, label, pos, size);
        }

        private static Button CreateNamedButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.22f, 0.35f, 1f);
            Button button = go.AddComponent<Button>();

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.text = label;
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private void RebuildCache()
        {
            classes.Clear();
            ResolveRefs();
            if (rosterService == null || rosterService.Catalog == null || rosterService.Catalog.classes == null)
            {
                return;
            }

            for (int i = 0; i < rosterService.Catalog.classes.Count; i++)
            {
                if (rosterService.Catalog.classes[i] != null)
                {
                    classes.Add(rosterService.Catalog.classes[i]);
                }
            }
        }

        private void FocusSelected()
        {
            if (rosterService == null || classes.Count == 0)
            {
                return;
            }

            string selected = rosterService.GetSelectedClassId();
            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i].classId == selected)
                {
                    classIndex = i;
                    return;
                }
            }
        }

        private void StepClass(int delta)
        {
            if (classes.Count == 0)
            {
                return;
            }

            classIndex = (classIndex + delta + classes.Count) % classes.Count;
            Refresh();
        }

        private void StepSlot(int delta)
        {
            if (rosterService == null)
            {
                return;
            }

            int max = rosterService.GetMaxPartySlots();
            partySlotIndex = (partySlotIndex + delta + max) % max;
            Refresh();
        }

        private void OnAssignToSlot()
        {
            Game.Data.HeroClassData data = Current();
            if (data == null || rosterService == null)
            {
                return;
            }

            if (rosterService.TryAssignToPartySlot(partySlotIndex, data.classId))
            {
                SetStatus($"Assigned {data.displayName} → Slot {partySlotIndex + 1}");
                Game.Save.GameSaveController.SaveGame();
                Refresh();
            }
            else
            {
                rosterService.CanAssignToPartySlot(partySlotIndex, data.classId, out string reason);
                SetStatus(reason ?? "Cannot assign");
            }
        }

        private void OnUnlockClass()
        {
            Game.Data.HeroClassData data = Current();
            if (data == null || rosterService == null)
            {
                return;
            }

            if (rosterService.TryUnlock(data.classId))
            {
                SetStatus($"Unlocked {data.displayName}");
                Game.Save.GameSaveController.SaveGame();
                Refresh();
            }
            else
            {
                rosterService.CanUnlock(data.classId, out string reason);
                SetStatus(reason ?? "Cannot unlock");
            }
        }

        private void OnUnlockSlot()
        {
            if (rosterService == null)
            {
                return;
            }

            if (rosterService.TryUnlockNextPartySlot())
            {
                partySlotIndex = rosterService.GetUnlockedPartySlots() - 1;
                SetStatus($"Unlocked party slot {partySlotIndex + 1}");
                Game.Save.GameSaveController.SaveGame();
                Refresh();
            }
            else
            {
                rosterService.CanUnlockNextPartySlot(out string reason);
                SetStatus(reason ?? "Cannot unlock slot");
            }
        }

        private void OnClearSlot()
        {
            if (rosterService == null)
            {
                return;
            }

            if (rosterService.TryClearPartySlot(partySlotIndex))
            {
                SetStatus($"Cleared slot {partySlotIndex + 1}");
                Game.Save.GameSaveController.SaveGame();
                Refresh();
            }
            else
            {
                SetStatus(partySlotIndex == 0 ? "Lead slot cannot be cleared" : "Cannot clear");
            }
        }

        private Game.Data.HeroClassData Current()
        {
            if (classes.Count == 0)
            {
                return null;
            }

            classIndex = Mathf.Clamp(classIndex, 0, classes.Count - 1);
            return classes[classIndex];
        }

        private void Refresh()
        {
            ResolveRefs();
            EnsurePartyButtons();
            if (classes.Count == 0)
            {
                RebuildCache();
            }

            Game.Data.HeroClassData data = Current();
            if (titleText != null)
            {
                titleText.text = "CLASS / PARTY";
            }

            if (bodyText != null)
            {
                StringBuilder sb = new StringBuilder(512);
                AppendPartySummary(sb);
                sb.AppendLine();
                if (data == null)
                {
                    sb.Append("No classes in catalog.");
                }
                else
                {
                    bool unlocked = rosterService != null && rosterService.IsUnlocked(data.classId);
                    sb.AppendLine($"Class: {data.displayName} ({classIndex + 1}/{classes.Count})");
                    sb.AppendLine(unlocked ? "Class: Unlocked" : "Class: Locked");
                    sb.AppendLine($"ATK {data.baseAttackDamage}  HP {data.baseMaxHealth}");
                    sb.AppendLine($"Unlock Lv {data.unlockLevel}" +
                                  (data.unlockGoldCost > 0 ? $"  /  {data.unlockGoldCost} gold" : ""));
                }

                bodyText.text = sb.ToString();
            }

            if (selectButton != null)
            {
                bool canAssign = data != null && rosterService != null
                                 && rosterService.CanAssignToPartySlot(partySlotIndex, data.classId, out _);
                selectButton.interactable = canAssign;
            }

            if (unlockButton != null)
            {
                bool locked = data != null && rosterService != null && !rosterService.IsUnlocked(data.classId);
                unlockButton.interactable = locked && rosterService.CanUnlock(data.classId, out _);
                unlockButton.gameObject.SetActive(locked);
            }

            if (unlockSlotButton != null)
            {
                unlockSlotButton.interactable = rosterService != null && rosterService.CanUnlockNextPartySlot(out _);
            }

            if (clearSlotButton != null)
            {
                clearSlotButton.interactable = partySlotIndex > 0
                    && rosterService != null
                    && rosterService.IsPartySlotUnlocked(partySlotIndex)
                    && !string.IsNullOrEmpty(rosterService.GetPartyClassId(partySlotIndex));
            }
        }

        private void AppendPartySummary(StringBuilder sb)
        {
            if (rosterService == null)
            {
                sb.AppendLine("Party: (service missing)");
                return;
            }

            int unlocked = rosterService.GetUnlockedPartySlots();
            int max = rosterService.GetMaxPartySlots();
            partySlotIndex = Mathf.Clamp(partySlotIndex, 0, max - 1);
            sb.AppendLine($"Party slots: {unlocked}/{max}  |  Focus Slot {partySlotIndex + 1}");

            for (int i = 0; i < max; i++)
            {
                string mark = i == partySlotIndex ? ">" : " ";
                if (!rosterService.IsPartySlotUnlocked(i))
                {
                    int cost = rosterService.GetPartySlotUnlockCost(i);
                    sb.AppendLine($"{mark} Slot {i + 1}: LOCKED ({cost}g)");
                    continue;
                }

                string id = rosterService.GetPartyClassId(i);
                Game.Data.HeroClassData cls = string.IsNullOrEmpty(id) ? null : rosterService.GetClass(id);
                string name = cls != null ? cls.displayName : (string.IsNullOrEmpty(id) ? "Empty" : id);
                sb.AppendLine($"{mark} Slot {i + 1}: {name}");
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }
    }
}
