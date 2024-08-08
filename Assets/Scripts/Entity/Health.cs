using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public enum DamageType { 
     Normal,
     Burn
}

public enum Affiliation {
    Player,
    Enemy
}

// I'm combining this and Damageable - idk why Damageable exists
public class Health : MonoBehaviour {
    [Header("Health Data")]
    [Tooltip("Maximum amount of health")]
    public float MaxHealth = 100f;

    [Tooltip("How fast this entity regenerates health per seocnd")]
    public float HealthRegenRate = 0.0f; // 1.0f for player

    [Tooltip("Health ratio at which the critical health vignette starts appearing")]
    public float CriticalHealthRatio = 0.3f;

    [Tooltip("If we should ignore damage")]
    public bool Invincible;

    [Tooltip("The affliation of this entity")]
    public Affiliation Affiliation = Affiliation.Enemy;

    // Enemies are going to have to play their own sounds, so we might as well put it in there
    // but players need to have an on hit sound too, so this might make sense?
    [Header("References")]
    [Tooltip("Sound that plays on damaged")]
    public AudioClip? OnDamageSfx;

    public float CurrentHealth { get; private set; }

    public UnityAction<float, Vector3, DamageType>? OnDamaged;
    public UnityAction? OnDeath;

    public bool IsDead { get; private set; }

    void Start() {
        CurrentHealth = MaxHealth;
    }

    private void Update() {
        // We regen health all of the time, even if we were just damaged (I think at least)
        Heal(HealthRegenRate * Time.deltaTime);
    }

    public void Heal(float healAmount) {
        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        // Call OnHealed
    }

    // Underlying function which subtracts the health from CurrentHealth, plays audio, and handles dying
    // 
    // Don't set the damagePosition (use default value) if the damage isn't applied in a specific "place"
    // e.g. for damage over time effects
    //
    // This is needed as status effects need to be able to apply damage without re-applying/stacking the status effect
    //
    // Pass Vector3.negativeInfinity if damagePosition isn't relevant
    public void TakeDamage(
        float damage,
        Vector3 damagePosition,
        Affiliation sourceAffiliation,
        DamageType damageType
    ) {
        bool sameAffiliation = this.Affiliation == sourceAffiliation;
        if (Invincible || IsDead || sameAffiliation)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        // Check if damagePosition is null maybe?

        OnDamaged?.Invoke(damage, damagePosition, damageType);

        // Play audio clip
        if (OnDamageSfx != null) {
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

    public void Kill() {
        TakeDamage(
            Mathf.Infinity,
            Vector3.negativeInfinity,
            Affiliation.Player, // Since this is only called by FreezeStatusEffect, setting as Player for now
            DamageType.Normal
        );
    }

    private void HandleDeath() {
        if (IsDead) // idk why we have this check
            return;

        if (CurrentHealth <= 0f) {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void IncreaseMaxHealth(float byAmount) {
        MaxHealth += byAmount;
        Heal(byAmount);
    }

    public void IncreaseRegenRate(float byAmount) {
        HealthRegenRate += byAmount;
    }
}
