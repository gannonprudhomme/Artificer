using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/Items/Gasoline")]
public class Gasoline : Item {
    [Header("Gasoline")]
    [Tooltip("VFX Prefab which is instantiated when this is triggered (aka an enemy dies)")]
    public VisualEffect? ExplosionVFX;

    [Tooltip("Prefab for the DamageArea which is created when this is triggered")]
    public DamageArea? DamageArea;

    public override string itemName => "Gasoline";
    public override ItemType itemType => ItemType.GASOLINE;
    public override string description => "Killing an enemy ignites other nearby enemies.";
    public override string longDescription => "Killing an enemy ignites all enemies within 12m (+4m per stack) for 150% base damage. Additionally, enemies burn for 150% (+75% per stack) base damage.";
    public override Rarity rarity => Rarity.COMMON;

    public override void OnEnemyKilled(Vector3 killedEnemyPosition, float playerBaseDamage, int itemCount) {
        float radius = 12 + ((itemCount - 1) * 4); // 12m + 4m per additional stack
        radius *= 2; // Increase radius since our units aren't 1:1 to ror2

        float percentModifier = 1.5f + ((itemCount - 1) * 0.75f); // 150% base damage + 75% per additional stack
        float damageToApply = playerBaseDamage * percentModifier;

        // Apply this to nearby enemies (if any)
        BurnStatusEffect burnToApply = new(
            damageToApply: damageToApply * 0.5f, // Do we reduce this by 50%?? I'm guessing so
            entityBaseDamage: playerBaseDamage,
            effectApplierAffiliation: Affiliation.Player
        );

        DamageArea damageAreaInstance = Instantiate(DamageArea!, position: killedEnemyPosition, rotation: Quaternion.identity);
        damageAreaInstance.EffectRadius = radius;

        damageAreaInstance.InflictDamageOverArea(
            damage: damageToApply,
            center: killedEnemyPosition,
            damageApplierAffiliation: Affiliation.Player,
            directHitCollider: null,
            statusEffectToApply: burnToApply,
            layers: -1 // We should just use Affiliation for this yeah?
        );

        Destroy(damageAreaInstance.gameObject, 1f); // Schedule the damage area to be destroyed

        PlayVFX(killedEnemyPosition: killedEnemyPosition, radius: radius);
    }

    private void PlayVFX(Vector3 killedEnemyPosition, float radius) {
        VisualEffect explosionVFXInstance = Instantiate(ExplosionVFX!, position: killedEnemyPosition, rotation: Quaternion.identity);
        explosionVFXInstance.SetFloat("Radius", radius);
        Destroy(explosionVFXInstance.gameObject, 1.0f); // Schedule it to be destroyed
    }
}
