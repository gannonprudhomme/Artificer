using UnityEditor;
using UnityEngine;

#nullable enable

// Freezing. Charge up a piercing nano-spear that deals 400%-1200% damage.
//
// The player can sprint while charging the Nano-Spear, but beginning to charge it will cancel sprinting.
// They will need to start charging and then sprint, and repeat this for each attack.
public class NanoSpearSpell : Spell {
    [Header("General (Nano Spear Spell)")]
    [Tooltip("Prefab for the Nano Spear Projectile")]
    public NanoSpearProjectile? ProjectilePrefab;

    [Tooltip("Transform for where we spawn the projectile")]
    public Transform? ProjectileSpawnPoint;

    [Tooltip("What we animate (rather than actually shoot)")]
    public GameObject? AnimatedProjectileInstance;

    [Tooltip("Curve over the lifetime of the charging that we animate the position of the reticle with")]
    public AnimationCurve? ChargingReticleAnimationCurve;

    public AnimationCurve? FiringReticleAnimationCurve;

    // This shouldn't charge if we're currently aiming
    public override float ChargeRate => 1f / 5f; // 5 second cooldown

    public override int MaxNumberOfCharges => 1;

    public override Color SpellColor => Color.yellow;

    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;

    private readonly float minDamageCoefficient = 4.0f; // 400%
    private readonly float maxDamageCoefficient = 12.0f; // 1200%

    private readonly float chargeDuration = 2.0f; // It's honestly like 2 seconds? Play w/ it
    private float entityBaseDamage;
    private float timeOfChargeStart = Mathf.NegativeInfinity;
    // If we're "charging" the attack (holding down the button to shoot & haven't "fired" yet)
    private bool isChargingAttack = false;

    private float timeOfFireStart = Mathf.NegativeInfinity;
    private readonly float fireAnimationDuration = 0.5f;

    private bool isPlayingFireAnimation {
        get {
            return Time.time - timeOfFireStart < fireAnimationDuration;
        }
    }

    private LayerMask? layerToIgnore;
    private Camera? spellCamera;

    // Start is called before the first frame update
    private void Start() {
        CurrentCharge = MaxNumberOfCharges;

        AnimatedProjectileInstance!.SetActive(false);
    }

    private void Update() {
        Recharge();

        if (isChargingAttack) {
            HandleChargingAttack();
        }

        // Well ideally we wouldn't set this *every frame* but bleh whatever
        // I could add a bool here to prevent this from being set once we already have but bleh
        if (!isPlayingFireAnimation) {
            PlayerAnimator!.SetBool("IsFiringNanoSpear", false);
        }
    }

    // We're starting to charge.
    // This won't be called when CanShoot() is false.
    public override void AttackButtonHeld(
        (Vector3 leftArm, Vector3 rightArm) muzzlePositions,
        GameObject owner,
        Camera spellCamera,
        float entityBaseDamage,
        LayerMask layerToIgnore
    ) {
        this.entityBaseDamage = entityBaseDamage;
        this.spellCamera = spellCamera;
        this.layerToIgnore = layerToIgnore;

        if (isChargingAttack) { // If we're already charging don't do anything since we already started
            Debug.LogError("We're already charging!"); // Should never happen
            return;
        }

        PlayerAnimator!.SetBool("IsChargingNanoSpear", true);

        AnimatedProjectileInstance!.SetActive(true);

        isChargingAttack = true;

        Debug.Log("Starting to charge!");

        // Start charging (this assumes we have enough)
        CurrentCharge -= 1;

        timeOfChargeStart = Time.time;
    }

    public override void AttackButtonReleased() { // We released - fire!
        Debug.Log("Attack button released!");
        if (!isChargingAttack) { // If we're not charging don't do anything
            return;
        }

        EndChargeAndFire();
    }

    // Called in Update()
    private void HandleChargingAttack() {
        bool isChargeCompleted = Time.time - timeOfChargeStart >= chargeDuration;
        if (isChargeCompleted) {
            EndChargeAndFire();
            return;
        }

        // Animate? This stuff should have already kicked off
        // idk if we need to do anything else tbh

        // Temporary - should be controlled by an Animation later
        float startScale = 0.1f;
        float endScale = 2.0f;

        float chargePercent = (Time.time - timeOfChargeStart) / chargeDuration;
        float currentZScale = Mathf.Lerp(startScale, endScale, chargePercent);

        float normalScale = 0.5f;
        AnimatedProjectileInstance!.transform.localScale = new Vector3(normalScale, normalScale, currentZScale);

        MeshRenderer renderer = AnimatedProjectileInstance!.GetComponentInChildren<MeshRenderer>();
        if (renderer != null) {
            renderer.material.SetFloat("_Age", chargePercent);
        } else {
            Debug.LogError("No renderer found on Nano Spear Projectile Instance!");
        }
    }

    private void EndChargeAndFire() { // We released - fire!
        timeOfFireStart = Time.time;

        isChargingAttack = false;
        PlayerAnimator!.SetBool("IsFiringNanoSpear", true);
        PlayerAnimator!.SetBool("IsChargingNanoSpear", false);

        float chargePercent = (Time.time - timeOfChargeStart) / chargeDuration;
        // float damageCofficient = Mathf.Lerp(minDamageCoefficient, maxDamageCoefficient, chargePercent); // This is a bit overkill
        // Calculate the damage coefficient as a percentage of the min and max damage coefficients
        float damageCoefficient = ((maxDamageCoefficient - minDamageCoefficient) * chargePercent) + minDamageCoefficient;

        Vector3 direction = FireballSpell.GetProjectileDirection(
            spellCamera: spellCamera!,
            muzzlePosition: ProjectileSpawnPoint!.position,
            playerLayerToIgnore: (LayerMask)layerToIgnore! // Why is this weird
        );

        // Spawn the projectile and shoot it where we're aiming (how tf do we know where we're aiming?)
        NanoSpearProjectile projectile = Instantiate(
            ProjectilePrefab!,
            ProjectileSpawnPoint!.transform.position,
            spellCamera!.transform.rotation
        );

        projectile.Shoot(
            owner: gameObject,
            ownerAffiliation: Affiliation.Player,
            spellCamera: spellCamera,
            entityBaseDamage: entityBaseDamage * damageCoefficient
        );

        // At the same time, disable what we wanimate
        AnimatedProjectileInstance!.SetActive(false);
    }

    public override bool CanShoot() {
        // I might need to do something here w/ this
        return CurrentCharge >= MaxNumberOfCharges;
    }

    private void Recharge() {
        // Don't recharge if we're aiming/charging the attack
        if (isChargingAttack) {
            return;
        }

        if (CurrentCharge < MaxNumberOfCharges) {
            CurrentCharge += ChargeRate * Time.deltaTime;

            // Don't let it get above MaxNumberOfCharges
            CurrentCharge = Mathf.Min(CurrentCharge, MaxNumberOfCharges);
        }
    }

    // Irrelevant for this
    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera spellCamera) { return true; }

    public override bool ShouldForceLookForward() {
        return isChargingAttack || isPlayingFireAnimation;
    }

    public override float? GetInnerReticleMultiplier() {
        if (!isChargingAttack && !isPlayingFireAnimation) {
            return null;
        } else if (isChargingAttack) {
            return ChargingReticleAnimationCurve!.Evaluate((Time.time - timeOfChargeStart) / chargeDuration);
        } else /* if (isPlayingFireAnimation) */ {
            return FiringReticleAnimationCurve!.Evaluate((Time.time - timeOfFireStart) / fireAnimationDuration);
        }
    }
}

