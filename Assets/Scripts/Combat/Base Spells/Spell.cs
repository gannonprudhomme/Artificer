using UnityEngine;
using UnityEngine.Events;

#nullable enable

// Due to our module dependencies this has to go in here (well, BaseSpells)
// Does it make sense? No. But I also don't really want to make a Utilities file
public enum CrosshairReplacementImage {
    Aiming, // E.g. icewall
    CantFire, // For icewall
    Sprinting
}

// Spells have to be "dumb" (externally controlled), as they'd be able to fire at the same time
// if they all controlled themselves (or would need to talk to other spells)
// as such the spells manager has to manage them all
public abstract class Spell : MonoBehaviour {
    [Header("Spell (Inherited)")]
    [Tooltip("Audio clip that plays when the spell is fired")]
    public AudioClip? ShootSfx;
    public Animator? PlayerAnimator { get; set; }
    public UnityAction<Vector3>? UpdatePlayerVelocity { get; set; }

    // We don't really need this anymore
    public Transform? SpellEffectsSpawnPoint { get; set; }

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
        (Vector3 leftArm, Vector3 rightArm) muzzlePositions,
        GameObject owner,
        Camera spellCamera,
	    // Current damage of the entity that owns this (Player)
	    float currDamage,
        LayerMask layerToIgnore
    );

    public virtual CrosshairReplacementImage? GetAimTexture() {
        return null;
    }

    public virtual float? GetInnerReticleMultiplier() {
        return null;
    }

    public abstract bool CanShootWhereAiming(Vector3 muzzlePosition, Camera spellCamera);

    public virtual bool ShouldForceLookForward() { return false; }
}
