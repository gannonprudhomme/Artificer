using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class LemurianFireballAttack: EnemyAttack {
    private const float DamageCoefficient = 1.0f; // 100%

    private readonly LemurianFireballProjectile ProjectilePrefab;

    // private AudioClip ShootSfx;

    // Transform of the lemurian so we can get the direction to the target
    private readonly GameObject Owner;
    // Transform of the target so we can get the direction to the target
    private readonly Transform Target;

    // Scales with attack speed
    private const float chargeDuration = 0.6f; // in seconds
    private float timeOfChargeStart;

    // How long in seconds until we can start charging again after we last fired
    // This is technically an "end lag" whatever that means
    private const float cooldown = 0.5f;
    private float timeOfLastFire;

    private bool isCharging = false;

    private float entityBaseDamage = 0.0f;

    public LemurianFireballAttack(
        LemurianFireballProjectile projectilePrefab,
        GameObject owner,
        Transform targetTransform
        // Pass in ShootSfx
	) {
        this.ProjectilePrefab = projectilePrefab;
        this.Owner = owner;
        this.Target = targetTransform;
	}

    public override void OnUpdate(float entityBaseDamage) {
        base.OnUpdate(entityBaseDamage);

        if (!canAttack) {
            ResetAttack();
            return;
	    }

		this.entityBaseDamage = entityBaseDamage;

        if (isCharging) {
            HandleCharging();
        }
    }

    // The Lemurian of this will call this when it's ready to fire
    // Maybe rename this to Begin()? Cause that could be charging or fire
    public void StartCharging() {
        if (!CanStartCharging()) { return; }

        timeOfChargeStart = Time.time;
        isCharging = true;
    }

    // TODO: Currently unused
    // We need to give access to this as the Lemurian strafes while it's charging
    public bool IsCharging() {
        return isCharging;
    }

    private bool CanStartCharging() {
        if (isCharging) { return false; }

        // TODO: Check line of sight to target
        float secondsSinceLastFired = Time.time - timeOfLastFire;
        return secondsSinceLastFired >= cooldown;
    }

    private void HandleCharging() {
        float secondsIntoCharge = Time.time - timeOfChargeStart;
        bool readyToFire = secondsIntoCharge >= chargeDuration;

        if (readyToFire) {
            // Should i call this in here or Fire()? Doing both rn
            isCharging = false;
            Fire();
            return;
		}
    }

    // Done charging attack, fire projectile
    private void Fire() {
        isCharging = false;
        timeOfLastFire = Time.time;

        // Play fired sfx
        Vector3 directionToTarget = (Target.position - Owner.transform.position).normalized;

        // Spawn projectile and shoot it
        Projectile newProjectile = Object.Instantiate(
            ProjectilePrefab,
            // TODO: This should be the muzzle position
            Owner.transform.position,
            Quaternion.LookRotation(directionToTarget)
        );

		// TODO:
        newProjectile.Shoot(Owner, null, entityBaseDamage * DamageCoefficient);
    }

    private void ResetAttack() {
        isCharging = false;
	}
}
