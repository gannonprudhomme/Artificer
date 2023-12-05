using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices.WindowsRuntime;
using Codice.CM.Client.Differences.Merge;
using UnityEngine;

// General attack
// Attacks should be "dumb" since the Enemy will be controlling between diff attacks
// Might not need this: attacks aren't reusable by other enemies (I don't think - there base logic might)
// and each enemy will only have like 1-4 attacks which we'll manually balance (not iterate over)
public abstract class Attack {
    // damage?
    // Doesn't work b/c damage over time
    // could have an enum tho (associated enums plss)

    public abstract void DoAttack();
}

// I don't really think this should be a MonoBehaviour? It's not going to be a component or anything
// And the settings for the attacks we'll probably just set on the Stone Golem
// But it will be nice to have a container for this attack

// This needs to:
// - Control the shader / LineRenderer for this
// - Deal with the AoE damage (or have some DamageArea thing that does that)
// - Control the timing of this once we're trying to attack
//
// The laser shouldn't be perfect - if the player strafes left/right the player should be able to avoid it
//
// From the wiki:
// - Tracks the player for 3 seconds
// - Deals 250% of base damage (20, +4 per level)
// - Must be within 10-45m of the target, and have line of sight
// - Has a cooldown of 5 seconds
public class GolemLaserAttack {
    // This should really be damage area
    // We should get the damage from the Stone Golem
    public const float damage = 2.5f * 20;

    private LineRenderer lineRenderer;
    // Where on the stone golem we're aiming from
    // the start of the line
    private Transform startAimPoint;
    // What we're aiming at (the player)
    // The end of the line
    private Health target;

    // How far we can move (per second?) to try to aim towards the target
    // Probably going to be divided by Time.deltaTime, but idk
    private const float aimMoveSpeed = 1f;
    // Where the end of the laser / line was last frame
    // though we can just read this from the line renderer really
    private Vector3 lastEndAimPoint; // TODO: Find a better name 

    // TODO: Put this shit below into structs we have too many

    // Are we in the middle of charging the laser (aka about to fire)?
    private bool isCharging;
    // Have we finished charging & we're in the middle of firing?
    private bool isFiring;

    // When we started firing the attack (after we finished charging)
    private float timeOfFireStart = Mathf.NegativeInfinity;
    // How long we play the "firing" animation for(?), before we start recharging again?
    // When timeOfFireStart > fireTime, we hide the line renderer
    private const float fireDuration = 1.0f;
    // When we started charging this attack
    private float timeOfChargeStart = Mathf.NegativeInfinity;
    // Charge the laser for 3 seconds before firing
    private const float chargeDuration = 3.0f;
    // Start flashing the lazer 2.5 seconds into the charge
    private const float aboutToFireFlashDuration = 1.5f;

    // The Time.time of when we last fired
    private float timeOfLastFire = Mathf.NegativeInfinity;
    private const float cooldown = 5.0f; // In seconds

    // Expect this to be called in Start() (or Awake()?) in the Stone Golem
    public GolemLaserAttack(
        LineRenderer lineRenderer,
        Transform aimPoint,
        Health target
    ) {
        this.lineRenderer = lineRenderer;
        this.startAimPoint = aimPoint;
        this.target = target;

        this.lineRenderer.enabled = true;
        this.lineRenderer.useWorldSpace = true;
        this.lineRenderer.positionCount = 2;

        // We need to set the width to the _max_ possible value so the firing animation can scale up as it needs to
        // We could theoretically change it in code depending on the is charging vs is firing state,
        // but I think doing it all in teh shader is going to be easier.
        this.lineRenderer.startWidth = 1.0f;
        this.lineRenderer.endWidth = 1.0f;
    }

    // Called in Update() in Stone Golem
    public void OnUpdate() {
        //SetLinePositions();
        // return;

        // boolean checks are just to prevent unnecessary checking
        // we don't need to check if we can attack if we're currently attacking
        if (!isCharging && !isFiring && CanAttack()) {
            StartAttack();
        }

        if (isCharging) {
            HandleChargingAttack();
        } else if (isFiring) {
            // We should do this in another if block really
            // as this should run when HandleChargingAttack() sets isFiring to true (within the same frame)
            HandleFiringAttack();
        } else { // Not doing either
            return; // return early
        }

        // Runs whenever we're charging or firing
        SetLinePositions();
    }

    // The laser is getting ready to fire (apply damage)
    // this is when the line is appearing but is the thinest
    private void HandleChargingAttack() {
        float secondsIntoCharge = Time.time - timeOfChargeStart;
        bool readyToFire = secondsIntoCharge >= chargeDuration;
        if (readyToFire) { // TODO: I'm not really sure if this is where we should do this
            StartFiring();
            return;
        }

        lineRenderer.material.SetInt("_IsFiring", 0);
        
        if (secondsIntoCharge >= aboutToFireFlashDuration) {
            // Tell the shader (material) to start flashing
            lineRenderer.material.SetInt("_ShouldChargeFlash", 1);
        } else { // We're charging normally (not flashing) so animate accordingly?
            lineRenderer.material.SetInt("_ShouldChargeFlash", 0);
        }
    }

    // We're in the middle of firing - handle it
    private void HandleFiringAttack() {
        // We're charging
        // Probably pass secondsIntoCharge into the
        float secondsIntoFiring = Time.time - timeOfFireStart;

        if (secondsIntoFiring < fireDuration) { // Are we in the middle of the firing (animation)
          // Set shader stuff I guess

        } else { // we're done, wrap up
            isFiring = false;
            lineRenderer.enabled = false;
            lineRenderer.material.SetInt("_IsFiring", 0);
        }
    }

    private void AimAtTarget() {
        // Attempt to move as much as we can this frame to aim at the target
    }

    // This shouldn't modify anything
    private bool CanAttack() {
        float timeSinceLastFire = Time.time - timeOfLastFire;

        if (timeSinceLastFire < cooldown) {
            // Can't attack since we haven't cooled down enough
            return false;
        }

        // We should really use the Stone Golem's main transform, not the aim point (it's gonna be farther away from the target?)
        // probs negligible though (REMOVE THIS)
        float distanceToTarget = Vector3.Distance(startAimPoint.position, target.transform.position);
        // Are we within 10 - 45m of the target (lets say 2m for the min for now, or whatever I set the stopping distance to)
        if (distanceToTarget < 45f) {
            // Debug.Log("Within distance, can attack!");
            return true;
        }

        // Debug.Log("Can't attack!");

        return false;
    }

    // In this case, start charging the laser.
    // The StoneGolem is handing over the reigns to the attack at this point
    public void StartAttack() {
        // Debug.Log("Starting to charge laser");
        isCharging = true;

        timeOfChargeStart = Time.time;
        lineRenderer.enabled = true;
    }

    // The laser is done charging, "fire" it!
    private void StartFiring() {
        Debug.Log("Starting to fire!");
        isCharging = false;
        isFiring = true;
        timeOfFireStart = Time.time;
        timeOfLastFire = Time.time; // I suppose we don't need two of these
        // Hide it when this is all over

        lineRenderer.material.SetInt("_IsFiring", 1);

        // For now just damage the target, assuming we hit them

        // We could do this much easier if we just used a layer mask for the player
        // Vector3 fromEyeToEndPoint = lastEndAimPoint - startAimPoint.position;
        Vector3 fromEyeToEndPoint = target.transform.position - startAimPoint.position;
        Debug.DrawLine(target.transform.position, startAimPoint.position, Color.red, 3f);
        RaycastHit[] hits = Physics.SphereCastAll(
            startAimPoint.position,
            1.0f, // radius, idk
            fromEyeToEndPoint.normalized,
            fromEyeToEndPoint.magnitude,
            -1,
            QueryTriggerInteraction.Ignore
        );

        // Find Health
        bool didHit = false;
        foreach ( RaycastHit hit in hits ) {
            if (hit.collider.TryGetComponent<Health>(out Health health)) {
                // Debug.Log("We hit, hell yeah");
                health.TakeDamage(damage);
                didHit = true;
                break;
            }
        }

        if (!didHit) {
            Debug.Log("Didn't hit wtf");
        }
    }

    private void SetLinePositions() {
        lineRenderer.SetPosition(0, startAimPoint.position);
        // lineRenderer.SetPosition(1, lastEndAimPoint);
        lineRenderer.SetPosition(1, target.transform.position);
    }
}
