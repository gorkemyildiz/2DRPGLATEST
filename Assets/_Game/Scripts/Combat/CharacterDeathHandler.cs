using System;
using UnityEngine;

namespace Game.Combat
{
    public class CharacterDeathHandler : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Health health;

        private bool hasHandledDeath = false;

        public event Action DeathStarted;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                Debug.LogError($"[CharacterDeathHandler] Health component not found on {gameObject.name}.", this);
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnHealthDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnHealthDied;
            }
        }

        private void OnHealthDied()
        {
            if (hasHandledDeath)
            {
                return;
            }

            hasHandledDeath = true;

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError($"[CharacterDeathHandler] Animator not found on {gameObject.name} or children.", this);
                return;
            }

            if (HasParameter(animator, "IsWalking"))
            {
                animator.SetBool("IsWalking", false);
            }
            else
            {
                Debug.LogWarning($"[CharacterDeathHandler] Animator on {gameObject.name} does not have 'IsWalking' parameter.");
            }

            if (HasParameter(animator, "Attack"))
            {
                animator.ResetTrigger("Attack");
            }
            else
            {
                Debug.LogWarning($"[CharacterDeathHandler] Animator on {gameObject.name} does not have 'Attack' parameter.");
            }

            if (HasParameter(animator, "IsDead"))
            {
                animator.SetBool("IsDead", true);
            }
            else
            {
                Debug.LogError($"[CharacterDeathHandler] Animator on {gameObject.name} does not have 'IsDead' parameter.", this);
            }

            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            DeathStarted?.Invoke();
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
