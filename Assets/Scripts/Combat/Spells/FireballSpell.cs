using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

#nullable enable

// The only reason this has to be a MonoBehavior in the first place
// is so we can assign it a projectile
// Is there really no other way? Ugh I want this to just be input/output
public class FireballSpell : Spell {
    [Header("Idk")]
    public Color SpellColorForUI;

    [Tooltip("Visual effect to spawn when this spell is fired")]
    public VisualEffect? FireVisualEffectPrefab;
    
    [Tooltip("The Fireball projectile we shoot out")]
    // This should really be FireballProjectile, but tbh to FireballSpell it doesn't matter
    public Projectile? ProjectilePrefab; // Projectile will determine it's damage? Or should this? How dumb should the Projectile be?

    [Tooltip("Particle system which plays when we shoot from the left hand")]
    public ParticleSystem? LeftShootParticleSystem;

    [Tooltip("Particle system which plays when we shoot from the right hand")]
    public ParticleSystem? RightShootParticleSystem;

    [Tooltip("Curve over the lifetime of the spell (animation) that we animate the position of the reticle with")]
    public AnimationCurve? ReticleAnimationCurve;

    /** Abstract Spell Properties **/

    // How much charge (1.0f is a charge) is restored a second
    // This should be set so it takes 1.3 sec to restore a charge (it's not rn)
    public override float ChargeRate => 1f / 1.3f; // 1 charge per 1.3 seconds (~0.77 charges per second)
    public override int MaxNumberOfCharges => 4; // Maybe rename to MaxCharge
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;
    // There's gotta be some way to configure this
    // maybe make this retrieve a value set from Unity?

    /** Local variables **/

    private readonly float DelayBetweenShots = 0.3f;

    private VisualEffect? fireVisualEffectInstance;
    private float lastTimeShot = Mathf.NegativeInfinity;

    private const float cancelSprintDuration = 0.3f;
    // how long we force the player to look forward after we shot
    // This should be similar to timeToNotFire
    private const float lookForwardDuration = 1f;
    private const float timeToNotFire = 1f;
    // Note this plays at 1.5x speed so technically this is wrong?
    // but I adjusted for it in the reticle animation curve so w/e
    private readonly float animationDuration = 0.625f; 

    private bool shouldFireWithLeftArm = false;

    void Start() {
        // Start off with max charges
        CurrentCharge = MaxNumberOfCharges;

        fireVisualEffectInstance = Instantiate(FireVisualEffectPrefab!, SpellEffectsSpawnPoint);
        fireVisualEffectInstance.Stop();
        fireVisualEffectInstance.enabled = true;
    }

    void Update() {
        // Even if we're firing, we should be recharging?
        // (this may or may not be shared between spells - which is why the Component architecture may be nice for this)
        Recharge();

        // Set animator values
        bool isFiring = Time.time - lastTimeShot < timeToNotFire;
        PlayerAnimator!.SetBool("IsFiringFireball", isFiring);
    }

    private void Recharge() {
        // We should probably check if we need to rProjectileecharge in the first place

        if (CurrentCharge < MaxNumberOfCharges) {
            CurrentCharge += ChargeRate * Time.deltaTime;

            // Don't let it get above MaxNumberOfCharges
            CurrentCharge = Mathf.Min(CurrentCharge, MaxNumberOfCharges);
        }
    }

    // If this doesn't get more complicated, this should be a property (computed var) probably?
    protected override bool CanShoot() {
        // Prevent this from being shot if a blocking spell is active
        if (IsBlockingSpellActive) {
            return false;
        }

        bool hasEnoughCharge = CurrentCharge >= CHARGE_PER_SHOT;
        bool enoughTimeSinceLastShot = (Time.time - lastTimeShot) >= DelayBetweenShots;

        return hasEnoughCharge && enoughTimeSinceLastShot && !IsBlockingSpellActive;
    }

    // We have enough charges - fire a projectile
    public override void AttackButtonHeld(
        (Vector3 leftArm, Vector3 rightArm) muzzlePositions,
        GameObject player,
        Camera spellCamera,
		float playerBaseDamage,
        LayerMask layerToIgnore
    ) {
        if (!CanShoot()) {
            return;
        }
    
        // TODO: Should I spawn this on the player
        AudioUtility.shared.CreateSFX(
            ShootSfx,
            player.transform.position,
            AudioUtility.AudioGroups.WeaponShoot,
            0f,
            10f
        );

        // reduce # of charges
        CurrentCharge -= 1;

        // Alternate between them
        Vector3 muzzlePosition;

        // Set animator values
        PlayerAnimator!.SetBool("IsFiringFireball", true);
        PlayerAnimator!.SetBool("IsFiringLeft", shouldFireWithLeftArm);

        if (shouldFireWithLeftArm) {
            LeftShootParticleSystem!.Play();
            muzzlePosition = muzzlePositions.leftArm;
        } else {
            RightShootParticleSystem!.Play();
            muzzlePosition = muzzlePositions.rightArm;
        }
        shouldFireWithLeftArm = !shouldFireWithLeftArm; // Flip it to the other spawn point

        Vector3 direction = GetProjectileDirection(spellCamera, muzzlePosition, playerLayerToIgnore: layerToIgnore);

        // Spawn a projectile
        Projectile newProjectile = Instantiate(
            ProjectilePrefab!,
            muzzlePosition,
            Quaternion.LookRotation(direction)
        );

        newProjectile.Shoot(player, Affiliation.Player, spellCamera, playerBaseDamage);

        lastTimeShot = Time.time;

        fireVisualEffectInstance!.transform.position = muzzlePosition;

        // Play VFX
        fireVisualEffectInstance!.Play();
    }
    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera SpellCamera) {
        return true;
    }

    // Returns the direction to fire the projectile in.
    // 
    // If we don't hit anything, we'll pick a "point" 100m away
    public static Vector3 GetProjectileDirection(Camera spellCamera, Vector3 muzzlePosition, LayerMask playerLayerToIgnore) {
        if (Physics.Raycast(
            origin: spellCamera.transform.position,
            direction: spellCamera.transform.forward,
            out RaycastHit hit,
            maxDistance: 1000f,
            layerMask: ~playerLayerToIgnore // We don't want to do this everytime we fire - pass it in
        )) {
            return (hit.point - muzzlePosition).normalized; // Aim towards the hit point
        } else {
            // Aim 100m away
            return ((spellCamera.transform.position + spellCamera.transform.forward * 100) - muzzlePosition).normalized;
        }
    }

    public override float? GetInnerReticleMultiplier() {
        bool isFiringAnimation = (Time.time - lastTimeShot) < animationDuration;
        if (!isFiringAnimation) {
            return null;
        }

        return ReticleAnimationCurve!.Evaluate((Time.time - lastTimeShot) / animationDuration);
    }

    public override bool ShouldForceLookForward() {
        return (Time.time - lastTimeShot) < lookForwardDuration;
    }

    public override bool ShouldCancelSprinting() {
        return (Time.time - lastTimeShot) < cancelSprintDuration;
    }

    public override bool ShouldBlockOtherSpells() {
        return false;
    }
}
