using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class NanoSpearProjectile : Projectile {
    // TODO: I have to make a prefab for the VFX and that's dumb - probably just make a note about it

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

            owner!.OnAttackHitEntity(hitEntity: entity);

            // Add it to the list of entities we've hit
            alreadyDamagedEntities.Add(entity);

        } else {
            // We didn't hit something, so it's probably the ground - the lifetime of the projectile is over!
            OnDeath();
        }

        PlayVFX(point: point, normal: normal);

        PlaySFX(point: point);
    }

    protected override BaseStatusEffect? GetStatusEffect() {
        return new FreezeStatusEffect();
    }
}
