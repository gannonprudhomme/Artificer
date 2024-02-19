using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

#nullable enable

// Component to go on the (parent-most) Level game object
// Intended to be the equivalent of NavMeshSurface
public class NavOctreeSpace : MonoBehaviour {
    [Header("Debug")]
    public bool DisplayLeaves = false; // Displays the leaves
    public bool DisplayNonLeaves = false;
    public bool DisplayCollisions = false;
    public bool DisplayIndices = false;
    public bool DisplayIsInBounds = false;
    public bool DisplayOutOfBounds = false;

    public bool DisplayBounds = false;

    public Octree? octree { get; private set; }

    private Bounds? calculatedBounds = null; // For debug displaying
    
    public void GenerateOctree() {
        Bounds bounds = GetBounds();
        calculatedBounds = bounds;

        octree = new Octree(
            min: bounds.min,
            max: bounds.max,
            smallestActorDimension: Vector3.one * 5f,
            center: bounds.center
        );

        // Generate and time it
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        octree.Generate(this.gameObject);
        stopwatch.Stop();

        // Debug output
        List<OctreeNode> allNodes = octree.GetAllNodes();
        List<OctreeNode> leaves = allNodes.FindAll((node) => node.children == null);
        Debug.Log($"Finished generating octree with {allNodes.Count} nodes and {leaves.Count} leaves in {stopwatch.ElapsedMilliseconds} ms");
    }

    // Save the generated octree to a file
    public void Save() {
        if (octree == null) {
            Debug.LogError("No octree to save!");
            return;
        }

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        using (var stream = File.Open(GetFileName(), FileMode.Create))
        using(var writer = new BinaryWriter(stream)) {
            OctreeSerializer.Serialize(octree, writer);
        }

        stopwatch.Stop();

        int nodeCount = octree.GetAllNodes().Count;

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Wrote Octree with {nodeCount} nodes to '{GetFileName()}' in {ms} ms");
    }

    // Load the generated octree from a file into memory (put in this.octree)
    public void Load() {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        using (var stream = File.Open(GetFileName(), FileMode.Open))
        using (var reader = new BinaryReader(stream)) {
            octree = OctreeSerializer.Deserialize(reader);
        }

        stopwatch.Stop();

        int nodeCount = octree.GetAllNodes().Count;

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Read Octree from '{GetFileName()}' and got {nodeCount} nodes in {ms} ms");
    }

    private Bounds GetBounds() {
        // This gets renderers from this GameObject(Component), as well as it's children recursively
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        // if (renderers.Count == 0) return new Bounds
        Bounds bounds = renderers[0].bounds;
        // Debug.Log($"Bounds: {bounds}");
        foreach(var renderer in renderers) {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    // Retrieves the filename 
    // Should probably be based off the GameObject name?
    private string GetFileName() {
        return $"{gameObject.name}.octree.bin";
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

        if (!(DisplayLeaves || DisplayCollisions || DisplayIndices || DisplayIsInBounds || DisplayOutOfBounds || DisplayNonLeaves)) return;

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
    }
}
