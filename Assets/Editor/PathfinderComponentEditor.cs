using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OldPathfinderComponent))]
public class PathfinderComponentEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        OldPathfinderComponent component = (OldPathfinderComponent)target;

        if (GUILayout.Button("Generate Path")) {
            component.GeneratePath();
        }
    }
}
