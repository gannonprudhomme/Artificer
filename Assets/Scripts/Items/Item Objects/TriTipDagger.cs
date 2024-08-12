using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/Items/Tri-Tip Dagger")]
public class TriTipDagger : Item {
    [Header("Tri-Tip Dagger")]
    [Tooltip("VFX Prefab we create whenever bleed is applied - only plays at the start")]
    public VisualEffect? BleedStartVFX;

    [Tooltip("VFX Prefab we create whenever bleed is applied - is played every tick")]
    public VisualEffect? BleedTickVFX;

    [Tooltip("Set to true to always trigger the bleed effect")]
    public bool DebugAlwaysTrigger = false;

    public override string itemName => "Tri-Tip Dagger";
    public override ItemType itemType => ItemType.TRI_TIP_DAGGER;
    public override string description => "10% (+10% per stack) chance to bleed an enemy for 240% base damage.";
    public override Rarity rarity => Rarity.COMMON;

    public override void OnEnemyHit(float playerBaseDamage, Entity entityHit, int itemCount) {
        float procCoefficient = 1.0f; // TODO: Pass proc chance so we can multiply it by the chance for e.g. Ukulele

        float value = Random.value;
        float percentChance = 0.1f * itemCount; // each stack increases chance by 10%
        bool didTriggerChance = value <= (percentChance * procCoefficient);

        if (!didTriggerChance && !DebugAlwaysTrigger) {
            return;
        };

        BleedStatusEffect bleedStatusEffect = new(
            damageToDeal: playerBaseDamage * 2.4f, // 240% base damage
            startVFXPrefab: BleedStartVFX!,
            tickVFXPrefab: BleedTickVFX!
        );
        
        entityHit.AddOrStackStatusEffect(
            appliedStatusEffect: bleedStatusEffect,
            damageApplierAffiliation: Affiliation.Player
        );
    }
}
