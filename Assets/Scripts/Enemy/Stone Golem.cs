using System.Collections;
using System.Collections.Generic;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEngine.AI;

/*
public interface IEnemy
{
    // RequireComponent Health
    // Might need access to the Player, but hopefully not? It just has to be able to target something which can get damaged
}
*/

// This needs to:
// - Aim at the player, and shoot when it can
//   - The shooting shouldn't be fast enough where the player can't dodge it - it should be doable if they're thinking about it (or really fast)
// - Know how to pathfind in general
// - Take damage + play damage effects
// - (Eventually) handle its own status effects, e.g. burning. Might want to combine this with the player's logic
// 
// It doesn't need to:
// - Have stealth-game-esque entity logic for searching/investigating for the player
// 
// Nice to haves:
// - Have "idle" pathfinding that it does when it doesn't know where the player is
// - Jumping (I really don't know when it does this)
// - Melee attack ("clap")
// - More complex logic where it chooses where to stop (or backup) for the lazer,
//   know how to pathfind with the lazer in mind (?)
// 
[RequireComponent(typeof(Health), typeof(NavMeshAgent), typeof(Animator))]
public class StoneGolem : MonoBehaviour {

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

    private static int ANIMATOR_PARAM_WALK_SPEED = Animator.StringToHash("WalkSpeed");

    // Range of [0, 1]
    // Should only apply to walking animation, not idle?
    private static int ANIMATOR_PARAM_WALK_SPEED_MODIFIER = Animator.StringToHash("WalkSpeedModifier");

    private float walkSpeedModifier {
        get {
            return navMeshAgent.velocity.magnitude / BaseMoveSpeed;
        }
    }

    void Start() {
        health = GetComponent<Health>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        navMeshAgent.speed = BaseMoveSpeed;

        // Set it to 1 at the beginning so it doesn't prevent movement completely? Idk
        // this.animator.SetFloat(ANIMATOR_PARAM_WALK_SPEED_MODIFIER, 1.0f);

        // Don't need
        if (navMeshAgent == null) {
            Debug.LogError("Stone Golem was not passed a NavMeshAgent!!");
        }

        if (Destination) {
            print("Setting destination");
            navMeshAgent.SetDestination(Destination.transform.position);
        } else
        {
            print("Destination not passed");
        }
    }

    void Update() {
        // Search for the player

        // If we have a target, do pathfinding? (Ensure we're in a good spot to shoot)

        // See if we have enough charge and search for the player
        SetPosition();
    }

    private void LateUpdate() {
        float speed = this.navMeshAgent.velocity.magnitude;
        print(speed);
        this.animator.SetFloat(ANIMATOR_PARAM_WALK_SPEED, speed);
        this.animator.SetFloat(ANIMATOR_PARAM_WALK_SPEED_MODIFIER, walkSpeedModifier);
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
