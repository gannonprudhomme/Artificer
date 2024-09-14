using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class OctreeNode {
    public readonly int nodeLevel;
    public readonly int[] index;
    // We really shouldn't need this
    // We just have it for bounds / sizes
    private readonly Octree octree;

    // These are public only for gizmos in NavOctreeSpace

    // The size of the node in world space.
    // This is an integer because it's how octree's work! Dividing by 2, cleanly
    public readonly int nodeSize;
    public readonly Vector3 center;

    public OctreeNode[,,]? children; 
    public bool containsCollision { get; private set; }

    public bool childrenContainsCollision { get; private set; }

    public bool isInBounds = false; // { get; private set; }

    public bool IsLeaf {
        get { return children == null; }
    }

    // Used when generating the Octree from a mesh
    public OctreeNode(
        int nodeLevel,
        int[] index,
        Octree octree
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;
        this.octree = octree;

        this.nodeSize = octree.Size / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octree);

        containsCollision = false;
        childrenContainsCollision = false;
    } 

    public OctreeNode(
        int nodeLevel,
        int[] index,
        Octree octree,
        // Vector3 center,
        bool containsCollision,
        bool childrenContainsCollision,
        bool isInBounds
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;
        this.octree = octree;
        this.containsCollision = containsCollision;
        this.childrenContainsCollision = childrenContainsCollision;
        this.isInBounds = isInBounds;

        nodeSize = octree.Size / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octree);
    }

    // Finds the node that contains this position, recursively,
    // assuming the position is in bounds of the node.
    public OctreeNode? FindNodeForPosition(Vector3 position) {
        // Check bounds
        bool isPositionInBoundsOfOctree = true; // We can do this with the node
        if (!isPositionInBoundsOfOctree) {
            Debug.LogError("Checking for position which isn't even in the bounds of this!");
            return null;
        }

        // Imagine we're starting at the root? Idk

        // We need the bounds of this to first check if the position is even in the bounds
        Bounds bounds = new(center, size: Vector3.one * nodeSize);

        bool isInBounds = bounds.Contains(position);
        if (!isInBounds) {
            Debug.LogError("Not in bounds! This should never happen");
            return null;
        } else if (IsLeaf) { // aka children == null
            return this;
        } else {
            // Since this isn't a leaf, we need to go into the children
            // which we'll do recursively

            // It's in bounds of this, so "normalize" it w/ the center
            // so if the center is index: (0, 0, 0) the signs of the relative position will indicate which index it is
            float childEdgeSize = nodeSize / 2;
            Vector3 relativePosition = (position - center) / childEdgeSize;

            // Now that we know the "direction" of it, we can get which indiex
            int xIdx = relativePosition.x < 0 ? 0 : 1;
            int yIdx = relativePosition.y < 0 ? 0 : 1;
            int zIdx = relativePosition.z < 0 ? 0 : 1;

            // Since this isn't a leaf (it has children)
            // we can go into the child for it
            OctreeNode child = children![xIdx, yIdx, zIdx];
            return child.FindNodeForPosition(position);
        }
    }

    // *** GENERATION FUNCTIONS **/

    public void DivideTriangleUntilLevel(
        Vector3 point1,
        Vector3 point2,
        Vector3 point3,
        int maxDivisionLevel
    ) {
        if (!DoesThisIntersectTriangle(point1, point2, point3)) return;

        // It does intersect the triangle! Lets break it up
        if (nodeLevel < maxDivisionLevel) { // If we're not at the smallest node level yet (we can keep dividing)
            // We're breaking it up because there's a collision
            // Thus we know this one's children will contains collisions, so mark it as such
            childrenContainsCollision = true;

            // Create children if necessary
            CreateChildrenIfHaventYet();
    
            // Call it for all of the children
            for(int x = 0; x < 2; x++) {
                for(int y = 0; y < 2; y++) {
                    for(int z = 0; z < 2; z++) {
                        children![x, y, z].DivideTriangleUntilLevel(point1, point2, point3, maxDivisionLevel);
                    }
                } 
            }
        } else { // We've divided as much as possible, lets mark it and don't divide any further.
            containsCollision = true;
        }
    }

    // This is basically directly copied from PathfindingEnhanced, and I really don't understand what it does
    // but it works so idrc
    private bool DoesThisIntersectTriangle(Vector3 p1, Vector3 p2, Vector3 p3, float tolerance = 0) {
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

        float radius = (nodeSize / 2) - tolerance;
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
        Vector3[] points = { p1, p2, p3 };
        Vector3[] pointsSubtractedFromEachOther = { p3 - p2, p1 - p3, p2 - p1 };
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                Vector3 a = Vector3.zero;
                a[i] = 1; // WHAT
                a = Vector3.Cross(a, pointsSubtractedFromEachOther[j]);

                float d1 = Vector3.Dot(points[j], a);
                float d2 = Vector3.Dot(points[(j + 1) % 3], a);

                float rr = radius * (Mathf.Abs(a[(i + 1) % 3]) + Mathf.Abs(a[(i + 2) % 3]));

                if (Mathf.Min(d1, d2) > rr || Mathf.Max(d1, d2) < -rr) {
                    return false;
                }
            }
        }

        return true;
    } 

    // Divide this node into children
    // Makes this no longer a leaf node!
    private void CreateChildrenIfHaventYet() {
        if (children != null) {
            // Debug.LogError("We've already split up this! Why are we doing it again?");
            return;
        }

        children = new OctreeNode[2, 2, 2]; // Create 8 children

        // Populate them
        for (int x = 0; x < 2; x++) {
            for(int y = 0; y < 2; y++) {
                for(int z = 0; z < 2; z++) {
                    int[] newIndex = {
                        index[0] * 2 + x, // x,y,z are either 0 or 1
                        index[1] * 2 + y,
                        index[2] * 2 + z
                    };

                    children[x, y, z] = new OctreeNode(nodeLevel + 1, newIndex, octree);
                }
            }
        }
    }

    public List<OctreeNode> GetAllNodes(bool onlyLeaves = false) {
        List<OctreeNode> ret = new();

        if (IsLeaf) { // if this one is a leaf
            // return just a list with just this and don't try to iterate over children
            ret.Add(this);
            return ret;
        };

        // Only adds this node if we're asking for all nodes (and not only leaves)
        if (!onlyLeaves) {
            ret.Add(this);
        }

        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 2; y++) {
                for (int z = 0; z < 2; z++) {
                    OctreeNode child = children![x, y, z];

                    ret.AddRange(child.GetAllNodes(onlyLeaves));
                }
            }
        }

        return ret;
    }

    // Calculate what the center for this node is
    // 
    // Called upon initialization to set OctreeNode.center
    private static Vector3 CalculateCenter(
        int[] index,
        int size,
        Octree octree
    ) {
        // Get the bottom-left (?) corner
        Vector3 indexVec = new(index[0], index[1], index[2]);
        Vector3 nodeCorner = octree.Corner + (size * indexVec);

        // Then move it by (0.5, 0.5, 0.5) [when size = 1] to get it to the center
        return nodeCorner + (Vector3.one * (size / 2));
    }

    public void DrawGizmos(bool displayIndicesText, Color textColor) {
        Gizmos.DrawWireCube(center, Vector3.one * nodeSize);

        if (displayIndicesText) {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            Vector3 offsetPosition = center + new Vector3(0, 0.5f, 0.0f);

            string output = $"{index[0]}, {index[1]}, {index[2]} ({nodeLevel})";
            GUIStyle style = new();
            style.normal.textColor = textColor;
            UnityEditor.Handles.Label(offsetPosition, output, style);
            #endif

        }
    }
}
