using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Apple;

public interface IEnemy
{
    // RequireComponent Health
    // Might need access to the Player, but hopefully not? It just has to be able to target something which can get damaged
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
    // We probably don't want this
    [Tooltip("Where the NavMeshAgent is going to navigate to")]
    public GameObject Destination;

    public float BaseMoveSpeed = 5f;

    // we have to set this because of the origin being weird and this not being centered. Annoying.
    private NavMeshAgent navMeshAgent;
    private Health health;
    private Animator animator;

    // Theoretically should set this up for multiplayer, but that is hard
    // We might need a guarantee that this has a Health that we can damage
    private Transform currentTarget;

    private Vector3 lastTargetPosition = Vector3.positiveInfinity;

    private static int ANIMATOR_PARAM_MOVEMENT_SPEED = Animator.StringToHash("MovementSpeed");
    
    private static int ANIMATOR_PARAM_IS_MOVING = Animator.StringToHash("IsMoving");

    private Vector2 velocity;
    private Vector2 smoothDeltaPosition;

    private const float isMovingMin = 0.5f;

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
            print("Setting destination");
            navMeshAgent.SetDestination(Destination.transform.position);
        } else {
            print("Destination not passed");
        }
    }

    void Update() {
        // Search for the player

        // If we have a target, do pathfinding? (Ensure we're in a good spot to shoot)

        // See if we have enough charge and search for the player
        SetPosition();

        SynchronizeAnimatorAndAgent();
    }

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
        float minDist = 5f;

        if (dist > minDist) {
            navMeshAgent.SetDestination(Destination.transform.position);

            lastTargetPosition = Destination.transform.position;
        }
    }
}
