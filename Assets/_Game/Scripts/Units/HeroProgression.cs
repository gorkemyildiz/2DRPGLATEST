using UnityEngine;

namespace Game.Units
{
    /// <summary>
    /// Applies player level + equipment + rune/passive skill bonuses to hero attack/health.
    /// </summary>
    public class HeroProgression : MonoBehaviour
    {
        [Header("Base Stats (Level 1)")]
        [Tooltip("Used only if HeroUnit is missing. Prefer editing HeroUnit → Attack Damage.")]
        [SerializeField] private int baseAttackDamage = 28;
        [SerializeField] private int baseMaxHealth = 200;

        [Header("Per Level Bonuses")]
        [SerializeField] private int attackPerLevel = 5;
        [SerializeField] private int healthPerLevel = 25;

        [Header("Level Up Behavior")]
        [SerializeField] private bool restoreFullHealthOnLevelUp = true;
        [SerializeField] private bool healPartialIfNotFullRestore = true;
        [SerializeField] [Range(0f, 1f)] private float partialHealPercent = 0.35f;

        [Header("References")]
        [SerializeField] private HeroUnit heroUnit;
        [SerializeField] private Game.Combat.Health health;
        [SerializeField] private Game.Rewards.RewardService rewardService;
        [SerializeField] private Game.Inventory.EquipmentService equipmentService;
        [SerializeField] private Game.Build.RuneService runeService;
        [SerializeField] private Game.Build.SkillService skillService;
        [SerializeField] private Game.Roster.RosterService rosterService;
        [SerializeField] private Game.UI.LevelUpPanel levelUpPanel;

        private int currentAppliedLevel = 1;

        public int CurrentAppliedLevel => currentAppliedLevel;
        public int CurrentAttackDamage => heroUnit != null ? heroUnit.AttackDamage : baseAttackDamage;
        public int CurrentMaxHealth => health != null ? health.MaxHealth : baseMaxHealth;

        private void Awake()
        {
            if (heroUnit == null)
            {
                heroUnit = GetComponent<HeroUnit>();
            }

            if (health == null)
            {
                health = GetComponent<Game.Combat.Health>();
            }
        }

        private void Start()
        {
            ResolveRefs();

            int startLevel = rewardService != null && rewardService.Progress != null
                ? Mathf.Max(1, rewardService.Progress.level)
                : 1;

            TrySubscribe();
            ApplyLevel(startLevel, isLevelUp: false);
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (rewardService != null)
            {
                rewardService.LevelUp -= OnLevelUp;
            }

            if (equipmentService != null)
            {
                equipmentService.EquipmentChanged -= OnBuildChanged;
            }

            if (runeService != null)
            {
                runeService.Changed -= OnBuildChanged;
            }

            if (skillService != null)
            {
                skillService.Changed -= OnBuildChanged;
            }

            if (rosterService != null)
            {
                rosterService.ClassChanged -= OnBuildChanged;
            }
        }

        private void ResolveRefs()
        {
            if (rewardService == null)
            {
                rewardService = FindFirstObjectByType<Game.Rewards.RewardService>();
            }

            if (equipmentService == null)
            {
                equipmentService = FindFirstObjectByType<Game.Inventory.EquipmentService>();
            }

            if (runeService == null)
            {
                runeService = FindFirstObjectByType<Game.Build.RuneService>();
            }

            if (skillService == null)
            {
                skillService = FindFirstObjectByType<Game.Build.SkillService>();
            }

            if (rosterService == null)
            {
                rosterService = FindFirstObjectByType<Game.Roster.RosterService>();
            }

            if (levelUpPanel == null)
            {
                levelUpPanel = FindFirstObjectByType<Game.UI.LevelUpPanel>(FindObjectsInactive.Include);
            }
        }

        private void TrySubscribe()
        {
            ResolveRefs();

            if (rewardService != null)
            {
                rewardService.LevelUp -= OnLevelUp;
                rewardService.LevelUp += OnLevelUp;
            }

            if (equipmentService != null)
            {
                equipmentService.EquipmentChanged -= OnBuildChanged;
                equipmentService.EquipmentChanged += OnBuildChanged;
            }

            if (runeService != null)
            {
                runeService.Changed -= OnBuildChanged;
                runeService.Changed += OnBuildChanged;
            }

            if (skillService != null)
            {
                skillService.Changed -= OnBuildChanged;
                skillService.Changed += OnBuildChanged;
            }

            if (rosterService != null)
            {
                rosterService.ClassChanged -= OnBuildChanged;
                rosterService.ClassChanged += OnBuildChanged;
            }
        }

        private void OnLevelUp(int newLevel)
        {
            ApplyLevel(newLevel, isLevelUp: true);

            if (levelUpPanel != null)
            {
                levelUpPanel.Show(newLevel, CurrentAttackDamage, CurrentMaxHealth);
            }
        }

        private void OnBuildChanged()
        {
            ApplyLevel(currentAppliedLevel, isLevelUp: false, restoreHealthOnApply: false);
        }

        public void ApplyLevel(int level, bool isLevelUp)
        {
            ApplyLevel(level, isLevelUp, restoreHealthOnApply: true);
        }

        public void ApplyLevel(int level, bool isLevelUp, bool restoreHealthOnApply)
        {
            ResolveRefs();
            level = Mathf.Max(1, level);
            currentAppliedLevel = level;

            int levelsAboveOne = level - 1;
            int equipmentAttack = equipmentService != null ? equipmentService.GetTotalAttackBonus() : 0;
            int equipmentHealth = equipmentService != null ? equipmentService.GetTotalHealthBonus() : 0;

            PartyMember partyMember = GetComponent<PartyMember>();
            string classId = partyMember != null && !string.IsNullOrEmpty(partyMember.ClassId)
                ? partyMember.ClassId
                : (rosterService != null ? rosterService.GetSelectedClassId() : null);

            int runeAttack;
            int runeHealth;
            int skillAttack;
            int skillHealth;
            if (!string.IsNullOrEmpty(classId) && runeService != null && skillService != null)
            {
                runeAttack = runeService.GetTotalAttackBonusForClass(classId);
                runeHealth = runeService.GetTotalHealthBonusForClass(classId);
                skillAttack = skillService.GetPassiveAttackBonusForClass(classId);
                skillHealth = skillService.GetPassiveHealthBonusForClass(classId);
            }
            else
            {
                runeAttack = runeService != null ? runeService.GetTotalAttackBonus() : 0;
                runeHealth = runeService != null ? runeService.GetTotalHealthBonus() : 0;
                skillAttack = skillService != null ? skillService.GetPassiveAttackBonus() : 0;
                skillHealth = skillService != null ? skillService.GetPassiveHealthBonus() : 0;
            }

            Game.Data.HeroClassData heroClass = null;
            if (!string.IsNullOrEmpty(classId) && rosterService != null)
            {
                heroClass = rosterService.GetClass(classId);
            }

            if (heroClass == null && rosterService != null)
            {
                heroClass = rosterService.GetActiveClass();
            }

            int classBaseAtk = heroClass != null ? heroClass.baseAttackDamage : (heroUnit != null ? heroUnit.BaseAttackDamage : baseAttackDamage);
            int classBaseHp = heroClass != null ? heroClass.baseMaxHealth : baseMaxHealth;

            int newAttack = classBaseAtk + (levelsAboveOne * attackPerLevel) + equipmentAttack + runeAttack + skillAttack;
            int newMaxHealth = classBaseHp + (levelsAboveOne * healthPerLevel) + equipmentHealth + runeHealth + skillHealth;

            if (heroUnit != null)
            {
                heroUnit.SetAttackDamage(newAttack);
            }

            if (health != null)
            {
                if (isLevelUp)
                {
                    if (restoreFullHealthOnLevelUp)
                    {
                        health.SetMaxHealth(newMaxHealth, restoreFull: true);
                    }
                    else
                    {
                        int previousMax = health.MaxHealth;
                        health.SetMaxHealth(newMaxHealth, restoreFull: false);

                        if (healPartialIfNotFullRestore)
                        {
                            int healAmount = Mathf.Max(1, Mathf.RoundToInt(newMaxHealth * partialHealPercent));
                            int maxIncrease = Mathf.Max(0, newMaxHealth - previousMax);
                            health.Heal(healAmount + maxIncrease);
                        }
                    }
                }
                else
                {
                    int previousMax = health.MaxHealth;
                    int previousCurrent = health.CurrentHealth;
                    health.SetMaxHealth(newMaxHealth, restoreFull: restoreHealthOnApply);

                    if (!restoreHealthOnApply)
                    {
                        int maxIncrease = Mathf.Max(0, newMaxHealth - previousMax);
                        int targetCurrent = Mathf.Min(newMaxHealth, previousCurrent + maxIncrease);
                        int healNeeded = targetCurrent - health.CurrentHealth;
                        if (healNeeded > 0)
                        {
                            health.Heal(healNeeded);
                        }
                    }
                }
            }

            Debug.Log(
                $"[HeroProgression] Level {level} applied. ClassATK={classBaseAtk} +Lv={levelsAboveOne * attackPerLevel} +EQ={equipmentAttack} +Rune={runeAttack} +Skill={skillAttack} => Attack={newAttack}, MaxHP={newMaxHealth}, levelUp={isLevelUp}");
        }

        public int GetAttackForLevel(int level)
        {
            ResolveRefs();
            level = Mathf.Max(1, level);
            int equipmentAttack = equipmentService != null ? equipmentService.GetTotalAttackBonus() : 0;
            PartyMember partyMember = GetComponent<PartyMember>();
            string classId = partyMember != null && !string.IsNullOrEmpty(partyMember.ClassId)
                ? partyMember.ClassId
                : (rosterService != null ? rosterService.GetSelectedClassId() : null);
            int runeAttack = !string.IsNullOrEmpty(classId) && runeService != null
                ? runeService.GetTotalAttackBonusForClass(classId)
                : (runeService != null ? runeService.GetTotalAttackBonus() : 0);
            int skillAttack = !string.IsNullOrEmpty(classId) && skillService != null
                ? skillService.GetPassiveAttackBonusForClass(classId)
                : (skillService != null ? skillService.GetPassiveAttackBonus() : 0);
            Game.Data.HeroClassData heroClass = !string.IsNullOrEmpty(classId) && rosterService != null
                ? rosterService.GetClass(classId)
                : (rosterService != null ? rosterService.GetActiveClass() : null);
            int classBaseAtk = heroClass != null ? heroClass.baseAttackDamage : (heroUnit != null ? heroUnit.BaseAttackDamage : baseAttackDamage);
            return classBaseAtk + ((level - 1) * attackPerLevel) + equipmentAttack + runeAttack + skillAttack;
        }

        public int GetMaxHealthForLevel(int level)
        {
            ResolveRefs();
            level = Mathf.Max(1, level);
            int equipmentHealth = equipmentService != null ? equipmentService.GetTotalHealthBonus() : 0;
            PartyMember partyMember = GetComponent<PartyMember>();
            string classId = partyMember != null && !string.IsNullOrEmpty(partyMember.ClassId)
                ? partyMember.ClassId
                : (rosterService != null ? rosterService.GetSelectedClassId() : null);
            int runeHealth = !string.IsNullOrEmpty(classId) && runeService != null
                ? runeService.GetTotalHealthBonusForClass(classId)
                : (runeService != null ? runeService.GetTotalHealthBonus() : 0);
            int skillHealth = !string.IsNullOrEmpty(classId) && skillService != null
                ? skillService.GetPassiveHealthBonusForClass(classId)
                : (skillService != null ? skillService.GetPassiveHealthBonus() : 0);
            Game.Data.HeroClassData heroClass = !string.IsNullOrEmpty(classId) && rosterService != null
                ? rosterService.GetClass(classId)
                : (rosterService != null ? rosterService.GetActiveClass() : null);
            int classBaseHp = heroClass != null ? heroClass.baseMaxHealth : baseMaxHealth;
            return classBaseHp + ((level - 1) * healthPerLevel) + equipmentHealth + runeHealth + skillHealth;
        }
    }
}
