[BurstCompile]
public struct OctreeGenerationJob: IJob {
    [ReadOnly]
    public NativeArray<int> meshTriangles;

    [ReadOnly]
    public NativeArray<Float3> meshVertsWorldSpace;

    [ReadOnly]
    public readonly float3 octreeCenter;

    [ReadOnly]
    public readonly long totalOctreeSize;

    [ReadOnly]
    public readonly int maxDivisionLevel;

    public NativeHashMap<int4, FlatOctreeNode> nodesOutput;

    private int startIndex;
    private int endIndex;

    public void Execute() {
        // iterate between each triangle in [startIndex, endIndex)
        // 
    }
}