using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Each level will have its own instance of a combat director
// Will probably want to make this conform to some abstract class
public class CombatDirector: MonoBehaviour { // Not sure if we want this to be a MonoBehavior, but I suppose it might as well
    public StoneGolem StoneGolemPrefab;
    public Lemurian LemurianPrefab;

    // Should this a property on the Enemy? Probably
    // But it would be nice to just have a single place to change these values, so I'll do this for now 
    private Dictionary<string, float> EnemyCostMapping => new() {
        { StoneGolemPrefab.EnemyIdentifier, 3.0f },
        { LemurianPrefab.EnemyIdentifier, 1.0f }
    };

    // EnemyManager is where the list of enemies will live.
    // We probably don't need to do this, but could serve as a dependency injection entrypoint?
    private EnemyManager enemyManager = EnemyManager.shared; 

    // How many credits currently have to spawn something
    private float numCredits;

    private void Start() {
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) {
            Debug.LogError("CombatDirector couldn't find EnemyManager!");
        }
    }

    private void Update() {
        
    }

    private void GenerateCredits() {

    }
}
