using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// I'm combining this and Damageable - idk why Damageable exists
public class Health : MonoBehaviour {
    [Header("Health Data")]
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

    // Material set by the Entity which the Health belongs to
    // I don't like how we have to do this
    public Material EntityMaterial { get; set; }

    public float CurrentHealth { get; private set; }

    public UnityAction<float, Vector3> OnDamaged;
    public UnityAction OnDeath;

    // Called when a new status effect is applied to this entity
    public UnityAction<BaseStatusEffect> OnStatusEffectAdded;
    // Called when a status effect is done applying to an entity & is being removed
    public UnityAction<BaseStatusEffect> OnStatusEffectRemoved;

    private bool isDead;

    // We should only have one StatusEffect and when they stack they should modify the "main" one
    // thus stores the status effects based on their name as the key
    public readonly Dictionary<string, BaseStatusEffect> statusEffects = new();

    void Start() {
        CurrentHealth = MaxHealth;
    }

    public void Heal(float healAmount) {
        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        // Call OnHealed
    }

    // This is what damage "appliers" (e.g. Projectiles / Spells) actually call.
    public void TakeDamage(
        float damage,
        GameObject damageSource, // Currently unused
        BaseStatusEffect appliedStatusEffect, // Optional
        Vector3 damagePosition // Used to place where the damage text spawns from
    ) {
        ApplyDamage(damage, damagePosition);

        // Handle status effects
        if (appliedStatusEffect != null) {
            if (statusEffects.ContainsKey(appliedStatusEffect.Name)) {
                statusEffects[appliedStatusEffect.Name].StackEffect(appliedStatusEffect);
            } else {
                statusEffects[appliedStatusEffect.Name] = appliedStatusEffect;

                OnStatusEffectAdded?.Invoke(appliedStatusEffect);
            }
        }
    }

    // Available so we can apply damage if we don't know where exactly on the Collider it happened
    public void TakeDamage(
        float damage,
        GameObject damageSource,
        BaseStatusEffect appliedStatusEffect
    ) {
        TakeDamage(damage, damageSource, appliedStatusEffect, Vector3.negativeInfinity);
    }

    // Take flat damage
    // TODO: Do I actually want this? I suppose I might?
    public void TakeDamage(float damage) {
        TakeDamage(damage, null, null);
    }

    // Underlying function which subtracts the health from CurrentHealth, plays audio, and handles dying
    // 
    // Don't set the damagePosition (use default value) if the damage isn't applied in a specific "place"
    // e.g. for damage over time effects
    //
    // This is needed as status effects need to be able to apply damage without re-applying/stacking the status effect
    private void ApplyDamage(float damage, Vector3 damagePosition) {
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
        foreach (var statusEffectName in statusEffects.Keys) {
            BaseStatusEffect statusEffect = statusEffects[statusEffectName];

            if (!statusEffect.HasEffectFinished()) {

                // There are better ways to do this
                // ideally we just wouldn't call this if it wasn't a Tick
                statusEffect.OnUpdate(EntityMaterial);
            }
        }
    }

    void FixedUpdate() {
        // Need to see if this renegerates over time
        // though honestly that could just be in a HealthRegen component
        // Note that some status effects will disable it

        // Handle OnTick for status effects
        HashSet<string> toRemove = new();
        foreach (var statusEffectName in statusEffects.Keys) {
            BaseStatusEffect statusEffect = statusEffects[statusEffectName];

            if (!statusEffect.HasEffectFinished()) {

                // There are better ways to do this
                // ideally we just wouldn't call this if it wasn't a Tick
                float damage = statusEffect.OnFixedUpdate(EntityMaterial);
                if (damage > 0.0f) {
                    ApplyDamage(damage, Vector3.negativeInfinity);
                }

            } else {
                statusEffect.Finished(EntityMaterial);
                toRemove.Add(statusEffectName);
                OnStatusEffectRemoved?.Invoke(statusEffect);
            }
        }

        // Remove all of the keys
        foreach(var toRemoveName in toRemove) {
            statusEffects.Remove(toRemoveName);
        }
    }

    private void HandleDeath() {
        if (isDead) // idk why we have this check
            return;

        if (CurrentHealth <= 0f) {
            isDead = true;
            OnDeath?.Invoke();
        }
    }
}
