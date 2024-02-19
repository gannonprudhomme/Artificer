using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

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

    // When generating it based on mesh(es)
    public Octree(
        Vector3 min, // Used for size calculation
        Vector3 max,  // Used for size calculation
        Vector3 smallestActorDimension, // Used for calculating MaxDivisionLevel,
        Vector3 center // The center of the Octree
    ) {
        Center = center;

        Size = CalculateSize(min, max);

        MaxDivisionLevel = CalculateMaxDivisionLevel(smallestActorDimension, Size);
        Debug.Log($"Calculated max division level: {MaxDivisionLevel} and size: {Size}");
    }

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

        // I'd also like to figure out how to better mark stuff that's out of bounds
        // as if we generate stuff below the level there's a chance the nearest graph node to the point we want to get to
        // is below the level. (we might be able to do some math to get around this, but I'd just like for it to be impossible to begin with)

        // I'm still not sure if we want to have mulitple octrees or not
        // or if we can just filter (with OctreeNode.doChildrenContainCollision) out the smallest nodes based on the agent
        // to generate the graph for an agent

        // Rein in a bit here though - this really doesn't need to be that complex/perfect for what I want to do

        root = new OctreeNode(0, new int[] { 0, 0, 0 }, this);

        GenerateForGameObject(rootGameObject, calculateForChildren: true);

        // Now that the Octree is generated, mark what is/isn't in bounds
        // by iterating through all leaves and Raycasting downwards
        // though note that if a OctreeNode contains a collision, it is automatically marked as in bounds
        // (not that it matters that much, since they're ignored during Graph generation)
        MarkInBoundsLeaves();
    }

    private void GenerateForGameObject(GameObject currGameObject, bool calculateForChildren = true) {
        if (currGameObject.TryGetComponent(out MeshFilter meshFilter)) {
            // We're using sharedMesh - don't modify it!
            VoxelizeForMesh(meshFilter.sharedMesh, currGameObject);
        }

        if (!calculateForChildren) return;

        // Calculate for the chilren (recursively)
        for(int i = 0; i < currGameObject.transform.childCount; i++) {
            GameObject childObj = currGameObject.transform.GetChild(i).gameObject;

            if (!childObj.activeInHierarchy) continue; // Only generate on active game objects

            GenerateForGameObject(childObj, calculateForChildren);
        }
    }

    private void VoxelizeForMesh(Mesh mesh, GameObject currGameObject) {
        int[] triangles = mesh.triangles;
        Vector3[] vertsLocalSpace = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] vertsWorldSpace = new Vector3[vertsLocalSpace.Length];

        // Convert the vertices into world space from local space
        // using currGameObject
        for(int i = 0; i < vertsLocalSpace.Length; i++) {
            vertsWorldSpace[i] = currGameObject.transform.TransformPoint(vertsLocalSpace[i]) + (currGameObject.transform.TransformDirection(normals[i]) * 0);
        }

        for(int i = 0; i < triangles.Length / 3; i++) {
            Vector3 point1 = vertsWorldSpace[triangles[3 * i]];
            Vector3 point2 = vertsWorldSpace[triangles[3 * i + 1]];
            Vector3 point3 = vertsWorldSpace[triangles[3 * i + 2]];

            root!.DivideTriangleUntilLevel(point1, point2, point3, MaxDivisionLevel);
        }
    }

    // Mark all of the leaves that are in bounds
    // 
    // A node is in bounds if:
    // 1. It contains a collision
    // 2. If we raycast downwards and hit something
    private void MarkInBoundsLeaves() {
        // Find all of the leaves
        // This is not "efficient" but eh idrc
        // TODO: Should we do this for ALL nodes? Maybe if we have multiple different-sized flying enemies
        // (for if we only want all nodes at {MaxDivisionLevel-1})
        List<OctreeNode> allLeaves = GetAllNodes().FindAll(node => node.IsLeaf);

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

    }

    public List<OctreeNode> GetAllNodes() {
        if (root == null) return new();

        return root.GetAllNodes();
    }

    private static int CalculateSize(Vector3 min, Vector3 max) {
        float length = max.x - min.x;
        float height = max.y - min.y;
        float width = max.z - min.z;

        // We need to use the longest side since we can only calculate Size as a cube
        float longestSide = Mathf.Max(length, Mathf.Max(width, height));
        float volume = longestSide * longestSide * longestSide;

        int currMinSize = 1;
        while ((currMinSize * currMinSize * currMinSize) < volume) {
            currMinSize *= 2; // Power of 2's!
        }

        int totalVolume = currMinSize * currMinSize * currMinSize;
        // Debug.Log($"With dimensions of {length}, {height}, {width} and volume {volume} got min size of {currMinSize} and min volume {totalVolume}");
        return currMinSize;
    }

    // Determine the smallest number of divisions we need in order to have the smallest OctreeNode
    // that is still just bigger (exclusive) than the smallest actor's volume
    private static int CalculateMaxDivisionLevel(Vector3 smallestActorDimension, int octreeSize) {
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
