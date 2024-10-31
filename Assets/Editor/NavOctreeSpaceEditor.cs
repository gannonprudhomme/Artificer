using System.Collections;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;


[CustomEditor(typeof(NavOctreeSpace))]
public class NavOctreeSpaceEditor : Editor {
    // The spaces in the Label strings are in fact purposeful - this is how I'm doing alignment lmfao
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        NavOctreeSpace navOctreeSpace = (NavOctreeSpace)target;

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
        if (navOctreeSpace.octree != null) {
            size = $"{navOctreeSpace.octree.Size}";
            maxDivisionLevel = $"{navOctreeSpace.octree.MaxDivisionLevel}";
        }

        GUILayout.Label($"Size\t\t\t    {size}");
        GUILayout.Label($"Max Division Level \t    {maxDivisionLevel}");

        GUILayout.Space(16);

        // Display buttons

        if (GUILayout.Button("Generate Octree")) {
            navOctreeSpace.SetOctree(null); // TODO: It looks weird sometimes
            IEnumerator coroutine = NewOctreeGenerator.GenerateOctree(
                navOctreeSpace,
                maxDivisionLevel: navOctreeSpace.MaxDivisionLevel,
                numJobs: 12 
            );

            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        if (GUILayout.Button("Save")) {
            OctreeSerializer.Save(navOctreeSpace);
        }

        if (GUILayout.Button("Load")) {
            navOctreeSpace.SetOctree(null);
            Octree octree = OctreeSerializer.Load(navOctreeSpace.GetFileName());
            navOctreeSpace.SetOctree(octree);
        }

        if (GUILayout.Button("Build Neighbors (Octree)")) {
            if (navOctreeSpace.octree == null) { Debug.LogError("No octree!"); return;}

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            GraphGenerator.PopulateOctreeNeighbors(navOctreeSpace.octree, shouldBuildDiagonals: true);
            stopwatch.Stop();

            double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
            double seconds = ms / 1000d;

            Debug.Log($"Finished building neighbors in {seconds:F2} sec ({ms:F0} ms)");
        }
    }
}
