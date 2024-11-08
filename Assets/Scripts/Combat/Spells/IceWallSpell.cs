using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

#nullable enable

public class IceWallSpell : Spell {
    [Header("Ice Wall Properties")]
    [Tooltip("The prefab to project on the ground so the user knows where they're aiming")]
    public GameObject? AimingDecalProjectorPrefab;

    public IceWall? IceWallPrefab; // We need a better name than Projectile (it's not a projectile really?)

    [Tooltip("The sound that plays when the button is pressed")]
    public AudioClip? StartChageSfx;

    [Header("Images")]
    [Tooltip("Image that's used when we're aiming the ice wall spell (at a valid target)")]
    public Texture2D? AimingImage;

    [Tooltip("Image used when we're aiming and can't spawn the ice wall where we're aiming")]
    public Texture2D? CantSpawnImage;

    [Header("VFX")]
    [Tooltip("VFX which plays on the left hand when we fire")]
    public VisualEffect? LeftHandShootVFXInstance;

    [Tooltip("VFX which plays on the right hand when we fire")]
    public VisualEffect? RightHandShootVFXInstance;

    [Header("Debug")]
    public bool DebugQuickRecharge = false;

    private const float DamageCoefficient = 1.0f;

    /** Abstract Spell Properties **/

    public override float ChargeRate => DebugQuickRecharge ? 0.1f : 1f / 12f; // 12 second cooldown 
    public override int MaxNumberOfCharges { get; set; } = 1;
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;

    /** Local variables **/

    private Entity? owner;
    private readonly float fireDuration = 1f;
    private float timeOfLastFire = Mathf.NegativeInfinity;
    private float entityBaseDamage = 0.0f;

    private bool isCharging = false;

    // We really shouldn't have to rely on this boolean - should just do it when it's first pressed?
    private bool hasPlayedChargeAudioThisCharge = false;

    // Where the ice wall should be spawned (when we finished aiming)
    private Vector3 aimingPoint;
    private Quaternion aimingRotation;

    // Instance of the aiming decal projector
    // will be destroyed after we fire
    private GameObject? aimingDecalProjectorInstance;

    private bool canShootWhereAiming = true;

    void Start() {
        CurrentCharge = MaxNumberOfCharges;
    }

    void Update() {
        // idk do nothing while we're not aiming or w/e

        Recharge();

        bool isFiring = Time.time - timeOfLastFire < fireDuration;
        PlayerAnimator!.SetBool("IsFiringIceWall", isFiring);
        PlayerAnimator!.SetBool("IsChargingIceWall", isCharging);
    }

    public override void AttackButtonReleased() {
        // If we were aiming at it was released, spawn the Ice Wall
        if (isCharging) {
            isCharging = false;
            hasPlayedChargeAudioThisCharge = false;

            // Destroy the aiming thing as we're not aiming anymore
            Destroy(aimingDecalProjectorInstance);

            if (!canShootWhereAiming) {
                // Reset it since we're not aiming anymore
                canShootWhereAiming = true;
                return;
            }

            // Play shoot sound ('around' the player - being offset doesn't really matter)
            AudioUtility.shared.CreateSFX(
                ShootSfx,
                transform.position,
                AudioUtility.AudioGroups.WeaponShoot,
                0f,
                10f
            );

            LeftHandShootVFXInstance!.Play();
            RightHandShootVFXInstance!.Play();

            SpawnIceWall(entityBaseDamage * DamageCoefficient);
        }
    }

    private void Recharge() {
        // We should probably check if we need to rProjectileecharge in the first place
        if (isCharging) { // Don't recharge if we were aiming
            return;
        }

        if (CurrentCharge < MaxNumberOfCharges) {
            CurrentCharge += ChargeRate * Time.deltaTime;

            // Don't let it get above MaxNumberOfCharges
            CurrentCharge = Mathf.Min(CurrentCharge, MaxNumberOfCharges);
        }
    }

    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera spellCamera) {
        return canShootWhereAiming;
    }

    // For the IceWall spell, consider this more of an "aiming" stan = Vector3.zerote
    // it's when we're constantly calling this then release it when we actually spawn the projectile
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

        // We want to play the audio no matter what
        if (!hasPlayedChargeAudioThisCharge) {
            hasPlayedChargeAudioThisCharge = true;

            AudioUtility.shared.CreateSFX(
                StartChageSfx!,
                spellCamera.transform.position, // position shouldn't matter b/c spatialBlend == 0
                AudioUtility.AudioGroups.WeaponShoot,
                0.0f, // we don't want any spatial?
                5f // isn't relevant
            );
        }

        // Determine the aiming point
        var raycastHit = Physics.RaycastAll(
            origin: spellCamera.transform.position,
            direction: spellCamera.transform.forward
        );

        if (raycastHit.Length == 0) {
            return; // No hits
        }

        RaycastHit bestHit = raycastHit[0];

        // Draw the decal on whatever we hit?
        aimingPoint = bestHit.point;
        aimingRotation = Quaternion.LookRotation(spellCamera.transform.right);

        // I figure we can do some math operation on
        // maybe a dot/cross product? Shouldn't be able to do it on any surface that's > 40deg or something

        // Spawn the aiming decal if it doesn't exist (or maybe if we weren't aimming last frame?)
        var eulerAngles = spellCamera.transform.rotation.eulerAngles;
        eulerAngles.x = 0;
        var rotation = Quaternion.Euler(eulerAngles);

        if (!isCharging) {
            isCharging = true;

            aimingDecalProjectorInstance = Instantiate(
                AimingDecalProjectorPrefab!,
                aimingPoint,
                rotation
            );
        } else {
            // Move it if the instance already exists
            aimingDecalProjectorInstance!.transform.SetPositionAndRotation(position: aimingPoint, rotation: rotation);
        }

        // Check if it's a wall - if it is we don't want to aim there
        // We should only be able to aim on floors, not walls,
        // so mark this as an invalid hit (and hide the aimingDecalProjectorInstance)
        if (IsWall(bestHit.normal)) {
            canShootWhereAiming = false;
            aimingDecalProjectorInstance.SetActive(false);
            return;
        }

        aimingDecalProjectorInstance.SetActive(true);

        // Wasn't a wall - we can shoot here!
        canShootWhereAiming = true;
    }

    protected override bool CanShoot() {
        return CurrentCharge >= CHARGE_PER_SHOT;
    }

    private void SpawnIceWall(float damagePerSpike) {
        CurrentCharge -= 1;

        IceWall iceWall = Instantiate(
            IceWallPrefab!,
            aimingPoint,
            aimingRotation
        );

        iceWall.DamagePerSpike = damagePerSpike;
        iceWall.owner = owner;
        timeOfLastFire = Time.time;
    }

    public override CrosshairReplacementImage? GetAimTexture() {
        if (isCharging) {
            if (canShootWhereAiming) {
                // return dot image
                return CrosshairReplacementImage.Aiming!;
            } else { // can't shoot
                // return X image
                return CrosshairReplacementImage.CantFire!;
            }
        } else {
            return null;
        }
    }

    private bool IsWall(Vector3 normal) {
        var angle = Vector3.Angle(Vector3.up, normal);
        if (angle > 45.0f) {
            return true;
        }

        return false;
    }

    public override bool ShouldCancelSprinting() {
        return isCharging;
    }

    public override bool ShouldBlockOtherSpells() {
        return isCharging;
    }

    // It only cancels Nano Spear
    public override bool ShouldCancelOtherSpells() {
        return isCharging;
    }
}
