using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathfindObject))] // Honestly no fucking clue
public class PathfinderEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        
        PathfindObject pathfindObject = (PathfindObject) target;

        if (GUILayout.Button("Generate raw path")) {
            pathfindObject.GenerateRawPath();
        }

        if (GUILayout.Button("Generate smoothed path")) {
            pathfindObject.GenerateSmoothedPath();
        }
        
        if (GUILayout.Button("Generate Spline")) {
            pathfindObject.GenerateSpline();
        }
        
        if (GUILayout.Button("Remove Splines")) {
            pathfindObject.RemoveSplines();
        }
    }
}
