using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// We need like a "Base Damage" which this is a subclass of
// as some damage sources will just be flat damage, like the first hit from the fireball
public abstract class BaseStatusEffect {
    // Returns how much damage it should do
    public abstract string Name { get; }

    public abstract int CurrentStacks { get; }

    // The name of the image in the Resources/Status Effects/ folder
    // for displaying in the UI
    public abstract string ImageName { get; }

    public virtual void OnStart(Entity entity) { } // Don't have to override it if you don't want!

    /// <summary>
    /// Returns the damage afflicted on that tick
    /// </summary>
    /// <param name="material"></param>
    /// <returns></returns>
    public abstract void OnFixedUpdate(Entity entity);
    
    public abstract void OnUpdate(Entity entity);

    public abstract bool HasEffectFinished();

    public abstract void OnFinished(Entity entity);

    // Stack the effect if it's already active on the Entity / Health
    public abstract void StackEffect(BaseStatusEffect effect);
}
