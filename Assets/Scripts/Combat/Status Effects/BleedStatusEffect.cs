using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

public class BleedStatusEffect : BaseStatusEffect {
    /** Abstract properties **/
    public override string Name => "Bleed";
    // this is a shit way of doing this wtf - should've done scriptable object
    public override string ImageName => "Bleed Status Effect"; 
    public override int CurrentStacks => currentStacks;

    /** Local variables **/
    private int currentStacks = 1;

    private const float duration = 3f;
    private float timeOfStart = -1;

    private float totalDamageToDeal;
    private readonly VisualEffect startVFXPrefab;
    private readonly VisualEffect tickVFXPrefab;
    private VisualEffect? startVFXInstance;
    private VisualEffect? tickVFXInstance;

    private readonly float ticksPerSecond = 4;
    private float lastTickTime = Mathf.NegativeInfinity;

    public BleedStatusEffect(
        float damageToDeal,
        VisualEffect startVFXPrefab,
        VisualEffect tickVFXPrefab
    ) {
        this.totalDamageToDeal = damageToDeal;
        this.startVFXPrefab = startVFXPrefab;
        this.tickVFXPrefab = tickVFXPrefab;
        timeOfStart = Time.time;
    }

    public override void OnStart(Entity entity) {
        // Start the VFX
        startVFXInstance = Object.Instantiate(startVFXPrefab, entity.BleedVFXSpawnTransform!);
        startVFXInstance.transform.position = entity.BleedVFXSpawnTransform!.position;
        // start VFX should play immediately

        // Schedule start VFX to be destroyed (it might be destroyed earlier than this if they die early?)
        Object.Destroy(startVFXInstance.gameObject, 1f);

        tickVFXInstance = Object.Instantiate(tickVFXPrefab, entity.BleedVFXSpawnTransform!);
        tickVFXInstance.transform.position = entity.BleedVFXSpawnTransform!.position;
        // tick won't play until we call .Play() (when a tick happens in FixedUpdate)
    }

    public override void OnFixedUpdate(Entity entity) {
        if (!IsFrameATick() || entity.health!.IsDead) return;

        lastTickTime = Time.time;

        tickVFXInstance!.Play(); // Emit a bleed tick vfx

        float damageThisTick = totalDamageToDeal / (duration * ticksPerSecond); // 20% per tick

        entity.TakeDamage(damageThisTick, Affiliation.Player, damageType: DamageType.Bleed);
    }

    public override bool HasEffectFinished() {
        return Time.time - timeOfStart > duration;
    }

    public override void OnFinished(Entity _) {
        if (tickVFXInstance && tickVFXInstance!.gameObject) { // If the tick VFX hasn't been destroyed yet
            Object.Destroy(tickVFXInstance.gameObject); // Destroy it
        }

        if (startVFXInstance && startVFXInstance!.gameObject) { // If the start VFX hasn't been destroyed yet
            Object.Destroy(startVFXInstance.gameObject); // Destroy it
        }
    }

    // An existing stack of Bleed will have its duration extended when a new stack with a longer duration is inflicted
    // (e.g. if a stack has 1 second remaining but a new 3-second stack is inflicted, the 1-second stack is refreshed to 3 seconds).
    // This means Bleed can be stacked infinitely as long as it is continuously inflicted.
    public override void StackEffect(BaseStatusEffect effect) {
        currentStacks += 1;

        // Reset the time remaining
        timeOfStart = Time.time;

        totalDamageToDeal += ((BleedStatusEffect) effect).totalDamageToDeal;
    }

    private bool IsFrameATick() {
        float timeDiff = Time.time - lastTickTime;
        float secondsPerTick = 1f / ticksPerSecond;

        if (timeDiff > secondsPerTick) {
            return true;
        }

        return false;
    }
}
