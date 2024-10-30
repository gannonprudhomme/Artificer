using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using UnityEngine;

#nullable enable

// Component to go on the (parent-most) Level game object
//
// Intended to be the equivalent of NavMeshSurface
//
// Generate the Octree in the editor using NavOctreeSpaceEditor 
public class NavOctreeSpace : MonoBehaviour {
    [Header("Debug")]
    public bool DisplayLeaves = false; // Displays the leaves
    public bool DisplayNonLeaves = false;
    public bool DisplayCollisions = false;
    public bool DisplayIndices = false;
    public bool DisplayIsInBounds = false;
    public bool DisplayOutOfBounds = false;

    public bool DisplayBounds = false;

    public bool DisplayNeighbors = false;

    public int MaxDivisionLevel = 9;

    [Tooltip("How many jobs to use for subdividing the octree")]
    public int NumberOfJobs = 12;

    // Must call Load() / LoadIfNeeded() to populate this
    public Octree? octree { get; private set; }

    private Bounds? calculatedBounds = null; // For debug displaying

    public void LoadIfNeeded() {
        if (octree != null) return;

        octree = OctreeSerializer.Load(GetFileName());
    }

    // needs to be public for OctreeGenerator
    public Bounds GetBounds() {
        // This gets renderers from this GameObject(Component), as well as it's children recursively
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach(var renderer in renderers) {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    // Retrieves the filename 
    // Should probably be based off the GameObject name?
    public string GetFileName() {
        return $"{gameObject.name}.octree.bin";
    }

    public void SetOctree(Octree newOctree) {
        this.octree = newOctree;
        
        #if UNITY_EDITOR
        // Destroy the previously cached gizmos list
        gizmosAllNodes = null;
        gizmosAllLeaves = null;
        gizmosNotLeaves = null;
        gizmosCollisionLeaves = null;
        gizmosNoCollisionLeaves = null;
        gizmosLeavesOutOfBounds = null;
        gizmosLeavesInBounds = null;
        gizmosNeighborsLines = null;
        #endif
    }
    
    #if UNITY_EDITOR
    private List<OctreeNode>? gizmosAllNodes = null;
    private List<OctreeNode>? gizmosAllLeaves = null;
    private List<OctreeNode>? gizmosNotLeaves = null;
    private List<OctreeNode>? gizmosCollisionLeaves = null;
    private List<OctreeNode>? gizmosNoCollisionLeaves = null;
    private List<OctreeNode>? gizmosLeavesOutOfBounds = null;
    private List<OctreeNode>? gizmosLeavesInBounds = null;
    private Vector3[]? gizmosNeighborsLines = null;

    private void OnDrawGizmos() {
        if (calculatedBounds != null && DisplayBounds) {
            float length = calculatedBounds.Value.max.x - calculatedBounds.Value.min.x;
            float height = calculatedBounds.Value.max.y - calculatedBounds.Value.min.y;
            float width = calculatedBounds.Value.max.z - calculatedBounds.Value.min.z;
            float longestSide = Mathf.Max(length, Mathf.Max(width, height));

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(calculatedBounds.Value.center, Vector3.one * longestSide);
        }
 
        if (!(DisplayLeaves || DisplayCollisions || DisplayIndices || DisplayIsInBounds || DisplayOutOfBounds || DisplayNonLeaves || DisplayNeighbors)) return;

        if (octree == null) return;

        gizmosAllNodes ??= octree.GetAllNodes();
        gizmosAllLeaves ??= octree.GetAllNodes(onlyLeaves: true);
        gizmosNotLeaves ??= gizmosAllNodes.FindAll(node => node.children != null);
        gizmosCollisionLeaves ??= gizmosAllLeaves.FindAll(leaf => leaf.containsCollision);
        gizmosNoCollisionLeaves ??= gizmosAllLeaves.FindAll(leaf => !leaf.containsCollision);
        gizmosLeavesOutOfBounds ??= gizmosAllLeaves.FindAll(leaf => !leaf.isInBounds);
        gizmosLeavesInBounds ??= gizmosAllLeaves.FindAll(leaf => leaf.isInBounds);

        if (DisplayLeaves) { // Display the leaves
            Gizmos.color = Color.green;
            foreach(OctreeNode leaf in gizmosAllLeaves) {
                leaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNonLeaves) {
            Gizmos.color = Color.blue;
            foreach(OctreeNode notLeaf in gizmosNotLeaves) {
                notLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayCollisions) {
            Gizmos.color = Color.red;
            foreach(OctreeNode collisionLeaf in gizmosCollisionLeaves) {
                collisionLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayOutOfBounds) {
            Gizmos.color = Color.magenta;
            foreach(OctreeNode outOfBoundsLeaf in gizmosLeavesOutOfBounds) {
                outOfBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayIsInBounds) {
            Gizmos.color = Color.yellow;
            foreach(OctreeNode inBoundsLeaf in gizmosLeavesInBounds) {
                inBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNeighbors) {
            gizmosNeighborsLines ??= GetNeighborLines(gizmosAllLeaves);

            if (gizmosNeighborsLines != null) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLineList(gizmosNeighborsLines);
            }
        }
    }

    private static Vector3[]? GetNeighborLines(List<OctreeNode> allLeaves) {
        if (allLeaves.Count == 0) return null;

        List<(Vector3, Vector3)> validNeighbors = new();

        foreach(OctreeNode node in allLeaves) {
            List<OctreeNode>? neighbors = node.inBoundsNeighborsWithoutCollisions;
            if (neighbors == null) continue;

            foreach(OctreeNode neighbor in neighbors) {
                validNeighbors.Add((node.center, neighbor.center));
            }
        }

        if (validNeighbors.Count == 0) return null;

        int currIndex = 0;
        Vector3[] linesToDraw = new Vector3[validNeighbors.Count * 2];
        foreach((Vector3, Vector3) pair in validNeighbors) {
            linesToDraw[currIndex] = pair.Item1;
            linesToDraw[currIndex + 1] = pair.Item2;

            currIndex += 2;
        }

        return linesToDraw;
    }
    #endif
}
