using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// We need like a "Base Damage" which this is a subclass of
// as some damage sources will just be flat damage, like the first hit from the fireball
public abstract class BaseStatusEffect {
    // Returns how much damage it should do
    public abstract string Name { get; }

    public abstract float OnFixedUpdate(Material material);
    
    public abstract void OnUpdate(Material material);

    public abstract bool HasEffectFinished();

    public abstract void Finished(Material material);

    // Stack the effect if it's already active on the Entity / Health
    public abstract void StackEffect(BaseStatusEffect effect);
}
