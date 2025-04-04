#nullable enable

public class Octree {
    public OctreeNode root;
    public int maxDivisionLevel;

    public int size;
    public float3 center;
}

public class OctreeNode {
    public byte nodeLevel;
    public int3 index;

    public bool containsCollision;

    public OctreeNode[]? children;
}
