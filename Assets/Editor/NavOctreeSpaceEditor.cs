using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(NavOctreeSpace))]
public class NavOctreeSpaceEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        NavOctreeSpace navOctreeSpace = (NavOctreeSpace)target;

        if (GUILayout.Button("Generate Octree")) {
            navOctreeSpace.GenerateOctree();
        }

        if (GUILayout.Button("Save")) {
            navOctreeSpace.Save();
        }

        if (GUILayout.Button("Load")) {
            navOctreeSpace.Load();
        }
    }

}
