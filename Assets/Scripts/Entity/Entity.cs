using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Parent of all players & enemies
// basically anything that has Health and status effects can apply to
[RequireComponent(typeof(Health))]
public abstract class Entity : MonoBehaviour {
    public Health health { get; private set; }

    // We should only have one StatusEffect and when they stack they should modify the "main" one
    // thus stores the status effects based on their name as the key
    public readonly Dictionary<string, BaseStatusEffect> statusEffects = new();

    // Called when a new status effect is applied to this entity
    public UnityAction<BaseStatusEffect> OnStatusEffectAdded;
    // Called when a status effect is done applying to an entity & is being removed
    public UnityAction<BaseStatusEffect> OnStatusEffectRemoved;

    // How should we handle this? We're going to have a *lot* of these
    protected bool canMove = true;

    public abstract Material GetMaterial();

    protected virtual void Start() {
        health = GetComponent<Health>();
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

    protected void FixedUpdate() {
        HashSet<string> toRemove = new();
        foreach (var statusEffectName in statusEffects.Keys) {
            BaseStatusEffect statusEffect = statusEffects[statusEffectName];

            Material material = GetMaterial();

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

    public void SetCanMove(bool canMove) {
        this.canMove = canMove;
    }

    // This is what damage "appliers" (e.g. Projectiles / Spells) actually call.
    public void TakeDamage(
        float damage,
        GameObject damageSource, // Currently unused
        BaseStatusEffect appliedStatusEffect, // Optional
        Vector3 damagePosition // Used to place where the damage text spawns from
    ) {
        health.TakeDamage(damage, damagePosition);

        // Handle status effects
        if (appliedStatusEffect != null) {
            // If we already have a stack of this type applied, stack them
            if (statusEffects.ContainsKey(appliedStatusEffect.Name)) {
                statusEffects[appliedStatusEffect.Name].StackEffect(appliedStatusEffect);
            } else { // If we don't, trigger it & add it to the dictionary
                appliedStatusEffect.OnStart(this);
                OnStatusEffectAdded?.Invoke(appliedStatusEffect);

                statusEffects[appliedStatusEffect.Name] = appliedStatusEffect;
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
        print($"entity doing damage {damage}");
        TakeDamage(damage, null, null);
    }
}
