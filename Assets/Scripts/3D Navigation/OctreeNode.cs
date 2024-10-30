using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

// An OctreeNode is a node in an Octree
//
// If this is a leaf, it will be used as a node in the Graph that we use for pathfinding 
public class OctreeNode {
    public readonly int nodeLevel; // TODO: change to byte
    public readonly int[] index; // TODO: consider converting this to an int3

    // The size of the node in world space.
    // This is an integer because it's how octree's work! Dividing by 2, cleanly
    private readonly int nodeSize; // TODO: Could be a ushort
    public readonly Vector3 center;

    // 1D array that's actually 2x2x2 (8 total)
    public OctreeNode[]? children;
    public readonly bool containsCollision;

    public readonly bool isInBounds;

    public bool IsLeaf => children == null;

    // All of our leaf neighbors that are in bounds & don't contain a collision
    //
    // If *this* node contains a collision this will still be populated (assuming it has valid in bounds / no collision neighbors)
    // but those valid neighbors won't have an edge to *this* node. (i.e. it will be one-directional invalid -> valid, not invalid <-> valid)
    // We do this so we can find the nearest valid node to a position, even if the position contains a collision
    public Dictionary<OctreeNode, float>? neighbors = null;
    // Used when generating the Octree from a mesh
    public OctreeNode(
        int nodeLevel,
        int[] index,
        int octreeSize,
        Vector3 octreeCorner,
        bool isInBounds,
        bool containsCollision
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;

        this.nodeSize = octreeSize / (1 << nodeLevel);
        this.center = CalculateCenter(index, nodeSize, octreeCorner);

        this.isInBounds = isInBounds;
        this.containsCollision = containsCollision;
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
    // This is pretty fast - it runs in constant time, or rather O(maxDepth), which is usually around 8.
    // since, starting from the root, we only go into the node which contains the position.
    public OctreeNode? FindNodeForPosition(Vector3 position) {
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
            float childEdgeSize = nodeSize / 2f;
            Vector3 relativePosition = (position - center) / childEdgeSize;

            // Now that we know the "direction" of it, we can get which index
            // TODO: This can be simplifed, using either:
            // 1. Fast Parallel Surface & Solid Voxelization on GPUs
            // 2. https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations/
            int xIdx = relativePosition.x < 0 ? 0 : 1;
            int yIdx = relativePosition.y < 0 ? 0 : 1;
            int zIdx = relativePosition.z < 0 ? 0 : 1;

            // Since this isn't a leaf (it has children)
            // we can go into the child for it
            OctreeNode child = children![Get1DIndex(xIdx, yIdx, zIdx)];
            return child.FindNodeForPosition(position);
        }
    }

    // Optimization made: Changing this to share the same List between all calls reduced this total execution on 1.7M nodes from 0.5 sec -> 0.07 sec (reduction of ~86%)
    // Getting all nodes in a flat based is still faster (0.01 sec vs 0.07 sec for 1.7M nodes), but if we only get leaves then the pointer-based is faster (0.07 sec vs 0.35 sec)
    // since we don't have to do List.FindAll
    //
    // onlyLeaves = true doesn't change how long this takes in practice, which makes sense.
    public void GetAllNodes(List<OctreeNode> ret, bool onlyLeaves = false) {
        if (IsLeaf) { // if this one is a leaf
            // return just a list with just this and don't try to iterate over children
            ret.Add(this);
            return;
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
            neighbors ??= new();
            // TODO: Square magnitude might be fine? I think it's only used as a relative value
            float distance = Vector3.Distance(center, neighbor.center);

            // TODO: We're apparently adding duplicate keys - change this to .Add(key, value) and you'll see
            neighbors[neighbor] = distance;
        }
    }

    // TODO: This is unnecessary - if we passed in the center in the constructor
    // then we could just use the parent's center for the children (like we do for NewOctreeNode)
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
        return nodeCorner + (Vector3.one * (size / 2f));
    }
   
    // Get the 1D index of a 2x2x2 array
    public static int Get1DIndex(int x, int y, int z) {
        // x + (y * xMax) + (z * xMax * yMax)
        return x + (y * 2) + (z * 4);
    }

    public static (int x, int y, int z) GetCoordinatesFrom1D(int index) {
        int z = index / 4;
        int y = (index % 4) / 2;
        int x = index % 2;
        return (x, y, z);
    }
    
    private static readonly Color[] colors = new Color[] {
        Color.black,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white,
        Color.gray,
        Color.blue,
        Color.green,
        Color.red,
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
