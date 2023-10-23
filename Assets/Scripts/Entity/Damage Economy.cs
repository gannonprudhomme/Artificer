using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageEconomy {
    /** Damage **/

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
