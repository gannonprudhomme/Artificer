using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

// The fireball attack aiming follows the transform.forward of the wisp (well, Wisp.AimPoint.forward).
// The fireball attack itself doesn't aim like GolemLaserAttack does
public class WispFireballAttack: EnemyAttack {
    private readonly Target target;
    private readonly Transform aimPoint;
    private readonly LineRenderer chargeLineRenderer;
    private readonly LineRenderer[] fireLineRenderers;
    private readonly Animator animator;
    private readonly VisualEffect chargeVisualEffect;

    private float entityBaseDamage;

    private bool isCharging = false;
    private float timeOfChargeStart = Mathf.NegativeInfinity;
    private const float chargeDuration = 1.6f; // 1.6 seconds to charge/aim (before firing)
    // private const float cooldown = 2.0f; // 2 second end "lag"

    // Used to determine how we animate the chargeLineRenderer over the charge duration
    // Set in StartCharging()
    private bool wasHittingPlayerAtChargeStart = false;

    private bool isFiring = false;
    private const float fireDuration = 0.1f;
    private float timeOfLastFire = Mathf.NegativeInfinity;
    private Vector3[] fireEndPositions = new Vector3[3];

    private const float maxAttackDistance = 70.0f; // Arbitrary number

    private const float cooldown = 4.0f;

    private const float damageCoefficient = 1.5f; // Does 150% of Wisp base damage
    private float damagePerProjectile {
        get { return entityBaseDamage * damageCoefficient; }
    }

    private const string ANIM_IS_FIRING = "IsFiring";
    private const string ANIM_IS_CHARGING = "IsCharging";

    public WispFireballAttack(
        Target target,
        Transform aimPoint,
        LineRenderer chargeLineRenderer,
        Animator animator,
        VisualEffect chargeVisualEffect,
        LineRenderer[] fireLineRenderers
    ) {
        this.target = target;
        this.aimPoint = aimPoint;
        this.chargeLineRenderer = chargeLineRenderer;
        this.fireLineRenderers = fireLineRenderers;
        this.animator = animator;
        this.chargeVisualEffect = chargeVisualEffect;

        chargeVisualEffect.Stop();
        chargeLineRenderer.enabled = false;
        foreach(var fireLineRenderer in fireLineRenderers) {
            fireLineRenderer.enabled = false;
        }
    }

    public override void OnUpdate(float entityBaseDamage) {
        if (!canAttack) {
            // Reset attack cooldown and stuff
            Reset();
            return;
        }
        
        this.entityBaseDamage = entityBaseDamage;

        // If it's been enough time to start charging again
        if (isCharging) {
            HandleCharging();
        }
        
        if (isFiring) {
            HandleFiring();
        }
    }

    public void StartCharging() {
        if (!CanStartCharging()) { return; }

        isCharging = true;
        timeOfChargeStart = Time.time;
        chargeLineRenderer.enabled = true;

        animator.SetBool(ANIM_IS_CHARGING, true);

        chargeVisualEffect.Play();

        // Mark if we were hitting the player when the charge started I guess?
        if (Physics.Raycast(aimPoint.position, aimPoint.forward, out RaycastHit hit, 40.0f) &&
            hit.collider.TryGetEntityFromCollider(out Entity entity) &&
            entity.gameObject == target.gameObject
        ) {
            wasHittingPlayerAtChargeStart = true;
        } else {
            wasHittingPlayerAtChargeStart = false;
        }
    }

    private bool CanStartCharging() {
        bool hasCooledDown = Time.time - timeOfLastFire >= cooldown;

        return hasCooledDown && !isCharging && !isFiring;
    }

    private void HandleCharging() {
        bool readyToFire = Time.time - timeOfChargeStart >= chargeDuration;
        if (readyToFire) {
            StartFiring();
            return;
        }

        SetChargeLineRendererPositions();
    }

    // Sets the line renderer positions for when we're charging.
    // Animates the line renderer so it grows over the charge duration
    //
    // Only called when we're charging 
    //
    // We can assume that we have line of sight w/ the player when this is being called
    // (technically we could start charging then lose line of sight but idc about that)
    private void SetChargeLineRendererPositions() {
        // We shouldn't ever actually reach it (we don't want the charge line to hit the player)
        // so artifically increase the charge duration by a bit so chargePercent's max value is like 0.8 (80%)
        float modifiedChargeDuration = chargeDuration * 1.2f; 
        float chargePercent = (Time.time - timeOfChargeStart) / modifiedChargeDuration;

        float currDist;
        if (wasHittingPlayerAtChargeStart) {
            float distToPlayer = Vector3.Distance(aimPoint.position, target.AimPoint.position);
            currDist = distToPlayer * chargePercent;

        } else {
            currDist = maxAttackDistance * chargePercent;
        }

        Vector3 endPos = aimPoint.position + (aimPoint.forward * currDist);

        chargeLineRenderer.SetPosition(0, aimPoint.position);
        chargeLineRenderer.SetPosition(1, endPos);
    }

    // "Fires" the actual attack (deals damage) and starts the animations for the firing
    private void StartFiring() {
        isCharging = false;
        isFiring = true;
        timeOfLastFire = Time.time;

        chargeVisualEffect.Stop();
        chargeLineRenderer.enabled = false;

        animator.SetBool(ANIM_IS_FIRING, true);
        animator.SetBool(ANIM_IS_CHARGING, false);

        fireEndPositions = new Vector3[3];

        // TODO: I kind of want below in a function it's really cluttering this up (but it would be weird for a func to return a value + do something [do the damaging])

        // Determine the end position for the "projectiles" (by raycasting)
        // and do the damage / spawn impact projectiles upon impact
        for(int i = 0; i < fireLineRenderers.Length; i++) {
            float range = 6f; // 6 deg range
            float spread = (Random.value * range) - (range / 2f); // Random value between [-2, 2]

            // Rotate it along the Wisp's up axis to determine what direction it should be w/ the spread applied
            Quaternion rotation = Quaternion.AngleAxis(spread, aimPoint.up);
            Vector3 spreadDir = rotation * aimPoint.forward;

            Vector3 endPos;

            // Determine if we hit something, and if so apply damage
            if (Physics.Raycast(
                origin: aimPoint.position,
                direction: spreadDir,
                out RaycastHit hit,
                maxDistance: maxAttackDistance
            )) {
                endPos = hit.point;

                OnProjectileHit(hit);
            } else {
                // Didn't hit, do the full max distance
                endPos = aimPoint.position + (spreadDir * maxAttackDistance);
            }

            fireEndPositions[i] = endPos;
        }
        
        // Enable all of the "projectiles"
        foreach(var fireLineRenderer in fireLineRenderers) {
            fireLineRenderer.enabled = true;
        }
    }

    private void HandleFiring() {
        bool isDoneFiring = Time.time - timeOfLastFire >= fireDuration;
        if (isDoneFiring) {
            animator.SetBool(ANIM_IS_FIRING, false);
            isFiring = false;

            // Disable all of the fireLineRenderers
            foreach(var fireLineRenderer in fireLineRenderers) {
                fireLineRenderer.enabled = false;
            }

            return;
        }

        float firingPercent = (Time.time - timeOfLastFire) / fireDuration;
        // Animate the line renderers
        for(int i = 0; i < fireLineRenderers.Length; i++) {
            LineRenderer fireLineRenderer = fireLineRenderers[i];
            Vector3 endPosition = fireEndPositions[i];
            Vector3 toEndPos = endPosition - aimPoint.position;

            Vector3 currPosition = aimPoint.position + (toEndPos * firingPercent);

            // TODO: Apparently these keep moving (like an actual projectile) so I might want to animate this too
            // (if it misses)
            // but we'll see
            fireLineRenderer.SetPosition(0, aimPoint.position);
            fireLineRenderer.SetPosition(1, currPosition);
        }
    }

    private void OnProjectileHit(RaycastHit hit) {
        if (hit.collider.TryGetEntityFromCollider(out Entity entityToDamage)) {
            entityToDamage.TakeDamage(damagePerProjectile, Affiliation.Enemy);
        }

        // TODO: Regardless of *what* we hit, spawn the on hit vfx
    }

    private void Reset() {
        isFiring = false;
        isCharging = false;
        animator.SetBool(ANIM_IS_CHARGING, false);
        animator.SetBool(ANIM_IS_FIRING, false);
    }
}

