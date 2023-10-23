using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceWallSpell : Spell {
    [Header("Idk")]
    public Color ExternalSpellColor;

    [Tooltip("The prefab to project on the ground so the user knows where they're aiming")]
    public GameObject AimingDecalProjectorPrefab;

    public IceWall IceWallPrefab; // We need a better name than Projectile (it's not a projectile really?)

    /** Abstract Spell Properties **/
    public override float ChargeRate => 0.2f;
    public override int MaxNumberOfCharges => 1;
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;
    public override Color SpellColor => ExternalSpellColor;

    /** Local variables **/

    private bool wasAimingLastFrame = false;

    // Where the ice wall should be spawned (when we finished aiming)
    private Vector3 aimingPoint;
    private Quaternion aimingRotation;

    // Instance of the aiming decal projector
    // will be destroyed after we fire
    private GameObject aimingDecalProjectorInstance;

    void Start() {
        CurrentCharge = MaxNumberOfCharges;
    }

    void Update() {
        // idk do nothing while we're not aiming or w/e

        Recharge();
    }

    public override void AttackButtonHeld() {
    }

    public override void AttackButtonReleased() {
        // If we were aiming at it was released, spawn the Ice Wall
        if (wasAimingLastFrame) {
            print("done!");
            wasAimingLastFrame = false;

            // Destroy the aiming thing since we're firing
            Destroy(aimingDecalProjectorInstance);

            SpawnIceWall();
        }
    }

    public override void AttackButtonPressed() {
    }

    private void Recharge() {
        // We should probably check if we need to rProjectileecharge in the first place
        if (wasAimingLastFrame) { // Don't recharge if we were aiming
            return;
        }

        if (CurrentCharge < MaxNumberOfCharges) {
            CurrentCharge += ChargeRate * Time.deltaTime;

            // Don't let it get above MaxNumberOfCharges
            CurrentCharge = Mathf.Min(CurrentCharge, MaxNumberOfCharges);
        }
    }

    // For the IceWall spell, consider this more of an "aiming" state
    // it's when we're constantly calling this then release it when we actually spawn the projectile
    public override void ShootSpell(Vector3 muzzlePosition, GameObject owner, Camera spellCamera) {
        // Determine the aiming point

        var raycastHit = Physics.RaycastAll(
            muzzlePosition,
            spellCamera.transform.forward // Is this what we should do?
            // We need to know what to hit in the first place tho - just level, not Entities?
               // or if we aim at an entity should we center it at their feet? (do simpler for now)
        );

        Debug.DrawRay(muzzlePosition, spellCamera.transform.forward, Color.red);
        Debug.DrawLine(muzzlePosition, spellCamera.transform.forward * 1000.0f, Color.blue);

        if (raycastHit.Length == 0) {
            return; // No hits
        }

        RaycastHit bestHit = raycastHit[0];

        // Draw the decal on whatever we hit?
        aimingPoint = bestHit.point;
        aimingRotation = Quaternion.LookRotation(spellCamera.transform.right);

        // Spawn the aiming decal if it doesn't exist (or maybe if we weren't aimming last frame?)
        if (!wasAimingLastFrame) {
            wasAimingLastFrame = true;

            aimingDecalProjectorInstance = Instantiate(
                AimingDecalProjectorPrefab,
                aimingPoint,
                spellCamera.transform.rotation // idk if this is what we want
            );
        } else {
            // Move it if the instance already exists
            aimingDecalProjectorInstance.transform.position = aimingPoint;
            aimingDecalProjectorInstance.transform.rotation = spellCamera.transform.rotation;
        }
    }

    public override bool CanShoot() {
        bool hasEnoughCharge = CurrentCharge >= CHARGE_PER_SHOT;
        return hasEnoughCharge;
    }

    private void SpawnIceWall() {
        CurrentCharge -= 1;

        IceWall iceWall = Instantiate(
            IceWallPrefab,
            aimingPoint,
            aimingRotation
        );
    }
}
