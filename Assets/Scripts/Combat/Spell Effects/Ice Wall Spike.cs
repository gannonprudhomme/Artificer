using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands.CheckIn.Progress;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class IceWallSpike : MonoBehaviour {
    [Tooltip("SFX to play when the ice spike detonates (by collision or after lifetime is reached)")]
    public AudioClip CollisionSfx; // Rename to DetonationSfx

    [Tooltip("SFX to play when the ice spike is spawned in (becomes active)")]
    public AudioClip SpawnSfx;

    [Tooltip("The prefab to spawn when this detonates")]
    public GameObject DetonateParticlePrefab;

    // How much damage this does on collision
    private const float damage = DamageEconomy.MediumDamage;

    // We expose the BoxCollider since it's needed by the IceWall in order to place it correctly vertically on a surface
    // (it might not be necessary though, this is just the strategy I picked for now)
    public BoxCollider boxCollider { get; private set; }

    // Lifetime before detonating in seconds
    public const float AverageLifetime = 7f;

    // The offsetted range the final lifetime can be
    private float lifetimeOffset => AverageLifetime / 10.0f;
    // The final calculated lifetime, calculated from DetermineLifetime()
    // Within the range of [AverageLifetime - lifetimeOffset, AverageLifetime + lifetimeOffset]
    private float lifetimeDuration; 

    // Initial rotation of the Ice Spike
    // Set in OnEnable() 
    private Quaternion InitialRotation = Quaternion.identity;

    // The time this is going to detonate. Set in OnEnable()
    private float detonationTime = Mathf.NegativeInfinity;

    private const float aboutToDetonatePercent = 0.9f; // 10% of the lifetime means "about to detonate"

    // The % of the lifetime the Ice Spike has "lived" - how close it is to detonating on its own.
    // When this is 1.0 (100%) we should detonate
    private float lifetimePercentage {
        get {
            return (Time.time - detonationTime) / lifetimeDuration;
        }
    }

    void Awake() {
        // Must be called on awake b/c Start() will be too late
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) {
            Debug.LogError("IceWallSpike didn't have a BoxCollider");
        }
    }

    void OnEnable() {
        InitialRotation = transform.parent.transform.rotation;
        lifetimeDuration = DetermineLifetime();
        detonationTime = Time.time + lifetimeDuration;

        //  do OnEnable since that's when/how the Ice Wall animation "spawns" this
        AudioUtility.shared.CreateSFX(
            SpawnSfx,
            transform.position,
            AudioUtility.AudioGroups.Impact,
            1f,
            1f
        );
    }

    void Update() {
        if (lifetimePercentage >= 1.0f) { // we've reached the lifetime, detonate
            Detonate();
            return;
        }

        // See if anything Entity is colliding with this
        bool isAboutToDetonate = lifetimePercentage >= aboutToDetonatePercent;
        if (isAboutToDetonate) {
            ShakeBeforeExplosion();
        }
    }

    private void OnTriggerEnter(Collider collider) {
        // We collided! See if this is an enemy / has a Damageable component
        // otherwise, ignore it!
        // We should be able to do this with Layers / a Collider LayerMask hopefully
        Health health = collider.GetComponent<Health>();
        if (health == null) {
            health = collider.GetComponentInParent<Health>();
            if (health == null) {
                print("collider and it's parent doesn't have Health component! ignoring");
                return; 
            }
        }

        health.TakeDamage(damage, this.gameObject, null);

        Detonate();
    }

    private void Detonate() {
        // TODO: Do an AoE attack? I'm not sure if it does that

        AudioUtility.shared.CreateSFX(
            CollisionSfx,
            transform.position,
            AudioUtility.AudioGroups.Impact,
            1f,
            5f
        );

        // Instantiate the particle on the parent
        // This really shouldn't be how this works but fuck it whatever
        Instantiate(DetonateParticlePrefab, transform.parent);

        // We need to destroy the other "idle" particles too
        Destroy(this.gameObject);
    }

    private void ShakeBeforeExplosion() {
        // I think it should be a factor of pi? Since that's what sin gets input
        // Need to determine how many seconds a "loop" should be, then go from there
        // For now we just choose random values which look right
        float startSpeed = 0.1f;
        float endSpeed = 5f;

        // Only do this when lifetimePercentage is >= aboutToDetonatePercent
        // When we're done this will be (1.0 - 0.9) / (1 - 0.9) = (0.1) / (0.1),
        // when aboutToDetonatePercent == 0.9
        //
        // 0.0f = 90% (or rather, 100% - aboutToDetonatePercent)
        // 1.0f = 100%
        // [0.0f, 1.0f]
        float lerpFactor = (lifetimePercentage - aboutToDetonatePercent) / (1.0f - aboutToDetonatePercent);

        float speed = Mathf.Lerp(startSpeed, endSpeed, lerpFactor);

        // I should really calculate this
        // it should be like "how long is a shake loop"
        // Should do how many degrees should this rotate in a certain amount of time (a loop?)
        float amount = 1.0f / 60f; 

        float xAngleRotate = Mathf.Rad2Deg * (Mathf.Sin(Time.time * speed) * amount);
        float zAngleRotate = Mathf.Rad2Deg * (Mathf.Sin(Time.time * speed) * amount);

        // Apply it to the parent since this is attached to the child (parent has the corrected transform)
        var parent = transform.parent.gameObject;
        parent.transform.rotation = InitialRotation * Quaternion.Euler(xAngleRotate, 0f, zAngleRotate);
    }

    private float DetermineLifetime() {
        // Randomly determine what the lifetime is
        float offset = Random.Range(-lifetimeOffset, lifetimeOffset);
        return AverageLifetime + offset;
    }
}
