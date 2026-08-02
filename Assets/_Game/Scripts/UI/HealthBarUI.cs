using UnityEngine;

namespace Game.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Game.Combat.Health health;
        [SerializeField] private RectTransform fillTransform;
        [SerializeField] private bool hideOnDeath = true;

        private Vector3 initialFillScale = Vector3.one;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Game.Combat.Health>();
            }

            if (health == null)
            {
                Debug.LogError($"[HealthBarUI] Health component not found on {gameObject.name} or parent.", this);
            }

            if (fillTransform == null)
            {
                Debug.LogError($"[HealthBarUI] Fill RectTransform not assigned on {gameObject.name}.", this);
            }

            if (fillTransform != null)
            {
                initialFillScale = fillTransform.localScale;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
                health.Died += OnDied;
                UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
                health.Died -= OnDied;
            }
        }

        private void Start()
        {
            if (health != null)
            {
                UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
            }
        }

        private void OnHealthChanged(int currentHealth, int maxHealth)
        {
            UpdateHealthBar(currentHealth, maxHealth);
        }

        private void UpdateHealthBar(int currentHealth, int maxHealth)
        {
            if (fillTransform == null)
            {
                return;
            }

            if (maxHealth <= 0)
            {
                Debug.LogWarning($"[HealthBarUI] MaxHealth is zero or negative on {gameObject.name}.");
                fillTransform.localScale = Vector3.zero;
                return;
            }

            float healthPercent = Mathf.Clamp01((float)currentHealth / maxHealth);

            Vector3 newScale = initialFillScale;
            newScale.x *= healthPercent;
            fillTransform.localScale = newScale;
        }

        private void OnDied()
        {
            if (hideOnDeath)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
