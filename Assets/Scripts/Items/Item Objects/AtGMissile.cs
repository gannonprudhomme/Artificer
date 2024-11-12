using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/Items/AtG Missile")]
public class AtGMissile : Item {
    [Header("AtG Missile")]
    [Tooltip("Projectile prefab we spawn when this is trggered")]
    public AtGMissileProjectile? ProjectilePrefab;

    [Tooltip("Whether we should fire on all hits (above 0.75 proc coefficient)")]
    public bool DebugFireOnAllHits = false;

    public override string itemName => "AtG Missle Mk. 1";
    public override string description => "Chance to fire a missile.";
    public override string longDescription => "10% chance to fire a missile that deals 300% (+300% per stack) TOTAL damage.";
    public override Rarity rarity => Rarity.UNCOMMON;
    public override ItemType itemType => ItemType.ATG_MISSILE;

    public override void OnEnemyHit(
        ItemsDelegate itemsController,
        MonoBehaviour owner,
        int itemCount,
        OnEntityHitData onHitData
    ) {
        float chance = 0.5f * onHitData.procCoefficient; // 10% chance of firing a missle (multiplied by proc coefficient)
        bool didTrigger = Random.value <= chance;

        bool didDebugTrigger = DebugFireOnAllHits && onHitData.procCoefficient > 0.75f;
        if (!(didTrigger || didDebugTrigger)) {
            return;
        }

        float damagePercent = 3 * itemCount; // 300% (+ 300% per stack), aka 3*n
        float damage = onHitData.attackTotalDamage * damagePercent;

        AtGMissileProjectile projectile = Instantiate(
            ProjectilePrefab!,
            itemsController.ATGMissileSpawnPoint!.position,
            Quaternion.identity
        );

        projectile.damageToDeal = damage;
    }
}
