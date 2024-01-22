using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

// Idk if this should necessarily be a monobehavior
// it's really just data + doing the computation for doing the damage
public class DamageArea : MonoBehaviour {
    [Tooltip("Radius this damage applies over")]
    public float EffectRadius;

    [Tooltip("The curve representing how much damage % we should apply based on how close something is from the center of the damage area")]
    public AnimationCurve DamageOverDistanceCurve;

    public void InflictDamageOverArea(
        float damage,
        Vector3 center,
        // The collider we actually hit to trigger this
        // used so we know which entity (if any) should get a "direct" hit (no falloff)
        Collider directHitCollider,
        LayerMask layers
    ) {
        // HashSet<Entity> entitiesToDamage = new();
        Dictionary<Entity, Collider> entitiesToDamage = new();
        Entity? directHitEntity = null;

        // First, check if we directly hit an entity so we always apply 100% damage to it
        if (directHitCollider.TryGetEntityFromCollider(out var _directHitEntity)) {
            directHitEntity = _directHitEntity;
            entitiesToDamage.Add(directHitEntity, directHitCollider); // So we don't add it multiple times
        }

        // Create a collection of unique health components that would be damaged in the area of effect (in order to avoid damaging a same entity multiple times)
        Collider[] affectedColliders = Physics.OverlapSphere(center, EffectRadius, layers, QueryTriggerInteraction.Ignore);
        // order the colliders by proximity, so we get the closest collider (when entities have multiple colliders)

        Collider[] collidersOrderedByProximityToCenter = affectedColliders.OrderBy( // This might be an expensive process? Might want to profile it
            collider => (center - collider.transform.position).sqrMagnitude
        ).ToArray();

        foreach (var collider in collidersOrderedByProximityToCenter) {
            if (collider.TryGetEntityFromCollider(out var entity) && !entitiesToDamage.ContainsKey(entity)) {
                Debug.Log($"Adding {entity.name}");
                entitiesToDamage.Add(entity, collider);
            }
        }

        // Rather than check every time in the foreach below,
        // apply direct damage to it and remove it from the Set
        if (directHitEntity is Entity _directHitEntity1) {
            _directHitEntity1.TakeDamage(damage);
            bool didRemove = entitiesToDamage.Remove(_directHitEntity1);
            // Debug.Log($"Applied direct hit to {_directHitEntity1.name}");

            // Just a error check, shouldn't ever happen really
            if (!didRemove) Debug.LogError("We were supposed to remove the direct hit entity but didn't!");
        }

        // Apply damage with distance falloff
        foreach(KeyValuePair<Entity, Collider> pair in entitiesToDamage) {
            Entity entity = pair.Key;
            Collider collider = pair.Value;

            // We should probably do the distance to the closest point on the Collider
            // but who cares if we're that accurate
            float distanceFromCollider = Vector3.Distance(collider.bounds.center, center);
            float damageAfterFalloff = damage * DamageOverDistanceCurve.Evaluate(distanceFromCollider / EffectRadius);
            //{
                Debug.Log($"Applying {damageAfterFalloff} based off of {damage} and distance {distanceFromCollider}, which is {distanceFromCollider / EffectRadius * 100.0f}%");

                entity.TakeDamage(damageAfterFalloff);
            //}
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, EffectRadius);
    }
}

// TODO: Move this to its own script
public static class ColliderExtensionMethod {
    public static Entity? GetEntityFromCollider(this Collider collider) {
        if (collider.TryGetComponent<ColliderParentPointer>(out var colliderParentPointer)) {
            return colliderParentPointer.entity;
        }
        
        if (collider.TryGetComponent<Entity>(out var entity)) {
            return entity;
        }

        return null;
    }

    // idk if outEntity should be nullable with TryGet. Going with no for now.
    public static bool TryGetEntityFromCollider(this Collider collider, out Entity outEntity) {
        if (collider.TryGetComponent<ColliderParentPointer>(out var colliderParentPointer)) {
            outEntity = colliderParentPointer.entity;
            return true;
        }
        
        if (collider.TryGetComponent<Entity>(out var entity)) {
            outEntity = entity;
            return true;
        }

        outEntity = null;
        return false;
    }
}
