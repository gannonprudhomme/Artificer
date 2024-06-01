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

    [Header("References")]
    [Tooltip("Reference to the current Nav Octree Space (on the level)")]
    // Used to load the Octree from memory
    public NavOctreeSpace? NavSpace = null;

    public List<Enemy>? activeEnemies { get; private set; }

    public UnityAction<Enemy>? OnEnemyAdded;

    public Graph? WispGraph { get; private set; }

    void Awake() {
        activeEnemies = new();

        if (shared != null) {
            Debug.LogError("There are two instances of Enemy Manager!");
        }

        shared = this;

        if (NavSpace != null) {
            NavSpace.LoadIfNeeded(); // Load it from memory. Takes < 100ms

            // Generate the graph to be used for navigation
            WispGraph = GraphGenerator.GenerateGraph(NavSpace.octree!, shouldBuildDiagonals: true);
        } else {
            Debug.LogError("NavOctreeSpace was not set!");
        }
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
