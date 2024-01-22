using System.Collections;
using System.Collections.Generic;
using Codice.Client.Commands.TransformerRule;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Search;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.VFX;

#nullable enable

public abstract class GroundedEnemy: Enemy {
    // Only relevant for things that shoot projectiles!
    // [Tooltip("Where on the Enemy we're going to shoot projectiles from")]
    // public Transform AimPoint;

    // [Tooltip("Where the NavMeshAgent is going to navigate to. Should be the player")]
    // public Transform Target;
}

[RequireComponent(
    typeof(NavMeshAgent),
    typeof(Animator),
    typeof(RigBuilder)
)]
public class Lemurian : Enemy {
    [Header("References")]
    [Tooltip("The mesh renderer used to display this")]
    public SkinnedMeshRenderer? MainMeshRenderer;

    [Tooltip("Where on the Lemurian we're going to shoot projectiles")]
    public Transform? AimPoint;

    [Tooltip("Reference to the AimTarget PositionConstraint for the look-at constraint. Needed so we can set it to copy the location of the player")]
    public PositionConstraint PositionConstraint;

    //[Tooltip("Rotation speed when the target is within NavMeshAgent's stopping distance")]
    //public float RotationSpeed = 0.1f;

    [Tooltip("Prefab for the fireball attack projectile")]
    public LemurianFireballProjectile? FireballProjectilePrefab;

    [Tooltip("Reference to the melee (swipe) particle system instance")]
    public ParticleSystem? MeleeParticleSystemInstance;

    [Tooltip("The list of colliders on this so we can disable all of them when this dies")]
    public List<BoxCollider> Colliders;

    public VisualEffect? FireballChargeVisualEffectInstance;

    private NavMeshAgent? navMeshAgent;
    private Animator? animator;

    // This isn't *really* optional since it's assigned in Start()
    private LemurianFireballAttack? fireballAttack;
    private LemurianMeleeAttack? meleeAttack;

    protected override float StartingBaseDamage => 12;
    public override float CurrentBaseDamage => StartingBaseDamage;

    // The point where the lemurian is strafing to
    private Vector3 strafePosition = Vector3.negativeInfinity;

    private State currentState = State.CHASE;

    // Used for synchronizing between Animator's root motion and the NavMeshAgent
    private Vector2 velocity;
    private Vector2 smoothDeltaPosition;
    private const float isMovingMin = 0.5f;

    private LayerMask lemurianMask;

    private const float minSecondaryDistance = 18.0f; // original is 6
    private const float minPrimaryDistance = 25.0f; // original is 15

    // For all intents and purposes, this can be considered the range of the fireball attack
    private const float maxPrimaryDistance = 60.0f; // original is 30

    // How far from the next strafe point we can be before stopping (and thus choosing a new strafe point)
    private const float strafeStoppingDistance = 0.5f;

    // How far from the chase target (player) we can be before stopping
    private const float chaseStoppingDistance = 3.5f;
    public override string EnemyIdentifier => "Lemurian";

    private const string ANIM_PARAM_IS_DEAD = "IsDead";

    private void OnAnimatorMove() {
        Vector3 rootPosition = animator!.rootPosition;
        // gotta ensure it matches the height
        rootPosition.y = navMeshAgent!.nextPosition.y;
        transform.position = rootPosition;
        navMeshAgent.nextPosition = rootPosition;

        // Set rotation if animator includes rotations here if needed, same as above
    }

    protected override void Start() {
        base.Start();

        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        lemurianMask = LayerMask.GetMask("Lemurian");

        animator!.SetBool(ANIM_PARAM_IS_DEAD, false);

        SetDestination();
        ConfigureAnimatorAndNavMeshAgent();

        fireballAttack = new(FireballProjectilePrefab!, FireballChargeVisualEffectInstance!, animator!, this.gameObject, Target.AimPoint, AimPoint!);
        meleeAttack = new(MeleeParticleSystemInstance!, this.gameObject, AimPoint, animator!, Target, lemurianMask!);

	    health!.OnDamaged += OnDamaged;
    }

    protected override void Update() {
        base.Update();

        if (health!.IsDead) { return; }

        // Sometimes this runs after Destroy is called,
        // so prevent that from happening
        if (!gameObject.activeSelf) { return; }

        if (isFrozen) {
            navMeshAgent!.isStopped = true;
            animator!.speed = 0;
            fireballAttack!.canAttack = false;

            PositionConstraint.constraintActive = false;

        } else { // Not frozen, we can move!
            navMeshAgent!.isStopped = false;
            animator!.speed = 1.25f;
            fireballAttack!.canAttack = true;

            PositionConstraint.constraintActive = true;

            SynchronizeAnimatorAndAgent();

            // RotateToTargetWhenWithinStoppinDistance();
		}

        currentState = DetermineCurrentState();
        PerformCurrentState();

        fireballAttack!.OnUpdate(CurrentBaseDamage);
        meleeAttack!.OnUpdate(CurrentBaseDamage);
    }

    // Controls the behavior of what this entity is doing
    private State DetermineCurrentState() {
        float distanceFromTarget = Vector3.Distance(transform.position, Target.AimPoint.position);
        bool hasLineOfSightToTarget = DoesHaveLineOfSightToTarget();

        if (!hasLineOfSightToTarget) {
            // if (currentState != State.CHASE) Debug.Log("Changing to chase b/c no line of sight");

            return State.CHASE;
        }

        if (distanceFromTarget <= 3.0f) {
            // if (currentState != State.USE_SECONDARY_AND_CHASE_SLOWING_DOWN) Debug.Log("Changing to use secondary & chase slowing down");

            return State.USE_SECONDARY_AND_CHASE_SLOWING_DOWN;
        } else if (distanceFromTarget <= minSecondaryDistance) {
            // if (currentState != State.USE_SECONDARY_AND_CHASE) Debug.Log("Changing to use secondary & chase");

            return State.USE_SECONDARY_AND_CHASE;
        } else if (distanceFromTarget >= minPrimaryDistance && distanceFromTarget <= maxPrimaryDistance) {
            // if (currentState != State.USE_PRIMARY_AND_STRAFE) Debug.Log("Changing to primary & strafe");

            return State.USE_PRIMARY_AND_STRAFE;
        // } else if (distanceFromTarget <= 7.0f) {
        //    if (currentState != State.CHASE_OFF_NODEGRAPH) Debug.Log("Changing to chase off node graph");

        //    return State.CHASE_OFF_NODEGRAPH;
        } else { // Only chase if we have line of sight and dist is >= 30.0f
            // if (currentState != State.CHASE) Debug.Log($"Changing to chase b/c dist is {distanceFromTarget}");

            // Debug.Log($"Chasing b/c distance is {distanceFromTarget}");
            return State.CHASE;
        }
    }

    // Given the result of DetermineCurrentState(), act on it
    private void PerformCurrentState() {
        switch (currentState) {
            // Do same as below, but reduce speed?
            // Decrease speed I guess?
            // fallthrough for now
            case State.USE_SECONDARY_AND_CHASE_SLOWING_DOWN:
            case State.USE_SECONDARY_AND_CHASE:
                if (isFrozen) break; // Might want to put this at the top

                fireballAttack!.canAttack = false;
                meleeAttack!.canAttack = true;

                // Should probably rename to BeginAttackIfPossible
                meleeAttack!.BeginAttack();

                DoChase();
                break;

            case State.STRAFE_WHILE_CHARGING_PRIMARY: // Falthrough for now
            case State.USE_PRIMARY_AND_STRAFE:
                if (isFrozen) break;

                fireballAttack!.canAttack = true;
                meleeAttack!.canAttack = false;

                fireballAttack!.StartCharging();

                DoStrafe();

                break;
            case State.CHASE_OFF_NODEGRAPH: // Fallthrough for now
            case State.CHASE:
                if (isFrozen) break;

                fireballAttack!.canAttack = false;
                meleeAttack!.canAttack = false;

                DoChase();

                break;
        }
    }

    private void DoStrafe() {
        // avMeshAgent!.updateRotation = false;
        navMeshAgent!.stoppingDistance = strafeStoppingDistance;

        const float lemurianHeight = 2.57f; // Should retrieve dynamically
        float distToStrafePosition = Vector3.Distance(transform.position, strafePosition);

        // Ensure we're not including the height in the distance calculation by subtracting it
        bool hasReachedStrafePosition = (distToStrafePosition - lemurianHeight) <= 0.5f; // Random value

        // Arbitrary values
        const float maxHorizontalStrafeDistance = 15.0f;
        const float maxForwardStrafeDist = 7.0f;

        // Chose a strafe position if we need to (we've reached it or it hasn't been set)
        if (hasReachedStrafePosition || strafePosition.x == Mathf.NegativeInfinity) {
            // Choose a strafe position
            
            // Convert to [-0.5f, 0.5f] then multiply by strafe dist
            float deltaRight = (Random.value - 0.5f) * (maxHorizontalStrafeDistance * 2.0f);
            // Convert it to [-0.2f, 0.8f] and multiply by forward strafe dist
            float deltaForward = (Random.value - 0.2f) * (maxForwardStrafeDist);

            // Find the right vector based on the target's position
            Vector3 dirToTarget = (Target.AimPoint.position - transform.position).normalized;
            Vector3 rightDir = Vector3.Cross(dirToTarget, Vector3.up); // Do we have to normalize this?

            Vector3 targetPos = transform.position + (rightDir * deltaRight);
            // Make it move forward / back
            targetPos += (dirToTarget * deltaForward);

            // SamplePosition's maxiumumDistance is recommended to be twice the agent's height

            if(NavMesh.SamplePosition(targetPos, out NavMeshHit hit, lemurianHeight * 2, -1)) { // Try to find a position close to it on the navmesh
                strafePosition = hit.position;
                navMeshAgent!.SetDestination(strafePosition);
            } else {
                Debug.LogError($"Lemurian: Can't find closest point from {targetPos}");
                // Force it to re-chose next time
                strafePosition = Vector3.negativeInfinity;
            }
        }
    }

    private void DoChase() {
        // Reset strafe position
        strafePosition = Vector3.negativeInfinity;
        navMeshAgent!.updateRotation = true;
        navMeshAgent!.stoppingDistance = chaseStoppingDistance;

        // Set the destination to be at the player
        // Set stopping distance
        SetDestination();
    }
    private void ConfigureAnimatorAndNavMeshAgent() {
        animator.applyRootMotion = true;
        // Want animator to drive movement, not agent
        navMeshAgent!.updatePosition = false;
        // So it's aligned where we're going (he has notes later in the video for setting this to false / when)
        navMeshAgent.updateRotation = true;

        if (Target) {
            navMeshAgent.SetDestination(Target.transform.position);

            // Make the TargetAim PositionConstraint copy the Destination's location
            // so the MultiAimConstraint correctly looks at the Destination (the player)
            ConstraintSource constraintSource = new() {
                sourceTransform = Target.AimPoint,
                weight = 1.0f
            };
            PositionConstraint.SetSource(0, constraintSource);

            // Zero the offset so it matches the position of the target (the player)
            PositionConstraint.translationOffset = Vector3.zero;
        } 
    }

    private void SynchronizeAnimatorAndAgent() {
        Vector3 worldDeltaPosition = navMeshAgent!.nextPosition - transform.position;
        worldDeltaPosition.y = 0;

        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new(dx, dy);

        float smooth = Mathf.Min(1, Time.deltaTime / 0.1f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        velocity = smoothDeltaPosition / Time.deltaTime;

        // So we perfectly come to a stop at the end of the path
        if (navMeshAgent!.remainingDistance <= navMeshAgent!.stoppingDistance) {
            velocity = Vector2.Lerp(
                Vector2.zero,
                velocity,
                navMeshAgent.remainingDistance / navMeshAgent.stoppingDistance
            );
        }

        bool shouldMove = velocity.magnitude > isMovingMin && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance;

        animator.SetBool("IsMoving", shouldMove);
        animator.SetFloat("MovementSpeed", velocity.magnitude); // Based on 1D blend tree, need to pass in velocity.x & velocity.y separately for 2D

        // This is causing a bug
        // TODO: Rename this cause I'm still not 100% sure what this boolean does
        // This 2f is a ratio which we need to play with
        /*
        bool isWithinNavMeshRadius = worldDeltaPosition.magnitude > navMeshAgent.radius / 2f;
        if (isWithinNavMeshRadius) {
            print("Within nav mesh radius or something");
            // Move position between where the animator root position and where the nav mesh agent can go
            // Without this the thing might walk through things not on the nav mesh
            transform.position = Vector2.Lerp(
                animator.rootPosition,
                navMeshAgent.nextPosition,
                smooth
            );
        }
        */
    }


    private void SetDestination() { 
        if (Target) {
            navMeshAgent!.SetDestination(Target.transform.position);
		}
    }

    private void OnDamaged(float damage, Vector3 damagePosition, DamageType damageType) {
        // if damage was > 15% of max health, Lemurian should be stunned (but for how long?)
	}

    protected override void OnDeath() {
        // Note we're intentionally not calling base.OnDeath() cause we don't want to Destroy this (yet)

        // I do think we want to do this?
        EnemyManager.shared.RemoveEnemy(this);

        animator.SetBool(ANIM_PARAM_IS_DEAD, true);

        PositionConstraint.constraintActive = false;

        foreach(var collider in Colliders) {
            collider.enabled = false;
        }

        Destroy(this.gameObject, 5f); // Destroy it in 5 seconds I guess? Probably should have an effect but w/e
    }

    // Returns true if we have line of sight to the target
    private bool DoesHaveLineOfSightToTarget() {
        // First check if the player is within the range (maxPrimaryDistance)
        // no point in doing a RayCast if they're not!
        // (Optimization)
        float sqrDistToTarget = (Target.TargetCollider.bounds.center - AimPoint!.position).sqrMagnitude;
        if (sqrDistToTarget > Mathf.Pow(maxPrimaryDistance, 2)) {
            // We're not even in fireball range! Return early
            return false;
        }

        // TODO: Do a "frustum check" so this returns false if the Lemurian isn't facing the right way around? (can't be facing backwards)
        // though this might conflict with the strafing, and the LookAt constraint might fuck this up a bit too

        // Didn't work when it was set to Target.AimPoint so just doing the center of the collider
        // Vector3 direction = Target.AimPoint.position - AimPoint!.position;
        Vector3 direction = (Target.TargetCollider.bounds.center - AimPoint!.position).normalized;

        // TODO: Shit this is going to ignore all Lemurians, we need to do better than this
        // We need this to ignore itself
        if (Physics.Raycast(
            AimPoint!.position,
            direction,
            out RaycastHit hit,
            Mathf.Infinity,
            ~lemurianMask // Ignore its own colliders...shit this also ignores other Lemurians
        )) {
            if (hit.collider == Target.TargetCollider) {
                return true;
            }
        }

        return false;
    }
    public override Material? GetMaterial() {
        return MainMeshRenderer!.material;
    }

    public override Vector3 GetMiddleOfMesh() {
        return MainMeshRenderer!.bounds.center;
    }

    enum State {
        // Must be within 3m from its target and have line of sight.
        // Secondary is not necessary to be off cooldown and it will chase its target directly w/o relying on the node graph points
        // Lasts for 0.5 sec
        USE_SECONDARY_AND_CHASE_SLOWING_DOWN,
        // Same requirements as above,
        // only diff is that the lemurian must be within 6m from its target and it will chase at normal speed
        USE_SECONDARY_AND_CHASE,
        // Must be within 15m - 30m from its target and have line of sight
        USE_PRIMARY_AND_STRAFE,
        // Must be within 15m-30m from its target and line of sight
        // basically same as above
        STRAFE_WHILE_CHARGING_PRIMARY,
        // Must be within 7m from its target and have line of sight
        CHASE_OFF_NODEGRAPH,
        // No requirements
        CHASE
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minSecondaryDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minPrimaryDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxPrimaryDistance);

        // Visualize melee attack range
        // Gizmos.color = Color.yellow;
        // float hitRange = LemurianMeleeAttack.hitRange;
        // Vector3 origin = AimPoint!.position + (Vector3.forward * (hitRange / 2.0f));
        // Gizmos.DrawSphere(origin, hitRange);
    }
}
