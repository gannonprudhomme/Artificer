using System.Collections;
using System.Collections.Generic;
using Codice.CM.Client.Differences.Merge;
using UnityEngine;

// This should:
// - Handle the timing of the shader effects?
// - Have an OnTick function which applies the damage?
//   - Which also needs to affect the timing of the shader effect
// - Apply the same to player's and enemies
//   - Except for the shaders, which won't apply
//
// - Status Effects have different OnTicks apparently
// - Do most of the work for the shaders, even though it won't apply to Players

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
public class BurnStatusEffect: BaseStatusEffect {
    public override int CurrentStacks {
        get {
            float numStacks = damageLeftToApply / damagePerStack;
            numStacks = Mathf.Ceil(numStacks);
            return (int)numStacks;
        }
    }

    public override string ImageName => "Burning Status Icon";

    // TODO: This is going to change as the player levels up
    // We also need to get this from somewhere dynamically
    private const float PlayerBaseDamage = 12.0f;
    private const int TicksPerSecond = 5;

    // Each tick is 10% of *player* base damage - or is it the base damage of the fireball / attack?
    // This doesn't seem right
    private const float DamagePerTick = PlayerBaseDamage * 0.1f; 
    
    // We're going to consider damagePerStack to be the base damage that is initially applied when this is created
    // This will make it so if we have different things which apply Burn the meaning of stack will change, but it will work for now since we only have Fireball.
    private float damagePerStack;

    // How much damage this is going to apply over the duration of the effect
    private float damageLeftToApply;

    // Used to count what frame should be considered a tick
    private float lastTickTime = Mathf.NegativeInfinity;

    private const string SHADER_IS_BURNING_PARAM = "_IsBurning";
    private const string SHADER_TIME_SINCE_LAST_TICK_PARAM = "_TimeSinceLastBurnTick";
    private const string SHADER_FLASH_TEXTURE_INDEX_1 = "_BurnFlashTextureIndex1";
    private const string SHADER_FLASH_TEXTURE_INDEX_2 = "_BurnFlashTextureIndex2";
    private const string SHADER_WAS_DAMAGED = "_WasDamaged";

    // Used so we can iterate through the available textures for the burn tick-flash shader effect
    private int tickNumber = 0;
    private const int numberOfBurnFlashTextures = 2;

    // TODO: I need to do this
    // We need to wait ~8 frames (~0.133... sec) to apply the burn effect
    private bool hasWaitedToApply = false;

    public BurnStatusEffect(
        // For now it's going to be 12.0f * 2.8
        float damage
    ) {
        this.damageLeftToApply = damage;
        this.damagePerStack = damage;
    }

    public override string Name {
        // Doesn't matter what this is, as long it's unique
        get {
            return "Burn";
        }
    }

    public override bool HasEffectFinished() {
        return damageLeftToApply <= 0f;
    }

    // Multipled by (Time.time - lastTickTime) * TicksPerSecond
    // The tick flash lasts for 6 frames, and there are 2 frames before the next one starts
    // If I set below to 1 then there will be 0 frames between, if I set it to 1.5 there will be 4 frames between, so 1.25 it is
    private const float ticksPerSecondModifier = 1.5f;

    public override void OnUpdate(Entity entity) {
        // We could just pass lastTickTime and have the shader do this
        // that way it being smooth is guaranteed
        // No variable names in a shader makes it a tougher sell tho
        float modifiedTimeSinceLastTick = (Time.time - lastTickTime) * TicksPerSecond * ticksPerSecondModifier;
        entity.GetMaterial().SetFloat(SHADER_TIME_SINCE_LAST_TICK_PARAM, modifiedTimeSinceLastTick);
    }

    // This isn't a MonoBehaviour, so the Entity component will call this FixedUpdate()
    // so this can handle its own OnTick functionality (as diff status effects have diff tick rates)
    public override void OnFixedUpdate(Entity entity) {
        Material material = entity.GetMaterial();

        HandleBurnTickAnimation(material);

        if (damageLeftToApply == 0) {
            // This isn't going to happen - it's handled by Entity,
            // but we still need a base case
            return;
        }

        // See if this is a tick
        // TODO: I wonder if I should use Coroutines for this?
        if (IsFrameATick()) {
            material.SetFloat(SHADER_TIME_SINCE_LAST_TICK_PARAM, 0);
            float damage = OnTick(material);

            entity.TakeDamage(damage);
        }
    }

    // Apply damage (or heal)
    private float OnTick(Material material) {
        damageLeftToApply -= DamagePerTick;
        // TODO: Check this depending on what we want the "start" of the animation to be
        tickNumber += 1;

        material.SetInt(SHADER_WAS_DAMAGED, 1);

        return DamagePerTick;
    }

    // Called in Health when HasEffectFinished() returns false
    public override void Finished(Entity entity) {
        // If this is an enemy, reset the shader
        // though hopefully the shader would do it on its own?

        // Remove this from the list of status effects on Health somehow
        entity.GetMaterial().SetInt(SHADER_IS_BURNING_PARAM, 0);
    }

    public override void StackEffect(BaseStatusEffect effect) {
        // Ensure they're the same type - if they're not do nothing (and log an error)
        if (effect.GetType() != typeof(BurnStatusEffect)) {
            Debug.LogError($"Trying to stack an effect of type {effect} with BurnStatusEffect");
            return;
        }

        BurnStatusEffect newBurnEffect = (BurnStatusEffect) effect;

        this.damageLeftToApply += newBurnEffect.damageLeftToApply;
    }

    // Returns true if this frame should be considered a tick
    private bool IsFrameATick() {
        float timeDiff = Time.time - lastTickTime;

        if (timeDiff > (1f / TicksPerSecond)) {
            lastTickTime = Time.time;
            return true;
        }

        return false;
    }

    private void HandleBurnTickAnimation(Material material) {
        material.SetInt(SHADER_IS_BURNING_PARAM, 1);

        // I need to get this in the range of [0,1] (really [1 -> 0]) so it blends correctly between ticks
        float modifiedTimeSinceLastTick = (Time.time - lastTickTime);
        modifiedTimeSinceLastTick *= TicksPerSecond; // Why do we do this?
        modifiedTimeSinceLastTick *= ticksPerSecondModifier; // See doc for ticksPerSecondModifier above for details

        // The first 4 frames it should go through this animation
        // next 2 it'll be fading out
        // last 2 (before next tick starts) it won't do anything.
        // This should be a function of TicksPerSecond
        // modifiedTimeSinceLastTick is in 1 -> 0, I need to make it [0, 1]
        // (it doesn't do this rn, remove this comment once I fix it)
        float frameStart = Mathf.Min(modifiedTimeSinceLastTick / ticksPerSecondModifier, 1.0f); // Ensure it doesn't get above 1. This also helps us so the last 4? frames are the last one
        frameStart *= 4; // multiply by # of frames
        frameStart = (int) frameStart; // [0, 1] range

        frameStart = Mathf.Min(frameStart, 3);
        float frameEnd = Mathf.Min(frameStart + 1, 3);

        // Debug.Log($"{(int) frameStart} {frameEnd} {modifiedTimeSinceLastTick / ticksPerSecondModifier}");

        material.SetFloat(SHADER_FLASH_TEXTURE_INDEX_1, frameStart);
        material.SetFloat(SHADER_FLASH_TEXTURE_INDEX_2, frameEnd);

    }
}
