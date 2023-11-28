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

    // Enemies are going to have to play their own sounds, so we might as well put it in there
    // but players need to have an on hit sound too, so this might make sense?
    [Header("References")]
    [Tooltip("Sound that plays on damaged")]
    public AudioClip OnDamageSfx;

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

        // Check if damagePosition is null maybe?

        OnDamaged?.Invoke(damage, damagePosition);


        // Play audio clip
        if (OnDamageSfx) {
            AudioUtility.shared.CreateSFX(
                OnDamageSfx,
                transform.position,
                AudioUtility.AudioGroups.DamageTick,
                // Still don't know what to put for these
                1f,
                1f
            );
        }


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
