using Codice.CM.Common.Tree;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

#nullable enable

// An OctreeNode is a node in an Octree
//
// If this is a leaf, it will be used as a node in the Graph that we use for pathfinding 
public struct OctreeNode {
    public readonly int nodeLevel;

    // While I'm here, this needs a better name
    // or at least I need to understand it better, cause I only kind of do
    //
    // E.g. why are there multiple (0, 0, 0) index nodes? Is that intended?
    public readonly UnsafeList<int> index; // TODO: Does this need to be an UnsafeList?
    // We really shouldn't need this
    // We just have it for bounds / sizes
    private readonly Vector3 octreeCorner;
    private readonly int octreeSize;

    // These are public only for gizmos in NavOctreeSpace

    // The size of the node in world space.
    // This is an integer because it's how octree's work! Dividing by 2, cleanly
    public readonly int nodeSize;
    public readonly Vector3 center;

    // Might need to do a native array
    // public OctreeNode[,,]? children; 

    // Fuck it I just need to make this a 1D array urghhh
    // [NativeDisableContainerSafetyRestriction] 
    // public NativeArray<OctreeNode> children;

    // This has to be an `UnsafeList`, rather than a `NativeArray`
    //
    // T
    public bool isChildrenCreated;
    public UnsafeList<OctreeNode> children; // TODO: Consider making optional for leaf nodes?

    public bool containsCollision { get; private set; }

    public bool childrenContainsCollision { get; private set; }

    public bool isInBounds; // = false;

    public readonly bool IsLeaf {
        get {
            // return children.IsEmpty;
            return !isChildrenCreated;
        }
    }

    // All leaf neighbors
    //
    // Only populated for leaves (regardless of whether they contain a collision or not)

    // This isn't going to work - it relies on pointers
    // and we only have copies
    // how else can we do this? Maybe store indices and then look them up?
    // public List<OctreeNode>? neighbors; // = null;

    // All of our leaf neighbors that are in bounds & don't contain a collision
    //
    // If *this* node contains a collision/isn't in bounds,
    // this will still be populated (assuming it has valid in bounds / no collision neighbors)
    // but those valid neighbors won't have an edge to *this* node. (i.e. it will be one-directional invalid -> valid, not invalid <-> valid)
    public UnsafeList<OctreeNode> inBoundsNeighborsWithoutCollisions;

    // Used when generating the Octree from a mesh
    public OctreeNode(
        int nodeLevel,
        int[] index,
        Vector3 octreeCorner,
        int octreeSize
    ) {
        if (index[0] == 0 && index[1] == 0 && index[2] == 0 && nodeLevel == 0) {
            // Debug.Log($"init for root {nodeLevel}");
        }

        this.nodeLevel = nodeLevel;
        this.index = new UnsafeList<int>(index.Length, Allocator.Persistent);
        for(int i = 0; i < index.Length; i++) {
            this.index.Add(index[i]);
        }

        this.octreeCorner = octreeCorner;
        this.octreeSize = octreeSize;

        this.isChildrenCreated = false;
        this.children = new(2 * 2 * 2, Allocator.Persistent);
        // this.children = new(0, Allocator.TempJob);
        this.isInBounds = false; // Ack
        this.inBoundsNeighborsWithoutCollisions = new(0, Allocator.Persistent);

        this.nodeSize = octreeSize / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octreeCorner);

        containsCollision = false;
        childrenContainsCollision = false;
    } 

    public OctreeNode(
        int nodeLevel,
        int[] index,
        Vector3 octreeCorner,
        int octreeSize,
        // Vector3 center,
        bool containsCollision,
        bool childrenContainsCollision,
        bool isInBounds
    ) {
        this.nodeLevel = nodeLevel;
        this.index = new UnsafeList<int>(index.Length, Allocator.Persistent);
        for(int i = 0; i < index.Length; i++) {
            this.index.Add(index[i]);
        }

        this.containsCollision = containsCollision;
        this.childrenContainsCollision = childrenContainsCollision;
        this.isInBounds = isInBounds;
        this.octreeCorner = octreeCorner;
        this.octreeSize = octreeSize;
        // TODO: I'm going to need to populate this
        this.isChildrenCreated = false;
        this.children = new(2 * 2 * 2, Allocator.Persistent);
       // this.children = new(0, Allocator.TempJob);
        this.inBoundsNeighborsWithoutCollisions = new(0, Allocator.Persistent);

        nodeSize = octreeSize / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octreeCorner);
    }

    // Finds the node that contains this position, recursively,
    // assuming the position is in bounds of the node.
    //
    // This is pretty fast - it runs in constant time, or rather O(maxNumberOfDivisions), which is usually around 8.j
    // since, starting from the root, we only go into the node which contains the position.
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
            // OctreeNode child = children![xIdx, yIdx, zIdx];
            OctreeNode child = children[Get1DIndex(xIdx, yIdx, zIdx)]; // This is return by value! we need to re-assign it

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
        string indexStr = $"({this.index[0]}, {this.index[1]}, {this.index[2]})";
        if (!DoesThisIntersectTriangle(point1, point2, point3)) {
            // Debug.Log($"Not dividing {indexStr}");
            return;
        }

        // Debug.Log($"Divide for index {indexStr}");

        // It does intersect the triangle! Lets break it up
        if (nodeLevel < maxDivisionLevel) { // If we're not at the smallest node level yet (we can keep dividing)
            // We're breaking it up because there's a collision
            // Thus we know this one's children will contains collisions, so mark it as such
            childrenContainsCollision = true;

            // Create children if necessary
            PopulateChildrenIfHaventYet();
    
            // Call it for all of the children
            for(int x = 0; x < 2; x++) {
                for(int y = 0; y < 2; y++) {
                    for(int z = 0; z < 2; z++) {
                        int index = Get1DIndex(x, y, z);
                        var childCopy = children[index];

                        // "So changing a structs state internally via calling the structs methods/properties will update the original struct.
                        // But changing a struct’s state externally by setting the struct’s fields directly will return a new copy of that struct with the updated state."
                        // Source: https://discussions.unity.com/t/changing-struct-is-changing-original-value/718582/6
                        // 
                        // So this should be fine?
                        childCopy.DivideTriangleUntilLevel(point1, point2, point3, maxDivisionLevel);

                        children[index] = childCopy;
                    }
                } 
            }
        } else { // We've divided as much as possible, lets mark it and don't divide any further.
            containsCollision = true;
        }

        // Debug.Log($"Length of all nodes for index {indexStr}: {GetAllNodes().Length}");
    }

    // This is basically directly copied from PathfindingEnhanced, and I really don't understand what it does
    // but it works so idrc
    private readonly bool DoesThisIntersectTriangle(Vector3 p1, Vector3 p2, Vector3 p3, float tolerance = 0) {
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

    // Divide this node into children
    // Makes this no longer a leaf node!
    private void PopulateChildrenIfHaventYet() {
        /*
        if (!children.IsEmpty) { // Will this ever fail? I don't think so
            // Debug.LogError("We've already split up this! Why are we doing it again?");
            return;
        }

        if (children.Length == 0) { // Bleh can I do tihs?
            if (index[0] == 0 && index[1] == 0 && index[2] == 0) {
                Debug.Log($"Creating for root! {children.IsEmpty} {children.IsCreated}");
            }

            children = new(2 * 2 * 2, Allocator.Persistent); // Could've sworn we can't do this?
        }
        */

        if (isChildrenCreated) return;

        isChildrenCreated = true;

        string indexStr = $"({this.index[0]}, {this.index[1]}, {this.index[2]})";

        /*
        if (index[0] == 0 && index[1] == 0 && index[2] == 0) {
            Debug.Log($"Creating for root! {children.IsEmpty} {children.IsCreated}");
        }
        */

        // Populate them
        for (int x = 0; x < 2; x++) {
            for(int y = 0; y < 2; y++) {
                for(int z = 0; z < 2; z++) {
                    // Creating managed memory!? Why can we even do this?
                    // why isn't it yelling at us?
                    int[] newIndex = {
                        index[0] * 2 + x, // x,y,z are either 0 or 1
                        index[1] * 2 + y,
                        index[2] * 2 + z
                    };

                    // children[Get1DIndex(x, y, z)] = new OctreeNode(nodeLevel + 1, newIndex, octreeCorner, octreeSize);
                    children.Add(new OctreeNode(nodeLevel + 1, newIndex, octreeCorner, octreeSize));
                }
            }
        }

        // Debug.Log($"Created children for level *{nodeLevel}* {indexStr}, length: {children.Length}");
    }

    // Shouldn't this have to be an UnsafeList? I thought we couldn't nest?
    // I guess we might be able to put an UnsafeList in a NativeList?
    public NativeList<OctreeNode> GetAllNodes(bool onlyLeaves = false) {
        NativeList<OctreeNode> ret = new(Allocator.Persistent); // TODO: This shouldn't be persistent, it should really be Temp (and we return a managed copy, e.g. List<T>)

        if (IsLeaf) { // if this one is a leaf
            // return just a list with just this and don't try to iterate over children
            // Debug.Log($"Adding leaf ({index[0]}, {index[1]}, {index[2]})");
            ret.Add(this);
            return ret;
        };

        // Only adds this node if we're asking for all nodes (and not only leaves)
        if (!onlyLeaves) {
            // Debug.Log($"Adding self ({index[0]}, {index[1]}, {index[2]})");
            ret.Add(this);
        }

        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 2; y++) {
                for (int z = 0; z < 2; z++) {
                    OctreeNode child = children[Get1DIndex(x, y, z)];

                    ret.AddRange(child.GetAllNodes(onlyLeaves).AsArray()); // Why can't I just append a list('s contents)
                }
            }
        }

        return ret;
    }

    public void AddEdgeTo(OctreeNode neighbor) {
        // We only want to connect to neighbors that are valid (in bounds & no collisions)
        bool neighborValid = neighbor.isInBounds && !neighbor.containsCollision;
        if (neighborValid) {
            // inBoundsNeighborsWithoutCollisions ??= new();

            inBoundsNeighborsWithoutCollisions.Add(neighbor);
        }
    }

    // Calculate what the center for this node is
    // 
    // Called upon initialization to set OctreeNode.center
    private static Vector3 CalculateCenter(
        int[] index,
        int size,
        Vector3 octreeCorner
    ) {
        // Get the bottom-left (?) corner
        Vector3 indexVec = new(index[0], index[1], index[2]);
        Vector3 nodeCorner = octreeCorner + (size * indexVec);

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

    public readonly OctreeNode? GetChildAt(int x, int y, int z) {
        if (x < 0 || x > 1 || y < 0 || y > 1 || z < 0 || z > 1) {
            Debug.LogError("Invalid child index");
            return null;
        }

        return children[Get1DIndex(x, y, z)];
    }

    // Get the 1D index of a 2x2x2 array
    //
    // I think this is a morton code?
    public static int Get1DIndex(int x, int y, int z) {
        // x + (y * xMax) + (z * xMax * yMax)
        return x + (y * 2) + (z * 2 + 2);
    }
}
