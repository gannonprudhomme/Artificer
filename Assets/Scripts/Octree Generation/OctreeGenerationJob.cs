using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

#nullable enable

[BurstCompile]
public struct OctreeGenerationJob: IJob {
    // triangles of the mesh?
    [ReadOnly]
    public NativeArray<int> meshTriangles; // Isn't this actually points on a triangle?

    [ReadOnly]
    public NativeArray<float3> meshVertsWorldSpace;

    [ReadOnly]
    public readonly float3 octreeCenter;

    [ReadOnly]
    public readonly long totalOctreeSize;

    [ReadOnly]
    public readonly int maxDivisionLevel;

    // wtf should the key be? morton code? does that even make sense?
    // honestly byte4 would be ideal
    // TODO: Add "output" to this?
    public NativeHashMap<int4, FlatOctreeNode> nodes; // This is what we'll return, probably?

    private int startIndexInclusive;
    private int endIndexExclusive;

    public OctreeGenerationJob(
        int startIndexInclusive,
        int endIndexExclusive,
        NativeArray<int> meshTriangles,
        NativeArray<float3> meshVertsWorldSpace,
        NativeHashMap<int4, FlatOctreeNode> nodes, // the output
        float3 octreeCenter,
        long totalOctreeSize,
        int maxDivisionLevel
    ) {
        this.startIndexInclusive = startIndexInclusive;
        this.endIndexExclusive = endIndexExclusive;
        this.meshTriangles = meshTriangles;
        this.meshVertsWorldSpace = meshVertsWorldSpace;
        this.nodes = nodes;
        this.octreeCenter = octreeCenter;
        this.totalOctreeSize = totalOctreeSize;
        this.maxDivisionLevel = maxDivisionLevel;
    }

    public void Execute() {
        int4 rootIndex = new(0);

        for (int i = startIndexInclusive; i < endIndexExclusive; i++) {
            var root = nodes[rootIndex];

            float3 point1 = meshVertsWorldSpace[meshTriangles[3 * i]];
            float3 point2 = meshVertsWorldSpace[meshTriangles[3 * i + 1]];
            float3 point3 = meshVertsWorldSpace[meshTriangles[3 * i + 2]];

            DivideTriangleUntilLevel(root, point1, point2, point3);
        }
    }

    private void DivideTriangleUntilLevel( // TODO: Better name?
        FlatOctreeNode node,
        float3 point1,
        float3 point2,
        float3 point3
    ) {
        if (!DoesNodeIntersectTriangle(node, point1, point2, point3)) {
            // Base case
            // Stop if this doesn't intersect this triangle
            return;
        }

        // Debug.Log($"Dividing for node {node.index}; {node.nodeLevel}");

        // I think this is making a copy anyways, so I'm just doing this to be explicit
        FlatOctreeNode copy = node;

        bool cantDivideAnyFurther = node.nodeLevel >= maxDivisionLevel;
        if (cantDivideAnyFurther) {
            // We've divided as much as possible, lets mark it and don't divide any further.
            copy.containsCollision = true;
            nodes[copy.dictionaryKey] = copy;
            return;
        }

        // if it doesn't have children, create the children for this
        if (!copy.hasChildren) {
            // Create the children & put them in the hashmap
            CreateChildrenForNode(copy);
        }

        // Iterate through all of the children
        for(int i = 0; i < 8; i++) {
            int4 childIndex = copy.GetChildKey(i);

            if (!nodes.TryGetValue(childIndex, out FlatOctreeNode child)) {
                Debug.LogError("Tried to get a child that doesn't exist?");
                continue;
            }

            DivideTriangleUntilLevel(
                node: child,
                point1,
                point2,
                point3
            );
        }
    }

    private void CreateChildrenForNode(FlatOctreeNode node) {
        float3 octreeCorner = octreeCenter - (new float3(1) * totalOctreeSize / 2);

        FlatOctreeNode currCopy = node;
        currCopy.hasChildren = true;
        nodes[currCopy.dictionaryKey] = currCopy;

        // Dunno which of these we should do
        for(int i = 0; i < 8; i++) {
            int3 childIndexOffset = FlatOctreeNode.childIndices[i];
            // x, y, z are all either 0 or 1
            var (x, y, z) = (childIndexOffset[0], childIndexOffset[1], childIndexOffset[2]);

            // E.g. the children of (1, 1, 1) (nodeLevel 1) will be (2, 2, 2), (2, 2, 3), (2, 3, 2), (2, 3, 3), ..., (3, 3, 3) (all w/ nodeLevel 2)
            int3 newChildIndex = new(
                (node.index.x * 2) + x,
                (node.index.y * 2) + y,
                (node.index.z * 2) + z
            );

            // TODO: Wondering if I should make this a custom constructor for OctreeNode
            // similar to what we do for the OOP version

            float childSize = node.size / 2;
            // TODO: check float3 conversion & see if I should just do this separate?
            float3 childCorner = octreeCorner + (childSize * new float3(newChildIndex));
            float3 childCenter = childCorner + (new float3(1) * (childSize / 2)); // Move it by e.g. (0.5, 0.5, 0.5) when size = 1

            FlatOctreeNode child = new(
                nodeLevel: (byte) (node.nodeLevel + 1),
                size: node.size / 2,
                index: newChildIndex,
                center: childCenter
            );

            // Add it to the dictionary
            // we might need to check if it already exists in here? But it shouldn't at this point
            nodes[child.dictionaryKey] = child;
        }
    }

    // Returns true if the given node (aka cube) intersects a triangle formed by { p1, p2, p3 }
    //
    // Heavily based off of https://github.com/supercontact/PathFindingEnhanced
    private static bool DoesNodeIntersectTriangle(
        FlatOctreeNode node,
        float3 p1, float3 p2, float3 p3,
        float tolerance = 0
    ) {
        // This code sucks (I'm basically just copy/pasting), probably find a better algo just so I understand this better

        // What is this doing
        // Probably print / visualize here
        p1 -= node.center;
        p2 -= node.center;
        p3 -= node.center;

        // Do axis check? Not sure what we're doing here
        float xMin = math.min(p1.x, math.min(p2.x, p3.x));
        float xMax = math.max(p1.x, math.max(p2.x, p3.x));
        float yMin = math.min(p1.y, math.min(p2.y, p3.y));
        float yMax = math.max(p1.y, math.max(p2.y, p3.y));
        float zMin = math.min(p1.z, math.min(p2.z, p3.z));
        float zMax = math.max(p1.z, math.max(p2.z, p3.z));

        float radius = (node.size / 2) - tolerance;
        if (xMin >= radius || xMax < -radius || yMin >= radius || yMax < -radius || zMin >= radius || zMax < -radius) return false;

        // Wtf is n and d here
        // I'm guessing this has something to do with the plane?
        float3 n = math.cross(p2 - p1, p3 - p1);
        float d = math.abs(math.dot(p1, n));

        float radiusModified = radius * (Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z));
        bool isDMoreThanRadiusModified = d > radiusModified; // If you can't tell idk what this is
        if (isDMoreThanRadiusModified) {
            return false;
        }

        // Okay what the fuck is this.
        NativeArray<float3> points = new(3, Allocator.Temp); // Temp = 1 frame
        points[0] = p1;
        points[1] = p2;
        points[2] = p3;

        NativeArray<float3> pointsSubtractedFromEachOther = new(3, Allocator.Temp);
        pointsSubtractedFromEachOther[0] = p3 - p2;
        pointsSubtractedFromEachOther[1] = p1 - p3;
        pointsSubtractedFromEachOther[2] = p2 - p1;

        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                float3 a = float3.zero;
                a[i] = 1; // WHAT
                a = math.cross(a, pointsSubtractedFromEachOther[j]);

                float d1 = math.dot(points[j], a);
                float d2 = math.dot(points[(j + 1) % 3], a);

                float rr = radius * (Mathf.Abs(a[(i + 1) % 3]) + math.abs(a[(i + 2) % 3]));

                if (math.min(d1, d2) > rr || math.max(d1, d2) < -rr) {
                    return false;
                }
            }
        }

        return true;
    }
}

// TODO: Comment this & explain it
[BurstCompile]
public struct ConvertVertsToWorldSpaceJob : IJobParallelFor {
    [ReadOnly]
    private readonly NativeArray<Vector3> vertsLocalSpaceInput;

    [ReadOnly]
    private readonly float4x4 transform;

    // TODO: Idk why I don't use this (or why it does or doesn't matter?)
    // [ReadOnly]
    // NativeArray<Vector3> normals; // input

    [WriteOnly]
    private NativeArray<float3> vertsWorldSpaceOutput;

    public ConvertVertsToWorldSpaceJob(
        NativeArray<Vector3> vertsLocalSpaceInput,
        float4x4 transform,
        NativeArray<float3> vertsWorldSpaceOutput
    ) {
        this.vertsLocalSpaceInput = vertsLocalSpaceInput;
        this.transform = transform;
        this.vertsWorldSpaceOutput = vertsWorldSpaceOutput;
    }

    public void Execute(int index) {
        vertsWorldSpaceOutput[index] = LocalToWorld(transform, vertsLocalSpaceInput[index]);
    }

    private static float3 LocalToWorld(float4x4 transform, float3 point) {
        return math.transform(transform, point);
    }
}

// TODO: Stretch goal, but I can't figure this out so it's probably not worth my time
// I keep getting some "trying to read from a [WriteOnly]" thing for GetValueArray, which I really don't get
//
// There should only be one of these jobs
// [BurstCompile]
public struct CombineAllNodesJob: IJob {
    // [ReadOnly]
    // [NativeDisableContainerSafetyRestriction]
    [ReadOnly]
    private UnsafeList<NativeHashMap<int4, FlatOctreeNode>> allNodeMaps;

    private NativeHashMap<int4, FlatOctreeNode> combined;

    public CombineAllNodesJob(
        UnsafeList<NativeHashMap<int4, FlatOctreeNode>> allNodeMaps,
        NativeHashMap<int4, FlatOctreeNode> combined
    ) {
        this.allNodeMaps = allNodeMaps;
        this.combined = combined;
    }

    public void Execute() {
        Debug.Log("Beginning to combine!");
        foreach (NativeHashMap<int4, FlatOctreeNode> nodesMap in allNodeMaps) {
            Debug.Log("Combining!");
            NativeArray<FlatOctreeNode> nodes = nodesMap.GetValueArray(Allocator.Temp);

            foreach(FlatOctreeNode node in nodes) {
                int4 key = node.dictionaryKey;

                if (!combined.ContainsKey(key)) {
                    combined.Add(key, node);
                } else {
                    FlatOctreeNode existingNode = combined[key];

                    // TODO: Try to make this more readable bleh
                    if (node.hasChildren && !existingNode.hasChildren) {
                        combined[key] = node;
                    } else if (node.hasChildren == existingNode.hasChildren) {
                        if (node.containsCollision && !existingNode.containsCollision) {
                            combined[key] = node;
                        }
                    }
                }
            }

            nodes.Dispose();
            nodesMap.Dispose(); // I think we want to do this here? Cause we're now done w/ them
        }
        Debug.Log("Done combining!");
    }
}
