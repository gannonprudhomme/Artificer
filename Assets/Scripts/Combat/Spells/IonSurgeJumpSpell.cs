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

    // TODO: We should actually calculate this based on the velocity of the player
    // Or rather, how long we expect for it to take for the player to reach the peak of the ion surge jump
    private readonly float animationDuration = 1.5f;
    private float timeOfLastFire = Mathf.NegativeInfinity;

    void Update() {
        Recharge();

        bool isActive = Time.time - timeOfLastFire < animationDuration;
        PlayerAnimator!.SetBool("IsIonSurgeActive", isActive);
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
}
