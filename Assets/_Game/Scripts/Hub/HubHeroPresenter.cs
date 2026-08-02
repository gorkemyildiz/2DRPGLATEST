using System.Collections.Generic;
using UnityEngine;

namespace Game.Hub
{
    /// <summary>
    /// Spawns/shows party heroes in the village plaza (visual only, no combat).
    /// </summary>
    public class HubHeroPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform heroStandPoint;
        [SerializeField] private GameObject heroPrefab;
        [SerializeField] private Transform unitsRoot;

        [Header("Display")]
        [SerializeField] private bool faceLeft = false;
        [SerializeField] private Vector3 displayScale = Vector3.one;
        [SerializeField] private float partySpacing = 1.2f;

        private readonly List<GameObject> heroInstances = new List<GameObject>();
        private Game.Roster.RosterService rosterService;

        private void Start()
        {
            ResolveRoster();
            PresentHero();
        }

        private void OnEnable()
        {
            ResolveRoster();
            if (rosterService != null)
            {
                rosterService.ClassChanged -= OnPartyChanged;
                rosterService.RosterChanged -= OnPartyChanged;
                rosterService.ClassChanged += OnPartyChanged;
                rosterService.RosterChanged += OnPartyChanged;
            }
        }

        private void OnDisable()
        {
            if (rosterService != null)
            {
                rosterService.ClassChanged -= OnPartyChanged;
                rosterService.RosterChanged -= OnPartyChanged;
            }
        }

        private void ResolveRoster()
        {
            if (rosterService == null)
            {
                rosterService = FindFirstObjectByType<Game.Roster.RosterService>();
            }
        }

        private void OnPartyChanged()
        {
            PresentHero();
        }

        public void PresentHero()
        {
            if (heroStandPoint == null)
            {
                Debug.LogWarning("[HubHeroPresenter] HeroStandPoint missing.");
                return;
            }

            ClearInstances();
            ResolveRoster();

            List<string> classIds = rosterService != null
                ? rosterService.GetFilledPartyClassIds()
                : new List<string> { "fighter" };

            if (classIds.Count == 0)
            {
                classIds.Add("fighter");
            }

            for (int i = 0; i < classIds.Count; i++)
            {
                GameObject prefabToUse = heroPrefab;
                if (rosterService != null)
                {
                    Game.Data.HeroClassData data = rosterService.GetClass(classIds[i]);
                    if (data != null && data.heroPrefab != null)
                    {
                        prefabToUse = data.heroPrefab;
                    }
                }

                GameObject instance;
                if (prefabToUse != null)
                {
                    Transform parent = unitsRoot != null ? unitsRoot : transform;
                    instance = Instantiate(prefabToUse, heroStandPoint.position, Quaternion.identity, parent);
                }
                else
                {
                    instance = new GameObject("HubHeroPlaceholder");
                    instance.transform.SetParent(unitsRoot != null ? unitsRoot : transform);
                    SpriteRenderer sr = instance.AddComponent<SpriteRenderer>();
                    sr.color = new Color(0.25f, 0.65f, 0.85f, 1f);
                }

                instance.name = i == 0 ? "HubHero" : $"HubHero_{i}";
                Vector3 pos = heroStandPoint.position;
                pos.x -= partySpacing * i;
                instance.transform.position = pos;
                instance.transform.localScale = displayScale;

                DisableCombatComponents(instance);
                HideWorldHealthBar(instance);

                SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = faceLeft;
                    spriteRenderer.color = Color.white;
                }

                Animator animator = instance.GetComponent<Animator>();
                if (animator != null && HasParameter(animator, "IsWalking"))
                {
                    animator.SetBool("IsWalking", false);
                }

                heroInstances.Add(instance);
            }

            Debug.Log($"[HubHeroPresenter] Party presented in hub ({heroInstances.Count}).");
        }

        private void ClearInstances()
        {
            for (int i = 0; i < heroInstances.Count; i++)
            {
                if (heroInstances[i] != null)
                {
                    Destroy(heroInstances[i]);
                }
            }

            heroInstances.Clear();
        }

        private static void DisableCombatComponents(GameObject hero)
        {
            var heroUnit = hero.GetComponent<Game.Units.HeroUnit>();
            if (heroUnit != null)
            {
                heroUnit.enabled = false;
            }

            var progression = hero.GetComponent<Game.Units.HeroProgression>();
            if (progression != null)
            {
                progression.enabled = false;
            }

            var health = hero.GetComponent<Game.Combat.Health>();
            if (health != null)
            {
                health.enabled = false;
            }

            var colliders = hero.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void HideWorldHealthBar(GameObject hero)
        {
            Game.UI.HealthBarUI[] bars = hero.GetComponentsInChildren<Game.UI.HealthBarUI>(true);
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i].gameObject.SetActive(false);
            }

            Canvas[] canvases = hero.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].GetComponentInChildren<Game.UI.HealthBarUI>(true) != null)
                {
                    canvases[i].gameObject.SetActive(false);
                }
            }
        }

        private static bool HasParameter(Animator anim, string paramName)
        {
            if (anim == null || anim.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.name == paramName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
