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

    public NewOctree? octree; /* { get; private set; } */

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

    // Ideally this would live in the Editor, since we don't need to do this at runtime
    public void Save() {


    }

    // This is fine to be in here, since we'll need to do this at runtime (Play mode)
    public void Load() {

    }

    /*
    public void SetOctree(Dictionary<int4, NewOctreeNode> nodes) {
        // octree = new NewOctree()
    }
    */

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

    private static int CalculateSize(Vector3 boundsMin, Vector3 boundsMax) {
        float length = boundsMax.x - boundsMin.x;
        float height = boundsMax.y - boundsMin.y;
        float width = boundsMax.z - boundsMin.z;

        // We need to use the longest side since we can only calculate Size as a cube
        float longestSide = Mathf.Max(length, Mathf.Max(width, height));
        float volume = longestSide * longestSide * longestSide;

        int currMinSize = 1;
        while ((currMinSize * currMinSize * currMinSize) < volume) {
            currMinSize *= 2; // Power of 2's!
        }

        int totalVolume = currMinSize * currMinSize * currMinSize;
        // Debug.Log($"With dimensions of {length}, {height}, {width} and volume {volume} got min size of {currMinSize} and min volume {totalVolume}");
        return currMinSize;
    }

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

        List<NewOctreeNode> allNodes = octree.GetAllNodes();
        List<NewOctreeNode> allLeaves = allNodes.FindAll(node => node.isLeaf);
        List<NewOctreeNode> notLeaves = allNodes.FindAll(node => !node.isLeaf);
        List<NewOctreeNode> collisionLeaves = allLeaves.FindAll(leaf => leaf.containsCollision);
        // List<NewOctreeNode> noCollisionLeaves = allLeaves.FindAll(leaf => !leaf.containsCollision);
        // List<NewOctreeNode> leavesOutOfBounds = allLeaves.FindAll(leaf => !leaf.isInBounds);
        List<NewOctreeNode> leavesInBounds = allLeaves.FindAll(leaf => leaf.inBounds);

        if (DisplayLeaves) { // Display the leaves
            Gizmos.color = Color.green;
            foreach(NewOctreeNode leaf in allLeaves) {
                leaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayNonLeaves) {
            Gizmos.color = Color.blue;
            foreach(var notLeaf in notLeaves) {
                notLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        if (DisplayCollisions) {
            Gizmos.color = Color.red;
            foreach(var collisionLeaf in collisionLeaves) {
                collisionLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }

        /*
        if (DisplayOutOfBounds) {
            Gizmos.color = Color.magenta;
            foreach(OctreeNode outOfBoundsLeaf in leavesOutOfBounds) {
                outOfBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }
        */

        if (DisplayIsInBounds) {
            Gizmos.color = Color.yellow;
            foreach(NewOctreeNode inBoundsLeaf in leavesInBounds) {
                inBoundsLeaf.DrawGizmos(DisplayIndices, Color.white);
            }
        }
    }
}
