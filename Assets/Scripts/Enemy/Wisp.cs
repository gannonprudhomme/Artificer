using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;

#nullable enable

[RequireComponent(
    typeof(Animator)
)]
public class Wisp : NavSpaceEnemy {
    [Header("References (Wisp)")]
    public Transform? AimPoint;

    public SkinnedMeshRenderer? MainMeshRenderer;

    [Tooltip("Reference to the main collider. Used for NavSpaceEnemy.ColliderCast")]
    public Collider? Collider;

    [Tooltip("Reference to the VFX to play when we start charging")]
    public VisualEffect? ChargeVisualEffect;

    [Tooltip("Reference to the charge line renderer")]
    public LineRenderer? ChargeLineRenderer;

    [Tooltip("References to the 3 line renderers that are used to 'fire' at the player")]
    public LineRenderer[]? FireLineRenderers;

    public float MoveSpeed = 8.0f; // Temp so we can manually tweak the speed in the editor

    // Degrees per second
    public float RotationSpeed = 20.0f; // public only so we can manually tweak the speed in the editor

    private WispFireballAttack? attack = null;

    // Any single instance of damage that deals more than 10% of the total health will stun it & interrupts its attacks or movement
    private const float stunHealthPercentage = 0.1f;
    private const float stunDuration = 0.3f; // How long the stun actually lasts
    private float timeOfLastStun = Mathf.NegativeInfinity;
    private const float stunCooldown = 1.5f; // Needs to be 1.5 seconds until the last stun ended to stun again

    private State currentState = State.CHASE;

    /* Abstract properties */
    public override string EnemyIdentifier => "Wisp";
    protected override float StartingBaseDamage => 3.5f;
    public override float CurrentBaseDamage => StartingBaseDamage;
    public override float Speed => MoveSpeed; // Probably just hardcode this later

    protected override void Start() {
        base.Start();

        Animator animator = GetComponent<Animator>();

        attack = new WispFireballAttack(
            target: Target,
            aimPoint: AimPoint!,
            chargeLineRenderer: ChargeLineRenderer!,
            fireLineRenderers: FireLineRenderers!,
            animator: animator,
            chargeVisualEffect: ChargeVisualEffect!
        );

        health!.OnDamaged += OnDamaged;

        if (EnemyManager.shared.WispGraph != null) {
            graph = EnemyManager.shared.WispGraph;
        } else {
            Debug.LogError("Did not have a graph to load - we can't navigate!");
        }
    }

    float lastTimePathSet = Mathf.NegativeInfinity;
    const float timeBetweenPathSets = 0.5f;

    protected override void Update() {
        base.Update();

        attack?.StartCharging();
        attack?.OnUpdate(CurrentBaseDamage);

        // What order should these be in? I never really know
        currentState = DetermineCurrentState();
        PerformCurrentState();

        if (Time.time - lastTimePathSet > timeBetweenPathSets) {
            CreatePathTo(Target.AimPoint.position + (Vector3.one * 3));
            lastTimePathSet = Time.time;
        }

        TraversePath();

        LookAtTarget();

        // Animate time last hit (if needed)
    }

    private State DetermineCurrentState() {
        // Apparently states have durations so we might need to check that here
        // though in reality it doesn't really matter

        return State.USE_PRIMARY_AND_STRAFE;
    }

    private void PerformCurrentState() {
        // Honestly I'm wondering if isFrozen should just be one of the states
        // Cause it basically is from how we handle it
        if (isFrozen) {
            // prevent it from moving
            // reset the ttack
            attack!.canAttack = false;

            // Prevent LookAt constraint (will we even have that? Idek)
            return;
        }

        switch(currentState) {
            case State.USE_PRIMARY_AND_FLEE:
                attack!.canAttack = true;
                DoStrafe();
                LookAtTarget();

                break;
            case State.USE_PRIMARY_AND_STRAFE:
                attack!.canAttack = true;
                DoStrafe();
                LookAtTarget();

                break;
            case State.STUNNED:
                attack!.canAttack = false;

                // Prevent moving
                // Reset the attack and prevent it from attacking

                break;
            case State.CHASE:
                DoChase();
                LookAtTarget(); // This'll work for now
                break;
        }
    }

    private void DoStrafe() {
        // Choose a point to strafe to
        // And calculate the path to it
        // then actually start movingGetComponent<MeshRenderer>()
    }

    private void DoChase() {
        // Ensure we're not re-calculating the path too frequently
        // e.g. if the target hasn't moved nearest GraphNodes
        // since changing it at the end would be super easy
        // Since re-calculating the path would probably be pretty time consuming

        // Or maybe we could only renegate part of the path? Idk if that makes sense, might be too complex of a problem for little gain
    }

    // Make this look at the player if we're aiming at it
    //
    // Should be called when we're attacking the player (USE_PRIMARY_)
    private void LookAtTarget() {
        Vector3 dirToTarget = (Target.AimPoint.position - transform.position).normalized;
        Quaternion lookRotationToTarget = Quaternion.LookRotation(dirToTarget);

        // So this isn't great b/c if we're close to the Wisp it takes too long to rotate
        // but when we're far away it's a fucking lock-on and we can't really avoid it, but I think that's fine
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotationToTarget, Time.deltaTime * RotationSpeed);
    }

    private void OnDamaged(float damage, Vector3 damagePosition, DamageType damageType) {
        // If a single piece of damage is > 10% of the Wisp's max health, stun it
        float damageAsPercentOfHealth = damage / health!.MaxHealth;
        if (damageAsPercentOfHealth > stunHealthPercentage) {
            currentState = State.STUNNED;
            timeOfLastStun = Time.time;
        }
    }

    public override Material GetMaterial() {
        return MainMeshRenderer!.material;
    }

    public override Vector3 GetMiddleOfMesh() {
        return Vector3.zero;
    }

    protected override bool ColliderCast(Vector3 position, out RaycastHit? hit) {
        if (Collider == null) {
            hit = null;
            return false;
        }

        Vector3 direction = (position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, position);

        // We could cache this, but eh
        Vector3 size = Collider.bounds.size;
        float biggestSide = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        // I couldn't figure out BoxCast so this is good enough
        if (Physics.SphereCast(
            transform.position,
            radius: biggestSide,
            direction: direction,
            hitInfo: out RaycastHit rayHit,
            maxDistance: distance 
        )) {
            if (rayHit.collider.gameObject != Target.gameObject) {
                hit = rayHit;
                return true;
            } else { 
                // If what we hit is the same thing as what we're aiming for, this is what we want!           
                // so act like we didn't hit anything
                // This is what we want, how does it not happen every time?
                // Debug.Log("We hit the player!");
            }
        }

        hit = null;
        return false;
    }

    enum State {
        // Must be within 20m from its target and have line of sight.
        // The primary is not necessary to be off cooldown.
        // This behavior will be active for 0.5 seconds.
        USE_PRIMARY_AND_FLEE,

        // Must be within 20-30m from its target and have line of sight.
        // The primary is not necessary to be off cooldown.
        // This behavior will be active for 2 seconds.
        USE_PRIMARY_AND_STRAFE,

        // Any single instance of damage that deals more than 10% of total health will stun it
        // and interrupt its attacks or movement
        STUNNED,

        // Chase the target
        CHASE
    }
}
