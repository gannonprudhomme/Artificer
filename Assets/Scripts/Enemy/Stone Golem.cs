using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Apple;

/*
[RequireComponent(
    typeof(Health),
    typeof(NavMeshAgent),
    typeof(Animator) // I think all will need this?
)]

public abstract class Enemy: MonoBehaviour {
    [Tooltip("The GameObject of the player we're going to attack")]
    public GameObject TargetForNavigation;

    [Tooltip("The Mesh Rendered used to display this enemy. Used to set properties on its shader (material)")]
    public SkinnedMeshRenderer MainMeshRenderer;

    // I guess this is going to be on Health rather than here?
    // I could see it work for either, but Health probably makes sense since it has to apply to the player
    public List<StatusEffect> statusEffects { get; set; } 

    // should call EnemyManager.RemoveEnemy(this);
    // then destroy itself
    public void Die();
}
*/

// This (mainly Enemy) needs to:
// - Aim at the player, and shoot when it can
//   - The shooting shouldn't be fast enough where the player can't dodge it - it should be doable if they're thinking about it (or really fast)
// - Know how to pathfind in general
// - Take damage + play damage effects
// - (Eventually) handle its own status effects, e.g. burning. Might want to combine this with the player's logic
// 
// It doesn't need to:
// - Have complex stealth-game-esque entity logic for searching/investigating for the player
// 
// Nice to haves:
// - Have "idle" pathfinding that it does when it doesn't know where the player is
// - Jumping (I really don't know when it does this)
// - Melee attack ("clap")
// - More complex logic where it chooses where to stop (or backup) for the lazer,
//   know how to pathfind with the lazer in mind (?)
// 
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
[RequireComponent(
    // typeof(Rigidbody), // So it hits triggers collider triggers (e.g. Ice Wall Spike)
    typeof(RigBuilder), // For controlling IK like the head look-at
    typeof(LineRenderer) // For displaying the laser
)] 
public class StoneGolem : Enemy {

    [Header("References")]
    [Tooltip("The Mesh Rendered used to display this enemy. Used to set properties on its shader (material)")]
    public SkinnedMeshRenderer MainMeshRenderer;

    // We probably don't want this
    [Tooltip("Where the NavMeshAgent is going to navigate to")]
    public Transform Destination;

    [Tooltip("Where on the Stone Golem we're going to aim its laser from")]
    public Transform AimPoint;

    [Tooltip("Reference to the HealthBar. Needed so we can set the target on the health bar (so it looks at the player)")]
    public EnemyHealthBar healthBar;

    [Tooltip("Reference to the AimTarget PositionConstraint for the look-at constraint. Needed so we can set it to copy the location of the player")]
    public PositionConstraint positionConstraint;

    [Header("Navigation")]
    [Tooltip("Rotation speed when the target is within NavMeshAgent's stopping distance")]
    public float RotationSpeed = 0.1f;

    [Header("Laser Attack")]
    [Tooltip("Audio clip that plays when the lasfalseer begins charging the laser (starts the attack)")]
    public AudioClip LaserChargeSfx;


    [Tooltip("Audio clip that plays when the laser is finished charging & fires")]
    public AudioClip LaserFireSfx;

    [Tooltip("The curve which controls the size of the laser when it fires")]
    public AnimationCurve LaserSizeCurve;

    // TODO: Remove this later, it was just for testing
    public float AimMoveSpeed = 15f;

    // TODO: We might want this to be in here instead of Health, but this is fine for now
    // [Tooltip("Sound that plays on damaged")]
    // public AudioClip OnDamageClip;

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private LineRenderer laserLineRenderer;

    private Vector3 lastTargetPosition = Vector3.positiveInfinity;

    // Used for synchronizing between Animator's root motion and the NavMeshAgent
    private Vector2 velocity;
    private Vector2 smoothDeltaPosition;
    private const float isMovingMin = 0.5f;


    private float lastDamagedTime = Mathf.Infinity;

    // I should probably
    // Used so I don't reset the Animator's speed back to 0 / 1 every time canMove is true
    // as well as Play() / Stop() for nav mesh agent
    // I could use a UnityAction for this, but this is less code
    private bool couldMoveLastFrame = true;

    // Attacks
    private GolemLaserAttack laserAttack;

    private void OnAnimatorMove() {
        Vector3 rootPosition = animator.rootPosition;
        // gotta ensure it matches the height
        rootPosition.y = navMeshAgent.nextPosition.y;
        transform.position = rootPosition;
        navMeshAgent.nextPosition = rootPosition;

        // Set rotation if animator includes rotations here if needed, same as above
    }

    protected override void Start() {
        base.Start();

        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        laserLineRenderer = GetComponent<LineRenderer>();

        laserAttack = new GolemLaserAttack(laserLineRenderer, AimPoint, Destination, LaserSizeCurve, LaserChargeSfx, LaserFireSfx);

        animator.applyRootMotion = true;
        // Want animator to drive movement, not agent
        navMeshAgent.updatePosition = false;
        // So it's aligned where we're going (he has notes later in the video for setting this to false / when)
        navMeshAgent.updateRotation = true;

        if (Destination) {
            navMeshAgent.SetDestination(Destination.transform.position);
            healthBar.Target = Destination;

            // Make the TargetAim PositionConstraint copy the Destination's location
            // so the MultiAimConstraint correctly looks at the Destination (the player)
            ConstraintSource constraintSource = new() {
                sourceTransform = Destination,
                weight = 1.0f
            };
            positionConstraint.SetSource(0, constraintSource);

            // Zero the offset so it matches the position of the target (the player)
            positionConstraint.translationOffset = Vector3.zero;
        } 

        health.OnDamaged += OnDamaged;
    }

    protected override void Update() {
        base.Update();

        // Search for the player

        // If we have a target, do pathfinding? (Ensure we're in a good spot to shoot)

        // See if we have enough charge and search for the player
        SetNavMeshDestination();

        if (!isFrozen) {
            // If we can move this frame but couldn't last frame
            if (!couldMoveLastFrame) {
                couldMoveLastFrame = true;
                // start up the animator & nav mesh agent
                animator.speed = 1;
                navMeshAgent.isStopped = false;
                
                // Make LookAt start working again (by moving the target, aka the player)
                positionConstraint.constraintActive = true;
            }

            SynchronizeAnimatorAndAgent();

            RotateToTargetWhenWithinStoppinDistance();
        } else if (couldMoveLastFrame) { // if we could move last frame but can't now
            couldMoveLastFrame = false;

            // stop animator & nav mesh agent
            animator.speed = 0;
            navMeshAgent.isStopped = true;

            // Stop the LookAt from moving by keeping the target in place
            positionConstraint.constraintActive = false;
        }

        // Set the animator's TimeSinceLastDamaged
        if (lastDamagedTime < Mathf.Infinity) {
            animator.SetFloat("TimeSinceLastDamaged", (Time.time - lastDamagedTime));
        } else {
            // Not positive if we need to do this
            animator.SetFloat("TimeSinceLastDamaged", Mathf.Infinity);
        }

        laserAttack.aimSpeed = AimMoveSpeed;
        laserAttack.OnUpdate();
    }

    public override Vector3 GetMiddleOfMesh() {
        return MainMeshRenderer.bounds.center;
    }


    // Synchronize the Animator's RootMotion with the NavMeshAgent
    // TODO: How does this work?
    // 
    // Source: https://www.youtube.com/watch?v=uAGjKxH4sDQ
    private void SynchronizeAnimatorAndAgent() {
        Vector3 worldDeltaPosition = navMeshAgent.nextPosition - transform.position;
        worldDeltaPosition.y = 0;

        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new(dx, dy);

        float smooth = Mathf.Min(1, Time.deltaTime / 0.1f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        velocity = smoothDeltaPosition / Time.deltaTime;

        // So we perfectly come to a stop at the end of the path
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) {
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

    // Remove this it was just for testing
    private void SetNavMeshDestination() {
        float dist = Vector3.Distance(Destination.position, lastTargetPosition);
        float minDist = 0.5f;

        if (dist > minDist) {
            navMeshAgent.SetDestination(Destination.position);

            lastTargetPosition = Destination.position;
        }
    }

    private void OnDamaged(float damage, Vector3 damagedPos) {
        lastDamagedTime = Time.time;
    }

    public override void SetIsFrozen(bool isFrozen) {
        base.SetIsFrozen(isFrozen);

        // May not be the best way to do this, idrk
        laserAttack.canAttack = !isFrozen;
    }

    // When the NavMeshAgent is within it's stopping distance, it won't rotate anymore.
    // This rotates the Golem when it's within the stoping distance.
    private void RotateToTargetWhenWithinStoppinDistance() {
        float rotationSpeed = RotationSpeed;
        float stoppingDistance = navMeshAgent.stoppingDistance;
        float distanceToTarget = Vector3.Distance(transform.position, navMeshAgent.destination);
        
        if (distanceToTarget < stoppingDistance) {
            // Do the movement

            Vector3 direction = (navMeshAgent.destination - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void OnDrawGizmos() {
        if (laserAttack != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(laserAttack.lastEndAimPoint, 1.0f);
        }
    }
    public override Material GetMaterial() {
        return MainMeshRenderer.material;
    }
}
