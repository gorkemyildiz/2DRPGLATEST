using System;
using UnityEngine;

namespace Game.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        private int currentHealth;
        private bool isDead = false;
        private bool hasTriggeredDeath = false;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public event Action<int, int> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            currentHealth = maxHealth;
            isDead = false;
            hasTriggeredDeath = false;
        }

        public void TakeDamage(int damage)
        {
            if (isDead)
            {
                return;
            }

            if (damage < 0)
            {
                Debug.LogWarning($"[Health] TakeDamage called with negative damage: {damage}. Ignoring.");
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0 && !hasTriggeredDeath)
            {
                isDead = true;
                hasTriggeredDeath = true;
                Died?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (isDead)
            {
                return;
            }

            if (amount < 0)
            {
                Debug.LogWarning($"[Health] Heal called with negative amount: {amount}. Ignoring.");
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void RestoreFullHealth()
        {
            if (isDead)
            {
                return;
            }

            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            hasTriggeredDeath = false;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(int newMaxHealth, bool restoreFull = true)
        {
            if (newMaxHealth <= 0)
            {
                Debug.LogWarning($"[Health] SetMaxHealth called with invalid value: {newMaxHealth}. Ignoring.");
                return;
            }

            maxHealth = newMaxHealth;
            isDead = false;
            hasTriggeredDeath = false;

            if (restoreFull)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
