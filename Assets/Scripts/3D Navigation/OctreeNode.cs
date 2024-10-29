using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

// An OctreeNode is a node in an Octree
//
// If this is a leaf, it will be used as a node in the Graph that we use for pathfinding 
public class OctreeNode {
    public readonly int nodeLevel;
    public readonly int[] index;

    // These are public only for gizmos in NavOctreeSpace

    // The size of the node in world space.
    // This is an integer because it's how octree's work! Dividing by 2, cleanly
    public readonly int nodeSize;
    public readonly Vector3 center;

    // 1D array that's actually 2x2x2 (8 total)
    public OctreeNode[]? children; 
    public bool containsCollision { get; private set; }

    public bool isInBounds = false;

    public bool IsLeaf => children == null;

    // All leaf neighbors
    //
    // Only populated for leaves (regardless of whether they contain a collision or not)
    public List<OctreeNode>? neighbors = null;

    // All of our leaf neighbors that are in bounds & don't contain a collision
    //
    // If *this* node contains a collision/isn't in bounds,
    // this will still be populated (assuming it has valid in bounds / no collision neighbors)
    // but those valid neighbors won't have an edge to *this* node. (i.e. it will be one-directional invalid -> valid, not invalid <-> valid)
    public List<OctreeNode>? inBoundsNeighborsWithoutCollisions = null;

    // Used for deserializing
    public int4 dictionaryKey {
        get {
            return new(index[0], index[1], index[2], nodeLevel);
        }
    }

    // Used when generating the Octree from a mesh
    public OctreeNode(
        int nodeLevel,
        int[] index,
        int octreeSize,
        Vector3 octreeCorner
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;

        this.nodeSize = octreeSize / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octreeCorner);

        containsCollision = false;
    } 

    public OctreeNode(
        int nodeLevel,
        int[] index,
        Octree octree,
        // Vector3 center,
        bool containsCollision,
        bool isInBounds
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;
        this.containsCollision = containsCollision;
        this.isInBounds = isInBounds;

        nodeSize = octree.Size / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octree.Corner);
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
            OctreeNode child = children![Get1DIndex(xIdx, yIdx, zIdx)];
            return child.FindNodeForPosition(position);
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
        
        for(int i = 0; i < 8; i++) {
            OctreeNode child = children![i];

            child.GetAllNodes(ret, onlyLeaves);
        }
    }

    public void AddEdgeTo(OctreeNode neighbor) {
        // We only want to connect to neighbors that are valid (in bounds & no collisions)
        bool neighborValid = neighbor.isInBounds && !neighbor.containsCollision;
        if (neighborValid) {
            inBoundsNeighborsWithoutCollisions ??= new();

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
   
    // Get the 1D index of a 2x2x2 array
    public static int Get1DIndex(int x, int y, int z) {
        // x + (y * xMax) + (z * xMax * yMax)
        return x + (y * 2) + (z * 4);
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

    public void DrawGizmos(bool displayIndicesText, Color textColor) {
        Gizmos.color = colors[nodeLevel % colors.Length];
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
