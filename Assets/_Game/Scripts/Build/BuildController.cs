using UnityEngine;

namespace Game.Build
{
    /// <summary>
    /// Holds RuneService + SkillService on one scene object (Hub / Battle).
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [SerializeField] private RuneService runeService;
        [SerializeField] private SkillService skillService;
        [SerializeField] private Game.Data.RuneCatalog runeCatalog;
        [SerializeField] private Game.Data.SkillCatalog skillCatalog;

        public RuneService Runes => runeService;
        public SkillService Skills => skillService;

        private void Awake()
        {
            EnsureServices();
            ApplyCatalogs();
        }

        public void EnsureServices()
        {
            if (runeService == null)
            {
                runeService = GetComponent<RuneService>();
                if (runeService == null)
                {
                    runeService = gameObject.AddComponent<RuneService>();
                }
            }

            if (skillService == null)
            {
                skillService = GetComponent<SkillService>();
                if (skillService == null)
                {
                    skillService = gameObject.AddComponent<SkillService>();
                }
            }
        }

        public void ApplyCatalogs()
        {
            EnsureServices();
            if (runeCatalog != null)
            {
                runeService.SetCatalog(runeCatalog);
            }

            if (skillCatalog != null)
            {
                skillService.SetCatalog(skillCatalog);
            }
        }

        public void SetCatalogs(Game.Data.RuneCatalog runes, Game.Data.SkillCatalog skills)
        {
            runeCatalog = runes;
            skillCatalog = skills;
            ApplyCatalogs();
        }
    }
}
