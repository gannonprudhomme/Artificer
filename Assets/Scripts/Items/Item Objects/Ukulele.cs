using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

// This was my first time using Coroutines, so don't judge past me too hard

[CreateAssetMenu(menuName = "ScriptableObjects/Items/Ukulele")]
public class Ukulele : Item {
    [Header("Ukulele")]
    [Tooltip("VFX which is instantiated when the effect triggers (aka when we hit an enemy and it procs)")]
    public VisualEffect? LightningVFX;

    [Tooltip("VFX which is instantiated when we actually damage an entity")]
    public VisualEffect? OnHitVFX;

    [Tooltip("Layer mask for enemies which we use for detecting collisions")]
    public LayerMask EnemyLayerMask;

    public override string itemName => "Ukulele";
    public override string description => "...and his music was electric.";
    public override string longDescription => "25% chance to fire chain lightning for 80% TOTAL damage on up to 3 (+2 per stack) targets within 20m (+2m per stack).";
    public override Rarity rarity => Rarity.UNCOMMON;
    public override ItemType itemType => ItemType.UKULELE;

    private const float vfxLifetime = 0.5f;
    private const float percentFromLightningStartToDamage = 0.25f; // Quarter of the way through the VFX lifetime
    private float timeBetweenLightningStrikes => vfxLifetime * percentFromLightningStartToDamage;

    // TODO: Need the proc coefficient here
    // Note this doesn't hit the entity that we actually hit (`entityHit`)
    public override void OnEnemyHit(ItemsDelegate _, MonoBehaviour owner, int itemCount, OnEntityHitData onHitData) {
        float chance = 0.25f * onHitData.procCoefficient;

        bool didTrigger = Random.value <= chance;
        if (!didTrigger) {
            return;
        }

        int numberOfEnemiesToHit = 3 + ((itemCount - 1) * 2); // 3 enemies + 2 per additional stack
        float radius = 20 + ((itemCount - 1) * 2); // 20m + 2m per additional stack
        radius *= 2f; // Increase radius to match our units
        float damagePerStrike = onHitData.attackTotalDamage * 0.8f; // 80% total damage (aka attack damage. This is not base damage)

        HashSet<int> alreadyHitEntityIDs = new() { onHitData.entityHit.GetInstanceID() }; // Shouldn't hit it anyways, but might as well
        Entity? current = FindClosestEntityNotYetHit(current: onHitData.entityHit, radius, alreadyHitEntityIDs);

        if (current == null) return;

        owner.StartCoroutine(
            StartLightningArc(
                owner: owner,
                currentToDamage: current,
                previous: onHitData.entityHit,
                currentHitCount: 0,
                totalToHit: numberOfEnemiesToHit,
                damage: damagePerStrike,
                radius: radius,
                alreadyHitEntityIDs: alreadyHitEntityIDs
            )
        );
    }

    private Entity? FindClosestEntityNotYetHit(Entity current, float radius, HashSet<int> alreadyHitEntityIDs) {
        Vector3 position = current.GetMiddleOfMesh();

        // Get all enemies within the radius
        Collider[] colliders = Physics.OverlapSphere(
            position,
            radius,
            EnemyLayerMask,
            QueryTriggerInteraction.Ignore // Ignore triggers?
        );

        List<Entity> entitiesInRadius = new(); // enemies in radius is more accurate

        foreach(var collider in colliders) {
            // Only get enemies
            if (collider.TryGetEntityFromCollider(out Entity entity) &&
                entity.health!.Affiliation != Affiliation.Player && // Don't want to get the player
                entity != current && // Don't want the entity we hit (though the HashSet should make this redundant)
                !alreadyHitEntityIDs.Contains(entity.GetInstanceID())
            ) {
                entitiesInRadius.Add(entity);
            }
        }

        // Sort them by distance, with closer ones first
        entitiesInRadius.OrderBy( // TODO: Check this
            entity => (position - entity.transform.position).sqrMagnitude
        );

        // If it's empty, return null
        if (entitiesInRadius.Count == 0) return null;

        return entitiesInRadius[0];
    }

    // Note that previous is the start position of the VFX, and currentToDamage is the end
    private IEnumerator StartLightningArc(
        MonoBehaviour owner, // So we can start the next coroutine
        Entity currentToDamage,
        Entity previous,
        int currentHitCount,
        int totalToHit,
        float damage,
        float radius,
        HashSet<int> alreadyHitEntityIDs
    ) {
        // Check for base case (if we're done)
        if (currentHitCount >= totalToHit) {
            yield break;
        }

        // Schedule the damaging + on hit VFX + spawning the next one
        owner.StartCoroutine(
            DamageAndPlayOnHitVFXAndStartNext(
                entityToDamage: currentToDamage,
                damage: damage,
                owner: owner,
                radius: radius,
                currentToDamage: currentToDamage,
                currentHitCount: currentHitCount,
                totalToHit: totalToHit,
                alreadyHitEntityIDs: alreadyHitEntityIDs
            )
        );

        // Trigger the VFX
        // position only matters for the VFX's bounds
        VisualEffect vfxInstance = Instantiate(LightningVFX!, position: previous.GetMiddleOfMesh(), rotation: Quaternion.identity);
        vfxInstance.SetFloat("_Lifetime", vfxLifetime);
        vfxInstance.SetVector3("Start Position", previous.GetMiddleOfMesh());
        vfxInstance.SetVector3("End Position", currentToDamage.GetMiddleOfMesh());
        vfxInstance.Play(); // removed the initial on play as otherwise the lifetime won't be right for the first one
        Destroy(vfxInstance.gameObject, vfxLifetime + 0.05f); // Schedule it to be destroyed

        // Set the start & end position every frame until it's destroyed
        // it'd be way better if we could access the property binder here
        float timeOfStart = Time.time;
        while (Time.time - timeOfStart < vfxLifetime) {
            if (vfxInstance == null) yield break; // Stop the coroutine if it's destroyed

            // There's a chance they're already dead (after we damaged them!)
            if (previous != null) {
                vfxInstance.SetVector3("Start Position", previous.GetMiddleOfMesh());
            }
            if (currentToDamage != null) {
                vfxInstance.SetVector3("End Position", currentToDamage.GetMiddleOfMesh());
            }

            yield return null; // wait for the next frame
        }
    }

    private IEnumerator DamageAndPlayOnHitVFXAndStartNext(
        Entity entityToDamage,
        float damage,
        
        // For Start Next
        MonoBehaviour owner,
        float radius,
        Entity currentToDamage,
        int currentHitCount,
        int totalToHit,
        HashSet<int> alreadyHitEntityIDs
    ) {
        float timeFromStartToDamage = timeBetweenLightningStrikes; // Just calling out that they're the same thing
        yield return new WaitForSeconds(timeFromStartToDamage); // Wait before we damage (time it with the actual "strike")

        alreadyHitEntityIDs.Add(entityToDamage.GetInstanceID());

        // Trigger the damage
        entityToDamage.TakeDamage(damage, procCoefficient: 0.2f, Affiliation.Player, DamageType.Normal); // Should actually be a yellow color

        VisualEffect onHitInstance = Instantiate(
            OnHitVFX!,
            position: entityToDamage.GetMiddleOfMesh(),
            rotation: Quaternion.identity
        );

        Destroy(onHitInstance.gameObject, 1f); // Schedule it to be destroyed

        Entity? nextToDamage = FindClosestEntityNotYetHit(current: currentToDamage, radius: radius, alreadyHitEntityIDs);
        if (nextToDamage == null) {
            yield break;
        }

        // Start the next lightning arc
        owner.StartCoroutine(
            StartLightningArc(
                owner: owner,
                currentToDamage: nextToDamage,
                previous: currentToDamage,
                currentHitCount: currentHitCount + 1,
                totalToHit: totalToHit,
                damage: damage,
                radius: radius,
                alreadyHitEntityIDs: alreadyHitEntityIDs
            )
        );
    }
} 