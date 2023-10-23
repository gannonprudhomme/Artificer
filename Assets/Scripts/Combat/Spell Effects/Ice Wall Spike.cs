using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class IceWallSpike : MonoBehaviour {
    public AudioClip CollisionSfx;

    private const float damage = DamageEconomy.MediumDamage;

    void Start() {
    }

    void Update() {
        // See if anything Entity is colliding with this
    }

    private void OnTriggerEnter(Collider collider) {
        print("entered!");
        // We collided! See if this is an enemy / has a Damageable component
        // otherwise, ignore it!

        Health health = collider.GetComponent<Health>();
        if (health == null) {
            print("collider doesn't have Health component! ignoring");
            return; 
        }

        health.TakeDamage(damage, this.gameObject);

        AudioUtility.shared.CreateSFX(
            CollisionSfx,
            transform.position,
            AudioUtility.AudioGroups.Impact,
            1f,
            5f
        );

        Destroy(this.gameObject);
    }
}
