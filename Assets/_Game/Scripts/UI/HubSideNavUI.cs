using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public enum HubNavTab
    {
        None = 0,
        Adventure = 1,
        Village = 2,
        Inventory = 3,
        Quit = 4
    }

    /// <summary>
    /// Left vertical icon rail for VillageHub.
    /// Selected icon scales up; swap sprites on each Icon Image.
    /// </summary>
    public class HubSideNavUI : MonoBehaviour
    {
        [System.Serializable]
        public class NavItem
        {
            public HubNavTab tab;
            public Button button;
            public RectTransform scaleRoot;
            public Image iconImage;
        }

        [SerializeField] private NavItem[] items;
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float selectedScale = 1.22f;
        [SerializeField] private float scaleDuration = 0.18f;

        private HubNavTab selected = HubNavTab.None;
        private Coroutine scaleRoutine;

        public HubNavTab Selected => selected;

        public void SetItems(NavItem[] navItems)
        {
            items = navItems;
            ApplyScalesImmediate();
        }

        public void Select(HubNavTab tab, bool animate = true)
        {
            selected = tab;
            if (animate && isActiveAndEnabled)
            {
                if (scaleRoutine != null)
                {
                    StopCoroutine(scaleRoutine);
                }

                scaleRoutine = StartCoroutine(AnimateScales());
            }
            else
            {
                ApplyScalesImmediate();
            }
        }

        public void ClearSelection(bool animate = true)
        {
            Select(HubNavTab.None, animate);
        }

        public Image GetIconImage(HubNavTab tab)
        {
            if (items == null)
            {
                return null;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].tab == tab)
                {
                    return items[i].iconImage;
                }
            }

            return null;
        }

        private void OnEnable()
        {
            // Quit must never remain oversized after a previous click.
            if (selected == HubNavTab.Quit)
            {
                selected = HubNavTab.Village;
            }

            ApplyScalesImmediate();
        }

        private void Awake()
        {
            // Normalize any stuck scales saved in the scene.
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null && items[i].scaleRoot != null)
                    {
                        items[i].scaleRoot.localScale = Vector3.one * normalScale;
                    }
                }
            }
        }

        private void ApplyScalesImmediate()
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                NavItem item = items[i];
                if (item == null || item.scaleRoot == null)
                {
                    continue;
                }

                float s = item.tab == selected ? selectedScale : normalScale;
                item.scaleRoot.localScale = Vector3.one * s;
            }
        }

        private IEnumerator AnimateScales()
        {
            if (items == null)
            {
                yield break;
            }

            Vector3[] from = new Vector3[items.Length];
            Vector3[] to = new Vector3[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                NavItem item = items[i];
                if (item == null || item.scaleRoot == null)
                {
                    from[i] = Vector3.one;
                    to[i] = Vector3.one;
                    continue;
                }

                from[i] = item.scaleRoot.localScale;
                float s = item.tab == selected ? selectedScale : normalScale;
                to[i] = Vector3.one * s;
            }

            float t = 0f;
            while (t < scaleDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / scaleDuration);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                for (int i = 0; i < items.Length; i++)
                {
                    NavItem item = items[i];
                    if (item == null || item.scaleRoot == null)
                    {
                        continue;
                    }

                    item.scaleRoot.localScale = Vector3.Lerp(from[i], to[i], eased);
                }

                yield return null;
            }

            ApplyScalesImmediate();
            scaleRoutine = null;
        }
    }
}
