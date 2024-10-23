using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.EditorCoroutines.Editor;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NewNavOctreeSpace))]
public class NewNavOctreeSpaceEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        NewNavOctreeSpace navOctreeSpace = (NewNavOctreeSpace) target;

        // Display the octree's size & maxDivisionLevel if it's available in memory
        // maybe we could precalculate it and display it before we call Generate()?
        // so we know how long it's going to take

        GUI.skin.label.fontStyle = FontStyle.Bold;

        GUILayout.Space(16);
        GUILayout.Label("Information");

        GUI.skin.label.fontStyle = FontStyle.Normal;

        string isAvailable = navOctreeSpace.octree != null ? "YES" : "NO";
        GUILayout.Label($"Is Loaded\t\t    {isAvailable}");

        string size = "-";
        string maxDivisionLevel = "-";

        /*
        if (navOctreeSpace.octree != null) {
            size = $"{navOctreeSpace.octree.Size}";
            maxDivisionLevel = $"{navOctreeSpace.octree.MaxDivisionLevel}";
        }
        */

        GUILayout.Label($"Size\t\t\t    {size}");
        GUILayout.Label($"Max Division Level \t    {maxDivisionLevel}");

        GUILayout.Space(16);

        // Display buttons

        if (GUILayout.Button("Generate Octree")) {
            IEnumerator coroutine = NewOctreeGenerator.GenerateOctree(
                navOctreeSpace,
                maxDivisionLevel: navOctreeSpace.maxDivisionLevel,
                numJobs: navOctreeSpace.numCores
            );

            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        if (GUILayout.Button("Build Neighbors")) {
            if (navOctreeSpace.octree == null) return;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var ret = NewGraphGenerator.GenerateNeighbors(navOctreeSpace.octree);
            stopwatch.Stop();
            double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
            Debug.Log($"Built neighbors in {ms} ms");

            navOctreeSpace.octree.edges = ret;
        }

        /*
        if (GUILayout.Button("Save")) {
            navOctreeSpace.Save();
        }

        if (GUILayout.Button("Load")) {
            navOctreeSpace.Load();
        }

        if (GUILayout.Button("Build Neighbors")) {
            navOctreeSpace.BuildNeighbors();
        }
        */
    }
}

