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
            // This is fine when we're switching scenes
            // Debug.LogError("There are two instances of Octree Manager!");
        }

        shared = this;

        // TODO: probably remove these timings, but leaving for now

        var mainStopwatch = new System.Diagnostics.Stopwatch();
        mainStopwatch.Start();
        NavSpace!.LoadIfNeeded();

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Restart();
        
        // Changed shouldBuildDiagonals to false as a last ditch effort to try to get pathfinding to be faster
        // for Distant Rooster
        GraphGenerator.PopulateOctreeNeighbors(NavSpace!.octree!, shouldBuildDiagonals: false);
        
        stopwatch.Stop();
        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;
        Debug.Log($"Populated neighbors in {seconds:F2} sec ({ms:F0} ms)");

        mainStopwatch.Stop();
        ms = ((double)mainStopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        seconds = ms / 1000d;
        Debug.Log($"Octree loading took {seconds:F2} sec ({ms:F0} ms)");
    }
}
