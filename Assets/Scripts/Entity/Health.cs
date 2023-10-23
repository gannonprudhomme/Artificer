using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


// I'm combining this and Damageable - idk why Damageable exists
public class Health : MonoBehaviour {
    [Tooltip("Maximum amount of health")]
    public float MaxHealth = 100f;

    [Tooltip("Health ratio at which the critical health vignette starts appearing")]
    public float CriticalHealthRatio = 0.3f;

    [Tooltip("If we should ignore damage")]
    public bool Invincible;

    public float CurrentHealth { get; private set; }

    // public UnityAction<float, GameObject> OnDamaged;
    public UnityAction OnDamaged;
    public UnityAction OnDie;

    private bool isDead;

    // Start is called before the first frame update
    void Start() {
        CurrentHealth = MaxHealth;


    }

    public void Heal(float healAmount) {
        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        // Call OnHealed
    }

    public void TakeDamage(float damage, GameObject damageSource) {
        if (Invincible)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        OnDamaged?.Invoke();

        HandleDeath();
    }

    void Update() {
        // Need to see if this renegerates over time
        // though honestly that could just be in a HealthRegen component
    }

    private void HandleDeath() {
        if (isDead) // idk why we have this check
            return;

        if (CurrentHealth <= 0f) {
            isDead = true;
            OnDie?.Invoke();
        }
    }
}
