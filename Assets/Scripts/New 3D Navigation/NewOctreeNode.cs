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
            return new int4(index[0], index[1], index[2], nodeLevel);
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
        int3 indexOffset = OctreeGenerationJob.childIndices[i];
        int childLevel = nodeLevel + 1;
        int3 childIndex = new(
            index[0] * 2 + indexOffset[0],
            index[1] * 2 + indexOffset[1],
            index[2] * 2 + indexOffset[2]
        );

        return new(childIndex, childLevel);
    }

    // TODO: I don't want this in here
    // move it elsewhere, probably in the generation job
    public readonly bool DoesIntersectTriangle(
        float3 p1, float3 p2, float3 p3,
        float tolerance = 0
    ) {
        // This code sucks (I'm basically just copy/pasting), probably find a better algo just so I understand this better

        // What is this doing
        // Probably print / visualize here
        p1 -= center;
        p2 -= center;
        p3 -= center;

        // Do axis check? Not sure what we're doing here
        float xMin, xMax, yMin, yMax, zMin, zMax;
        xMin = Mathf.Min(p1.x, p2.x, p3.x);
        xMax = Mathf.Max(p1.x, p2.x, p3.x);
        yMin = Mathf.Min(p1.y, p2.y, p3.y);
        yMax = Mathf.Max(p1.y, p2.y, p3.y);
        zMin = Mathf.Min(p1.z, p2.z, p3.z);
        zMax = Mathf.Max(p1.z, p2.z, p3.z);

        float radius = (size / 2) - tolerance;
        if (xMin >= radius || xMax < -radius || yMin >= radius || yMax < -radius || zMin >= radius || zMax < -radius) return false;

        // Wtf is n and d here
        // I'm guessing this has something to do with the plane?
        Vector3 n = Vector3.Cross(p2 - p1, p3 - p1);
        float d = Mathf.Abs(Vector3.Dot(p1, n));

        float radiusModified = radius * (Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z));
        bool isDMoreThanRadiusModified = d > radiusModified; // If you can't tell idk what this is
        if (isDMoreThanRadiusModified) {
            return false;
        }

        // Okay what the fuck is this.
        NativeArray<Vector3> points = new(new Vector3[] { p1, p2, p3 }, Allocator.Temp); // Temp = 1 frame
        NativeArray<Vector3> pointsSubtractedFromEachOther = new(new Vector3[] { p3 - p2, p1 - p3, p2 - p1 }, Allocator.Temp);
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                Vector3 a = Vector3.zero;
                a[i] = 1; // WHAT
                a = Vector3.Cross(a, pointsSubtractedFromEachOther[j]);

                float d1 = Vector3.Dot(points[j], a);
                float d2 = Vector3.Dot(points[(j + 1) % 3], a);

                float rr = radius * (Mathf.Abs(a[(i + 1) % 3]) + Mathf.Abs(a[(i + 2) % 3]));

                if (Mathf.Min(d1, d2) > rr || Mathf.Max(d1, d2) < -rr) {
                    points.Dispose();
                    pointsSubtractedFromEachOther.Dispose();

                    return false;
                }
            }
        }

        // TODO: Do we even need to do this? Won't it get cleaned up automatically?
        // https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/allocator-overview.html
        points.Dispose();
        pointsSubtractedFromEachOther.Dispose();

        return true;

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
}

