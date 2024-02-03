using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Can I move this into Scripts/Editor? I like it better there
[CustomEditor(typeof(Octree))]
public class TestEditor : Editor {
    public override void OnInspectorGUI() {
        // base.OnInspectorGUI(); // They might do the same thing idk
        DrawDefaultInspector();

        Octree octree = (Octree)target;
        
        if (GUILayout.Button("Bake")) {
            octree.Bake();
        }

        if (GUILayout.Button("Save to file")) {
            // Gotta figure out how to serialize the Octree
            octree.Save();
        }

        if (GUILayout.Button("Load from file")) {
            octree.Load();
        }
    }
}
