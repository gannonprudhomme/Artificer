using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

#nullable enable

public struct NewOctreeNode {
    public readonly byte nodeLevel;

    // Length of a side
    public readonly float size;

    // TODO: Should I convert this to uint3?
    // Per https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations/
    // I could just store this as a single int, since 2^10 is 1024 which *should* be plenty for my needs (i.e. my needed max division levels / depth)
    public readonly int3 index;

    public readonly float3 center; // aka position

    public bool hasChildren;
    public bool containsCollision;
    public bool inBounds;

    // Somehow computed this every time it's needed is faster than creating it & storing it upon initialization.
    // I do not understand how. 
    public readonly int4 dictionaryKey => new int4(index.x, index.y, index.z, nodeLevel);

    public readonly bool isLeaf {
        get {
            return !hasChildren;
        }
    }

    public NewOctreeNode(
        byte nodeLevel,
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

        // E.g. if the index is (0, 1, 1) level 1 and the offset is (0, 0, 1), which is the bottom left child
        // then the child's index will (0, 2, 3) level 2
        int3 childIndex = new(
            (index[0] * 2) + indexOffset.x,
            (index[1] * 2) + indexOffset.y,
            (index[2] * 2) + indexOffset.z
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

