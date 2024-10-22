using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

public struct OctreeGenerationJob: IJob {
    // triangles of the mesh?
    [ReadOnly]
    public NativeArray<int> meshTriangles; // Isn't this actually points on a triangle?

    [ReadOnly]
    public NativeArray<float3> meshVertsWorldSpace;

    [ReadOnly]
    public readonly float3 octreeCenter;

    [ReadOnly]
    public readonly int totalOctreeSize; // Should be input

    [ReadOnly]
    public readonly int maxDivisionLevel; // Should be an input

    // wtf should the key be? morton code? does that even make sense?
    // honestly byte4 would be ideal
    // TODO: Add "output" to this?
    public NativeHashMap<int4, NewOctreeNode> nodes; // This is what we'll return, probably?

    // TODO: Ideally we wouldn't make these static
    public static int size = 0;
    public static int status = 0;

    private int startIndexInclusive;
    private int endIndexExclusive;

    public OctreeGenerationJob(
        int startIndexInclusive,
        int endIndexExclusive,
        NativeArray<int> meshTriangles,
        NativeArray<float3> meshVertsWorldSpace,
        NativeHashMap<int4, NewOctreeNode> nodes, // the output
        float3 octreeCenter,
        int totalOctreeSize,
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

        // TODO: Dispose of the input properties? Where should I do that?
    }

    private void DivideTriangleUntilLevel(
        /*ref*/ NewOctreeNode node, // TODO: Should this be ref?
        float3 point1,
        float3 point2,
        float3 point3
        // int maxDivisionLevel
    ) {
        if (!node.DoesIntersectTriangle(point1, point2, point3)) {
            // Base case
            // Stop if this doesn't intersect this triangle
            return;
        }

        // Debug.Log($"Dividing for node {node.index}; {node.nodeLevel}");

        // I think this is making a copy anyways, so I'm just doing this to be explicit
        NewOctreeNode copy = node;

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

            if (!nodes.TryGetValue(childIndex, out NewOctreeNode child)) {
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

    // TODO: Find a better name for this
    // Can I even use this in a job? an array is managed
    public static readonly int3[] childIndices = new int3[8]{
        new(0, 0, 0),
        new(0, 0, 1),
        new(1, 0, 0),
        new(1, 0, 1),
        new(0, 1, 0),
        new(0, 1, 1),                   
        new(1, 1, 0),
        new(1, 1, 1)
    };

    private void CreateChildrenForNode(NewOctreeNode node) {
        float3 octreeCorner = octreeCenter - (new float3(1) * totalOctreeSize / 2);

        NewOctreeNode currCopy = node;
        currCopy.hasChildren = true;
        nodes[currCopy.dictionaryKey] = currCopy;

        // Dunno which of these we should do
        for(int i = 0; i < 8; i++) {
            int3 childIndexOffset = childIndices[i];
            var (x, y, z) = (childIndexOffset[0], childIndexOffset[1], childIndexOffset[2]);

            int3 newChildIndex = new(
                node.index[0] * 2 + x,
                node.index[1] * 2 + y,
                node.index[2] * 2 + z
            );

            // TODO: Wondering if I should make this a custom constructor for OctreeNode
            // similar to what we do for the OOP version

            float childSize = node.size / 2;
            // TODO: check float3 conversion & see if I should just do this separate
            float3 childCorner = octreeCorner + (childSize * new float3(newChildIndex));
            float3 childCenter = childCorner + (new float3(1) * (childSize / 2)); // Move it by e.g. (0.5, 0.5, 0.5) when size = 1

            NewOctreeNode child = new(
                nodeLevel: node.nodeLevel + 1,
                size: node.size / 2,
                index: newChildIndex,
                center: childCenter
            );

            // Add it to the dictionary
            // we might need to check if it already exists in here? But it shouldn't at this point
            nodes[child.dictionaryKey] = child;
        }
    }
}

public struct ConvertVertsToWorldSpaceJob : IJobParallelFor {
    [ReadOnly]
    private readonly NativeArray<Vector3> vertsLocalSpaceInput;

    [ReadOnly]
    private readonly float4x4 transform;

    [WriteOnly]
    public NativeArray<float3> vertsWorldSpaceOutput;

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

    public static ConvertVertsToWorldSpaceJob CreateJob(
        GameObject gameObject
    ) {
        if (!gameObject.TryGetComponent(out MeshFilter meshFilter)) {
            Debug.LogError("no mesh!");
        }

        NativeArray<Vector3> vertsLocalSpace = new(meshFilter.sharedMesh.vertices, Allocator.Persistent);

        float4x4 transform = gameObject.transform.localToWorldMatrix; // Technically returns a Matrix4x4

        NativeArray<float3> vertsWorldSpaceOutput = new(vertsLocalSpace.Length, Allocator.Persistent);

        return new(vertsLocalSpace, transform, vertsWorldSpaceOutput);
    }
}
