using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// I'm honestly not sure this even needs to exist
// This also really shouldn't have anything to do w/ Fireballs
// it should be pretty generic, but I'll leave it as specific for now
[RequireComponent(typeof(CinemachineImpulseSource))]
public class FireballProjectile : Projectile {
    private CinemachineImpulseSource? impulseSource;

    protected void Awake() {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    protected override void OnHit(Vector3 point, Vector3 normal, Collider collider) {
        base.OnHit(point, normal, collider);

        Vector3 impulseDirection = new(1f, 1f, 0f);
        impulseSource!.GenerateImpulseAtPositionWithVelocity(
            position: point,
            velocity: impulseDirection * 0.075f
        );
    }

    protected override BaseStatusEffect? GetStatusEffect() {
        return new BurnStatusEffect(
            // Deals 50% of the damage the Fireball does
            damageToApply: (entityBaseDamage * DamageMultipler) * 0.5f,
            entityBaseDamage: entityBaseDamage,
            Affiliation.Player
        );
    }
}
