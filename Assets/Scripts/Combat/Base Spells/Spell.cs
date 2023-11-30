using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Don't actually use this - just brainstorming
enum SpellTypes {
    Manual, // Idk if we need this
    // Fires while you're holding it
    // Not sure if this has to be different than Charge? We'll see
    // However HoldManual will also fire if you just tap it (for fireballs)
    HoldManual,
    // May fire when it's done charging, or 
    Charge, 
    HoldToAimReleaseToFire
}

// Really the only point of this is me thinking
// it's not actually going to be referenced anywhere
// (maybe the UI? But PlayerSpellsController needs a MonoBehavior I think)
// Seriously remove this lol
public interface ISpell {
    // What will the UI need?

    // Regardless of the spell type, everything is going to have a cooldown
    // If we don't want we can just set it to 0.0f anyways
    float ChargeRate { get; } // in seconds

    // If this is set to 1, we won't display anything
    // Needed for the UI
    int MaxNumberOfCharges { get; }

    // Should this actually be a float publicly? 
    // I figure so (so we can reflect it in the UI)
    float CurrentCharges { get; }

    // Well we won't have an icon _yet_, but we will eventually.
    // Lets just do a color for now
    // public Image Icon { get; protected set; }
    public Color SpellColor { get; }

    // What will PlayerSpellsManager need?
    void AttackButtonPressed();
    void AttackButtonHeld();
    void AttackButtonReleased();

    // Some spells will always block (most) other spells, like the ice wall
    bool DoesBlockOtherSpells { get; }
    // Some spells will ignore the firing of other spells (like the lightning launch)
    bool IsBlockedByOtherSpells { get; }

    // What will animations need?
    // Idk but it's going to need *something*
}

public interface IHoldManualSpell : ISpell {
    
}

// Spells have to be "dumb" (externally controlled), as they'd be able to fire at the same time
// if they all controlled themselves (or would need to talk to other spells)
// as such the spells manager has to manage them all
public abstract class Spell : MonoBehaviour {
    [Tooltip("Audio clip that plays when the spell is fired")]
    public AudioClip ShootSfx;

    // TODO: This is editable in the UI, and we def don't want that
    public float CurrentCharge = 0.0f;

    public const float CHARGE_PER_SHOT = 1f;

    public abstract float ChargeRate { get; }
    public abstract int MaxNumberOfCharges { get; }
    public abstract Color SpellColor { get; }
    public abstract bool DoesBlockOtherSpells { get; }
    public abstract bool IsBlockedByOtherSpells { get; }

    // This should be implemented differently by everything?
    // Though some of the things are going to be the same,
    // so maybe we want a base (abstract?) class for some of them?
    public abstract void AttackButtonPressed();
    public abstract void AttackButtonHeld();
    public abstract void AttackButtonReleased();

    public abstract bool CanShoot();

    public abstract void ShootSpell(
        Vector3 muzzlePosition,
        GameObject owner,
        Camera spellCamera
    );

    public abstract bool CanShootWhereAiming(Vector3 muzzlePosition, Camera spellCamera);
}
