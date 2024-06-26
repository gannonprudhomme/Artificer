using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class NanoSpearProjectile : Projectile {
    protected override BaseStatusEffect? GetStatusEffect() {
        return new FreezeStatusEffect();
    }
}
