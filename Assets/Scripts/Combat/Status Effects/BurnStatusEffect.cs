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
public class BurnStatusEffect: BaseStatusEffect {
    // The entity which caused this damage (the player or an enemy)
    // I need to know if this is being applied to an enemy or a player

    // How much damage this is going to apply over the duration of the effect
    private float totalDamage;

    // How long the effect is going to last for
    private float effectDuration;

    // When the effect started.
    //
    // Used so we know when we should end the effect
    private float effectStart;

    private float timeSinceLastTick = Mathf.NegativeInfinity;

    // private float intensity;

    private const float TicksPerSecond = 5;
    private const string SHADER_IS_ON_FIRE_PARAM = "_IsOnFire";

    public BurnStatusEffect(
        float totalDamage
        // float intensity = 1 // base intensity (100%)
    ) {
        // this.owner = owner;
        this.totalDamage = totalDamage;

        // Store the current time
        this.effectStart = Time.time;
        this.effectDuration = 2.0f; // totalDamage / 10;
        // this.intensity = intensity;
    }

    public override bool HasEffectFinished() {
        float currentDuration = Time.time - effectStart;

        return currentDuration >= effectDuration;
    }

    // This isn't a MonoBehaviour, so the Health component will call this OnUpdate
    // so this can handle its own OnTick functionality (as diff status effects have diff tick rates)
    public override float OnFixedUpdate(Material material) {
        // Check if we should be done
        float currentDuration = Time.time - effectStart;

        material.SetInt(SHADER_IS_ON_FIRE_PARAM, 1);

        if (currentDuration > effectDuration) {
            // This isn't going to happen - it's handled by Health
            // we're done
            // Remove this status effect from Health
            return 0f;
        }

        // See if this is a tick
        // TODO: I wonder if I should use Coroutines for this?
        if (IsFrameATick()) {
            return OnTick(material);
        }

        return 0f;
    }

    // Apply damage (or heal)
    private float OnTick(Material material) {
        float damagePerTick = totalDamage / effectDuration / TicksPerSecond;

        // Set some tick property on the shader

        // health.TakeDamage(damagePerTick, owner);
        return damagePerTick;
    }

    // Called in Health when HasEffectFinished() returns false
    public override void Finished(Material material) {
        // If this is an enemy, reset the shader
        // though hopefully the shader would do it on its own?

        // Remove this from the list of status effects on Health somehow
        material.SetInt(SHADER_IS_ON_FIRE_PARAM, 0);
    }

    public override void StackEffect(BaseStatusEffect effect) {
        // Ensure they're the same type - if they're not do nothing (and log an error)
    }

    // Returns true if this frame should be considered a tick
    private bool IsFrameATick() {
        float timeDiff = Time.time - timeSinceLastTick;

        if (timeDiff > (1f / TicksPerSecond)) {
            timeSinceLastTick = Time.time;
            return true;
        }

        return false;
    }
}
