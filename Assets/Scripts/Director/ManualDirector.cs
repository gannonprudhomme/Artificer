using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// A Director used for testing
//
// Spawns an enemy at the point the player is looking at
public class ManualDirector : MonoBehaviour {
    [Header("Reference")]
    [Tooltip("Reference to the main camera used for the player")]
    public Camera PlayerCamera;

    public Target PlayerTarget;

    public StoneGolem StoneGolemPrefab;
    public Lemurian LemurianPrefab;

    // What transform to place the enemies under
    [Tooltip("The transform to place the enemies under. Probably the Level")]
    public Transform SpawnTransform;

    private EnemyManager enemyManager = EnemyManager.shared;

    private bool spawnLemurianWasHeld = false;
    private bool spawnStoneGolemWasHeld = false;

    private void Update() {
        // Should probs be an enum, but just for testing so w/e
        bool lemurianPressed = IsSpawnLemurianPressed();
        bool golemPressed = IsSpawnStoneGolemPressed();

        // This nesting sucks but w/e it's just for testing anyways
        if (lemurianPressed || golemPressed) {
            string agentName;
            if (lemurianPressed) {
                agentName = "Lemurian";
            } else { // Golem pressed
                agentName = "Stone Golem";
            }
            
            Vector3? position = GetSpawnPointFromLookAt(agentName);
            if (position is Vector3 _position) {
                Enemy enemyToSpawn;

                if (lemurianPressed) {
                    enemyToSpawn = Instantiate(LemurianPrefab, SpawnTransform);
                } else { // Golem pressed
                    // enemyToSpawn = Instantiate(StoneGolemPrefab, SpawnTransform);
                    enemyToSpawn = Instantiate(StoneGolemPrefab, _position, Quaternion.identity);
                }

                enemyToSpawn.transform.position = _position;
                enemyToSpawn.Target = PlayerTarget;
            } else {
                Debug.LogError("Couldn't find a point! Probably shouldn't have spawned it but whatever");
            }
        }
    }

    private void LateUpdate() {
        spawnLemurianWasHeld = IsSpawnLemurianHeld();
        spawnStoneGolemWasHeld = IsSpawnStoneGolemHeld();
    }

    private Vector3? GetSpawnPointFromLookAt(string agentName) {
        if (Physics.Raycast(
            PlayerCamera.transform.position,
            PlayerCamera.transform.forward,
            out RaycastHit raycastHit
        )) {
            // int thing = NavMesh.GetAreaFromName(navMeshName);
            // int thing = GetAgentTypeIDByName(agentName);

            NavMeshQueryFilter filter = new();
            filter.agentTypeID = GetAgentTypeIDByName(agentName);

            // Found a hit, find the nearest point on the nav mesh to spawn the entity
            if (NavMesh.SamplePosition(raycastHit.point, out NavMeshHit hit, 5f, -1)) {
                return hit.position;
            } else
            {
                Debug.LogError("Couldn't sample");
            }
        }

        return null;
    }

    // Gets the nav mesh index so I can pick the right nav mesh to spawn an enemy on
    // Stolen from: https://forum.unity.com/threads/solved-navmeshsurface-bug-or-feature-sampleposition.499069/
    private int GetAgentTypeIDByName(string agentName) {
        for (int i = 0; i < NavMesh.GetSettingsCount(); ++i) {
            var settings = NavMesh.GetSettingsByIndex(i);

            int agentTypeID = settings.agentTypeID;

            var settingsName = NavMesh.GetSettingsNameFromID(agentTypeID);

            if (settingsName == agentName) {
                // Debug.Log($"Found {agentName} for {settings}");
                return settings.agentTypeID;
            }
        }

        Debug.Log("Couldn't find");

        return -1;
    }

    // I don't expect this to be long-lived, otherwise I would just add this to InputHandler + reference it
    private bool IsSpawnLemurianPressed() {
        return IsSpawnLemurianHeld() && !spawnLemurianWasHeld;
    }

    private bool IsSpawnLemurianHeld() {
        return Input.GetButton("SpawnLemurian");
    }

    private bool IsSpawnStoneGolemPressed() {
        return IsSpawnStoneGolemHeld() && !spawnStoneGolemWasHeld;
    } 
    private bool IsSpawnStoneGolemHeld() {
        return Input.GetButton("SpawnStoneGolem");
    }
}
