using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public struct NewOctreeNode {
    public readonly int nodeLevel; // TODO: byte maybe? This will only be max of maybe 16?

    // Length of a side
    public readonly float size;

    public readonly int3 index;

    public readonly float3 center; // aka position

    public bool hasChildren;
    public bool containsCollision;
    public bool inBounds;

    public readonly int4 dictionaryKey {
        get {
            return new int4(index.x, index.y, index.z, nodeLevel);
        }
    }

    public readonly bool isLeaf {
        get {
            return !hasChildren;
        }
    }

    public NewOctreeNode(
        int nodeLevel,
        float size,
        int3 index,
        float3 center
    ) {
        this.nodeLevel = nodeLevel;
        this.size = size;
        this.index = index;
        this.center = center;
        this.hasChildren = false;
        this.containsCollision = false;
        this.inBounds = false;
    }

    public readonly int4 GetChildKey(int i) {
        int3 indexOffset = childIndices[i];
        int childLevel = nodeLevel + 1;
        int3 childIndex = new(
            index[0] * 2 + indexOffset.x,
            index[1] * 2 + indexOffset.y,
            index[2] * 2 + indexOffset.z
        );

        return new(childIndex, childLevel);
    }

    public static readonly int3[] childIndices = new int3[8]{
        new(0, 0, 0),
        new(0, 0, 1),
        new(1, 0, 0),
        new(1, 0, 1),
        new(0, 1, 0),
        new(0, 1, 1),
        new(1, 1, 0),
        new(1, 1, 1)
    };

    // TODO: Idk if I should have this in here
    public readonly void DrawGizmos(bool displayIndicesText, Color textColor) {
        Gizmos.color = colors[nodeLevel % colors.Length];
        Gizmos.DrawWireCube(center, Vector3.one * size);

        if (displayIndicesText) {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            Vector3 offsetPosition = new Vector3(center[0], center[1], center[2]) + new Vector3(0, 0.5f, 0.0f);

            string output = $"{index[0]}, {index[1]}, {index[2]} ({nodeLevel})";
            GUIStyle style = new();
            style.normal.textColor = textColor;
            UnityEditor.Handles.Label(offsetPosition, output, style);
            #endif
        }
    }

    private static readonly Color[] colors = new Color[] {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white,
        Color.black,
        Color.gray
    };
}

