using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#nullable enable

public class NewNavOctreeSpace : MonoBehaviour {
    [Header("Debug")]
    public bool DisplayLeaves = false; // Displays the leaves
    public bool DisplayNonLeaves = false;
    public bool DisplayCollisions = false;
    public bool DisplayIndices = false;
    public bool DisplayIsInBounds = false;
    public bool DisplayOutOfBounds = false;

    public bool DisplayBounds = false;

    public bool DisplayNeighbors = false;

    public NewOctree? octree { get; private set; }

    private Bounds? calculatedBounds = null;

    // TODO: I don't love doing this in here
    public JobHandle CreateRaycastInBoundCommands(
        List<NewOctreeNode> leaves,
        NativeArray<RaycastHit> results
    ) {
        NativeArray<RaycastCommand> commands = new(leaves.Count, Allocator.Persistent);

        for(int i = 0; i < leaves.Count; i++) {
            NewOctreeNode leaf = leaves[i];

            QueryParameters queryParams = new(hitMultipleFaces: false, hitTriggers: QueryTriggerInteraction.UseGlobal, hitBackfaces: false);

            commands[i] = new RaycastCommand(from: leaf.center, direction: Vector3.down, queryParams);
        }

        JobHandle jobHandle = RaycastCommand.ScheduleBatch(
            commands: commands,
            results: results,
            minCommandsPerJob: 1,
            maxHits: 1
        );

        return jobHandle;
    }

    // This is fine to be in here, since we'll need to do this at runtime (Play mode)
    public void Load() {

    }

    public Bounds GetBounds() {
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

    public static long CalculateSize(Vector3 boundsMin, Vector3 boundsMax) {
        float length = boundsMax.x - boundsMin.x;
        float height = boundsMax.y - boundsMin.y;
        float width = boundsMax.z - boundsMin.z;

        // We need to use the longest side since we can only calculate Size as a cube
        float longestSide = Mathf.Max(length, Mathf.Max(width, height));
        float volume = longestSide * longestSide * longestSide;

        long currMinSize = 1;
        while ((currMinSize * currMinSize * currMinSize) < volume) {
            currMinSize *= 2; // Power of 2's!
        }

        return currMinSize;
    }

    public void SetOctree(NewOctree octree) {
        this.octree = octree;

        // Destroy the previously cached gizmo lists
        gizmosAllNodes = null;
        gizmosAllLeaves = null;
        gizmosNotLeaves = null;
        gizmosCollisionLeaves = null;
        gizmosLeavesOutOfBounds = null;
        gizmosLeavesInBounds = null;
        gizmosNeighborsLines = null;
    }

    // Cached gizmo lists so we don't do this every frame
    private List<NewOctreeNode>? gizmosAllNodes = null;
    private List<NewOctreeNode>? gizmosAllLeaves = null;
    private List<NewOctreeNode>? gizmosNotLeaves = null;
    private List<NewOctreeNode>? gizmosCollisionLeaves = null;
    private List<NewOctreeNode>? gizmosLeavesOutOfBounds = null;
    private List<NewOctreeNode>? gizmosLeavesInBounds = null;
    private Vector3[]? gizmosNeighborsLines = null;

    private void OnDrawGizmos() {
         if (calculatedBounds != null && DisplayBounds) {
            float length = calculatedBounds.Value.max.x - calculatedBounds.Value.min.x;
            float height = calculatedBounds.Value.max.y - calculatedBounds.Value.min.y;
            float width = calculatedBounds.Value.max.z - calculatedBounds.Value.min.z;
            float longestSide = Mathf.Max(length, Mathf.Max(width, height));

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(calculatedBounds.Value.center, Vector3.one * longestSide);
            
            if (octree != null) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(octree.center, 3f);
            }
        }
       
        if (!(DisplayLeaves || DisplayCollisions || DisplayIndices || DisplayIsInBounds || DisplayOutOfBounds || DisplayNonLeaves || DisplayNeighbors)) return;

        if (octree == null) return;

        gizmosAllNodes ??= octree.GetAllNodes();
        gizmosAllLeaves ??= gizmosAllNodes.FindAll(node => node.isLeaf);
        gizmosNotLeaves ??= gizmosAllNodes.FindAll(node => !node.isLeaf);
        gizmosCollisionLeaves ??= gizmosAllLeaves.FindAll(leaf => leaf.containsCollision);
        // List<NewOctreeNode> noCollisionLeaves = allLeaves.FindAll(leaf => !leaf.containsCollision);
        gizmosLeavesOutOfBounds ??= gizmosAllLeaves.FindAll(leaf => !leaf.inBounds);
        gizmosLeavesInBounds ??= gizmosAllLeaves.FindAll(leaf => leaf.inBounds);

        if (DisplayLeaves) { // Display the leaves
            Gizmos.color = Color.green;
            foreach(NewOctreeNode leaf in gizmosAllLeaves) {
                leaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNonLeaves) {
            Gizmos.color = Color.blue;
            foreach(var notLeaf in gizmosNotLeaves) {
                notLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayCollisions) {
            Gizmos.color = Color.red;
            foreach(var collisionLeaf in gizmosCollisionLeaves) {
                collisionLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayOutOfBounds) {
            Gizmos.color = Color.magenta;
            foreach(var outOfBoundsLeaf in gizmosLeavesOutOfBounds) {
                outOfBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayIsInBounds) {
            Gizmos.color = Color.yellow;
            foreach(NewOctreeNode inBoundsLeaf in gizmosLeavesInBounds) {
                inBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNeighbors) {
            gizmosNeighborsLines ??= GetNeighborLines(gizmosAllLeaves);

            Gizmos.color = Color.blue;
            Gizmos.DrawLineList(gizmosNeighborsLines);
        }
    }

    private Vector3[] GetNeighborLines(List<NewOctreeNode> allLeaves) {
        List<(Vector3, Vector3)> validNeighbors = new();

        foreach(NewOctreeNode node in allLeaves) {
            List<int4>? neighbors = octree!.GetNeighborsForNode(node);
            if (neighbors == null) continue;

            foreach(int4 neighborKey in neighbors) {
                NewOctreeNode neighbor = octree.nodes[neighborKey];

                validNeighbors.Add((node.center, neighbor.center));
            }
        }

        int currIndex = 0;
        Vector3[] linesToDraw = new Vector3[validNeighbors.Count * 2];
        foreach((Vector3, Vector3) pair in validNeighbors) {
            linesToDraw[currIndex] = pair.Item1;
            linesToDraw[currIndex + 1] = pair.Item2;

            currIndex += 2;
        }

        return linesToDraw;
    }
}
