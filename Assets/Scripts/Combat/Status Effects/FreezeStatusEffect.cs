using UnityEngine;

#nullable enable

// A status effect should be able to:
// - Apply damage to an entity at whatever pace they want
// - Apply particle effects to the entity, however the particle (or entity?) wants?
//   - Does this mean I'll have to have a bunch of like "OnFrozen" callbacks on entities?
//   - Some effects apply diff to enemies than they do players...I think?
//      - Was going to say freeze but I don't think that happens to players? Bad UX
//      - Also what would we do if some enemies can't get the status effect on them? Can fire enemies get burning
// - Ideally we could share the particle / shader effects with all entities

// Something
// 
// Freeze auto-kills enemy when they're under 30% health
public class FreezeStatusEffect : BaseStatusEffect {
    public override string Name => StatusEffectNames.Freeze;

    public override int CurrentStacks => 0;

    public override string? ImageName => null;

    // We'll reset this each time a stack gets applied
    private float lastTimeTriggered = Mathf.NegativeInfinity;

    // apparently it can range from 2-4 sec
    private const float effectDuration = 2f;

    public const string SHADER_IS_FROZEN = "_IsFrozen";

    public override void OnStart(Entity entity) {
        Material? entityMaterial = entity.GetMaterial();
        if (entityMaterial is Material _entityMaterial) { 
			_entityMaterial.SetInt(SHADER_IS_FROZEN, 1);
		}

        entity.SetIsFrozen(true);

        lastTimeTriggered = Time.time;
    }

    public override void OnFinished(Entity entity) {
        Material? entityMaterial = entity.GetMaterial();
        if (entityMaterial is Material _entityMaterial) {
			_entityMaterial.SetInt(SHADER_IS_FROZEN, 0);
		}

        // Trigger the explosion particles
        // When that's done, destroy this
        // Might have to do some weird stuff w/ durationRemaining to do that though
        entity.SetIsFrozen(false);

        // Add the particle effect
        // TODO:
        /*
	    if (entity.OnEndFreezeParticleSystemPrefab is ParticleSystem prefab) {
			entity.AddParticleEffect(prefab);
	    }
        */

        if (entity.OnEndFreezeSfx != null) {
            AudioUtility.shared.CreateSFX(
                entity.OnEndFreezeSfx,
                entity.transform.position,
                AudioUtility.AudioGroups.EnemyEffects,
                1f,
                1f
            );
        }
    }

    public override bool HasEffectFinished() {
        return (Time.time - lastTimeTriggered) >= effectDuration;
    }

    public override void OnUpdate(Entity entity) {
        // If at any point this entity's health is at 30%, kill it
        float healthPercentage = entity.health!.CurrentHealth / entity.health.MaxHealth;
        
        if (healthPercentage <= 0.3f) {
            // We should really only go through entity, not entity.health
            entity.health!.Kill();
		}
    }

    public override void StackEffect(BaseStatusEffect effect) {
        lastTimeTriggered = Time.time;
    }
}
