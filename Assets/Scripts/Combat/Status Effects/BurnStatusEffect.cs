using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Burn applies a percent of damage over time and disables health regeneration, and is stackable
// The duration of Burn is proportional to the strength of the inflicting hit (relative to what?)
// 
// E.g. an attack that deals 1000% base damage will inflict Ignite for 10 seconds,
//   but an attack dealing 100% base damage will inflict it for only 1 second.
//   Each tick deals a constant 10% base damage (with 5 ticks per second),
//   and Ignite's duration scales such that it always deals 50% total damage over its entire duration
//   (for instance, a stack of Ignite inflicted by the above 1000% base damage attack would deal an additional 500% base damage over its duration)
//
// To keep things simple to start I'm just going to make it last for a bit

// The duration of this is a dependent on the amount of damage we need to apply
// totalDurationInSec = totalDamageToApply / DamagePerTick / TicksPerSecond
// not that totalDamageToApply is 50% of the damage applied on the fireball, but thats applied in FireballProjectile not here
//
// Note that b/c Fireball does 280% (2.8x) of base damage and it applies ignite for 50% of that damage, 140% = 1.4 sec
// and each tick does 10% base damage (per stack), which is 12 dmg * 0.1 = 1.2 dmg per tick (which is rounded down for DamageTextSpawner - should it be rounded up? Ceil'd?)
// 
// TODO: Make it disable health regeneration?
public class BurnStatusEffect: BaseStatusEffect {
    // Bleh we could just have multiple instances of BurnStatusEffect
    // which would definitely make my life easier
    // but coordinating for VFX between them would be complicated, so I'm going to leave it as one instance per N stacks
    private class BurnStack {
        public float damageToApply;

        public BurnStack(
            float damageToApply
        ) {
            this.damageToApply = damageToApply;
        }
    }

    public override int CurrentStacks => stacks.Count;

    public override string Name => StatusEffectNames.Burn;
    public override string ImageName => "Burning Status Icon";

    private const int TicksPerSecond = 5;

    private readonly float playerBaseDamage;

    // Each tick is 10% of *player* base damage
    private float DamagePerTickPerStack => playerBaseDamage * 0.1f;

    private readonly Affiliation effectApplierAffiliation;

    // How much damage this is going to apply over the duration of the effect
    // Really only stored so we can use it to stack these (and create a BurnStack to add to the list)
    private float damageToApply;

    // Used to count what frame should be considered a tick
    private float lastTickTime = Mathf.NegativeInfinity;

    // Random seed used to change the base burn texture for the shader
    // so each burn looks slightly different
    // Generated on init
    private readonly float burnSeed;

    private List<BurnStack> stacks = new();
    
    // Multipled by (Time.time - lastTickTime) * TicksPerSecond
    // The tick flash lasts for 6 frames, and there are 2 frames before the next one starts
    // If I set below to 1 then there will be 0 frames between, if I set it to 1.5 there will be 4 frames between, so 1.25 it is
    private const float shaderTicksPerSecondModifier = 1.5f;

    private const string SHADER_IS_BURNING_PARAM = "_IsBurning";
    private const string SHADER_TIME_SINCE_LAST_TICK_PARAM = "_TimeSinceLastBurnTick";
    private const string SHADER_WAS_DAMAGED = "_WasDamaged";
    private const string SHADER_TICK_SEED_1 = "_TickSeed1";
    private const string SHADER_TICK_SEED_2 = "_TickSeed2";
    private const string SHADER_BURN_SEED = "_BurnSeed";

    public BurnStatusEffect(
        float damageToApply,
        float entityBaseDamage,
        Affiliation effectApplierAffiliation
    ) {
        this.damageToApply = damageToApply;
        this.playerBaseDamage = entityBaseDamage;
        this.effectApplierAffiliation = effectApplierAffiliation;

        this.burnSeed = Random.value * 100;

        // Add this as a stack
        // If it's not the first one it doesn't even matter - it'll be garbage collected
        stacks.Add(new BurnStack(damageToApply: damageToApply));
    }

    public override bool HasEffectFinished() {
        return stacks.Count == 0;
    }

    public override void OnUpdate(Entity entity) {
        // Pass timeSinceLastTick so we can animate the burn effect shader
        if (entity.GetMaterial() is Material material) {
            float modifiedTimeSinceLastTick = (Time.time - lastTickTime) * TicksPerSecond * shaderTicksPerSecondModifier;
            material.SetFloat(SHADER_TIME_SINCE_LAST_TICK_PARAM, modifiedTimeSinceLastTick);
        }
    }

    // This isn't a MonoBehaviour, so the Entity component will call this FixedUpdate()
    // so this can handle its own OnTick functionality (as diff status effects have diff tick rates)
    public override void OnFixedUpdate(Entity entity) {
        Material? material = entity.GetMaterial();
        if (material != null) { 
            material.SetInt(SHADER_IS_BURNING_PARAM, 1);
            material.SetFloat(SHADER_BURN_SEED, burnSeed);
		} 

        if (stacks.Count == 0) {
            // This isn't going to happen - it's handled by Entity,
            // but we still need a base case
            return;
        }

        // Check if this is a tick
        if (IsFrameATick()) {
            OnTick(entity);
        }
    }

    // Apply damage
    private void OnTick(Entity entity) { // TODO: This is weird - do we even need this? This doesn't help much
        lastTickTime = Time.time;

        // Check if this is a tick
        if (entity.GetMaterial() is Material _material) {
            _material.SetInt(SHADER_WAS_DAMAGED, 1);
            _material.SetFloat(SHADER_TICK_SEED_1, Random.value * 100);
            _material.SetFloat(SHADER_TICK_SEED_2, Random.value * 100);
            _material.SetFloat(SHADER_TIME_SINCE_LAST_TICK_PARAM, 0);
        }

        float damagePerTick = DamagePerTickPerStack * stacks.Count;
        entity.TakeDamage(damagePerTick, procCoefficient: 0f, effectApplierAffiliation, DamageType.Burn);

        // Decrease the stacks damage left to apply - then if they're at 0, then remove them
        for(int i = stacks.Count - 1; i >= 0; i--) { // Need to go in reverse b/c we might be removing
            stacks[i].damageToApply -= DamagePerTickPerStack;

            if (stacks[i].damageToApply <= 0) {// if it's at 0, remove it
                stacks.RemoveAt(i);
                Debug.Log($"Removing stack at {i}");
            }
        }
    }

    // Called in Health when HasEffectFinished() returns false
    public override void OnFinished(Entity entity) {
        // If this is an enemy, reset the shader
        // though hopefully the shader would do it on its own?

        // Remove this from the list of status effects on Health somehow
        if (entity.GetMaterial() is Material material) {
            material.SetInt(SHADER_IS_BURNING_PARAM, 0);
        }
    }

    public override void StackEffect(BaseStatusEffect effect) {
        // Ensure they're the same type - if they're not do nothing (and log an error)
        if (effect.GetType() != typeof(BurnStatusEffect)) {
            Debug.LogError($"Trying to stack an effect of type {effect} with BurnStatusEffect");
            return;
        }

        BurnStatusEffect newBurnEffect = (BurnStatusEffect) effect;

        // Create a new stack
        BurnStack stack = new(newBurnEffect.damageToApply);
        stacks.Add(stack);
    }

    // Returns true if this frame should be considered a tick
    private bool IsFrameATick() {
        float timeDiff = Time.time - lastTickTime;
        float secondsPerTick = 1f / TicksPerSecond;

        if (timeDiff > secondsPerTick) {
            return true;
        }

        return false;
    }
}
