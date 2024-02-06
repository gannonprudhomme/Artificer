using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OldGraphGeneratorComponent))]
public class GraphGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        OldGraphGeneratorComponent graphGenerator = (OldGraphGeneratorComponent)target;

        if (GUILayout.Button("Build Graph")) {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            graphGenerator.GenerateFullGraph();
            stopwatch.Stop();
            // Debug.Log($"Generated graph in {stopwatch.ElapsedMilliseconds} ms");
        }

        if (GUILayout.Button("Step")) {
            graphGenerator.Step();
        }

        if (GUILayout.Button("Reset")) {
            graphGenerator.ResetGenerator();
        }
    }
}
