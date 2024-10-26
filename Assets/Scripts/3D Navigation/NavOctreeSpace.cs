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

    public void BuildNeighbors() {
        if (octree == null) { Debug.LogError("No octree!"); return;}

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        GraphGenerator.PopulateOctreeNeighbors(octree, shouldBuildDiagonals: true);
        stopwatch.Stop();

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;

        Debug.Log($"Finished building neighbors in {seconds:F2} sec ({ms:F0} ms)");
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
    }

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

        List<OctreeNode> allNodes = octree.GetAllNodes();
        List<OctreeNode> allLeaves = allNodes.FindAll(node => node.children == null);
        List<OctreeNode> notLeaves = allNodes.FindAll(node => node.children != null);
        List<OctreeNode> collisionLeaves = allLeaves.FindAll(leaf => leaf.containsCollision);
        List<OctreeNode> noCollisionLeaves = allLeaves.FindAll(leaf => !leaf.containsCollision);
        List<OctreeNode> leavesOutOfBounds = allLeaves.FindAll(leaf => !leaf.isInBounds);
        List<OctreeNode> leavesInBounds = allLeaves.FindAll(leaf => leaf.isInBounds);

        if (DisplayLeaves) { // Display the leaves
            Gizmos.color = Color.green;
            foreach(OctreeNode leaf in allLeaves) {
                leaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNonLeaves) {
            Gizmos.color = Color.blue;
            foreach(OctreeNode notLeaf in notLeaves) {
                notLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayCollisions) {
            Gizmos.color = Color.red;
            foreach(OctreeNode collisionLeaf in collisionLeaves) {
                collisionLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayOutOfBounds) {
            Gizmos.color = Color.magenta;
            foreach(OctreeNode outOfBoundsLeaf in leavesOutOfBounds) {
                outOfBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayIsInBounds) {
            Gizmos.color = Color.yellow;
            foreach(OctreeNode inBoundsLeaf in leavesInBounds) {
                inBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNeighbors) {
            List<(Vector3, Vector3)> validNeighbors = new();

            foreach(OctreeNode node in allLeaves) {
                if (node.inBoundsNeighborsWithoutCollisions != null) {
                    foreach (OctreeNode neighbor in node.inBoundsNeighborsWithoutCollisions) {
                        validNeighbors.Add((node.center, neighbor.center));
                    }
                }
            }

            Gizmos.color = Color.blue;
            DrawLineList(validNeighbors);
        }
    }

    private static void DrawLineList(List<(Vector3, Vector3)> lines) {
        int currIndex = 0;
        Vector3[] linesToDraw = new Vector3[lines.Count * 2];
        foreach((Vector3, Vector3) pair in lines) {
            linesToDraw[currIndex] = pair.Item1;
            linesToDraw[currIndex + 1] = pair.Item2;

            currIndex += 2;
        }

        Gizmos.DrawLineList(linesToDraw);
    }
}
