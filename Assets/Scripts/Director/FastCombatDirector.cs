using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ideally we'd get the references for these from the GameManager or something
// or something spawned both of these
public sealed class FastDirector: ContinuousDirector {
    protected override float experienceMultipler => 0.2f;
    protected override (float, float) minAndMaxFailureSpawnTime => (3.0f, 4.5f);
}
