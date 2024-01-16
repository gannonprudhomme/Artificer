using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

public class LemurianFireballAttack: EnemyAttack {
    private const float DamageCoefficient = 1.0f; // 100%

    private readonly LemurianFireballProjectile ProjectilePrefab;
    private readonly VisualEffect ChargeVisualEffectInstance;

    // private AudioClip ShootSfx;

    // Animator of the Lemurian so we can start the fireball animation
    private readonly Animator animator;
    // Transform of the lemurian so we can get the direction to the target
    private readonly GameObject Owner;
    // Transform of the target so we can get the direction to the target
    private readonly Transform Target;
    private readonly Transform FirePoint;

    // Scales with attack speed
    private const float chargeDuration = 0.6f; // in seconds
    private float timeOfChargeStart;

    // How long in seconds until we can start charging again after we last fired
    // This is technically an "end lag" whatever that means
    private const float cooldown = 0.5f;
    private float timeOfLastFire;

    private bool isCharging = false;

    private float entityBaseDamage = 0.0f;

    private const string ANIM_IS_CHARGING_FIREBALL = "IsChargingFireball";
    private const string ANIM_IS_FIRING_FIREBALL = "IsFiringFireball";

    public LemurianFireballAttack(
        LemurianFireballProjectile projectilePrefab,
        VisualEffect chargeVisualEffectInstance,
        Animator animator,
        GameObject owner,
        Transform targetTransform,
        Transform firePoint
	) {
        this.ProjectilePrefab = projectilePrefab;
        this.ChargeVisualEffectInstance = chargeVisualEffectInstance;
        this.animator = animator;
        this.Owner = owner;
        this.Target = targetTransform;
        this.FirePoint = firePoint;

        // We shouldn't have to do this, but it's just a safety
        chargeVisualEffectInstance.Stop();
        // By default set it to false
        animator.SetBool(ANIM_IS_CHARGING_FIREBALL, false);
        animator.SetBool(ANIM_IS_FIRING_FIREBALL, false);
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

        // I really don't know when I should reset this. Doing it 0.1 sec after it fires I guess
        if (Time.time - timeOfLastFire >= 0.1f) {
            animator.SetBool(ANIM_IS_FIRING_FIREBALL, false);
        }
    }

    // The Lemurian of this will call this when it's ready to fire
    // Maybe rename this to Begin()? Cause that could be charging or fire
    public void StartCharging() {
        if (!CanStartCharging()) { return; }

        ChargeVisualEffectInstance.Play();

        // Start the charging (skeleton) animation
        animator.SetBool(ANIM_IS_CHARGING_FIREBALL, true);
        animator.SetBool(ANIM_IS_FIRING_FIREBALL, false); // Set firing to false just in case

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
        Vector3 directionToTarget = (Target.position - FirePoint.position).normalized;

        // Spawn projectile and shoot it
        Projectile newProjectile = Object.Instantiate(
            ProjectilePrefab,
            // TODO: This should be the muzzle position
            FirePoint.position,
            Quaternion.LookRotation(directionToTarget)
        );

        // Debug.Log("Firing, setting to false");
        ChargeVisualEffectInstance.Stop();
        
        animator.SetBool(ANIM_IS_CHARGING_FIREBALL, false);
        animator.SetBool(ANIM_IS_FIRING_FIREBALL, true);

        newProjectile.Shoot(Owner, null, entityBaseDamage * DamageCoefficient);
    }

    private void ResetAttack() {
        if (isCharging) {
            Debug.Log("Resetting attack");
        }
        isCharging = false;
        animator.SetBool(ANIM_IS_CHARGING_FIREBALL, false);
        animator.SetBool(ANIM_IS_FIRING_FIREBALL, false);

        // We should actually let it finish firing if we reset it, but this will work for now?
        ChargeVisualEffectInstance.Stop();
	}
}
