using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This isn't how this is going to work
// Everything is going to be a multiplier of the character's "base damage",
// which increases every level
// E.g. the artificer starts with 12 base damage, and gains +2.4 every level.
// The flame bolt appies 280% damage which applies Burn (Ignite), which applies 50% extra total damage
public static class DamageEconomy {
    /** Damage **/
    public const float PlayerBaseDamage = 12.0f;

    // Should this be per hit, or per damage-per-second?
    // Probably per hit, though dps might be better for per-tick stuff?
    // Idk I don't like how this works, but I do like Health
    // it'll be good to have these for a reference at least
    public const float LightDamage = 5f;

    public const float MediumDamage = 10f;

    public const float HeavyDamage = 25f;


    /** Health **/

    // Idk how to sync this to the player
    public const float PlayerHealth = 100f;

    public const float EasyEnemyHealth = 20f;

    public const float MediumEnemyHealth = 100f;

    public const float HardEnemyHealth = 100f;

    /** Regen **/

}
