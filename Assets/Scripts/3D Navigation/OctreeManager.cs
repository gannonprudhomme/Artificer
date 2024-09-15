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

    public Octree? Octree { 
        get {
            return NavSpace!.octree;
        }
    } 
    public Graph? Graph { get; set; } // Used to be "WispGraph"

    private void Awake() {
        if (shared != null) {
            Debug.LogError("There are two instances of Enemy Manager!");
        }

        shared = this;

        NavSpace!.LoadIfNeeded();

        Graph = GraphGenerator.GenerateGraph(NavSpace!.octree!, shouldBuildDiagonals: true);

        GraphGenerator.PopulateOctreeNeighbors(NavSpace!.octree!, shouldBuildDiagonals: true);
    }
}
