using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using System.Data;
using Unity.Jobs;


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

        if (GUILayout.Button("Get all nodes")) {
            navOctreeSpace.TempGetAllNodes();
        }

        if (GUILayout.Button("Generate Octree")) {
            OctreeGenerator.GenerateOctreeJob? job = navOctreeSpace.GenerateOctree();

            if (job is OctreeGenerator.GenerateOctreeJob generateJob) {
                JobHandle jobHandle = generateJob.Schedule();

                EditorCoroutineUtility.StartCoroutine(ReportGenerateProgress(generateJob, jobHandle, navOctreeSpace), this);
            }
        }

        if (GUILayout.Button("Mark In-Bounds leaves")) {
            navOctreeSpace.MarkInboundsLeaves();
        }

        if (GUILayout.Button("Save")) {
            navOctreeSpace.Save();
        }

        if (GUILayout.Button("Load")) {
            navOctreeSpace.Load();
        }

        if (GUILayout.Button("Build Neighbors")) {
            navOctreeSpace.BuildNeighbors();
        }
    }

    private IEnumerator ReportGenerateProgress(OctreeGenerator.GenerateOctreeJob job, JobHandle jobHandle, NavOctreeSpace space) {
        Debug.Log($"Started job with {OctreeGenerator.GenerateOctreeJob.size} triangles");
        // Create a new progress indicator
        int progressId = Progress.Start("Generate octree");
        while (OctreeGenerator.GenerateOctreeJob.status < OctreeGenerator.GenerateOctreeJob.size - 1) {
            Progress.Report(progressId, (float) OctreeGenerator.GenerateOctreeJob.status / (float) OctreeGenerator.GenerateOctreeJob.size);
            yield return null;
        }

        jobHandle.Complete();

        space.octree.root = job.root.Value;

        // The task is finished. Remove the associated progress indicator.
        Progress.Remove(progressId);
    }
}
