using UnityEngine;

#nullable enable

public class StunnedStatusEffect : BaseStatusEffect {
    public override string Name => StatusEffectNames.Stunned;

    public override int CurrentStacks => HasEffectFinished() ? 0 : 1;

    // Might need to make this optional cause it's not going to have an image (it'll have VFX) 
    public override string? ImageName => null;

    private readonly float duration;
    private readonly Affiliation effectApplierAffiliation;

    private readonly float timeOfStart;

    public StunnedStatusEffect(
        Affiliation effectApplierAffiliation,
        float duration = 2f
    ) {
        this.duration = duration;
        this.effectApplierAffiliation = effectApplierAffiliation;

        this.timeOfStart = Time.time;
    }

    public override bool HasEffectFinished() {
        // TODO: Double check
        return (Time.time - timeOfStart) > duration;
    }

    public override void OnFixedUpdate(Entity entity) { }
    public override void OnUpdate(Entity entity) { }
    public override void OnFinished(Entity entity) { }

    public override void StackEffect(BaseStatusEffect effect) {
        // Don't do anything - it shouldn't stack?
        // maybe just reset it? I don't think this situation will happen anytime soon though
        Debug.LogError("Stun status effect almost stacked!");
    }
}
