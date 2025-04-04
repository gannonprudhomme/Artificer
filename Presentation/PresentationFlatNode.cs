
[BurstCompile]
public struct OctreeGenerationJob: IJob {
    [ReadOnly]
    public NativeArray<float3> vertices;

    [ReadOnly]
    public byte maxDivisionLevel;

    public NativeHashMap<int4, FlatOctreeNode> nodesOutput;

    private int startIndex;
    private int endIndex;
}

public struct FlatOctreeNode {
    public byte nodeLevel;
    public int3 index;
    public bool containsCollision;

    public bool isLeaf;
}





