using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// I'm honestly not sure this even needs to exist
// This also really shouldn't have anything to do w/ Fireballs
// it should be pretty generic, but I'll leave it as specific for now
public class FireballProjectile : Projectile {
    protected override BaseStatusEffect? GetStatusEffect() {
        return new BurnStatusEffect(
            // Deals 50% of the damage the Fireball does
            damageToApply: (entityBaseDamage * DamageMultipler) * 0.5f,
            entityBaseDamage: entityBaseDamage,
            Affiliation.Player
        );
    }
}
