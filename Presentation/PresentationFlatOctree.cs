#nullable enable

public class FlatOctree {
    public Dictionary<int4, FlatOctreeNode> nodes;
    public int maxDivisionLevel;

    public int size;
    public float3 center;
}

public struct FlatOctreeNode {
    public byte nodeLevel;
    public int size;
    public int3 index;
    public float3 center;
    public bool containsCollision;

    public bool isLeaf;
}
