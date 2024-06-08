using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public class IonSurgeJumpSpell : Spell {
    public override float ChargeRate => 1f / 8f; // 8s cooldown...?
    public override int MaxNumberOfCharges => 1;
    public override Color SpellColor => Color.blue;
    public override bool DoesBlockOtherSpells => false;
    public override bool IsBlockedByOtherSpells => false;

    [Header("General (Ion Surge)")]
    public float SurgeJumpForce = 90f;

    [Tooltip("VFX instance for the left hand")]
    public VisualEffect? LeftHandVFX;

    [Tooltip("VFX instance for the right hand")]
    public VisualEffect? RightHandVFX;


    // TODO: We should actually calculate this based on the velocity of the player
    // Or rather, how long we expect for it to take for the player to reach the peak of the ion surge jump
    private readonly float animationDuration = 90f / 55f; // about 1.64 sec - SurgeJumpForce / GravityDownForce
    private float timeOfLastFire = Mathf.NegativeInfinity;

    private void Awake() {
        StopVFX();
    }

    void Update() {
        Recharge();

        bool isActive = Time.time - timeOfLastFire < animationDuration;
        PlayerAnimator!.SetBool("IsIonSurgeActive", isActive);

        if (!isActive) {
            StopVFX();
        }
    }

    private void Recharge() {
        CurrentCharge += ChargeRate * Time.deltaTime;

        CurrentCharge = Mathf.Clamp(CurrentCharge, 0f, MaxNumberOfCharges);
    }

    public override void ShootSpell(
        (Vector3 leftArm, Vector3 rightArm) muzzlePositions,
        GameObject owner,
        Camera spellCamera,
        float currDamage,
        LayerMask layerToIgnore
    ) {
        CurrentCharge -= 1;

        // Launch the player in the air
        if (UpdatePlayerVelocity != null)
            UpdatePlayerVelocity(Vector3.up * SurgeJumpForce);

        timeOfLastFire = Time.time;

        PlayVFX();
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

    public override Texture2D? GetAimTexture() {
        return null;
    }

    private void PlayVFX() {
        LeftHandVFX!.enabled = true;
        RightHandVFX!.enabled = true;

        LeftHandVFX!.Play();
        RightHandVFX!.Play();
    }

    private void StopVFX() {
        LeftHandVFX!.enabled = false;
        RightHandVFX!.enabled = false;

        LeftHandVFX!.Stop();
        RightHandVFX!.Stop();
    }
}
