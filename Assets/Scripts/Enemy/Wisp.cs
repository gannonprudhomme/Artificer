using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

public class WispFireballAttack: EnemyAttack {
    private float entityBaseDamage;

    private const float chargeDuration = 1.6f; // 1.6 seconds to charge/aim (before firing)
    // private const float cooldown = 2.0f; // 2 second end "lag"
    private const float cooldown = 4.0f;


    public override void OnUpdate(float entityBaseDamage) {
        if (!canAttack) {
            // Reset attack cooldown and stuff
            return;
        }
        
        this.entityBaseDamage = entityBaseDamage;
    }
}


public class Wisp : NavSpaceEnemy {
    [Header("References (Wisp)")]
    public MeshRenderer? MainMeshRenderer;

    public float _Speed = 6.0f; // Temp so we can manually tweak the speed in the editor

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
    public override float Speed => _Speed; // Probably just hardcode this later

    protected override void Start() {
        base.Start();

        attack = new WispFireballAttack();

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
        Vector3 dirToTarget = Target.AimPoint.position - transform.position;
        transform.rotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
    }

    // Make the wisp look towards it's path (forwards)
    //
    // Should be called when we're chasing the player
    private void LookTowardsPath() {

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
