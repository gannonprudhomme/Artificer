using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

// Parent of all players & enemies
// basically anything that has Health and status effects can apply to
[RequireComponent(typeof(Health))]
public abstract class Entity : MonoBehaviour {
    [Header("Entity (Inherited)")]
    // [Tooltip("Prefab for the particle system which plays when the freeze status effect ends")]
    // public ParticleSystem? OnEndFreezeParticleSystemPrefab;

    [Tooltip("AudioClip which plays when the freeze status effect ends")]
    public AudioClip? OnEndFreezeSfx;

    public Health? health { get; private set; }

    // We should only have one StatusEffect and when they stack they should modify the "main" one
    // thus stores the status effects based on their name as the key
    public readonly Dictionary<string, BaseStatusEffect> statusEffects = new();

    // Called when a new status effect is applied to this entity
    public UnityAction<BaseStatusEffect>? OnStatusEffectAdded;
    // Called when a status effect is done applying to an entity & is being removed
    public UnityAction<BaseStatusEffect>? OnStatusEffectRemoved;

    // How should we handle this? We're going to have a *lot* of these b/c of items
    // I initially called this canMove, but changed to isFrozen since EnemyHealthBar
    // needs to read this.
    protected bool isFrozen = false;

    // Later add damage increase per level and stuff. Same with health
    protected abstract float StartingBaseDamage { get; }
    public abstract float CurrentBaseDamage { get; }

    public abstract Material? GetMaterial();
    public abstract Vector3 GetMiddleOfMesh();

    public virtual void OnAttackHitEntity(Entity hitEntity) { }

    protected virtual void Awake() {
        health = GetComponent<Health>();
    }

    protected virtual void Start() {
    }

    protected virtual void Update() {
        foreach (var statusEffectName in statusEffects.Keys) {
            BaseStatusEffect statusEffect = statusEffects[statusEffectName];

            if (!statusEffect.HasEffectFinished()) {

                // TODO: There are better ways to do this
                // ideally we just wouldn't call this if it wasn't a Tick
                statusEffect.OnUpdate(this);
            }
        }
    }

    protected virtual void FixedUpdate() {
        HashSet<string> toRemove = new();
        foreach (var statusEffectName in statusEffects.Keys) {
            BaseStatusEffect statusEffect = statusEffects[statusEffectName];

            if (!statusEffect.HasEffectFinished()) {

                // There are better ways to do this
                // ideally we just wouldn't call this if it wasn't a Tick
                statusEffect.OnFixedUpdate(this);
            } else {
                statusEffect.OnFinished(this);
                toRemove.Add(statusEffectName);
                OnStatusEffectRemoved?.Invoke(statusEffect);
            }
        }

        // Remove all of the keys
        foreach(var toRemoveName in toRemove) {
            statusEffects.Remove(toRemoveName);
        }
    }

    // This is what damage "appliers" (e.g. Projectiles / Spells) actually call.
    public void TakeDamage(
        float damage,
        Affiliation damageApplierAffiliation, // Affiliation of who caused this damage
        BaseStatusEffect? appliedStatusEffect, // Optional
        Vector3 damagePosition, // Used to place where the damage text spawns from
        DamageType damageType = DamageType.Normal
    ) {
        if (health!.IsDead) return;

        health!.TakeDamage(damage, damagePosition, damageApplierAffiliation, damageType);

        // Handle status effects
        if (appliedStatusEffect != null) {
            AddOrStackStatusEffect(appliedStatusEffect);
        }
    }

    // Available so we can apply damage if we don't know where exactly on the Collider it happened
    public void TakeDamage(
        float damage,
        Affiliation damageApplierAffiliation,
        BaseStatusEffect? appliedStatusEffect,
        DamageType damageType = DamageType.Normal
    ) {
        TakeDamage(damage, damageApplierAffiliation, appliedStatusEffect, Vector3.negativeInfinity, damageType);
    }

    // TODO: I don't actually think I need this, I can just make default-values for above null
    // Take flat damage
    // TODO: Do I actually want this? I suppose I might?
    public void TakeDamage(float damage, Affiliation damageApplierAffiliation, DamageType damageType = DamageType.Normal) {
        TakeDamage(damage, damageApplierAffiliation, null, damageType);
    }

    public bool IsStunned() {
        // I hate that I can't really use an enum here
        return statusEffects.ContainsKey(StatusEffectNames.Stunned);
    }

    public virtual void SetIsFrozen(bool isFrozen) {
        this.isFrozen = isFrozen;
    }

    public bool GetIsFrozen() {
        return isFrozen;
    }

    public void AddOrStackStatusEffect(BaseStatusEffect appliedStatusEffect) {
        // If we already have a stack of this type applied, stack them
        if (statusEffects.ContainsKey(appliedStatusEffect.Name)) {
            Debug.Log($"Stacking status effect {appliedStatusEffect.Name} to {gameObject.name}");
            statusEffects[appliedStatusEffect.Name].StackEffect(appliedStatusEffect);
        } else { // If we don't, trigger it & add it to the dictionary
            Debug.Log($"Adding status effect {appliedStatusEffect.Name} to {gameObject.name}");
            appliedStatusEffect.OnStart(this);
            OnStatusEffectAdded?.Invoke(appliedStatusEffect);

            statusEffects[appliedStatusEffect.Name] = appliedStatusEffect;
        }
    }
}
