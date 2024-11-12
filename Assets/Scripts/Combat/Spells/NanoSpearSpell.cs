using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

// Freezing. Charge up a piercing nano-spear that deals 400%-1200% damage.
//
// The player can sprint while charging the Nano-Spear, but beginning to charge it will cancel sprinting.
// They will need to start charging and then sprint, and repeat this for each attack.
public class NanoSpearSpell : Spell {
    [Header("General (Nano Spear Spell)")]
    public AudioClip? ChargeSFX;

    [Tooltip("Prefab for the Nano Spear Projectile")]
    public NanoSpearProjectile? ProjectilePrefab;

    [Tooltip("Transform for where we spawn the projectile")]
    public Transform? ProjectileSpawnPoint;

    [Tooltip("What we animate (rather than actually shoot)")]
    public GameObject? AnimatedProjectileInstance;

    [Header("VFX Instances")]
    [Tooltip("VFX 'flakes' instance that plays around the projectile when charging")]
    public VisualEffect? FlakesChargeProjectileVFXInstance;

    [Tooltip("VFX instance that plays on the left hand when we fire")]
    public VisualEffect? LeftHandFireVFXInstance;

    [Tooltip("VFX instance that plays on the right hand when we fire")]
    public VisualEffect? RightHandFireVFXInstance;

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

    [Header("Debug")]
    public bool DebugQuickRecharge = false;

    // This shouldn't charge if we're currently aiming
    public override float ChargeRate => !DebugQuickRecharge ? 1f / 5f : 1f; // 5 second cooldown

    public override int MaxNumberOfCharges { get; set; } = 1;

    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;

    private readonly float minDamageCoefficient = 4.0f; // 400%
    private readonly float maxDamageCoefficient = 12.0f; // 1200%

    private float minChargeDuration => chargeDuration * 0.3f;
    private readonly float chargeDuration = 2.0f; // It's honestly like 2 seconds? Play w/ it
    private bool didReleaseEarly = false;

    // TODO: Make this work I need it to
    private bool hasReleasedSinceFiring = true;
    private float cooldownBetweenFires = 0.2f;

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

    private readonly float cancelSprintDuration = 0.2f;

    private bool isPlayingFireAnimation {
        get {
            // isChargingAttack check is incase we're charging the *next* attack (i.e. in case of backup magazine)
            return Time.time - timeOfFireStart < fireAnimationDuration && !isChargingAttack;
        }
    }

    private bool hasReachedMinChargeDuration {
        get {
            return Time.time - timeOfChargeStart >= minChargeDuration;
        }
    }
    private bool hasReachedMinDurationBetweenFires {
        get {
            return Time.time - timeOfFireStart >= cooldownBetweenFires;
        }
    }

    private Entity? owner;
    private LayerMask? layerToIgnore;
    private Camera? spellCamera;
    private GameObject? chargeSound;

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
        Entity owner,
        Camera spellCamera,
        float entityBaseDamage,
        LayerMask layerToIgnore
    ) {
        if (!CanShoot()) {
            return;
        }

        this.owner = owner;
        this.entityBaseDamage = entityBaseDamage;
        this.spellCamera = spellCamera;
        this.layerToIgnore = layerToIgnore;

        if (isChargingAttack) { // If we're already charging don't do anything since we already started
            return;
        }

        // Start charging! This will only be called once (next time isChargingAttack will be true)
        didReleaseEarly = false;

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

        // Play charge sound
        chargeSound = AudioUtility.shared.CreateSFX(
            clip: ChargeSFX!,
            position: transform.position,
            audioGroup: AudioUtility.AudioGroups.WeaponShoot,
            spatialBlend: 0.0f,
            rolloffDistanceMin: 10.0f
        );
    }

    public override void AttackButtonReleased() { // We released - fire!
        if (!isChargingAttack) { // If we're not charging don't do anything
            return;
        } else if (!hasReachedMinChargeDuration) { // Released the mouse but didn't charge enough
            didReleaseEarly = true;
            return;
        } else if (didReleaseEarly) { // If we've already set it, keep going - we'll check in Update() to see if we should fire
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

        // Pass the charge percent (age) to the charge projectile color shader
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

        LeftHandFireVFXInstance!.Play();
        RightHandFireVFXInstance!.Play();

        // If it hasn't been destroyed yet, destroy the charge sound so it stops playing
        if (chargeSound != null) {
            Destroy(chargeSound);
        }

        // Play fire sound
        AudioUtility.shared.CreateSFX(
            clip: ShootSfx!,
            position: transform.position,
            audioGroup: AudioUtility.AudioGroups.WeaponShoot,
            spatialBlend: 0.0f,
            rolloffDistanceMin: 10.0f
        );

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
            owner: owner!,
            ownerAffiliation: Affiliation.Player,
            spellCamera: spellCamera,
            entityBaseDamage: entityBaseDamage * damageCoefficient,
            procCoefficient: 1f
        );

        // At the same time, disable what we wanimate
        AnimatedProjectileInstance!.SetActive(false);
    }

    // Got cancelled by another spell, end it without firing
    // This is a subset of EndChargeAndFire's logic
    public override void Cancel() {
        isChargingAttack = false;

        PlayerAnimator!.SetBool("IsFiringNanoSpear", false);
        PlayerAnimator!.SetBool("IsChargingNanoSpear", false);

        FlakesChargeProjectileVFXInstance!.Stop();

        if (chargeSound != null) {
            Destroy(chargeSound);
        }

        AnimatedProjectileInstance!.SetActive(false);
    }

    protected override bool CanShoot() {
        // I might need to do something here w/ this
        // TODO: We should be fine with not having reached the min duration if we released & clicked it again probably?
        return CurrentCharge >= CHARGE_PER_SHOT && !IsBlockingSpellActive && hasReachedMinDurationBetweenFires /* && hasReleasedSinceFiring */;
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

    public override bool ShouldBlockOtherSpells() {
        return isChargingAttack;
    }

    // it should only cancel the start of the charge - we should be able to sprint while charging otherwise
    public override bool ShouldCancelSprinting() {
        return Time.time - timeOfChargeStart < cancelSprintDuration;
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

