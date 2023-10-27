using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The only reason this has to be a MonoBehavior in the first place
// is so we can assign it a projectile
// Is there really no other way? Ugh I want this to just be input/output
public class FireballSpell : Spell {
    [Header("Idk")]
    public Color SpellColorForUI;

    [Tooltip("The Fireball projectile we shoot out")]
    // This should really be FireballProjectile, but tbh to FireballSpell it doesn't matter
    public Projectile ProjectilePrefab; // Projectile will determine it's damage? Or should this? How dumb should the Projectile be?

    [Tooltip("How long in seconds between shots")]
    public float DelayBetweenShots = 0.5f;

    /** Abstract Spell Properties **/

    public override float ChargeRate => 0.5f;
    public override int MaxNumberOfCharges => 5; // Maybe rename to MaxCharge
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;
    // There's gotta be some way to configure this
    // maybe make this retrieve a value set from Unity?
    public override Color SpellColor => SpellColorForUI;

    /** Local variables **/

    private float lastTimeShot = Mathf.NegativeInfinity;
    private bool isAttackButtonHeld = false;

    void Start() {
        // Start off with max charges
        CurrentCharge = MaxNumberOfCharges;
    }

    void Update() {
        // Even if we're firing, we should be recharging?
        // (this may or may not be shared between spells - which is why the Component architecture may be nice for this)
        Recharge();
    }

    public override void AttackButtonHeld() {
        isAttackButtonHeld = true;
    }

    public override void AttackButtonReleased() {
        isAttackButtonHeld = false;
    }
    public override void AttackButtonPressed() {
        // I'm really not sure if we care about this
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
        Camera spellCamera
    ) {
        AudioUtility.shared.CreateSFX(
            ShootSfx,
            spellCamera.transform.position,
            AudioUtility.AudioGroups.WeaponShoot,
            0f,
            10f
        );

        print($"shooting, current charge is now at {CurrentCharge}");
        // reduce # of charges
        CurrentCharge -= 1;

        print($"muzzle pos: {muzzlePosition.ToString()}");

        Vector3 direction = spellCamera.transform.forward;

        // Spawn a projectile
        Projectile newProjectile = Instantiate(
            ProjectilePrefab,
            muzzlePosition,
            Quaternion.LookRotation(direction)
        );

        newProjectile.Shoot(owner, spellCamera);

        lastTimeShot = Time.time;
    }
}
