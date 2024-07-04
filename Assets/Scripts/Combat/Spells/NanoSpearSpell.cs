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

    [Header("VFX Instances")]
    [Tooltip("VFX 'flakes' instance that plays around the projectile when charging")]
    public VisualEffect? FlakesChargeProjectileVFXInstance;


    [Header("Reticle Animation Curves")]
    [Tooltip("Curve over the lifetime of the charging that we animate the position of the reticle with from [0, 1]")]
    public AnimationCurve? ChargingReticleAnimationCurve;

    [Tooltip("Curve over the lifetime of the fire animation that we animate the position of the reticle with. From [0, 1]")]
    public AnimationCurve? FiringReticleAnimationCurve;

    [Header("Projectile Size Animation Curves")]
    [Tooltip("Curve for how the VFX projectile grows length-wise during the charge, NOT normalized")]
    public AnimationCurve? ProjectileSizeAnimationCurve; // TODO: Rename to length

    [Tooltip("Curve for how the VFX projectile grows on the Z-plane (x/y axii) during the charge, NOT normalized")]
    public AnimationCurve? ProjectileHorizontalSizeAnimationCurve;

    // This shouldn't charge if we're currently aiming
    public override float ChargeRate => 1f / 5f; // 5 second cooldown

    public override int MaxNumberOfCharges => 1;

    public override Color SpellColor => Color.yellow;

    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;

    private readonly float minDamageCoefficient = 4.0f; // 400%
    private readonly float maxDamageCoefficient = 12.0f; // 1200%

    private float minChargeDuration => chargeDuration * 0.3f;
    private readonly float chargeDuration = 2.0f; // It's honestly like 2 seconds? Play w/ it
    private bool didReleaseEarly = false;

    private float entityBaseDamage;
    private float timeOfChargeStart = Mathf.NegativeInfinity;
    // If we're "charging" the attack (holding down the button to shoot & haven't "fired" yet)
    private bool isChargingAttack = false;

    private float timeOfFireStart = Mathf.NegativeInfinity;
    private readonly float fireAnimationDuration = 1.25f; // Ideally we'd get this from the animation directly

    private float chargeEndPercentage = 0f;
    private readonly float baseReticleMultiplier = 1f;

    private readonly float maxProjectileSize = 1.0f;

    private readonly Vector3 chargeProjectileRotationSpeed = new(0f, 0f, 360f * 1.5f);

    private bool isPlayingFireAnimation {
        get {
            return Time.time - timeOfFireStart < fireAnimationDuration;
        }
    }

    private bool hasReachedMinChargeDuration {
        get {
            return Time.time - timeOfChargeStart >= minChargeDuration;
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

        // If we released early, wait until we've reached the min charge duration before firing
        if (didReleaseEarly && hasReachedMinChargeDuration) {
            EndChargeAndFire();
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

        // Start charging! This will only be called once (next time isChargingAttack will be true)

        PlayerAnimator!.SetBool("IsChargingNanoSpear", true);

        AnimatedProjectileInstance!.SetActive(true);

        isChargingAttack = true;

        // Start charging (this assumes we have enough)
        CurrentCharge -= 1;

        timeOfChargeStart = Time.time;

        // Do it for the first frame so we can update the size immediately upon charge
        // We either do this or we reset the size at the end of the animation
        HandleChargingAttack();

        FlakesChargeProjectileVFXInstance!.Play();
    }

    public override void AttackButtonReleased() { // We released - fire!
        if (!isChargingAttack) { // If we're not charging don't do anything
            return;
        } else if (!hasReachedMinChargeDuration) {
            didReleaseEarly = true;
            return;
        } else if (didReleaseEarly) { // If we've already set it, keep going
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

        
        float chargePercent = (Time.time - timeOfChargeStart) / chargeDuration;
        float currentZScale = ProjectileSizeAnimationCurve!.Evaluate(chargePercent) * (maxProjectileSize);

        float normalScale = ProjectileHorizontalSizeAnimationCurve!.Evaluate(chargePercent) * maxProjectileSize;
        AnimatedProjectileInstance!.transform.localScale = new Vector3(normalScale, normalScale, currentZScale);

        // Pass it to the VFX so it can spawn in the correct position
        FlakesChargeProjectileVFXInstance!.SetFloat("Charge Projectils Z Scale", currentZScale);

        // Animate the rotation
        AnimatedProjectileInstance!.transform.rotation *= Quaternion.Euler(chargeProjectileRotationSpeed * Time.deltaTime);

        MeshRenderer renderer = AnimatedProjectileInstance!.GetComponentInChildren<MeshRenderer>();
        if (renderer != null) {
            renderer.material.SetFloat("_Age", chargePercent);
        } else {
            Debug.LogError("No renderer found on Nano Spear Projectile Instance!");
        }
    }

    private void EndChargeAndFire() { // We released - fire!
        timeOfFireStart = Time.time;

        didReleaseEarly = false; // Whether we released early or not, reset it

        isChargingAttack = false;
        PlayerAnimator!.SetBool("IsFiringNanoSpear", true);
        PlayerAnimator!.SetBool("IsChargingNanoSpear", false);

        FlakesChargeProjectileVFXInstance!.Stop();


        // TODO: I should probably normalize this so 0 is actually minChargeDuration
        float chargePercent = (Time.time - timeOfChargeStart) / chargeDuration;
        // Calculate the damage coefficient as a percentage of the min and max damage coefficients
        float damageCoefficient = ((maxDamageCoefficient - minDamageCoefficient) * chargePercent) + minDamageCoefficient;

        Vector3 direction = FireballSpell.GetProjectileDirection( // Note we're just reusing the function, not related to Fireball
            spellCamera: spellCamera!,
            muzzlePosition: ProjectileSpawnPoint!.position,
            playerLayerToIgnore: (LayerMask)layerToIgnore! // Why is this weird
        );

        // Spawn the projectile and shoot it where we're aiming (how tf do we know where we're aiming?)
        NanoSpearProjectile projectile = Instantiate(
            ProjectilePrefab!,
            ProjectileSpawnPoint!.transform.position,
            Quaternion.LookRotation(direction)
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
            float time = (Time.time - timeOfChargeStart) / chargeDuration;
            chargeEndPercentage = time;
            // AnimationCurve's are from [0, 1]
            return baseReticleMultiplier + ChargingReticleAnimationCurve!.Evaluate(time);
        } else /* if (isPlayingFireAnimation) */ {
            float modifiedFireAnimDuration = fireAnimationDuration * 0.5f; // Should animate in half of the fire anim time?
            float time = (Time.time - timeOfFireStart) / modifiedFireAnimDuration;

            // calculate the modified time so that the curve starts at the chargeEndPercentage
            // E.g. if we only charged 30% of the full charge time, then we should skip the first 30% of the firing curve
            // if the chargeEndPercentage is 100% then we should animate the whole curve
            // this way no matter where the reticle ends up it animates smoothly

            float modifiedTime = time + Mathf.Clamp01(1 - chargeEndPercentage);

            return baseReticleMultiplier + FiringReticleAnimationCurve!.Evaluate(modifiedTime);
        }
    }
}

