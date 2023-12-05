using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.AI;
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
[RequireComponent(typeof(Health), typeof(NavMeshAgent), typeof(Animator))]
[RequireComponent(
    // typeof(Rigidbody), // So it hits triggers collider triggers (e.g. Ice Wall Spike)
    typeof(RigBuilder) // For controlling IK like the head look-at
)] 
public class StoneGolem : MonoBehaviour {

    [Header("References")]
    [Tooltip("The Mesh Rendered used to display this enemy. Used to set properties on its shader (material)")]
    public SkinnedMeshRenderer MainMeshRenderer;

    // We probably don't want this
    [Tooltip("Where the NavMeshAgent is going to navigate to")]
    public GameObject Destination;

    [Tooltip("Where on the Stone Golem we're going to aim its laser from")]
    public Transform AimPoint;

    [Tooltip("Rotation speed when the target is within NavMeshAgent's stopping distance")]
    public float RotationSpeed = 0.1f;

    // TODO: We might want this to be in here instead of Health, but this is fine for now
    // [Tooltip("Sound that plays on damaged")]
    // public AudioClip OnDamageClip;

    private NavMeshAgent navMeshAgent;
    private Health health;
    private Animator animator;

    private Vector3 lastTargetPosition = Vector3.positiveInfinity;

    // Used for synchronizing between Animator's root motion and the NavMeshAgent
    private Vector2 velocity;
    private Vector2 smoothDeltaPosition;
    private const float isMovingMin = 0.5f;

    private EnemyManager enemyManager;

    private float lastDamagedTime = Mathf.Infinity;

    private void OnAnimatorMove() {
        Vector3 rootPosition = animator.rootPosition;
        // gotta ensure it matches the height
        rootPosition.y = navMeshAgent.nextPosition.y;
        transform.position = rootPosition;
        navMeshAgent.nextPosition = rootPosition;

        // Set rotation if animator includes rotations here if needed, same as above
    }

    void Start() {
        health = GetComponent<Health>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager == null) {
            Debug.LogError("Couldn't find EnemyManager!");
        }

        enemyManager.AddEnemy(this.gameObject);

        animator.applyRootMotion = true;
        // Want animator to drive movement, not agent
        navMeshAgent.updatePosition = false;
        // So it's aligned where we're going (he has notes later in the video for setting this to false / when)
        navMeshAgent.updateRotation = true;

        // Don't need
        if (navMeshAgent == null) {
            Debug.LogError("Stone Golem was not passed a NavMeshAgent!!");
        }

        if (Destination) {
            navMeshAgent.SetDestination(Destination.transform.position);
        }

        health.OnDamaged += OnDamaged;
        health.OnDeath += OnDeath;
        health.EntityMaterial = MainMeshRenderer.material;
    }

    void Update() {
        // Search for the player

        // If we have a target, do pathfinding? (Ensure we're in a good spot to shoot)

        // See if we have enough charge and search for the player
        SetPosition();

        SynchronizeAnimatorAndAgent();

        RotateToTargetWhenWithinStoppinDistance();

        // Set the animator's TimeSinceLastDamaged
        if (lastDamagedTime < Mathf.Infinity) {
            animator.SetFloat("TimeSinceLastDamaged", (Time.time - lastDamagedTime));
        } else {
            // Not positive if we need to do this
            animator.SetFloat("TimeSinceLastDamaged", Mathf.Infinity);
        }
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

        // TODO: Rename this cause I'm still not 100% sure what this boolean does
        // This 2f is a ratio which we need to play with
        bool isWithinNavMeshRadius = worldDeltaPosition.magnitude > navMeshAgent.radius / 2f;
        if (isWithinNavMeshRadius) {
            // Move position between where the animator root position and where the nav mesh agent can go
            // Without this the thing might walk through things not on the nav mesh
            transform.position = Vector2.Lerp(
                animator.rootPosition,
                navMeshAgent.nextPosition,
                smooth
            );
        }
    }

    // Remove this it was just for testing
    private void SetPosition() {
        float dist = Vector3.Distance(Destination.transform.position, lastTargetPosition);
        float minDist = 0.5f;

        if (dist > minDist) {
            navMeshAgent.SetDestination(Destination.transform.position);

            lastTargetPosition = Destination.transform.position;
        }
    }

    // All Enemies need to do this - we probably can do this in the abstract Enemy class
    private void OnDeath() {
        enemyManager.RemoveEnemy(this.gameObject);

        Destroy(this.gameObject);
    }

    private void OnDamaged(float damage, Vector3 damagedPos) {
        lastDamagedTime = Time.time;
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
}
