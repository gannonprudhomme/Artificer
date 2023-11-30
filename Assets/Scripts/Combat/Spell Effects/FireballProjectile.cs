using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// I'm honestly not sure this even needs to exist
// This also really shouldn't have anything to do w/ Fireballs
// it should be pretty generic, but I'll leave it as specific for now
public class FireballProjectile : Projectile {
    //void Start() {
        // add our on hit to OnHitActions
    //}

    protected override BaseStatusEffect GetStatusEffect() {
        // It does 50% of the damage the Fireball does
        return new BurnStatusEffect(DamageMultipler * DamageEconomy.PlayerBaseDamage * 0.5f);
    }

    void FireballOnHit() {
        // Apply fire or something
    }
}
