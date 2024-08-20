using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/Events/OnEnemyKilled")]
public class OnEnemyKilledEvent : ScriptableObject {
    public UnityAction<Vector3>? Event; // We call OnEnemyKilledEvent.Event, so no need to give it an accurate name
}
