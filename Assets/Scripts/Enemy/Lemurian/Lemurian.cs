using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#nullable enable

public abstract class GroundedEnemy: Enemy {
    // Only relevant for things that shoot projectiles!
    // [Tooltip("Where on the Enemy we're going to shoot projectiles from")]
    // public Transform AimPoint;

    // [Tooltip("Where the NavMeshAgent is going to navigate to. Should be the player")]
    // public Transform Target;
}

[RequireComponent(typeof(NavMeshAgent))]
public class Lemurian : Enemy {
    [Header("References")]
    [Tooltip("The mesh renderer used to display this")]
    public MeshRenderer? MainMeshRenderer;

    [Tooltip("Where on the Lemurian we're going to shoot projectiles")]
    public Transform? AimPoint;

    //[Tooltip("Rotation speed when the target is within NavMeshAgent's stopping distance")]
    //public float RotationSpeed = 0.1f;

    [Tooltip("Prefab for the fireball attack projectile")]
    public Projectile? FireballProjectilePrefab;

    private NavMeshAgent? navMeshAgent;

    // This isn't *really* optional since it's assigned in Start()
    private LemurianFireballAttack? fireballAttack;

    protected override float StartingBaseDamage => 12;
    public override float CurrentBaseDamage => StartingBaseDamage;

    protected override void Start() {
        base.Start();

        navMeshAgent = GetComponent<NavMeshAgent>();

        SetDestination();

        fireballAttack = new(FireballProjectilePrefab!, this.gameObject, Target);

	    health.OnDamaged += OnDamaged;
    }

    protected override void Update() {
        base.Update();

        // Sometimes this runs after Destroy is called,
        // so prevent that from happening
        if (!gameObject.activeSelf) { return; }

        if (isFrozen) {
            navMeshAgent!.isStopped = true;
        } else { 
            navMeshAgent!.isStopped = false;
			SetDestination();
		}

        fireballAttack!.OnUpdate(CurrentBaseDamage);
        fireballAttack!.StartCharging();
    }

    private void SetDestination() { 
        if (Target) {
            navMeshAgent!.SetDestination(Target.transform.position);
		}
    }

    public override Material? GetMaterial() {
        return MainMeshRenderer!.material;
    }

    public override Vector3 GetMiddleOfMesh() {
        return MainMeshRenderer!.bounds.center;
    }

    private void OnDamaged(float damage, Vector3 damagePosition, DamageType damageType) {
        // if damage was > 15% of max health, Lemurian should be stunned (but for how long?)
	}
}
