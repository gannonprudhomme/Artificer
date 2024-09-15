using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


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
            navOctreeSpace.GenerateOctree();
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
}
