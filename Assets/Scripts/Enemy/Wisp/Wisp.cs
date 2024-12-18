using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;

#nullable enable

[RequireComponent(
    typeof(Animator),
    typeof(Rigidbody)
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

    [Tooltip("VFX *prefab* to play when the entity dies")]
    public VisualEffect? DeathVFX;

    [Tooltip("Audio clip to play when the Wisp charges its attack")]
    public AudioClip? ChargeSFX;

    [Tooltip("Audio clip to play when the Wisp fires")]
    public AudioClip? FireSFX;

    public float MoveSpeed = 8.0f; // Temp so we can manually tweak the speed in the editor

    // Degrees per second
    public float RotationSpeed = 20.0f; // public only so we can manually tweak the speed in the editor

    private WispFireballAttack? attack;
    private Rigidbody? rigidBody;

    // Any single instance of damage that deals more than 10% of the total health will stun it & interrupts its attacks or movement
    private const float stunFromDamageHealthPercentage = 0.1f;
    private const float stunFromDamageDuration = 0.3f; // How long the stun actually lasts
    private float timeOfLastStun = Mathf.NegativeInfinity;
    private const float stunFromDamageCooldown = 1.5f; // Needs to be 1.5 seconds until the last stun ended to stun again

    private State currentState = State.CHASE;

    /* Abstract properties */
    public override string EnemyIdentifier => "Wisp";
    public override float Speed => MoveSpeed; // Probably just hardcode this later

    protected override float StartingHealth => 35f;
    protected override float StartingBaseDamage => 3.5f;
    protected override float HealthIncreasePerLevel => 10f;
    protected override float DamageIncreasePerLevel => 0.7f;

    protected override void Start() {
        base.Start();

        // Needs a RigidBody for colliding with triggers
        Animator animator = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.isKinematic = true;

        attack = new WispFireballAttack(
            target: Target!,
            aimPoint: AimPoint!,
            chargeLineRenderer: ChargeLineRenderer!,
            fireLineRenderers: FireLineRenderers!,
            animator: animator,
            chargeVisualEffect: ChargeVisualEffect!,
            chargeSFX: ChargeSFX!,
            fireSFX: FireSFX!,
            maxAttackDistance: maxPrimaryStrafe + (MoveSpeed * 2.0f) // max attack "range" + how much we can move in 2 seconds (USE_PRIMARY_AND_STRAFE time) 
        );

        health!.OnDamaged += OnDamaged;
    }

    float lastTimePathSet = Mathf.NegativeInfinity;
    const float timeBetweenPathSets = 0.5f;

    protected override void Update() {
        base.Update();

        attack?.OnUpdate(CurrentBaseDamage);

        // What order should these be in? I never really know
        previousState = currentState;
        currentState = DetermineCurrentState();
        PerformCurrentState();
        LookAtTarget();

        // Animate time last hit (if needed)
    }

    // The time until we can change states again
    private float timeOfNextStateChange = -1f; // TODO: Better name

    private const float minPrimaryFlee = 30.0f * 2.0f;
    private const float maxPrimaryStrafe = 40.0f * 2.5f;
    private State previousState = State.CHASE;

    private State DetermineCurrentState() {
        // I figure we should do this first?
        if (isFrozen || IsStunned()) {
            // timeOfNextStateChange = Time.time + 1.0f;
            return State.STUNNED;
        }

        if (Time.time < timeOfNextStateChange) {
            return currentState;
        }

        float distanceToTarget = Vector3.Distance(transform.position, Target!.AimPoint!.position);
        bool hasLineOfSight = DoesHaveLineOfSightToTarget(withinMaxDistance: maxPrimaryStrafe);

        // We shouldn't start trying to use the primary unless we have line of sight
        // (starting by if the player is within FOV)
        // once we start on this we shouldn't move on
        if (hasLineOfSight && distanceToTarget <= minPrimaryFlee) {
            timeOfNextStateChange = Time.time + 0.5f;
            return State.USE_PRIMARY_AND_FLEE;
        } else if (hasLineOfSight && distanceToTarget <= maxPrimaryStrafe) {
            timeOfNextStateChange = Time.time + 2.0f;
            return State.USE_PRIMARY_AND_STRAFE;
        } else {
            timeOfNextStateChange = Time.time + 2.0f;
            return State.CHASE;
        }
    }

    private void PerformCurrentState() {
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
            case State.STUNNED: // Frozen + stunned status effects are both considered .STUNNED as well as from damage 
                // prevent it from attacking
                // TODO: Reset the attack
                attack!.canAttack = false;

                // Prevent moving (by doing nothing)

                break;
            case State.CHASE:
                attack!.canAttack = false;

                DoChase();
                // Look towards where we're moving?
                // We have to look at *something*
                // maybe just lock onto the player?
                LookAtTarget(); // This'll work for now
                break;
        }
    }

    // Somehow I need to know if this hasn't been set yet and reset it
    private Vector3 strafeEndPosition = Vector3.negativeInfinity;

    private void DoStrafe() {
        float distanceToStrafeEndPos = (strafeEndPosition - transform.position).magnitude;
        bool hasReachedStrafeEndPos = distanceToStrafeEndPos <= 0.1f;

        // I honestly don't remember why I added this condition
        // but I think it's so we know we should pick a new strafe position?
        // as these states are the ones we strafe in
        bool didWeNotStrafeInPrevState = previousState != State.USE_PRIMARY_AND_STRAFE && previousState != State.USE_PRIMARY_AND_FLEE;

        // Chose a new strafe position if we need to
        bool shouldChooseNewStrafePosition = hasReachedStrafeEndPos || didWeNotStrafeInPrevState;
        if (shouldChooseNewStrafePosition) {
            if (FindStrafePosition(out Vector3 newPosition)) {
                strafeEndPosition = newPosition;
                CreatePathTo(strafeEndPosition);

            } else { // Couldn't find it (unlikely, hopefully)
                strafeEndPosition = Vector3.positiveInfinity; // Reset it so we're forced to check it next time
                Debug.LogError("Wisp: Couldn't find a valid position to strafe to");
                return;
            }
        }

        TraversePath();
    }

    private bool FindStrafePosition(out Vector3 newStrafePosition) {
        for (int i = 0; i < 10; i++) {
            Vector3 randomMove = Random.insideUnitSphere * 50f;
            randomMove.y = Mathf.Clamp(randomMove.y, -2.0f, 2.0f); // Clamp the y so we don't move too much up & down, just side-to-side
            randomMove += transform.position;

            // Check that this is a valid position that we can move to
            // If we don't do this then we'll end up in invalid positions, e.g. below the map
            // TODO: Should this be exact? Or nearest?
            OctreeNode? nearestNode = OctreeManager.shared!.Octree!.FindClosestValidToPosition(randomMove);

            if (nearestNode == null || nearestNode.containsCollision || !nearestNode.isInBounds) {
                continue;
            }

            newStrafePosition = randomMove;
            return true;
        }

        Debug.LogError("Wisp: Couldn't find a valid position in 10 tries");

        newStrafePosition = Vector3.positiveInfinity;
        return false;
    }

    private void DoChase() {
        // Ensure we're not re-calculating the path too frequently
        // e.g. if the target hasn't moved nearest GraphNodes
        // since changing it at the end would be super easy
        // Since re-calculating the path would probably be pretty time consuming
        if (Time.time - lastTimePathSet > timeBetweenPathSets) {
            CreatePathTo(Target!.AimPoint!.position + (Vector3.one * 3));
            lastTimePathSet = Time.time;
        }

        TraversePath();
    }

    // Make this look at the player if we're aiming at it
    //
    // Should be called when we're attacking the player (USE_PRIMARY_)
    private void LookAtTarget() {
        Vector3 dirToTarget = (Target!.AimPoint!.position - transform.position).normalized;
        Quaternion lookRotationToTarget = Quaternion.LookRotation(dirToTarget);

        // So this isn't great b/c if we're close to the Wisp it takes too long to rotate
        // but when we're far away it's a fucking lock-on and we can't really avoid it, but I think that's fine
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotationToTarget, Time.deltaTime * RotationSpeed);
    }

    private void OnDamaged(float damage, Vector3? _, DamageType __) {
        // If a single piece of damage is > 10% of the Wisp's max health, stun it
        float damageAsPercentOfHealth = damage / health!.MaxHealth;
        if (damageAsPercentOfHealth > stunFromDamageHealthPercentage) {
            currentState = State.STUNNED;
            timeOfLastStun = Time.time;
        }
    }

    private bool DoesHaveLineOfSightToTarget(float withinMaxDistance) {
        // optimization
        float sqrDistToTarget = (Target!.TargetCollider!.bounds.center - AimPoint!.position).sqrMagnitude;
        if (sqrDistToTarget > Mathf.Pow(withinMaxDistance, 2)) {
            // We're not even in fireball range! Return early
            return false;
        }

        // Is it facing it?
        Vector3 dirToPlayer = (Target!.AimPoint!.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

        if (angleToPlayer > 30.0f) {
            return false;
        }

        Vector3 direction = (Target!.TargetCollider!.bounds.center - AimPoint!.position).normalized;

        if (Physics.Raycast(
             origin: AimPoint.position,
             direction: direction,
             out RaycastHit hit,
             maxDistance: withinMaxDistance
         )) {
            if (hit.collider.TryGetEntityFromCollider(out var entity)
                && entity.gameObject == Target!.gameObject
            ) {
                return true;
            }
        }

        return false;
    }

    public override Material GetMaterial() {
        return MainMeshRenderer!.material;
    }

    public override Vector3 GetMiddleOfMesh() {
        // I should honestly just do this manually w/ a Transform
        // Bleed VFX should also probably just use this
        return MainMeshRenderer!.bounds.center;
    }

    protected override bool ColliderCast(Vector3 position, Vector3 startPosition, out RaycastHit? hit) {
        if (Collider == null) {
            hit = null;
            return false;
        }

        Vector3 direction = (position - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, position);

        // We could cache this, but eh
        Vector3 size = Collider.bounds.size;
        float biggestSide = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        if (Physics.SphereCast(
            transform.position,
            radius: biggestSide,
            direction: direction,
            hitInfo: out RaycastHit rayHit,
            maxDistance: distance 
        )) {
            if (rayHit.collider.gameObject != Target!.gameObject) {
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

    protected override void OnDeath() {
        base.OnDeath();

        // Stop the charge SFX in case it's playing
        attack!.OnDeath();

        if (DeathVFX != null) {
            VisualEffect deathVFX = Instantiate(DeathVFX, transform.position, Quaternion.identity);
            deathVFX.Play();

            Destroy(deathVFX.gameObject, 1.0f);
        }
    }

    protected void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minPrimaryFlee);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxPrimaryStrafe);
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
        //
        // Note: This includes the frozen + stunned status effects, as well as being stunned from damage
        STUNNED,

        // Chase the target
        CHASE
    }
}
