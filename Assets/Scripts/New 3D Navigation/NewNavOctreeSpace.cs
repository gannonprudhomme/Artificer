using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

#nullable enable

public class NewNavOctreeSpace : MonoBehaviour {
    [Header("Debug")]
    public bool DisplayLeaves = false; // Displays the leaves
    public bool DisplayNonLeaves = false;
    public bool DisplayCollisions = false;
    public bool DisplayIndices = false;
    public bool DisplayIsInBounds = false;
    [Tooltip("In Bounds & no collisions, aka what we use for pathfinding")]
    public bool DisplayValidLeaves = false;
    public bool DisplayOutOfBounds = false;

    public bool DisplayBounds = false;

    public bool DisplayNeighbors = false;

    public NewOctree? octree { get; private set; }

    private Bounds? calculatedBounds = null;

    public int maxDivisionLevel = 9;

    public int batchThing = 1024;

    // TODO: Probably remove this? It shouldn't really be used
    // though honestly it's not a bad idea to expose
    // This can be incredibly high and still give improvements
    // so this should really be Num Jobs
    public int numCores = 8;

    // TODO: I don't love doing this in here
    public JobHandle CreateRaycastInBoundCommands(
        List<NewOctreeNode> leaves,
        NativeArray<RaycastHit> results
    ) {
        NativeArray<RaycastCommand> commands = new(leaves.Count, Allocator.Persistent);

        for(int i = 0; i < leaves.Count; i++) {
            // TODO: We can skip leaves that contain a collision & mark them as in boundssince we know that they're in bounds?
            // I'm not even sure if that's true - it could contain a collision but be at the bottom of/under the map

            NewOctreeNode leaf = leaves[i];

            // Below are the default parametes (for now)
            // if I don't change them just use QueryParameters.Default
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

        #if UNITY_EDITOR
        // Destroy the previously cached gizmo lists
        gizmosAllNodes = null;
        gizmosAllLeaves = null;
        gizmosNotLeaves = null;
        gizmosCollisionLeaves = null;
        gizmosLeavesOutOfBounds = null;
        gizmosLeavesInBounds = null;
        gizmosValidLeaves = null;
        gizmosNeighborsLines = null;
        #endif
    }

#if UNITY_EDITOR
    // Cached gizmo lists so we don't do this every frame
    private List<NewOctreeNode>? gizmosAllNodes = null;
    private List<NewOctreeNode>? gizmosAllLeaves = null;
    private List<NewOctreeNode>? gizmosNotLeaves = null;
    private List<NewOctreeNode>? gizmosCollisionLeaves = null;
    private List<NewOctreeNode>? gizmosLeavesOutOfBounds = null;
    private List<NewOctreeNode>? gizmosLeavesInBounds = null;
    private List<NewOctreeNode>? gizmosValidLeaves = null;
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
       
        if (!(DisplayLeaves || DisplayCollisions || DisplayIndices || DisplayIsInBounds || DisplayOutOfBounds || DisplayNonLeaves || DisplayNeighbors || DisplayValidLeaves)) return;

        if (octree == null) return;

        // Sort it by node level so we can prioritize drawing the smaller leaves last (makes the Gizmos look better / more clear)
        gizmosAllNodes ??= octree.GetAllNodes().OrderBy(node => node.nodeLevel).ToList();
        gizmosAllLeaves ??= gizmosAllNodes.FindAll(node => node.isLeaf);
        gizmosNotLeaves ??= gizmosAllNodes.FindAll(node => !node.isLeaf);
        gizmosCollisionLeaves ??= gizmosAllLeaves.FindAll(leaf => leaf.containsCollision);
        gizmosLeavesOutOfBounds ??= gizmosAllLeaves.FindAll(leaf => !leaf.inBounds);
        gizmosLeavesInBounds ??= gizmosAllLeaves.FindAll(leaf => leaf.inBounds);
        gizmosValidLeaves ??= gizmosLeavesInBounds.FindAll(leaf => !leaf.containsCollision);

        if (DisplayLeaves) { // Display the leaves
            Gizmos.color = Color.green;
            foreach(NewOctreeNode leaf in gizmosAllLeaves) {
                leaf.DrawGizmos(DisplayIndices, textColor: Color.white);
            }
        }

        if (DisplayNonLeaves) {
            Gizmos.color = Color.blue;
            foreach(var notLeaf in gizmosNotLeaves) {
                notLeaf.DrawGizmos(DisplayIndices, textColor: Color.white);
            }
        }

        if (DisplayCollisions) {
            Gizmos.color = Color.red;
            foreach(var collisionLeaf in gizmosCollisionLeaves) {
                collisionLeaf.DrawGizmos(DisplayIndices, textColor: Color.white);
            }
        }

        if (DisplayOutOfBounds) {
            Gizmos.color = Color.magenta;
            foreach(var outOfBoundsLeaf in gizmosLeavesOutOfBounds) {
                outOfBoundsLeaf.DrawGizmos(DisplayIndices, textColor: Color.white);
            }
        }

        if (DisplayIsInBounds) {
            Gizmos.color = Color.yellow;
            foreach(NewOctreeNode inBoundsLeaf in gizmosLeavesInBounds) {
                inBoundsLeaf.DrawGizmos(DisplayIndices, textColor: Color.white);
            }
        }

        if (DisplayValidLeaves) {
            foreach(var validLeaf in gizmosValidLeaves) {
                validLeaf.DrawGizmos(DisplayIndices, textColor: Color.white);
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

    private Vector3[]? GetNeighborLines(List<NewOctreeNode> allLeaves) {
        if (allLeaves.Count == 0) return null;

        List<(Vector3, Vector3)> validNeighbors = new();

        foreach(NewOctreeNode node in allLeaves) {
            List<int4>? neighbors = octree!.GetNeighborsForNode(node);
            if (neighbors == null) continue;

            foreach(int4 neighborKey in neighbors) {
                NewOctreeNode neighbor = octree.nodes[neighborKey];

                // Not doing this check since generating the neighbors should do it
                //if (neighbor.inBounds && !neighbor.containsCollision) {
                    validNeighbors.Add((node.center, neighbor.center));
                //}
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
