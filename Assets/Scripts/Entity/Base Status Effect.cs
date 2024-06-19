#nullable enable

public struct StatusEffectNames {
    public static string Stunned = "Stunned";
    public static string Burn = "Burn";
    public static string Freeze = "Freeze";
}

// We need like a "Base Damage" which this is a subclass of
// as some damage sources will just be flat damage, like the first hit from the fireball
public abstract class BaseStatusEffect {
    // Returns how much damage it should do
    public abstract string Name { get; }

    public abstract int CurrentStacks { get; }

    // The name of the image in the Resources/Status Effects/ folder
    // for displaying in the UI
    public abstract string? ImageName { get; }

    public virtual void OnStart(Entity entity) { } // Don't have to override it if you don't want!

    public abstract void OnFixedUpdate(Entity entity);
    
    public abstract void OnUpdate(Entity entity);

    public abstract bool HasEffectFinished();

    public abstract void OnFinished(Entity entity);

    // Stack the effect if it's already active on the Entity / Health
    public abstract void StackEffect(BaseStatusEffect effect);
}
