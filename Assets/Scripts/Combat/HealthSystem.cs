using System;
using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Manages fighter HP, invulnerability frames, block chip damage (85% reduction), and K.O. events.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Configuration")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField, Range(0f, 1f)] private float blockDamageReduction = 0.85f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(CurrentHealth / maxHealth) : 0f;
        public bool IsDead => CurrentHealth <= 0f;
        public bool IsInvulnerable { get; set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<float, bool> OnDamageTaken;
        public event Action OnKO;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            IsInvulnerable = false;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        /// <summary>
        /// Applies damage after considering invulnerability and frontal block (85% reduction -> 15% chip damage).
        /// Returns the final damage applied.
        /// </summary>
        public float TakeDamage(float rawDamage, bool isBlocked)
        {
            if (IsDead || IsInvulnerable || rawDamage <= 0f)
            {
                return 0f;
            }

            float finalDamage = isBlocked
                ? rawDamage * (1f - blockDamageReduction)
                : rawDamage;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);

            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnDamageTaken?.Invoke(finalDamage, isBlocked);

            if (CurrentHealth <= 0f)
            {
                OnKO?.Invoke();
            }

            return finalDamage;
        }
    }
}
