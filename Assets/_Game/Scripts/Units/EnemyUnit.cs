using System.Collections;
using UnityEngine;

namespace Game.Units
{
    public class EnemyUnit : MonoBehaviour
    {
        [Header("Combat (runtime)")]
        [Tooltip("Set by EnemyData on spawn. Edit Assets/_Game/ScriptableObjects/Enemies — NOT this prefab field (Dog + DogBoss share this prefab).")]
        [SerializeField] private float moveSpeed = 1.5f;
        [Tooltip("Set by EnemyData on spawn. Edit the EnemyData asset on the wave.")]
        [SerializeField] private float attackRange = 1.5f;
        [Tooltip("Set by EnemyData on spawn. Edit the EnemyData asset (e.g. Enemy_Dog.asset), not this prefab.")]
        [SerializeField] private int attackDamage = 8;
        [Tooltip("Set by EnemyData on spawn.")]
        [SerializeField] private float attackCooldown = 1.4f;
        [Tooltip("Set by EnemyData on spawn.")]
        [SerializeField] private float attackHitDelay = 0.35f;
        [Tooltip("Set by EnemyData on spawn.")]
        [SerializeField] private float deathAnimationDuration = 1f;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Game.Combat.Health health;

        private HeroUnit currentTarget;
        private float lastAttackTime = -999f;
        private bool isBusy = false;
        private Coroutine attackCoroutine;
        private Game.Data.EnemyData enemyData;

        public Game.Combat.Health Health => health;
        public float DeathAnimationDuration => deathAnimationDuration;
        public Game.Data.EnemyData EnemyData => enemyData;
        public int AttackDamage => attackDamage;
        public float MoveSpeed => moveSpeed;
        public float AttackRange => attackRange;

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
                Debug.LogError($"[EnemyUnit] Health component not found on {gameObject.name}.", this);
            }
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

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            if (currentTarget == null || currentTarget.Health == null || currentTarget.Health.IsDead)
            {
                SetWalking(false);
                return;
            }

            float distance = Mathf.Abs(transform.position.x - currentTarget.transform.position.x);

            if (distance > attackRange)
            {
                MoveTowardsTarget();
            }
            else
            {
                SetWalking(false);

                if (Time.time >= lastAttackTime + attackCooldown && !isBusy)
                {
                    TryAttack();
                }
            }
        }

        /// <summary>
        /// Applies combat stats from the wave's EnemyData asset.
        /// Prefab Inspector combat fields are ignored after this (same prefab can be Dog ATK 4 and DogBoss ATK 18).
        /// </summary>
        public void Configure(Game.Data.EnemyData data)
        {
            if (data == null)
            {
                Debug.LogError($"[EnemyUnit] Configure called with null EnemyData on {gameObject.name}.", this);
                return;
            }

            int prefabAtkBefore = attackDamage;
            enemyData = data;
            moveSpeed = Mathf.Max(0f, data.moveSpeed);
            attackRange = Mathf.Max(0f, data.attackRange);
            attackDamage = Mathf.Max(0, data.attackDamage);
            attackCooldown = Mathf.Max(0.01f, data.attackCooldown);
            attackHitDelay = Mathf.Max(0f, data.attackHitDelay);
            deathAnimationDuration = Mathf.Max(0f, data.deathAnimationDuration);

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Boss art faces right by default; flip so they look toward the hero (left).
                spriteRenderer.flipX = data.isBoss;
                spriteRenderer.color = Color.white;
            }

            if (health != null)
            {
                health.SetMaxHealth(Mathf.Max(1, data.maxHealth), true);
            }

            Debug.Log(
                $"[EnemyUnit] '{data.displayName}' ({data.enemyId}) stats from EnemyData: " +
                $"ATK={attackDamage}, HP={data.maxHealth}, SPD={moveSpeed}, Range={attackRange}. " +
                $"(Prefab Inspector ATK was {prefabAtkBefore} — edit the .asset, not the prefab.)",
                this);
        }

        public void SetTarget(HeroUnit hero)
        {
            currentTarget = hero;
        }

        public void ClearTarget()
        {
            currentTarget = null;
            SetWalking(false);
        }

        private void SetWalking(bool isWalking)
        {
            if (animator != null && HasParameter(animator, "IsWalking"))
            {
                animator.SetBool("IsWalking", isWalking);
            }
        }

        private void MoveTowardsTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            SetWalking(true);
            // Side-scroller: only chase on X so enemies don't drag the fight onto a different ground line.
            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, currentTarget.transform.position.x, moveSpeed * Time.deltaTime);
            transform.position = p;
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

            yield return new WaitForSeconds(attackHitDelay);

            if (currentTarget != null && currentTarget.Health != null && !currentTarget.Health.IsDead)
            {
                currentTarget.Health.TakeDamage(attackDamage);
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
        }

        private void StopAllAttacks()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
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
