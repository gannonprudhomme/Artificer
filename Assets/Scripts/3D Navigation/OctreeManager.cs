using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Just holds the Graph so both Items & Enemies can access it
public class OctreeManager : MonoBehaviour {
    public static OctreeManager? shared;

    [Header("References")]
    [Tooltip("Reference to the current Nav Octree Space (on the level)")]
    // Used to load the Octree from memory
    public NavOctreeSpace? NavSpace = null;

    public Octree? Octree => NavSpace!.octree;

    private void Awake() {
        if (shared != null) {
            Debug.LogError("There are two instances of Enemy Manager!");
        }

        shared = this;

        NavSpace!.LoadIfNeeded();

        GraphGenerator.PopulateOctreeNeighbors(NavSpace!.octree!, shouldBuildDiagonals: true);
    }
}
