using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GraphComponent))]
public class GraphComponentEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GraphComponent graphComponent = (GraphComponent)target;

        if (GUILayout.Button("Build Graph")) {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            graphComponent.GenerateGraph();
            stopwatch.Stop();
            Debug.Log($"Generated graph in {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
