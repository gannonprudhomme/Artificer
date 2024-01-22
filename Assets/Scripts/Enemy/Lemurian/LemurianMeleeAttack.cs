using System.Collections;
using System.Collections.Generic;
using PlasticGui.WorkspaceWindow;
using UnityEngine;

// Has an attack animation of 1 sec (presumably it actually "attacks" at the end of it)
// and has a cooldown of 1 second.
// Deals 200% base damage
public class LemurianMeleeAttack: EnemyAttack {
    // Instance of the swipe particle system
    // we Play it 
    private readonly ParticleSystem SwipeParticleInstance;
    // Transform of the target so we can see if we hit them
    private readonly Target Target;

    // Animator of the Lemurian so we can control the Melee animation
    private readonly Animator animator;
    // Reference to the lemurian (owner) so we can get distance to target
    // and spawn the particle system under it
    private readonly GameObject Owner;

    // The AimPoint of the owner, where we "spawn" melee attack hits from
    private readonly Transform OwnerAimPoint;

    private const float DamageCoefficient = 2.0f; // 200% of base damage
    private float entityBaseDamage = 0.0f;

    private bool isInMiddleOfAttack = false;
    // Time the current attack started
    private float timeOfAttackStart = Time.time;
    // Time the last attack finished
    private float timeOfLastAttackFinish = Time.time;

    // How long in seconds it takes for this attack to "fire" / trigger from when it began
    private const float attackDuration = 1.0f;
    // How long in seconds we need to wait until we can begin an attack again
    private const float cooldownDuration = 1.0f;

    // The radius of the hit-sphere (hit-box) when we do the actual hit/attack
    // Set arbitrarily based off of the swipe particle system's size (so it feels right)
    private static readonly float hitRange = 2.0f;

    // The minimum distance we need to be from the target in order to attack
    // I set this value pretty arbitrarily just from playing around
    private const float minimumBeginAttackDistance = 7.5f; // Should be half of Lemurian.minSecondaryDist maybe?

    private LayerMask lemurianMask;

    private const string ANIM_IS_CHARGING_MELEE = "IsChargingMelee";
    private const string ANIM_IS_FIRING_MELEE = "IsFiringMelee";

    public LemurianMeleeAttack(
        ParticleSystem swipeParticleInstance,
        GameObject owner,
        Transform ownerAimPoint,
        Animator animator,
        Target target,
        LayerMask lemurianMask
    ) {
        this.SwipeParticleInstance = swipeParticleInstance;
        this.Owner = owner;
        this.animator = animator;
        this.OwnerAimPoint = ownerAimPoint;
        this.Target = target;
        this.lemurianMask = lemurianMask;

        animator.SetBool(ANIM_IS_CHARGING_MELEE, false);
        animator.SetBool(ANIM_IS_FIRING_MELEE, false);
    }

    public override void OnUpdate(float entityBaseDamage) {
        base.OnUpdate(entityBaseDamage);

        this.entityBaseDamage = entityBaseDamage;

        if (!canAttack) {
            ResetAttack();
            return;
        }

        // Handle the middle of the attack
        if (isInMiddleOfAttack) {
            HandleAttack();
        }
    }

    // Begin the attack
    public void BeginAttack() {
        if (!CanBeginAttack()) {
            return;
        }

        animator.SetBool(ANIM_IS_CHARGING_MELEE, true);
        animator.SetBool(ANIM_IS_FIRING_MELEE, false);

        // Start the animation
        isInMiddleOfAttack = true;
        timeOfAttackStart = Time.time;
    }

    // We're in the middle of the attack animation, handle it
    private void HandleAttack() {
        // See how long it's been, and if we've reached the 1 sec mark, do the attack
        float timeSinceAttackStart = Time.time - timeOfAttackStart;
        bool isReadyToTrigger = timeSinceAttackStart >= attackDuration; // aka isReadyToFire

        if (isReadyToTrigger) {
            TriggerAttack();
        }
    }

    // We've hit the time in the animation, trigger the actual attack
    private void TriggerAttack() {
        // Start the cooldown
        isInMiddleOfAttack = false;
        timeOfLastAttackFinish = Time.time;

        // Spawn particle system
        SwipeParticleInstance.time = 0.0f;
        SwipeParticleInstance.Play();

        animator.SetBool(ANIM_IS_CHARGING_MELEE, false);
        animator.SetBool(ANIM_IS_FIRING_MELEE, true);

        Vector3 origin = OwnerAimPoint.position + (Vector3.forward * (hitRange / 2.0f));
        Collider[] colliders = Physics.OverlapSphere(origin, hitRange, ~lemurianMask);
        foreach(var collider in colliders) {
            if (collider.TryGetComponent<Entity>(out var entity)) {
                entity.TakeDamage(entityBaseDamage * DamageCoefficient, Affiliation.Enemy, null);
            } else {
                Debug.LogError("Couldn't find Entity component! This might be fine");
            }
        }

        // Reset values once we're done
        ResetAttack();
    }

    private bool CanBeginAttack() {
        // First ensure we can attack in the first place
        if (!canAttack) {
            return false;
        }

        // Don't want to attack in the middle of one!
        if (isInMiddleOfAttack) {
            return false;
        }

        // Check if we're within the cooldown
        float timeSinceLastAttackFinish = Time.time - timeOfLastAttackFinish;
        bool hasCooldownCompleted = timeSinceLastAttackFinish >= cooldownDuration;

        if (!hasCooldownCompleted) {
            return false;
        }

        // Check if we're in range to start the attack
        // We're doing Owner.transform.position, do we want to do OwnerAimPoint.position?
        float distanceToTarget = Vector3.Distance(Owner.transform.position, Target.AimPoint.position);
        bool isTargetWithinAttackRange = distanceToTarget <= minimumBeginAttackDistance;

        if (!isTargetWithinAttackRange) {
            return false;
        }

        return true;
    }

    private void ResetAttack() {
        isInMiddleOfAttack = false;
        animator.SetBool(ANIM_IS_CHARGING_MELEE, false);
        animator.SetBool(ANIM_IS_FIRING_MELEE, false);
    }
}
