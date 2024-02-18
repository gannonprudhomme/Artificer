using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

[Serializable]
public class OldOctreeNode {
    public readonly int nodeLevel;

    // The index of this node in the Octree
    // Used to do WHAT
    [SerializeField]
    public readonly int[] index;

    // private readonly OctreeNode parent; // I'm not sure if we actually need the parent?

    // Reference to the main Octree
    // Used so we can do WHAT
    private readonly OldOctree tree;

    // Note that this might not have any children!
    // How do we know something is a leaf?
    public OldOctreeNode[,,]? children { get; set; }

    public bool doesChildrenContainCollision = false;

    // Whether we would've divided further but couldn't b/c of the max level
    public bool isInBounds = true;
    public bool containsCollision { get; private set; }
    private float size {
        get { return tree.Size / (1 << nodeLevel); }
    }
    public float GetSize() { return size; } // We shouldn't actually have this its just for doing DrawGizmos()

    public Vector3 center {
        // Get the bottom left(?) corner, then move it to the center (0.5, 0.5, 0.5) [when size = 1]
        get { return GetCorners(0) + ((size / 2) * Vector3.one); }
    }

    private bool IsLeaf {
        get { return children == null; }
    }

    /*
    public int[] cornerIndex(int n, OldOctree tree) {
        int s = 1 << (tree.MaxDivisionLevel - nodeLevel);
        return new int[] {
            (index[0] + OldOctree.cornerDir[n, 0]) * s,
            (index[1] + OldOctree.cornerDir[n, 1]) * s,
            (index[2] + OldOctree.cornerDir[n, 2]) * s
        };
    }
    */


    public OldOctreeNode(
        int nodeLevel,
        int[] index,
        OldOctreeNode? parent,
        OldOctree tree
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;
        this.children = null;
        // this.parent = parent;
        this.tree = tree;
        this.containsCollision = false;
    }

    // For deserializing
    public OldOctreeNode(
        int nodeLevel,
        int[] index,
        bool containsCollision,
        bool isInBounds,
        OldOctree octree
    ) {
        this.nodeLevel = nodeLevel;
        this.index = index;
        this.children = null;
        this.tree = octree;
        this.containsCollision = containsCollision;
        this.isInBounds = isInBounds;
    }

    public List<OldOctreeNode> GetLeaves() {
        List<OldOctreeNode> leaves = new();

        if (children == null) {
            leaves.Add(this);
        } else {
            for (int x = 0; x < 2; x++) {
                for (int y = 0; y < 2; y++) {
                    for (int z = 0; z < 2; z++) {
                        List<OldOctreeNode> childLeaves = children[x, y, z].GetLeaves();
                        leaves.AddRange(childLeaves);
                    }
                }
            }
        }

        return leaves;
    }

    public List<OldOctreeNode> GetAllNodes() {
        List<OldOctreeNode> allNodes = new();

        allNodes.Add(this);

        if (children == null)
            return allNodes;

        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 2; y++) {
                for (int z = 0; z < 2; z++) {
                    var child = children[x, y, z];
                    if (child == null) continue;

                    List<OldOctreeNode> childNodes = children[x, y, z].GetAllNodes();
                    allNodes.AddRange(childNodes);
                }
            }
        }

        return allNodes;
    }

    // For our purposes I think cornerNum is always 0
    private Vector3 GetCorners(int cornerNum) {
        // I don't actually know what this vector represents
        Vector3 indicesMovedByCorners = new(
            index[0] + OldOctree.cornerDir[cornerNum, 0],
            index[1] + OldOctree.cornerDir[cornerNum, 1],
            index[2] + OldOctree.cornerDir[cornerNum, 2]
        );

        return tree.Corner + (size * indicesMovedByCorners);
    }


    // What does this do?
    public void DivideTriangleUntilLevel(
        Vector3 point1,
        Vector3 point2,
        Vector3 point3,
        int maxLevel // How many layers we can divide the Octree into
        // bool markAsBlocked = false
    ) {
        if (!DoesThisIntersectTriangle(point1, point2, point3)) { return; }

        // It does intersect! Lets break it up

        if (this.nodeLevel < maxLevel) {
            // Divide this
            CreateChildrenIfHaventYet();

            for(int x = 0; x < 2; x++) {
                for(int y = 0; y < 2; y++) {
                    for(int z = 0; z < 2; z++) {
                        children![x, y, z].DivideTriangleUntilLevel(point1, point2, point3, maxLevel);
                    }
                }
            }

        } else { // we're too deep, mark as can't 
            containsCollision = true;
        }
    }

    // Returns true if this voxel intersects with the given triangle
    private bool DoesThisIntersectTriangle(Vector3 p1, Vector3 p2, Vector3 p3, float tolerance = 0) {
        // This code sucks (I'm basically just copy/pasting), probably find a better algo just so I understand this better

        // Debug.Log($"Checking node [{index[0]}, {index[1]}, {index[2]}] for points {p1} {p2} {p3}");

        // What is this doing?
        // Probably print / visualize here
        p1 -= center;
        p2 -= center;
        p3 -= center;

        // Do axis check? Not sure what we're doing here
        float xMin, xMax, yMin, yMax, zMin, zMax;
        xMin = Mathf.Min(p1.x, p2.x, p3.x);
        xMax = Mathf.Max(p1.x, p2.x, p3.x);
        yMin = Mathf.Min(p1.y, p2.y, p3.y);
        yMax = Mathf.Max(p1.y, p2.y, p3.y);
        zMin = Mathf.Min(p1.z, p2.z, p3.z);
        zMax = Mathf.Max(p1.z, p2.z, p3.z);

        float radius = size / 2 - tolerance;
        if (xMin >= radius || xMax < -radius || yMin >= radius || yMax < -radius || zMin >= radius || zMax < -radius) return false;

        // Wtf is n and d here
        // I'm guessing this has something to do with the plane?
        Vector3 n = Vector3.Cross(p2 - p1, p3 - p1);
        float d = Mathf.Abs(Vector3.Dot(p1, n));

        float radiusModified = radius * (Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z));
        bool isDMoreThanRadiusModified = d > radiusModified; // If you can't tell idk what this is
        if (isDMoreThanRadiusModified) {
            return false;
        }

        // Okay what the fuck is this.
        Vector3[] points = { p1, p2, p3 };
        Vector3[] pointsSubtractedFromEachOther = { p3 - p2, p1 - p3, p2 - p1 };
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                Vector3 a = Vector3.zero;
                a[i] = 1; // WHAT
                a = Vector3.Cross(a, pointsSubtractedFromEachOther[j]);

                float d1 = Vector3.Dot(points[j], a);
                float d2 = Vector3.Dot(points[(j + 1) % 3], a);

                float rr = radius * (Mathf.Abs(a[(i + 1) % 3]) + Mathf.Abs(a[(i + 2) % 3]));

                if (Mathf.Min(d1, d2) > rr || Mathf.Max(d1, d2) < -rr) {
                    return false;
                }
            }
        }

        return true;
    } 

    // Divide this node into children
    // Makes it no longer a leaf!
    private void CreateChildrenIfHaventYet() {
        if (children != null) {
            // We've already created children! Don't try to again
            return;
        }

        children = new OldOctreeNode[2, 2, 2]; // Create 8 children
        for (int x = 0; x < 2; x++) {
            for(int y = 0; y < 2; y++) {
                for(int z = 0; z < 2; z++) {
                    int[] newIndex = {
                        index[0] * 2 + x, // x,y,z are either 0 or 1
                        index[1] * 2 + y,
                        index[2] * 2 + z
                    };
                    children[x, y, z] = new OldOctreeNode(nodeLevel + 1, newIndex, this, tree);
                }
            }
        }
    }

    public int CountVoxels() {
        if (children == null)
            return 1;

        int ret = 0; // Count itself? No I don't think so
        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 2; y++) {
                for (int z = 0; z < 2; z++) {
                    ret += children[x, y, z].CountVoxels();
                }
            }
        }

        return ret;
    }

    public string IndexToString() {
        return $"({index[0]}, {index[1]}, {index[2]})";
    }

    // Create MeshRenderer and display that bitch
    public void DrawGizmos(bool shouldDisplayVoxels, bool shouldDisplayText, Color textColor) {
        // Why does this happen
        if (tree == null) return;

        Vector3 position = center;

        if (shouldDisplayVoxels) {
            Gizmos.DrawWireCube(position, Vector3.one * size);
        }

        if (shouldDisplayText) {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            string output = $"{index[0]}, {index[1]}, {index[2]} ({nodeLevel})";
            Vector3 offsetPosition = position + new Vector3(0, 0.5f, 0.0f);

            GUIStyle style = new();
            style.normal.textColor = textColor;
            UnityEditor.Handles.Label(offsetPosition, output, style);
            #endif
        }

    }
}
