using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathfinderComponent))]
public class PathfinderComponentEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        PathfinderComponent component = (PathfinderComponent)target;

        if (GUILayout.Button("Generate Path")) {
            component.GeneratePath();
        }
    }
}
