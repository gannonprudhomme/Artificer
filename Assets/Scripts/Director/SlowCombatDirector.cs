using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowDirector: ContinuousDirector {
    protected override float experienceMultipler => 0.2f;
    protected override (float, float) minAndMaxFailureSpawnTime => (22.5f, 30.0f);
}
