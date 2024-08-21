using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public readonly struct OnEntityHitData {
    // MonoBehaviour owner;
    public readonly float playerBaseDamage;
    public readonly float attackTotalDamage;
    public readonly Entity entityHit;
    // Proc Coefficient of what damaged the entityHit
    public readonly float procCoefficient;

    // The affiliation of who is inflicting this
    public readonly Affiliation inflicterAffiliation;

    public OnEntityHitData(
        float playerBaseDamage,
        float attackTotalDamage,
        Entity entityHit,
        float procCoefficient,
        Affiliation inflicterAffiliation
    ) {
        this.playerBaseDamage = playerBaseDamage;
        this.attackTotalDamage = attackTotalDamage;
        this.entityHit = entityHit;
        this.procCoefficient = procCoefficient;
        this.inflicterAffiliation = inflicterAffiliation;
    }
}

[CreateAssetMenu(menuName = "ScriptableObjects/Events/OnEntityHit")]
public class OnEntityHitEvent : ScriptableObject {
    public UnityAction<OnEntityHitData>? Event;
}
