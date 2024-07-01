using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class NanoSpearProjectile : Projectile {
    // AnimationController?

    // Entities we've already hit that we're going to now
    private List<Entity> alreadyDamagedEntities = new();

    // We implement our own OnHit so we can do the "piercing" effect (ignore already damaged entities)
    protected override void OnHit(Vector3 point, Vector3 normal, Collider collider) {
        // Try to get the entity
        if (collider.TryGetEntityFromCollider(out Entity entity)) {
            // If we've already hit this entity, don't hit it again
            if (alreadyDamagedEntities.Contains(entity)) {
                // Debug.Log($"Already damaged {entity.gameObject.name}!");
                return;
            }

            // Otherwise, hit it
            entity.TakeDamage(entityBaseDamage * DamageMultipler, ownerAffiliation, GetStatusEffect(), point);

            // Add it to the list of entities we've hit
            alreadyDamagedEntities.Add(entity);

        } else {
            // We didn't hit something, so it's probably the ground - the lifetime of the projectile is over!
            Debug.Log($"Hit {collider.gameObject.name} - dying");
            OnDeath();
        }

        // Figure we'll want to play VFX on whatever we hit
        PlayVFX(point: point, normal: normal);
    }

    protected override BaseStatusEffect? GetStatusEffect() {
        return new FreezeStatusEffect();
    }
}
