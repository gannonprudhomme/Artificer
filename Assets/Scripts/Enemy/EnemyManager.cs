using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Contains all of the Enemies in the
//
// Currently used for getting access to the Health object of all enemies
// so we get CrosshairDamageIndicatorUI.OnDamaged anytime an enemy is damaged
public class EnemyManager : MonoBehaviour {
    // This should really be a singleton
    // public static EnemyManager shared;

    // TODO: Change this to be List<Enemy>
    public List<GameObject> activeEnemies { get; private set; }

    public UnityAction<GameObject> OnEnemyAdded;

    void Awake() {
        activeEnemies = new();
    }

    void Update() {
    }

    public void AddEnemy(GameObject enemy) {
        activeEnemies.Add(enemy);

        OnEnemyAdded?.Invoke(enemy);
    }

    // Remove an enemy from the list (when they die)
    public void RemoveEnemy(GameObject enemy) {
        activeEnemies.Remove(enemy);
    }
}
