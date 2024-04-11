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

    [Tooltip("How long in seconds between shots")]
    public float DelayBetweenShots = 0.5f;

    /** Abstract Spell Properties **/

    // How much charge (1.0f is a charge) is restored a second
    // This should be set so it takes 1.3 sec to restore a charge (it's not rn)
    public override float ChargeRate => 1f / 1.3f; // 1 charge per 1.3 seconds (~0.77 charges per second)
    public override int MaxNumberOfCharges => 5; // Maybe rename to MaxCharge
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;
    // There's gotta be some way to configure this
    // maybe make this retrieve a value set from Unity?
    public override Color SpellColor => SpellColorForUI;

    /** Local variables **/

    private VisualEffect? fireVisualEffectInstance;
    private float lastTimeShot = Mathf.NegativeInfinity;

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
    }

    public override void AttackButtonHeld() { }
    public override void AttackButtonReleased() { }
    public override void AttackButtonPressed() { }

    private void Recharge() {
        // We should probably check if we need to rProjectileecharge in the first place

        if (CurrentCharge < MaxNumberOfCharges) {
            CurrentCharge += ChargeRate * Time.deltaTime;

            // Don't let it get above MaxNumberOfCharges
            CurrentCharge = Mathf.Min(CurrentCharge, MaxNumberOfCharges);
        }
    }

    // If this doesn't get more complicated, this should be a property (computed var) probably?
    public override bool CanShoot() {
        // Should we check "Blocked by other spells" here?
        // Or in PlayerSpellsController

        bool hasEnoughCharge = CurrentCharge >= CHARGE_PER_SHOT;
        bool enoughTimeSinceLastShot = (Time.time - lastTimeShot) >= DelayBetweenShots;

        return hasEnoughCharge && enoughTimeSinceLastShot;
    }

    // We have enough charges - fire a projectile
    public override void ShootSpell(
        Vector3 muzzlePosition,
        GameObject owner,
        Camera spellCamera,
		float playerBaseDamage,
        LayerMask layerToIgnore
    ) {
        AudioUtility.shared.CreateSFX(
            ShootSfx,
            spellCamera.transform.position,
            AudioUtility.AudioGroups.WeaponShoot,
            0f,
            10f
        );

        // reduce # of charges
        CurrentCharge -= 1;

        Vector3 direction = GetProjectileDirection(spellCamera, muzzlePosition, playerLayerToIgnore: layerToIgnore);

        // Spawn a projectile
        Projectile newProjectile = Instantiate(
            ProjectilePrefab!,
            muzzlePosition,
            Quaternion.LookRotation(direction)
        );

        newProjectile.Shoot(owner, Affiliation.Player, spellCamera, playerBaseDamage);

        lastTimeShot = Time.time;

        // Play VFX
        fireVisualEffectInstance!.Play();
    }
    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera SpellCamera) {
        return true;
    }

    // Returns the direction to fire the projectile in.
    // 
    // If we don't hit anything, we'll pick a "point" 100m away
    private static Vector3 GetProjectileDirection(Camera spellCamera, Vector3 muzzlePosition, LayerMask playerLayerToIgnore) {
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

    public override Texture2D? GetAimTexture() {
        return null;
    }
}
