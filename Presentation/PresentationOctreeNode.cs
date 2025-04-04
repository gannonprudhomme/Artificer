
public class Octree {
    public OctreeNode root;
    public byte maxDivisionLevel; // max depth

    public int size; // power of 2
    public Vector3 center;
}

public class OctreeNode {
    public byte nodeLevel; // node depth
    public int[] index; // length of 3

    public bool containsCollision;

    public OctreeNode[]? children;
}




