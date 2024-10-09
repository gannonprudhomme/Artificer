using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
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

    // When generating it based on mesh(es)
    public Octree(
        Vector3 min, // Used for size calculation
        Vector3 max,  // Used for size calculation
        Vector3 smallestActorDimension, // Used for calculating MaxDivisionLevel,
        Vector3 center // The center of the Octree
    ) {
        Center = center;

        Size = OctreeGenerator.CalculateSize(min, max);

        // MaxDivisionLevel = OctreeGenerator.CalculateMaxDivisionLevel(smallestActorDimension, Size);
        MaxDivisionLevel = 2;
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
    public OctreeGenerator.GenerateOctreeJob? Generate(GameObject rootGameObject) {
        // Shit I should probably read that research paper
        // Cause they create the Octree differently

        // I also need to figure out how to prevent the "hollow mesh" problem, where if the mesh is big enough (relative to the octree nodes)
        // it will say it's empty space inside of the mesh
        // I could just remove all of the disjoint graphs (other than the biggest one) since it shouldn't be navigatable anyways

        // I'm still not sure if we want to have mulitple octrees or not
        // or if we can just filter (with OctreeNode.doChildrenContainCollision) out the smallest nodes based on the agent
        // to generate the graph for an agent

        // Rein in a bit here though - this really doesn't need to be that complex/perfect for what I want to do

        root = new OctreeNode(0, new int[] { 0, 0, 0 }, Corner, Size);

        OctreeGenerator.GenerateOctreeJob? generateJob = OctreeGenerator.CreateGenerateJob(rootGameObject, root!.Value, MaxDivisionLevel, calculateForChildren: true);

        if (generateJob is not OctreeGenerator.GenerateOctreeJob job) {
            Debug.LogError("Couldn't create generate job");
            return null;
        }

        return job;
    }

    public void MarkInboundsLeaves() {
        if (root == null) {
            Debug.LogError("Octree: No root to mark inbounds leaves for!");
            return;
        }

        OctreeGenerator.MarkInBoundsLeaves(root: root!.Value);
    }

    public OctreeNode? FindNodeForPosition(Vector3 position) {
        if (root == null) {
            Debug.LogError("Octree: No root to find nearest node for!");
            return null;
        }

        // I should probably check if this is even in the bounds of the Octree

        return root!.Value.FindNodeForPosition(position);
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

            if (currentNode!.Value.containsCollision) {
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
        return OctreeGenerator.GetAllNodes(root: root!.Value, onlyLeaves);
    }
}

public class OctreeGenerator {
    // TODO:
    // The job system usually runs multiple chains of job dependencies, so if you break up long running tasks into multiple pieces there is a chance for multiple job chains to progress.
    // If instead the job system is filled with long running jobs, they might completely consume all worker threads and block independent jobs from executing.
    // This might push out the completion time of important jobs that the main thread explicitly waits for, resulting in stalls on the main thread that otherwise wouldn�t exist.
    //
    // Per: https://docs.unity3d.com/Manual/JobSystemCreatingJobs.html, "Avoid using long running jobs"
    public struct GenerateOctreeJob : IJob {
        [ReadOnly]
        public NativeArray<int> meshTriangles;

        [ReadOnly]
        public NativeArray<Vector3> meshVertsWorldSpace;

        public NativeReference<OctreeNode> root;

        public int maxDivisionLevel;
        public bool calculateForChildren;

        // [NativeDisableUnsafePtrRestriction]
        // This surprisingly works, but we def shouldn't be doing this
        public static int status = 0;
        public static int size = 0;
        public static bool isDone = false;

        public void Execute() {
            size = meshTriangles.Length / 3;

            for(int i = 0; i < meshTriangles.Length / 3; i++) {
                // If we don't store & re-assign the copy
                // which somehow gets made *regardless*
                var copy = root.Value;

                Vector3 point1 = meshVertsWorldSpace[meshTriangles[3 * i]];
                Vector3 point2 = meshVertsWorldSpace[meshTriangles[3 * i + 1]];
                Vector3 point3 = meshVertsWorldSpace[meshTriangles[3 * i + 2]];

                copy.DivideTriangleUntilLevel(point1, point2, point3, maxDivisionLevel);

                root.Value = copy;

                status = i;
            }

            // status = size;
            // isDone = true;

            var all = root.Value.GetAllNodes();
            var onlyLeaves = root.Value.GetAllNodes(onlyLeaves: true);

            Debug.Log($"Job is done! Has {all.Length} children and {onlyLeaves.Length} leaves");
        }
    }

    public static GenerateOctreeJob? CreateGenerateJob(
        GameObject currGameObject,
        OctreeNode root,
        int maxDivisionLevel,
        bool calculateForChildren = true
    ) {
        // Create vertsWorldSpace

        if (!currGameObject.TryGetComponent(out MeshFilter meshFilter)) {
            return null;
        }

        // Rename to managed? Is this managed? Idfk
        Vector3[] vertsWorldSpace = CreateVertsWorldSpaceForMesh(meshFilter.sharedMesh, currGameObject);

        NativeArray<Vector3> vertsWorldSpaceNative = new NativeArray<Vector3>(vertsWorldSpace, Allocator.TempJob); // wtf is tempjob
        NativeArray<int> triangles = new NativeArray<int>(meshFilter.sharedMesh.triangles, Allocator.TempJob);

        return new GenerateOctreeJob() {
            meshTriangles = triangles,
            meshVertsWorldSpace = vertsWorldSpaceNative,
            root = new(root, Allocator.Persistent),
            maxDivisionLevel = maxDivisionLevel,
            calculateForChildren = calculateForChildren
        };
    }

    public static void GenerateForGameObject(GameObject currGameObject, OctreeNode root, int maxDivisionLevel, bool calculateForChildren = true) {
        if (currGameObject.TryGetComponent(out MeshFilter meshFilter)) {
            // We're using sharedMesh - don't modify it!
            VoxelizeForMesh(meshFilter.sharedMesh, root, currGameObject, maxDivisionLevel);
        }

        /*
        if (!calculateForChildren) return;

        // This entire function probably has to be split into separate jobs
        // I'll disable it for now
        // Calculate for the children GameObjects of currGameObject (recursively)
        for(int i = 0; i < currGameObject.transform.childCount; i++) {
            GameObject childObj = currGameObject.transform.GetChild(i).gameObject;

            if (!childObj.activeInHierarchy) continue; // Only generate on active game objects

            // Unfortunately these can't be separate jobs
            GenerateForGameObject(childObj, root, maxDivisionLevel, calculateForChildren);
        }
        */
    }

    private static Vector3[] CreateVertsWorldSpaceForMesh(Mesh mesh, GameObject currGameObject) {
        Vector3[] vertsLocalSpace = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] vertsWorldSpace = new Vector3[vertsLocalSpace.Length];

        // Convert the vertices into world space from local space using currGameObject
        for(int i = 0; i < vertsLocalSpace.Length; i++) {
            // TODO: Why tf am I doing *0 here?
            vertsWorldSpace[i] = currGameObject.transform.TransformPoint(vertsLocalSpace[i]) + (currGameObject.transform.TransformDirection(normals[i]) * 0);
        }

        return vertsWorldSpace;
    }
    
    // We need to figure out how to pass around OctreeNode's
    // and somehow make these fields readonly?
    public static void VoxelizeForMesh(Mesh mesh, OctreeNode root, GameObject currGameObject, int maxDivisionLevel) {
        int[] triangles = mesh.triangles; // 1D matrix of the triangle point-indices, where every 3 elements is a triangle

        Vector3[] vertsWorldSpace = CreateVertsWorldSpaceForMesh(mesh, currGameObject);

        // Iterate through the 1D array of triangle point-indices,
        // where every 3 ints are a point (or rather, the index of a point in vertsWorldSpace)
        for(int i = 0; i < triangles.Length / 3; i++) {
            Vector3 point1 = vertsWorldSpace[triangles[3 * i]];
            Vector3 point2 = vertsWorldSpace[triangles[3 * i + 1]];
            Vector3 point3 = vertsWorldSpace[triangles[3 * i + 2]];

            root.DivideTriangleUntilLevel(point1, point2, point3, maxDivisionLevel);
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
        List<OctreeNode> allLeaves = new List<OctreeNode>(GetAllNodes(root: root)).FindAll(node => node.IsLeaf);
        int outOfBoundsCount = 0;
        // foreach(var leaf in allLeaves) {
        for(int i = 0; i < allLeaves.Count; i++) {
            var leaf = allLeaves[i]; // TODO: Is this a copy?

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
        /*
        if (root == null) {
            Debug.LogError("GetAllNodes: Root was null!");
            return new();
        }
        */

        NativeList<OctreeNode> allNodesNative = root.GetAllNodes(onlyLeaves);
        List<OctreeNode> allNodes = new(allNodesNative.Length);

        for(int i = 0; i < allNodesNative.Length; i++) {
            allNodes.Add(allNodesNative[i]);
        }

        return allNodes;
    }

    public static int CalculateSize(Vector3 min, Vector3 max) {
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
