using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// An Octree is a tree data structure in which each internal node has exactly eight children.
// They are used here to divide the level by recursively subdividing the space into eight octants, or OctreeNodes in this codebase.
//
// This is specifically a pointer-based spare-voxel octree, in which we only store the root node, and we access other nodes (e.g. leaf nodes) by traversing the tree.
// The "sparse-voxel" means we only subdivide an octree node if there is actually a collision in it - if it's empty space, it will be represented by a larger node.
// More collisions = more subdivisions.
// This allows to significantly reduce the number of nodes in the octree, and thus the memory usage.
// 
// This class is intended as the data structure which holds the OctreeNodes, just like a LinkedList contains functions to operate w/ the nodes
// It uses the class OctreeGenerator to generate itself
// 
// It is held by NavOctreeSpace (equivalent of NavMeshSurface), which itself is accessed through OctreeManager (singleton)
//
// Heavily based off of https://github.com/supercontact/PathFindingEnhanced
public class Octree  {
    // Should be calculated based on the smallest agent's size
    public int MaxDivisionLevel { get; private set; }

    // Should be calculated based on the max size of the mesh
    // Must be a power of 2 so the octree divides evenly
    // Size^3 is the volume of the Octree
    public int Size { get; private set; }
    public Vector3 Center { get; private set; }
    public Vector3 Corner {
        get {
            return Center - (Vector3.one * Size / 2);
        }
    }

    public OctreeNode? root; // Is public so we can do GetAllNodesAndSetParentMap in OctreeSerializer, and set root during Deserializing

    // When created from Deserializing / loading from a file
    public Octree(
        int size,
        int maxDivisionLevel,
        Vector3 center
    ) {
        Size = size;
        MaxDivisionLevel = maxDivisionLevel;
        Center = center;
    }
    
    public OctreeNode? FindNodeForPosition(Vector3 position) {
        if (root == null) {
            Debug.LogError("Octree: No root to find nearest node for!");
            return null;
        }

        // I should probably check if this is even in the bounds of the Octree

        return root.FindNodeForPosition(position);
    }

    // Returns true if there's a collision between origin and endPosition.
    //
    // While this does run pretty fast, it could be much faster:
    //
    // Currently we sample by moving along the ray by a fixed distance (sampleDistance) and checking if we hit anything then move forward.
    // In actuality we should use the DDA Algorithm, which is like a "smart step" in that it knows exactly how far forward
    // along the ray we need move to find the bounds of the next entry, in our case to the next OctreeNode.
    //
    // But I'm currently too lazy to figure out the math, as it's complicated given we need to know the size of the OctreeNode we're in,
    // on top of the DDA Algorithm itself.
    public bool Raycast(Vector3 origin, Vector3 endPosition) {
        const float sampleDistance = 1f; // TODO: Replace this with DDA Algorithm later

        OctreeNode? currentNode = FindNodeForPosition(origin);
        if (currentNode == null) {
            Debug.LogError($"Couldn't find the node for position {origin}");
            return true; // Since true is the "stop" case
        }

        Vector3 rayDirection = (endPosition - origin).normalized;

        Vector3 currentPosition = origin;
        // Need to get a "time" for where we are on the line (with origin as start & endPosition as the end)
        float totalDistance = Vector3.Distance(origin, endPosition);

        while (Vector3.Distance(currentPosition, origin) < totalDistance) { // Aka while we haven't reached the end position
            currentNode = FindNodeForPosition(currentPosition);

            if (currentNode!.containsCollision) {
                // We could theoretically figure out where we intersected this node, but it doesn't matter
                return true;
            }

            // Move it forward!
            // TODO: This is about where we'll put DDA Algorithm in
            currentPosition += rayDirection * sampleDistance;
        }

        // We didn't hit anything (since we never returned and the loop break'd)
        return false;
    }

    public List<OctreeNode> GetAllNodes(bool onlyLeaves = false) {
        if (root == null) {
            Debug.LogError("Root was null!");
            return new();
        }

        List<OctreeNode> ret = new();

        root.GetAllNodes(ret, onlyLeaves);

        return ret;
    }
}

