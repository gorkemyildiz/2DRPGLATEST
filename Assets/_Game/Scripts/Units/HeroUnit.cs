using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Units
{
    public class HeroUnit : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float attackRange = 1.5f;
        [Tooltip("Base ATK authored in Inspector. Runtime value is set by HeroProgression (level + gear + build).")]
        [SerializeField] private int attackDamage = 28;
        [SerializeField] private float attackCooldown = 1.1f;
        [SerializeField] private float attackHitDelay = 0.3f;
        [SerializeField] private float skillHitDelay = 0.25f;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Game.Combat.Health health;
        [SerializeField] private Game.Build.SkillService skillService;
        [SerializeField] private Game.Build.RuneService runeService;

        private EnemyUnit currentTarget;
        private bool isMovementEnabled = true;
        private float lastAttackTime = -999f;
        private bool isBusy = false;
        private Coroutine attackCoroutine;
        private Coroutine skillCoroutine;
        private float lockedY;
        private bool hasLockedY;
        private int runtimeAttackDamage;
        private readonly Dictionary<string, float> skillReadyAt = new Dictionary<string, float>();

        public Game.Combat.Health Health => health;
        public bool IsBusy => isBusy;
        /// <summary>Inspector base ATK (before level/gear/build). Edit this — HeroProgression reads it.</summary>
        public int BaseAttackDamage => attackDamage;
        /// <summary>Live ATK after HeroProgression applies level + equipment + build.</summary>
        public int AttackDamage => runtimeAttackDamage;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (health == null)
            {
                health = GetComponent<Game.Combat.Health>();
            }

            if (health == null)
            {
                Debug.LogError($"[HeroUnit] Health component not found on {gameObject.name}.", this);
            }

            runtimeAttackDamage = Mathf.Max(0, attackDamage);
            LockGroundY(transform.position.y);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }

            StopAllAttacks();
        }

        /// <summary>
        /// Side-scroller: keep the authored ground line. Call after placing the hero.
        /// </summary>
        public void LockGroundY(float y)
        {
            lockedY = y;
            hasLockedY = true;
            Vector3 p = transform.position;
            p.y = lockedY;
            transform.position = p;
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            EnforceGroundY();

            if (!isMovementEnabled)
            {
                return;
            }

            if (currentTarget == null || currentTarget.Health == null || currentTarget.Health.IsDead)
            {
                if (!isBusy)
                {
                    SetWalking(false);
                }

                return;
            }

            if (isBusy)
            {
                return;
            }

            float distanceX = Mathf.Abs(transform.position.x - currentTarget.transform.position.x);

            if (distanceX > attackRange)
            {
                MoveTowardsTargetX();
            }
            else
            {
                if (TryAutoCastSkill())
                {
                    return;
                }

                TryAttack();
            }
        }

        public void SetTarget(EnemyUnit enemy)
        {
            currentTarget = enemy;
        }

        public void ClearTarget()
        {
            currentTarget = null;
            if (!isBusy)
            {
                SetWalking(false);
            }
        }

        public void SetAttackDamage(int damage)
        {
            runtimeAttackDamage = Mathf.Max(0, damage);
        }

        public void SetWalking(bool isWalking)
        {
            if (animator != null && HasParameter(animator, "IsWalking"))
            {
                animator.SetBool("IsWalking", isWalking);
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            isMovementEnabled = enabled;
        }

        public void MoveToPosition(Vector3 targetPosition, float speed)
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, targetPosition.x, speed * Time.deltaTime);
            if (hasLockedY)
            {
                p.y = lockedY;
            }

            transform.position = p;
            SetWalking(true);
        }

        private void MoveTowardsTargetX()
        {
            if (currentTarget == null)
            {
                return;
            }

            SetWalking(true);
            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, currentTarget.transform.position.x, moveSpeed * Time.deltaTime);
            if (hasLockedY)
            {
                p.y = lockedY;
            }

            transform.position = p;
        }

        private void EnforceGroundY()
        {
            if (!hasLockedY)
            {
                return;
            }

            Vector3 p = transform.position;
            if (!Mathf.Approximately(p.y, lockedY))
            {
                p.y = lockedY;
                transform.position = p;
            }
        }

        private void ResolveBuildServices()
        {
            if (skillService == null)
            {
                skillService = FindFirstObjectByType<Game.Build.SkillService>();
            }

            if (runeService == null)
            {
                runeService = FindFirstObjectByType<Game.Build.RuneService>();
            }
        }

        private bool TryAutoCastSkill()
        {
            ResolveBuildServices();
            if (skillService == null || isBusy)
            {
                return false;
            }

            PartyMember partyMember = GetComponent<PartyMember>();
            List<Game.Data.SkillData> actives = partyMember != null && !string.IsNullOrEmpty(partyMember.ClassId)
                ? skillService.GetEquippedActivesForClass(partyMember.ClassId)
                : skillService.GetEquippedActives();

            if (actives == null || actives.Count == 0)
            {
                return false;
            }

            float cdr = runeService != null ? runeService.GetCooldownReducePercent() : 0f;
            float now = Time.time;

            for (int i = 0; i < actives.Count; i++)
            {
                Game.Data.SkillData skill = actives[i];
                if (skill == null || string.IsNullOrEmpty(skill.skillId))
                {
                    continue;
                }

                if (!skillReadyAt.TryGetValue(skill.skillId, out float readyAt))
                {
                    readyAt = 0f;
                }

                if (now < readyAt)
                {
                    continue;
                }

                float cd = Mathf.Max(0.5f, skill.cooldown) * (1f - Mathf.Clamp01(cdr));
                skillReadyAt[skill.skillId] = now + cd;

                if (skillCoroutine != null)
                {
                    StopCoroutine(skillCoroutine);
                }

                skillCoroutine = StartCoroutine(SkillCoroutine(skill));
                return true;
            }

            return false;
        }

        private IEnumerator SkillCoroutine(Game.Data.SkillData skill)
        {
            isBusy = true;
            SetWalking(false);

            if (animator != null && HasParameter(animator, "Attack"))
            {
                animator.SetTrigger("Attack");
            }

            yield return new WaitForSeconds(skillHitDelay);

            if (currentTarget != null && currentTarget.Health != null && !currentTarget.Health.IsDead)
            {
                int damage = Mathf.Max(1, Mathf.RoundToInt(runtimeAttackDamage * Mathf.Max(0.1f, skill.damageMultiplier)));
                currentTarget.Health.TakeDamage(damage);
                Debug.Log($"[HeroUnit] Skill '{skill.displayName}' hit for {damage}.");
            }

            isBusy = false;
            skillCoroutine = null;
        }

        private void TryAttack()
        {
            if (currentTarget == null || currentTarget.Health == null || currentTarget.Health.IsDead)
            {
                return;
            }

            if (health != null && health.IsDead)
            {
                return;
            }

            if (Time.time < lastAttackTime + attackCooldown || isBusy)
            {
                SetWalking(false);
                return;
            }

            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
            }

            attackCoroutine = StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            isBusy = true;
            lastAttackTime = Time.time;

            if (animator != null && HasParameter(animator, "Attack"))
            {
                animator.SetTrigger("Attack");
            }

            SetWalking(false);

            yield return new WaitForSeconds(attackHitDelay);

            if (currentTarget != null && currentTarget.Health != null && !currentTarget.Health.IsDead)
            {
                currentTarget.Health.TakeDamage(runtimeAttackDamage);
            }
            else
            {
                isBusy = false;
                attackCoroutine = null;
                yield break;
            }

            float remainingCooldown = attackCooldown - attackHitDelay;
            if (remainingCooldown > 0)
            {
                yield return new WaitForSeconds(remainingCooldown);
            }

            isBusy = false;
            attackCoroutine = null;
        }

        private void OnDied()
        {
            StopAllAttacks();
            SetWalking(false);
            SetMovementEnabled(false);
        }

        private void StopAllAttacks()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            if (skillCoroutine != null)
            {
                StopCoroutine(skillCoroutine);
                skillCoroutine = null;
            }

            isBusy = false;
        }

        private bool HasParameter(Animator anim, string paramName)
        {
            if (anim == null) return false;

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
