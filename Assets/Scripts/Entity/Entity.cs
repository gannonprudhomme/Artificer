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
    [Tooltip("AudioClip which plays when the freeze status effect ends")]
    public AudioClip? OnEndFreezeSfx;

    [Tooltip("Transform where we spawn the bleed VFX")]
    public Transform? BleedVFXSpawnTransform;

    [Tooltip("ScriptableObject which we use to publish OnEntityHit events for items (e.g. Ukulele)")]
    public OnEntityHitEvent? OnEntityHitEvent;

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

    protected virtual void Awake() {
        health = GetComponent<Health>();
    }

    protected virtual void Start() { }

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
        float procCoefficient,
        Affiliation damageApplierAffiliation, // Affiliation of who caused this damage
        BaseStatusEffect? appliedStatusEffect, // Optional
        Vector3? damagePosition, // Used to place where the damage text spawns from
        DamageType damageType = DamageType.Normal
    ) {
        if (health!.IsDead) return;

        health!.TakeDamage(damage, damagePosition, damageApplierAffiliation, damageType);

        if (damageApplierAffiliation == Affiliation.Player) {
            // yes, this is more accurately described as on OnEnemyHit
            OnEntityHitEvent!.Event!.Invoke(new OnEntityHitData(
                playerBaseDamage: CurrentBaseDamage,
                attackTotalDamage: damage,
                entityHit: this,
                procCoefficient: procCoefficient,
                inflicterAffiliation: damageApplierAffiliation
            ));
        }

        // Handle status effects
        if (appliedStatusEffect != null) {
            AddOrStackStatusEffect(appliedStatusEffect, damageApplierAffiliation);
        }
    }

    // Available so we can apply damage if we don't know where exactly on the Collider it happened
    public void TakeDamage(
        float damage,
        float procCoefficient,
        Affiliation damageApplierAffiliation,
        BaseStatusEffect? appliedStatusEffect,
        DamageType damageType = DamageType.Normal
    ) {
        TakeDamage(damage, procCoefficient, damageApplierAffiliation, appliedStatusEffect, damagePosition: null, damageType);
    }

    // TODO: I don't actually think I need this, I can just make default-values for above null
    // Take flat damage
    // TODO: Do I actually want this? I suppose I might?
    public void TakeDamage(
        float damage,
        float procCoefficient,
        Affiliation damageApplierAffiliation,
        DamageType damageType = DamageType.Normal
    ) {
        TakeDamage(damage, procCoefficient, damageApplierAffiliation, null, damageType);
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

    public void AddOrStackStatusEffect(
        BaseStatusEffect appliedStatusEffect,
        Affiliation damageApplierAffiliation
    ) {
        // If the same affiliation, don't do anything
        if (health!.Affiliation == damageApplierAffiliation) {
            return;
        }

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
