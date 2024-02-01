using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OldGraphComponent))]
public class OldGraphComponentEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        OldGraphComponent graphComponent = (OldGraphComponent)target;

        if (GUILayout.Button("Build Graph")) {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            graphComponent.GenerateGraph();
            stopwatch.Stop();
            // Debug.Log($"Generated graph in {stopwatch.ElapsedMilliseconds} ms");
        }

        if (GUILayout.Button("Step")) {
            graphComponent.DoStep();
        }

        if (GUILayout.Button("Reset")) {
            graphComponent.ResetSteps();
        }
    }
}
