using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

// Contains all of the Enemies in the (...level?)
//
// Currently used for getting access to the Health object of all enemies
// so we get CrosshairDamageIndicatorUI.OnDamaged anytime an enemy is damaged
public class EnemyManager : MonoBehaviour {
    public static EnemyManager? shared;

    public List<Enemy>? activeEnemies { get; private set; }

    public UnityAction<Enemy>? OnEnemyAdded;

    void Awake() {
        activeEnemies = new();

        if (shared != null) {
            Debug.LogError("There are two instances of Enemy Manager!");
        }

        shared = this;
    }

    public void AddEnemy(Enemy enemy) {
        activeEnemies!.Add(enemy);

        OnEnemyAdded?.Invoke(enemy);
    }

    // Remove an enemy from the list (when they die)
    public void RemoveEnemy(Enemy enemy) {
        activeEnemies!.Remove(enemy);
    }
}
