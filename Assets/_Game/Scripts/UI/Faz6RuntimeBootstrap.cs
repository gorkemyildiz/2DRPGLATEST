using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Creates missing Roster/Weekly UI and service objects at runtime (editor Play Mode / MVP).
    /// Prefer wizard wiring for persistent scene objects.
    /// </summary>
    public static class Faz6RuntimeBootstrap
    {
        public static RosterServiceEnsureResult EnsureRosterService()
        {
            Game.Roster.RosterService existing = Object.FindFirstObjectByType<Game.Roster.RosterService>();
            if (existing != null)
            {
                return new RosterServiceEnsureResult { service = existing, created = false };
            }

            Transform parent = FindSystemsRoot();
            GameObject go = new GameObject("RosterController");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            Game.Roster.RosterService service = go.AddComponent<Game.Roster.RosterService>();
            return new RosterServiceEnsureResult { service = service, created = true };
        }

        public static WeeklyServiceEnsureResult EnsureWeeklyService()
        {
            Game.Dungeon.WeeklyDungeonService existing =
                Object.FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
            if (existing != null)
            {
                return new WeeklyServiceEnsureResult { service = existing, created = false };
            }

            Transform parent = FindSystemsRoot();
            GameObject go = new GameObject("WeeklyDungeonController");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            Game.Dungeon.WeeklyDungeonService service = go.AddComponent<Game.Dungeon.WeeklyDungeonService>();
            return new WeeklyServiceEnsureResult { service = service, created = true };
        }

        public static RosterPanelUI EnsureRosterPanel()
        {
            RosterPanelUI existing = Object.FindFirstObjectByType<RosterPanelUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            Transform canvas = FindScreenCanvas();
            if (canvas == null)
            {
                return null;
            }

            GameObject panel = CreateDimPanel(canvas, "RosterPanel");
            GameObject card = CreateCard(panel.transform, new Vector2(520f, 360f), new Color(0.07f, 0.06f, 0.12f, 0.98f));
            Text title = CreateLabel(card.transform, "RosterTitle", "CLASS", new Vector2(0f, 140f), 24, FontStyle.Bold);
            title.color = new Color(0.78f, 0.7f, 1f);
            Button close = CreateBtn(card.transform, "CloseRosterButton", "Close", new Vector2(200f, 140f), new Vector2(72f, 32f));
            Text body = CreateLabel(card.transform, "RosterBody", "Classes", new Vector2(0f, 10f), 14, FontStyle.Normal);
            body.alignment = TextAnchor.UpperLeft;
            body.rectTransform.sizeDelta = new Vector2(460f, 180f);
            Text status = CreateLabel(card.transform, "RosterStatus", "", new Vector2(0f, -100f), 13, FontStyle.Normal);
            status.color = new Color(1f, 0.85f, 0.45f);
            Button prev = CreateBtn(card.transform, "PrevClassButton", "<", new Vector2(-160f, -140f), new Vector2(80f, 34f));
            Button next = CreateBtn(card.transform, "NextClassButton", ">", new Vector2(-70f, -140f), new Vector2(80f, 34f));
            Button select = CreateBtn(card.transform, "SelectClassButton", "Select", new Vector2(40f, -140f), new Vector2(100f, 34f));
            Button unlock = CreateBtn(card.transform, "UnlockClassButton", "Unlock", new Vector2(160f, -140f), new Vector2(100f, 34f));

            RosterPanelUI ui = panel.AddComponent<RosterPanelUI>();
            ui.Configure(panel, title, body, status, close, prev, next, select, unlock);
            panel.SetActive(false);
            return ui;
        }

        public static WeeklyDungeonPanelUI EnsureWeeklyPanel()
        {
            WeeklyDungeonPanelUI existing =
                Object.FindFirstObjectByType<WeeklyDungeonPanelUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            Transform canvas = FindScreenCanvas();
            if (canvas == null)
            {
                return null;
            }

            GameObject panel = CreateDimPanel(canvas, "WeeklyDungeonPanel");
            GameObject card = CreateCard(panel.transform, new Vector2(520f, 340f), new Color(0.05f, 0.08f, 0.1f, 0.98f));
            Text title = CreateLabel(card.transform, "WeeklyTitle", "WEEKLY DUNGEON", new Vector2(0f, 130f), 22, FontStyle.Bold);
            title.color = new Color(0.55f, 0.9f, 0.95f);
            Button close = CreateBtn(card.transform, "CloseWeeklyButton", "Close", new Vector2(200f, 130f), new Vector2(72f, 32f));
            Text body = CreateLabel(card.transform, "WeeklyBody", "Dungeon", new Vector2(0f, 10f), 14, FontStyle.Normal);
            body.alignment = TextAnchor.UpperLeft;
            body.rectTransform.sizeDelta = new Vector2(460f, 160f);
            Text status = CreateLabel(card.transform, "WeeklyStatus", "", new Vector2(0f, -90f), 13, FontStyle.Normal);
            status.color = new Color(1f, 0.85f, 0.45f);
            Button claim = CreateBtn(card.transform, "ClaimStoneButton", "Claim Stone", new Vector2(-90f, -135f), new Vector2(160f, 36f));
            Button enter = CreateBtn(card.transform, "EnterWeeklyButton", "Enter", new Vector2(90f, -135f), new Vector2(160f, 36f));

            WeeklyDungeonPanelUI ui = panel.AddComponent<WeeklyDungeonPanelUI>();
            ui.Configure(panel, title, body, status, close, enter, claim);
            panel.SetActive(false);
            return ui;
        }

        public static Button EnsureClassButton(Transform inventoryRoot)
        {
            if (inventoryRoot == null)
            {
                return null;
            }

            Transform card = inventoryRoot.Find("Card") ?? inventoryRoot;
            Transform existing = card.Find("ClassButton");
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            return CreateBtn(card, "ClassButton", "CLASS", new Vector2(-160f, 210f), new Vector2(130f, 42f),
                new Color(0.35f, 0.28f, 0.55f, 1f));
        }

        public static Button EnsureWeeklyButton(Transform mapSelectRoot)
        {
            if (mapSelectRoot == null)
            {
                return null;
            }

            Transform card = mapSelectRoot.Find("Card") ?? mapSelectRoot;
            Transform existing = card.Find("WeeklyDungeonButton");
            if (existing == null)
            {
                existing = mapSelectRoot.Find("WeeklyDungeonButton");
            }

            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            return CreateBtn(card, "WeeklyDungeonButton", "WEEKLY", new Vector2(220f, -210f), new Vector2(150f, 40f),
                new Color(0.15f, 0.42f, 0.48f, 1f));
        }

        private static Transform FindSystemsRoot()
        {
            GameObject systems = GameObject.Find("Systems");
            return systems != null ? systems.transform : null;
        }

        private static Transform FindScreenCanvas()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            return canvas != null ? canvas.transform : null;
        }

        private static GameObject CreateDimPanel(Transform canvas, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(canvas, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image dim = panel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            return panel;
        }

        private static GameObject CreateCard(Transform parent, Vector2 size, Color color)
        {
            GameObject card = new GameObject("Card");
            card.transform.SetParent(parent, false);
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            Image bg = card.AddComponent<Image>();
            bg.color = color;
            return card;
        }

        private static Text CreateLabel(
            Transform parent,
            string name,
            string text,
            Vector2 anchored,
            int fontSize,
            FontStyle style)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(420f, 32f);
            Text label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateBtn(
            Transform parent,
            string name,
            string label,
            Vector2 anchored,
            Vector2 size,
            Color? color = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = color ?? new Color(0.2f, 0.25f, 0.3f, 1f);
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
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        public struct RosterServiceEnsureResult
        {
            public Game.Roster.RosterService service;
            public bool created;
        }

        public struct WeeklyServiceEnsureResult
        {
            public Game.Dungeon.WeeklyDungeonService service;
            public bool created;
        }
    }
}
