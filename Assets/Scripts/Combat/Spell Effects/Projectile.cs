using System.Collections;
using System.Collections.Generic;
using PlasticGui.WorkspaceWindow;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

// Again ideally this would be an interface,
// but this has to be an abstract class since it has to be a MonoBehavior (man fuck C# I miss protocols)

// This is a *Projectile* not a general spell effect
// it spawns stuff like the Fireball Projectile
// Though maybe the Ice Wall, Lightning Jump, and Fireball effect should all share some base class / interface
// but I should only do that if there's a point (externally)
public abstract class Projectile : MonoBehaviour {
    [Header("General")]
    [Tooltip("Radius of this projectile's collision detection")]
    public float Radius = 0.01f; // Why is this a property? Shouldn't it just be 'detected'?

    // This is the parent object basically
    [Tooltip("Transform representing the root of the projectile (used for accurate collision detection")]
    public Transform Root;

    [Tooltip("Transform representing the tip of the projectile (used for accurate collision detection")]
    public Transform Tip;

    [Tooltip("Lifetime of the projectile")]
    public float MaxLifeTime = 5f; // I feel like this should be externally controlled

    [Tooltip("Reference to the visual effect for the explosion (that's played on hit)")]
    public GameObject? ImpactVfx;

    [Tooltip("Lifetime of the VFX before being destroyed")]
    public float ImpactVfxLifetime = 5f;

    [Tooltip("Clip to play on impact")]
    public AudioClip? ImpactSfxClip;

    // TODO: I don't think we ever actually set this to anything other than other everything
    [Tooltip("Layers this projectile can collide with")]
    public LayerMask HittableLayers = -1;

    [Header("Movement")]
    [Tooltip("Speed of the projectile")]
    public float Speed = 20f; // Can I override this for FireballProjectile? I guess not

    [Tooltip("Downward acceleration from gravity")]
    public float GravityDownAcceleration = 0f;

    [Header("Damage")]
    [Tooltip("Damage Multiplier relative to base damage")]
    public float DamageMultipler = 2.8f;

    [Tooltip("Area of Damage. Keep it empty if you dont' want area damage")]
    public DamageArea? DamageArea = null;

    [Header("Debug")]
    [Tooltip("Color of the projectile radius for debug view")]
    public Color RadiusColor = Color.cyan * 0.2f;

    /** Idk divider **/
    public Vector3 InitialPosition { get; protected set; }
    public Vector3 InitialDirection { get; protected set; }

    /** Local variables **/

    private Vector3 lastRootPosition;
    private Vector3 velocity;
    private List<Collider> ignoredColliders = new();

    private GameObject? owner;
    private Affiliation ownerAffiliation;

    // Used so we can let effects wrap up playing after a collision
    protected bool IsDead = false;

    // This is set every time we shoot rather than when a collision happens
    // so it could technically be out of date if the player levels up when this is in flight
    // but I think that's fine
    private float entityBaseDamage = 0.0f;

    /** Abstract functions **/
    protected virtual BaseStatusEffect? GetStatusEffect() { return null; }

    /** Functions **/
    void OnEnable() { // We do this instead of Start() for some reason
        Destroy(this.gameObject, MaxLifeTime);
    }

    // I intentionally put this above Update() since this will be called before it (probably)
    // Was originally OnShoot() I guess
    public virtual void Shoot(
        GameObject owner, // (Root) Player game object
        Affiliation ownerAffiliation,
        Camera? spellCamera,  // don't actually need, remove this
	    float entityBaseDamage
    ) {
        lastRootPosition = Root.position; 
        velocity = transform.forward * Speed;
        ignoredColliders = new List<Collider>(); // Idk why we need to do this frankly it's not like this gets reused
        this.owner = owner;
        this.ownerAffiliation = ownerAffiliation;

        // Ignore colliders of owner
        //Collider[] ownerColliders = owner.GetComponents<Collider>();
        Collider[] ownerChildColliders = owner.GetComponentsInChildren<Collider>();
        // ignoredColliders.AddRange(ownerColliders);
        ignoredColliders.AddRange(ownerChildColliders);

		this.entityBaseDamage = entityBaseDamage;
    }

    protected virtual void Update() {
        if (IsDead) return;

        transform.position += velocity * Time.deltaTime;

        // TODO: do later - I think this is actually making a difference
        // Drift towards trajectory override
        // so that projectiles can be centered with the camera center
        // even though the actual weapon (spawn point) is offset 

        // Orient the projectile towards the velocity
        // (presumably so it points the right way)
        transform.forward = velocity.normalized;

        // TODO: Deal with gravity here (if needed ofc)

        // Handle hit detection

        RaycastHit closestHit = new();
        closestHit.distance = Mathf.Infinity;
        bool foundHit = false;

        // Sphere cast (should it be a capsule cast? Probably, but depends on projectile)
        Vector3 displacementSinceLastFrame = Tip.position - lastRootPosition;
        RaycastHit[] hits = Physics.SphereCastAll(
            lastRootPosition, // I'm really not sure why we do lastRootPosition
            Radius,
            displacementSinceLastFrame.normalized,
            displacementSinceLastFrame.magnitude,
            HittableLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach(var hit in hits) {
            if (IsHitValid(hit) && hit.distance < closestHit.distance) {
                foundHit = true;
                closestHit = hit;
            }
        }

        if (foundHit) {
            // Handle case of casting while already inside a collider
            // when tf does this happen?
            if (closestHit.distance <= 0f) {
                closestHit.point = Root.position;
                closestHit.normal = -transform.forward;
            }
                
            OnHit(closestHit.point, closestHit.normal, closestHit.collider);
        }

        lastRootPosition = Root.position;
    }

    private bool IsHitValid(RaycastHit hit) {
        // ignore hits with an ignore component
        // TODO: Add this when I make IgnoreHitDetection
        // but frankly idk why we wouldn't use layer masks for this

        // ignore hits with triggers that don't have a damageable component
        // TODO: Idek if I want this

        // ignore hits with specific ignored colliders (self colliders, by default)
        if (ignoredColliders != null && ignoredColliders.Contains(hit.collider)) {
            return false;
        }

        // Check if they have the same affiliation?

        return true;
    }

    protected virtual void OnHit(Vector3 point, Vector3 normal, Collider collider) {
        float damageAfterMultipler = entityBaseDamage * DamageMultipler;

        if (DamageArea is DamageArea _DamageArea) { // If a DamageArea was provided, use that
            _DamageArea.InflictDamageOverArea(
                damageAfterMultipler,
                point,
                ownerAffiliation,
                collider,
                GetStatusEffect(),
                -1
            );
        } else { // Otherwise, do point damage
            if (collider.TryGetComponent<ColliderParentPointer>(out var colliderParentPointer)) {
                Entity entity = colliderParentPointer.entity;

                entity.TakeDamage(damageAfterMultipler, ownerAffiliation, GetStatusEffect(), point);
            } else if (collider.TryGetComponent<Entity>(out var entity)) {
                entity.TakeDamage(damageAfterMultipler, ownerAffiliation, GetStatusEffect(), point);
            }
        }

        // impact vfx
        if (ImpactVfx) {
            GameObject impactVfx = Instantiate(ImpactVfx!, point, Quaternion.LookRotation(normal));
            
            if (impactVfx.TryGetComponent(out VFXLightController vfxLightController)) {
                Debug.Log("playing vfxLightController");
                vfxLightController.Play();
            } else {
                impactVfx.GetComponent<VisualEffect>().Play();
            }

            Destroy(impactVfx, ImpactVfxLifetime);
        }

        if (ImpactSfxClip) {
            AudioUtility.shared.CreateSFX(ImpactSfxClip, point, AudioUtility.AudioGroups.Impact, 1f, 5f);
        }

        OnDeath();
    }

    protected virtual void OnDeath() {
        IsDead = true;

        // Self destruct
        Destroy(this.gameObject);
    }
}
