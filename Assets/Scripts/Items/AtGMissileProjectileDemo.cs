using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Splines;

#nullable enable

// This is not a subclass of a Projectile as it's vastly different from a projectile,
// in that it *knows* where it's going
//
// It uses OctreeNavigator to pathfind
[RequireComponent(typeof(CapsuleCollider))]
public class AtGMissileProjectileDemo : MonoBehaviour {
    [Tooltip("VFX prefab we spawn when we hit an enemy")]
    public VisualEffect? OnHitVFXPrefab;

    [Tooltip("Audio Clip that plays when we hit an enemy")]
    public AudioClip? OnHitSFX;

    public bool DebugDrawGraph = false;

    private OctreeNavigator? octreeNavigator;

    public Transform? currentTarget;

    private CapsuleCollider? capsuleCollider;

    public float damageToDeal;

    private float timeOfLastPositionUpdate = Mathf.NegativeInfinity;
    private const float timeBetweenPositionUpdates = 5f / 60f;

    private float timeOfStart = 0;

    private float distanceToFlyUpwards = 20f;
    private float timeToFlyUpwards => distanceToFlyUpwards / speed;
    private const float speed = 60f; // meters / second

    private Vector3 upPosition;
    private Vector3 startPosition;

    private const float timeBeforeSelfDestructWithoutTarget = 1f; // If we can't find a target for a second, self-destruct
    private float? timeOfStartWithoutTarget = null;

    private void Start() {
        capsuleCollider = GetComponent<CapsuleCollider>();

        octreeNavigator = new OctreeNavigator(
            ownerTransform: transform,
            octree: OctreeManager.shared!.Octree!,
            speed: speed,
            colliderCast: ColliderCast
        );

        timeOfStart = Time.time;

        startPosition = transform.position;

        upPosition = transform.position + (Vector3.up * distanceToFlyUpwards);

        transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.up);
    }

    private void Update() {
        float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distToTarget < 1.0f) {
            // Even if we hit something that wasn't an entity, we should still self-destruct

            // Create & play On Hit VFX
            VisualEffect onHitVFX = Instantiate(
                OnHitVFXPrefab!,
                transform.position,
                Quaternion.identity
            );
            Destroy(onHitVFX.gameObject, 1f); // Destroy it in a second
            Destroy(gameObject);
            return;
        }
        
        bool shouldBeFlyingUpwards = Time.time - timeOfStart < timeToFlyUpwards;

        bool shouldUpdatePosition = Time.time - timeOfLastPositionUpdate > timeBetweenPositionUpdates;
        bool currentTargetDead = false;

        // Handle finding a target & creating a path

        // If we should be "flying upwards", pathfind to the target a bit differently:
        if (shouldBeFlyingUpwards && (shouldUpdatePosition || currentTarget == null)) {
            timeOfLastPositionUpdate = Time.time;

            if (currentTarget != null) {
                octreeNavigator!.CreatePathGuaranteeingPositions(
                    currentTarget.position,
                    pathStartPosition: upPosition,
                    originalPosition: startPosition
                );
            }

        } else if (currentTargetDead || shouldUpdatePosition) {
            if (currentTargetDead) {
                // currentTarget = FindTargetEntity();
            }

            timeOfLastPositionUpdate = Time.time;

            if (currentTarget != null) { // can't assume we found one
                octreeNavigator!.CreatePathTo(currentTarget.position);
            }
        }

        // Handle path navigation, depending on whether we have a target or not

        if (currentTarget != null) {// Weren't able to find a target
            timeOfStartWithoutTarget = null;

            transform.rotation = Quaternion.LookRotation(octreeNavigator!.CurrentSplineTangent, upwards: Vector3.up);

            octreeNavigator!.TraversePath();
        } else { // Couldn't find a target
            if (timeOfStartWithoutTarget == null) {
                timeOfStartWithoutTarget = Time.time;
            }

            // Been X seconds without a target, self-destruct
            if (Time.time - timeOfStartWithoutTarget > timeBeforeSelfDestructWithoutTarget) { 
                Debug.Log("AtG Missile Projectile couldn't find a target");
                Destroy(gameObject);
                return;
            }

            // No target - fly upwards
            transform.position += Vector3.up * (speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(Vector3.up); // Face upwards
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetEntityFromCollider(out Entity entity)) { // if let
            // Damage the entity (whichever it is - even if it wasn't the target)
            Debug.Log($"AtG: Dealing {damageToDeal} damage to entity");
            entity.TakeDamage(damageToDeal, procCoefficient: 0.05f, Affiliation.Player, DamageType.Normal);
        }

        // Even if we hit something that wasn't an entity, we should still self-destruct

        // Create & play On Hit VFX
        VisualEffect onHitVFX = Instantiate(
            OnHitVFXPrefab!,
            transform.position,
            Quaternion.identity
        );
        Destroy(onHitVFX.gameObject, 1f); // Destroy it in a second

        // Self-destruct the game object
        Destroy(gameObject);
    }
    private bool ColliderCast(Vector3 startPosition, Vector3 targetPosition, out RaycastHit? hit) {
        if (capsuleCollider == null || currentTarget == null) {
            hit = null;
            return false;
        }

        Vector3 direction = (targetPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, targetPosition);

        // We could cache this, but eh
        Vector3 size = capsuleCollider.bounds.size;
        float biggestSide = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        // TODO: Why am I not doing a CapsuleCast?
        if (Physics.SphereCast(
            startPosition,
            radius: biggestSide,
            direction: direction,
            hitInfo: out RaycastHit rayHit,
            maxDistance: distance 
        )) {
            // Did we hit something that wasn't what we're aiming for? I.e. something is in the way
            if (rayHit.collider.gameObject != currentTarget.gameObject) {
                hit = rayHit;
                return true;
            } else { 
                // If what we hit is the same thing as what we're aiming for, this is what we want!           
                // so act like we didn't hit anything
            }
        }

        hit = null;
        return false;
    }
}

