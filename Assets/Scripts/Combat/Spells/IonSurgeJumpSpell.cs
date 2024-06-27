using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

public class IonSurgeJumpSpell : Spell {
    public override float ChargeRate => 1f / 8f; // 8 second cooldown
    public override int MaxNumberOfCharges => 1;
    public override Color SpellColor => Color.blue;
    public override bool DoesBlockOtherSpells => false;
    public override bool IsBlockedByOtherSpells => false;

    [Header("General (Ion Surge)")]
    public float SurgeJumpForce = 90f;

    [Tooltip("DamageArea instance for the spell")]
    public DamageArea? DamageArea;

    [Tooltip("VFX instance for the left hand")]
    public VisualEffect? LeftHandVFX;

    [Tooltip("VFX instance for the right hand")]
    public VisualEffect? RightHandVFX;

    [Tooltip("VFX prefab for the explosion")]
    public VisualEffect? MainExplosionVFXPrefab;

    [Tooltip("Light prefab for when the spell is triggered")]
    public Light? ExplosionLightPrefab;

    [Tooltip("Curve to multiply the intensity of the light by")]
    public AnimationCurve? LightIntensityCurve;

    [Header("Debug")]
    public bool DisableUpForce = false;

    // Controls all of the VFX for this, as there is *a lot*
    private IonSurgeVFXHelper? vfxHelper;

    private readonly float damageCoefficient = 8.0f; // 800%

    // TODO: We should actually calculate this based on the velocity of the player
    // Or rather, how long we expect for it to take for the player to reach the peak of the ion surge jump
    private float animationDuration {
        get {
            float gravityDownForce = 70f;
            // about 1.64 sec (w/ 90 jump force) - SurgeJumpForce / GravityDownForce
            return SurgeJumpForce / gravityDownForce;
        }
    }

    private float timeOfLastFire = Mathf.NegativeInfinity;

    private void Start() {
        vfxHelper = new IonSurgeVFXHelper(
            playerTransform: transform,
            leftHandVFXInstance: LeftHandVFX!,
            rightHandVFXInstance: RightHandVFX!,
            mainExplosionVFXPrefab: MainExplosionVFXPrefab!,
            explosionLightPrefab: ExplosionLightPrefab!,
            explosionLightIntensityCurve: LightIntensityCurve!
        );
    }

    private void Update() {
        Recharge();

        bool isActive = Time.time - timeOfLastFire < animationDuration;
        PlayerAnimator!.SetBool("IsIonSurgeActive", isActive);
        if (!isActive) { // if not active, stop VFX
            // We need to do this a bit sooner b/c of the trails bleh
            vfxHelper!.StopVFX();
        }

        vfxHelper!.OnUpdate(timeOfLastFire:timeOfLastFire);
    }

    // really don't know if I need to do this or not but w/e doesn't hurt
    private void OnDestroy() {
    }

    private void Recharge() {
        CurrentCharge += ChargeRate * Time.deltaTime;

        CurrentCharge = Mathf.Clamp(CurrentCharge, 0f, MaxNumberOfCharges);
    }

    public override void ShootSpell(
        (Vector3 leftArm, Vector3 rightArm) muzzlePositions,
        GameObject owner,
        Camera spellCamera,
        float entityBaseDamage,
        LayerMask layerToIgnore
    ) {
        CurrentCharge -= 1;

        DamageArea!.InflictDamageOverArea(
            damage: entityBaseDamage * damageCoefficient,
            center: transform.position,
            damageApplierAffiliation: Affiliation.Player,
            directHitCollider: null,
            statusEffectToApply: new StunnedStatusEffect(
                effectApplierAffiliation: Affiliation.Enemy,
                duration: 2f
            ),
            layers: -1
        );    

        // Launch the player in the air
        if (!DisableUpForce)
            UpdatePlayerVelocity?.Invoke(Vector3.up * SurgeJumpForce);

        timeOfLastFire = Time.time;

        vfxHelper!.StopVFX(); // Temporary for testing! (since I'm spamming it);
        vfxHelper!.PlayVFX();

        // Play sound effect
        AudioUtility.shared.CreateSFX(
            ShootSfx,
            transform.position,
            AudioUtility.AudioGroups.WeaponShoot,
            0f,
            10f
        );
    }

    public override void AttackButtonPressed() { }
    public override void AttackButtonHeld() { }
    public override void AttackButtonReleased() { }

    public override bool CanShoot() { // This is basically AttackButtonHeld() lol
        return CurrentCharge >= MaxNumberOfCharges;
    }

    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera spellCamera) {
        return true;
    }
}
