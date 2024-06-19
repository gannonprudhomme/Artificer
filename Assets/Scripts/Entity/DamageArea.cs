using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

// Idk if this should necessarily be a monobehavior
// it's really just data + doing the computation for doing the damage
public class DamageArea : MonoBehaviour {
    [Header("General")]
    [Tooltip("Radius this damage applies over")]
    public float EffectRadius;

    [Tooltip("The curve representing how much damage % we should apply based on how close something is from the center of the damage area")]
    public AnimationCurve? DamageOverDistanceCurve;

    [Header("Debug")]
    [Tooltip("Enable to show the radius")]
    public bool DebugShowRadius = false;

    public void InflictDamageOverArea(
        float damage,
        Vector3 center,
        Affiliation damageApplierAffiliation,
        // The collider we actually hit to trigger this. Used so we know which entity (if any) should get a "direct" hit (no falloff)
        Collider? directHitCollider,
        BaseStatusEffect? statusEffectToApply,
        LayerMask layers
    ) {
        Dictionary<Entity, Collider> entitiesToDamage = new();
        Entity? directHitEntity = null;

        // First, check if we directly hit an entity so we always apply 100% damage to it
        if (directHitCollider != null && directHitCollider.TryGetEntityFromCollider(out var _directHitEntity)) {
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
                // Debug.Log($"Adding {entity.name}");
                entitiesToDamage.Add(entity, collider);
            }
        }

        // Rather than check every time in the foreach below,
        // apply direct damage to it and remove it from the Set
        if (directHitEntity is Entity _directHitEntity1) {
            _directHitEntity1.TakeDamage(damage, damageApplierAffiliation, statusEffectToApply);
            bool didRemove = entitiesToDamage.Remove(_directHitEntity1);

            // Just a error check, shouldn't ever happen really
            if (!didRemove) Debug.LogError("We were supposed to remove the direct hit entity but didn't!");
        }

        // Apply damage with distance falloff
        foreach(KeyValuePair<Entity, Collider> pair in entitiesToDamage) {
            // Should we check if this entity & the source have the same affliation?

            Entity entity = pair.Key;
            Collider collider = pair.Value;

            // We should probably do the distance to the closest point on the Collider
            // but who cares if we're that accurate
            float distanceFromCollider = Vector3.Distance(collider.bounds.center, center);
            float damageAfterFalloff = damage * DamageOverDistanceCurve!.Evaluate(distanceFromCollider / EffectRadius);
            // Debug.Log($"Applying {damageAfterFalloff} based off of {damage} and distance {distanceFromCollider}, which is {distanceFromCollider / EffectRadius * 100.0f}%");

            entity.TakeDamage(damageAfterFalloff, damageApplierAffiliation, statusEffectToApply);
        }
    }

    private void OnDrawGizmosSelected() {
        if (DebugShowRadius) {
            var color = Color.red;
            color.a = 0.5f;
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, EffectRadius);
        }
    }
}

