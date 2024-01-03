using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

public class LemurianFireballProjectile : Projectile {
    [Header("Lemurian Fireball Projectile")]
    [Tooltip("Reference to the visual effect")]
    public VisualEffect MainFireballVisualEffect;

    private const float MainFireballParticleLifetime = 0.5f;

    // Should be equal to the lifetime from the particle system
    // So the last particle emitted keeps animating when we collide
    private float TimeTillDestroyFromHit => MainFireballParticleLifetime;

    public override void Shoot(GameObject owner, Camera? spellCamera, float entityBaseDamage) {
        base.Shoot(owner, spellCamera, entityBaseDamage);

        MainFireballVisualEffect.SetFloat("Lifetime", MainFireballParticleLifetime);
        MainFireballVisualEffect.SetInt("IsMoving", 1);
    }

    protected override void Update() {
        base.Update();

        MainFireballVisualEffect.SetVector3("Target", transform.position);
    }

    protected override void OnHit(Vector3 point, Vector3 normal, Collider collider) {
        base.OnHit(point, normal, collider);

        // Make the fireball particle system stop emitting
        MainFireballVisualEffect.Stop();
        // Make the existing particles stop moving
        MainFireballVisualEffect.SetInt("IsMoving", 0);
        
        // TODO: Play the OnHit particle system
    }

    protected override void OnDeath() {
        // Note we're intentionally NOT calling the OnDeath from the base class
        // as that destroys instantly
        IsDead = true;

        Destroy(this.gameObject, TimeTillDestroyFromHit);
    }
}
