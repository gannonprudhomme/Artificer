using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// An Octree is a tree data structure in which each internal node has exactly eight children.
// They are used here to divide the level by recursively subdividing the space into eight octants, or OctreeNodes in this codebase.
// 
// This class is intended as the data structure which holds the OctreeNodes, just like a LinkedList really just contains functions to operate w/ the nodes
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

    // Aka bake
    public void Generate(GameObject rootGameObject) {
        // Shit I should probably read that research paper
        // Cause they create the Octree differently

        // I also need to figure out how to prevent the "hollow mesh" problem, where if the mesh is big enough (relative to the octree nodes)
        // it will say it's empty space inside of the mesh
        // I could just remove all of the disjoint graphs (other than the biggest one) since it shouldn't be navigatable anyways

        // I'm still not sure if we want to have mulitple octrees or not
        // or if we can just filter (with OctreeNode.doChildrenContainCollision) out the smallest nodes based on the agent
        // to generate the graph for an agent

        // Rein in a bit here though - this really doesn't need to be that complex/perfect for what I want to do

        root = new OctreeNode(0, new int[] { 0, 0, 0 }, this.Size, this.Corner);

        OctreeGenerator.GenerateForGameObject(rootGameObject, root, MaxDivisionLevel, Corner, Size, calculateForChildren: true);

        Debug.Log("Finished! Marking in bounds leaves");

        // Now that the Octree is generated, mark what is/isn't in bounds
        // by iterating through all leaves and Raycasting downwards
        // though note that if a OctreeNode contains a collision, it is automatically marked as in bounds
        // (not that it matters that much, since they're ignored during Graph generation)
        OctreeGenerator.MarkInBoundsLeaves(root: root);
    }

    public void MarkInboundsLeaves() {
        if (root == null) {
            Debug.LogError("Octree: No root to mark inbounds leaves for!");
            return;
        }

        OctreeGenerator.MarkInBoundsLeaves(root: root);
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
    // Currently we sample by moving along the ray by a fixed distance (0.25f) and checking if we hit anything then move forward.
    // In actuality we should use the DDA Algorithm, which is like a "smart step" in that it knows exactly how far forward
    // along the ray we should move to find the bounds of the next entry, in our case to the next OctreeNode.
    //
    // But I'm currently too lazy to figure out the math, as it's complicated given we need to know the size of the OctreeNode we're in,
    // on top of the DDA Algorithm itself.
    public bool Raycast(Vector3 origin, Vector3 endPosition) {
        float sampleDistance = 1f; // TODO: Replace this with DDA Algorithm later

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
        return OctreeGenerator.GetAllNodes(root: root!, onlyLeaves);
    }
}

public class OctreeGenerator {
    // We have to pass octreeCorner & octreeSize solely to pass it to the OctreeNode so it can figure out its center
    // This is unnecessary, and a point of improvement.
    public static void GenerateForGameObject(GameObject currGameObject, OctreeNode root, int maxDivisionLevel, Vector3 octreeCorner, int octreeSize, bool calculateForChildren = true) {
        if (currGameObject.TryGetComponent(out MeshFilter meshFilter)) {
            // We're using sharedMesh - don't modify it!
            VoxelizeForMesh(meshFilter.sharedMesh, root, currGameObject, maxDivisionLevel, octreeCorner, octreeSize);
        }

        if (!calculateForChildren) return;

        // Calculate for the children GameObjects of currGameObject (recursively)
        for(int i = 0; i < currGameObject.transform.childCount; i++) {
            GameObject childObj = currGameObject.transform.GetChild(i).gameObject;

            if (!childObj.activeInHierarchy) continue; // Only generate on active game objects

            GenerateForGameObject(childObj, root, maxDivisionLevel, octreeCorner, octreeSize, calculateForChildren);
        }
    }

    public static void VoxelizeForMesh(Mesh mesh, OctreeNode root, GameObject currGameObject, int maxDivisionLevel, Vector3 octreeCorner, int octreeSize) {
        int[] triangles = mesh.triangles; // 1D matrix of the triangle point-indices, where every 3 elements is a triangle
        Vector3[] vertsLocalSpace = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] vertsWorldSpace = new Vector3[vertsLocalSpace.Length];

        // Convert the vertices into world space from local space using currGameObject
        for(int i = 0; i < vertsLocalSpace.Length; i++) {
            // TODO: Why tf am I doing *0 here?
            vertsWorldSpace[i] = currGameObject.transform.TransformPoint(vertsLocalSpace[i]) + (currGameObject.transform.TransformDirection(normals[i]) * 0);
        }

        // Iterate through the 1D array of triangle point-indices,
        // where every 3 ints are a point (or rather, the index of a point in vertsWorldSpace)
        for(int i = 0; i < triangles.Length / 3; i++) {
            Vector3 point1 = vertsWorldSpace[triangles[3 * i]];
            Vector3 point2 = vertsWorldSpace[triangles[3 * i + 1]];
            Vector3 point3 = vertsWorldSpace[triangles[3 * i + 2]];

            root.DivideTriangleUntilLevel(point1, point2, point3, maxDivisionLevel, octreeCorner, octreeSize);
        }
    }

    // Mark all of the leaves that are in bounds
    // 
    // A node is in bounds if:
    // 1. It contains a collision
    // 2. If we raycast downwards and hit something
    public static void MarkInBoundsLeaves(OctreeNode root) {
        // Find all of the leaves
        // This is not "efficient" but eh idrc
        // TODO: Should we do this for ALL nodes? Maybe if we have multiple different-sized flying enemies
        // (for if we only want all nodes at {MaxDivisionLevel-1})
        List<OctreeNode> allLeaves = GetAllNodes(root: root).FindAll(node => node.IsLeaf);
        int outOfBoundsCount = 0;
        foreach(var leaf in allLeaves) {
            // No need to raycast if it contains a collision - we already know we care about this
            // So mark collision nodes as in bounds automatically
            if (leaf.containsCollision) {
                leaf.isInBounds = true;
                continue;
            }

            // Leafs that don't contain a collision
            if (Physics.Raycast(leaf.center, Vector3.down, 100_000_00.0f)) {
                leaf.isInBounds = true;
            } else {
                leaf.isInBounds = false;
                outOfBoundsCount++;
            }
        }

        Debug.Log($"Marked {outOfBoundsCount} nodes out-of-bounds and {allLeaves.Count - outOfBoundsCount} in-bounds");
    }

    public static List<OctreeNode> GetAllNodes(OctreeNode root, bool onlyLeaves = false) {
        if (root == null) {
            Debug.LogError("GetAllNodes: Root was null!");
            return new();
        }

        return root.GetAllNodes(onlyLeaves);
    }

    public static int CalculateSize(Vector3 min, Vector3 max) {
        float length = max.x - min.x;
        float height = max.y - min.y;
        float width = max.z - min.z;

        // We need to use the longest side since we can only calculate Size as a cube
        double longestSide = (double) Mathf.Max(length, Mathf.Max(width, height));
        double volume = longestSide * longestSide * longestSide;

        int currMinSize = 1;
        long currVolume = 1;
        while ((currVolume) < volume) {
            currMinSize *= 2; // Power of 2's!
            // I'm terrified of integer overflow and am too lazy to figure out how to do this confidently
            long minSize = currMinSize;
            currVolume = minSize * minSize * minSize;
        }

        long totalVolume = currMinSize * currMinSize * currMinSize;
        // Debug.Log($"With dimensions of {length}, {height}, {width} and volume {volume} got min size of {currMinSize} and min volume {totalVolume}");
        return currMinSize;
    }

    // Determine the smallest number of divisions we need in order to have the smallest OctreeNode
    // that is still just bigger (exclusive) than the smallest actor's volume
    public static int CalculateMaxDivisionLevel(Vector3 smallestActorDimension, int octreeSize) {
        float smallestActorVolume = smallestActorDimension.x * smallestActorDimension.y * smallestActorDimension.z;

        int currMinDivisionLevel = 0;
        int currMinLevelSize = octreeSize;

        // Keep increasing the number of division (division leveL) until we have a division level that is smaller than the smallest actor's volume
        // The goal is to have the least number of divisions (smallest div level) that is still bigger than the smallest actor's volume
        // Note that we can't have an OctreeNode that is the same size or smaller than the smallest actor/enmy
        while ((currMinLevelSize * currMinLevelSize * currMinLevelSize) > smallestActorVolume) { // Keep going until the volume is smaller than actor volume
            // Debug.Log($"At {currMinLevelSize * currMinLevelSize * currMinLevelSize} with level of {currMinDivisionLevel}");
            // currMinDivisionLevel *= 2;
            currMinDivisionLevel++;
            currMinLevelSize = octreeSize / (1 << currMinDivisionLevel); // size / (2^currMinDivisionLevel)
        }

        // Now that we've gotten a division level that makes the smallest node that is smaller than the smallestActorVolume
        // decrease it by 1 (increase size of node) since we know that'll be bigger than it
        currMinDivisionLevel--;
        // currMinLevelSize = octreeSize / (1 << currMinDivisionLevel); // Just for debug output

        // int volume = currMinLevelSize * currMinLevelSize * currMinLevelSize;
        // Debug.Log($"Calculated min division level of {currMinDivisionLevel} which has a volume of {volume} to encapsulate an actor volume of {smallestActorVolume}");

        return currMinDivisionLevel;
    }
}
