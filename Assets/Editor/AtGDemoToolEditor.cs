using System.Collections;
using System.Collections.Generic;
using Codice.Client.Commands;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AtGDemoTool))] // Honestly no fucking clue
public class AtGDemoToolEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        
        AtGDemoTool demoTool = (AtGDemoTool) target;

        if (GUILayout.Button("Spawn")) {
            demoTool.SpawnProjectile();
        }

        if (GUILayout.Button("hide")) {
            Tools.current = Tool.None;
        }

        if (GUILayout.Button("show")) {
            Tools.current = Tool.Transform;
        }
    }
}
