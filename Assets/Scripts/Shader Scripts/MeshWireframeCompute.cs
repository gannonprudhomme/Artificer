using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshWireframeCompute : MonoBehaviour {
#if UNITY_EDITOR
    private void OnValidate() { // what does this do
        UpdateMesh();
    }
#endif

    [ContextMenu("Update Mesh")]
    public void UpdateMesh(){
        if (!gameObject.activeSelf || !GetComponent<MeshRenderer>().enabled) {
            Debug.LogError("Object wasn't active or MeshRenderer was disabled");
        }

        Mesh m = GetComponent<MeshFilter>().sharedMesh;
        if (m == null) {
            Debug.LogError("MeshFilter was null");
            return;
        }

        // compute and store vertex colors for the wireframe shader
        Color[] colors = sortedColoring(m);

        if (colors != null) {
            print("Setting colors");
            m.SetColors(colors);
        } else {
            Debug.LogError("Colors were null");
        }
    }

    // Graph coloring algorithm, copy & pasted (from https://www.youtube.com/watch?v=xEmyl5_wYqk)
    private Color[] sortedColoring(Mesh mesh) {
        Color[] _COLORS = new Color[] {
            Color.red,
            Color.green,
            Color.blue,
        };

        int n = mesh.vertexCount;
        int[] labels = new int[n];

        List<int[]> triangles = getSortedTriangles(mesh.triangles);
        triangles.Sort((int[] t1, int[] t2) => {
            int i = 0;
            while (i < t1.Length && i < t2.Length) {
                if (t1[i] < t2[i]) return -1;
                if (t1[i] > t2[i]) return 1;
                i += 1;
            }
            if (t1.Length < t2.Length) return -1;
            if (t1.Length > t2.Length) return 1;
            return 0;
        });

        foreach (int[] triangle in triangles) {
            List<int> availableLabels = new List<int>() { 1, 2, 3 };
            foreach (int vertexIndex in triangle) {
                if (availableLabels.Contains(labels[vertexIndex]))
                    availableLabels.Remove(labels[vertexIndex]);
            }
            foreach (int vertexIndex in triangle) {
                if (labels[vertexIndex] == 0) {
                    if (availableLabels.Count == 0) {
                        Debug.LogError("Could not find color");
                        return null;
                    }
                    labels[vertexIndex] = availableLabels[0];
                    availableLabels.RemoveAt(0);
                }
            }
        }

        Color[] colors = new Color[n];
        for (int i = 0; i < n; i++)
            colors[i] = labels[i] > 0 ? _COLORS[labels[i] - 1] : _COLORS[0];

        return colors;
    }

    private List<int[]> getSortedTriangles(int[] triangles) {
        List<int[]> result = new List<int[]>();
        for (int i = 0; i < triangles.Length; i += 3) {
            List<int> t = new List<int> { triangles[i], triangles[i + 1], triangles[i + 2] };
            t.Sort();
            result.Add(t.ToArray());
        }

        return result;
    }
}
