using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

#nullable enable

// Helper class for generating a path using standard A* on the Octree
public static class Pathfinder {
    // Generates a path using A*
    //
    // Runs pretty damn fast as-is so didn't look into theta* or lazy theta*
    public static List<Vector3> GeneratePath(Octree octree, Vector3 start, Vector3 end) {
        OctreeNode? startNode = octree.FindClosestValidToPosition(start);
        OctreeNode? endNode = octree.FindClosestValidToPosition(end);

        if (startNode == null || endNode == null) {
            Debug.LogError("Couldn't find start/end node for GeneratePath!");
            return new();
        }

        HashSet<OctreeNode> closedSet = new();

        // F cost = G cost + H cost, and it's the main thing we sort on for the min-heap
        // G cost is our distance from the node to the start (or end?), and increased over time as we calculate stuff
        Dictionary<OctreeNode, float> gCosts = new();
        // H cost is the distance from the node to the end (or end?)
        Dictionary<OctreeNode, float> hCosts = new();
        Dictionary<OctreeNode, OctreeNode> parents = new();
        Dictionary<OctreeNode, C5.IPriorityQueueHandle<OctreeNode>> handlesDict = new();

        if (startNode == endNode) {
            return new List<Vector3>() {
                start,
                end
            };
        }

        HashSet<OctreeNode> openSet = new(); // Only used b/c heap.Contains is really slow for some reason
        var heap = new C5.IntervalHeap<OctreeNode>(new OctreeNodeComparer(gCosts, hCosts));

        #nullable disable // Nullable won't work here inherently
        C5.IPriorityQueueHandle<OctreeNode> currHandle = null;
        heap.Add(ref currHandle, startNode); // Do we want to do the handle stuff?
        openSet.Add(startNode);
        #nullable enable

        handlesDict[startNode] = currHandle;

        OctreeNode current = startNode; // Redundant (we're about to pop it), just ensures it's never null
        while(!heap.IsEmpty) {
            // Pop the top of the heap, which is the minimum f-cost node (f = g + h) and mark it as the current node
            current = heap.DeleteMin();
            openSet.Remove(current);

            closedSet.Add(current);

            if (current == endNode) { // we're done!
                break;
            }

            // Populate all current node's neighbors
            foreach ((OctreeNode neighbor, float edgeDistance) in current.neighbors!) {

                if (closedSet.Contains(neighbor)) continue; // Skip this if neighbor we've already visited it

                bool isNeighborInHeap = openSet.Contains(neighbor); // aka heap.Contains(neighbor)

                float currG = gCosts.GetValueOrDefault(current, 0);
                float movementCostToNeighbor = currG + edgeDistance;
                float neighborG = gCosts.GetValueOrDefault(neighbor, 0);

                if (movementCostToNeighbor < neighborG || !isNeighborInHeap) {
                    // We're going to act on this neighbor, update it's g & h costs now
                    gCosts[neighbor] = movementCostToNeighbor;

                    // Might want to do square magnitude, idk
                    hCosts[neighbor] = Vector3.Distance(neighbor.center, endNode.center);

                    // Set the parent so we can traverse this path at the end
                    parents[neighbor] = current;

                    if (!isNeighborInHeap) {
                        #nullable disable
                        C5.IPriorityQueueHandle<OctreeNode> neighborHandle = null;
                        heap.Add(ref neighborHandle, neighbor);
                        openSet.Add(neighbor);
                        #nullable enable

                        handlesDict[neighbor] = neighborHandle;
                    } else {
                        C5.IPriorityQueueHandle<OctreeNode> neighborHandle = handlesDict[neighbor];

                        heap.Replace(neighborHandle, neighbor); // Update it with the new values we set above
                    }
                }
            }
        }

        List<Vector3> path = new();
        path.Add(end); // note we're adding the end position, not the end (nearest) GraphNode

        while (parents.TryGetValue(current, out OctreeNode currParent) && current != startNode) {
            path.Add(current.center);

            parents.Remove(current);
            current = currParent;
        }

        // We're going to cut through the start node (not add it to the path) and hope it makes it so we don't go backwards lol
        // As we get an issue where the node keeps moving backwards and gets stuck when we constantly re-generate the path
        // path.Add(startNode.center);
        path.Add(start);

        path.Reverse();

        return path;
    }

    // Generates a "smoothed" path using A*, which is a path that contains no "redundant" points
    // 
    // E.g. if there are 3 points in the resulting path, you cannot draw a straight-line without colliding between the first & last points 
    public static List<Vector3> GenerateSmoothedPath(
        Vector3 start,
        Vector3 end,
        Octree octree,
        HashSet<Vector3>? positionsToKeep = null
    ) {
        List<Vector3> rawPath = GeneratePath(octree, start, end);

        return SmoothPath(rawPath, octree, positionsToKeep); // Technically this modified rawPath
    }

    // We need to be able to *not* smooth certain positions
    private static List<Vector3> SmoothPath(
        List<Vector3> inputPath,
        Octree octree,
        HashSet<Vector3>? positionsToKeep = null
    ) {
        if (inputPath.Count == 0) {
            Debug.LogError("SmoothPath was given an empty inputPath!");
            return inputPath;
        }


        // Starting with the first point, draw a line between it and every point from the end backwards
        // the first point that doens't collide with anything is where we draw a point
        // then we move forward, selecting the "new" second point and continue
        // We need to use the Octree to check for collisions
        // 
        // We theoretically could do an actual Physics.Raycast, but we have an Octree for that!
        // 
        // This will reduce redudant points / turns and thus make the path feel way less jaggy
        // I'd say it gets over the worst part of the Octree, but this probably happens to any of the ways to represent space

        int index = 0;

        // Keep going until we're at the last point
        while (index < inputPath.Count) { // Note inputPath.Count is going to change over time

            AttemptToRemovePointsStartingFrom(index, inputPath, octree, positionsToKeep);

            index++; // Move to the next point
        }

        return inputPath;
    }

    // Starting from startIndex, iterates from the end of the path to the point right after startIndex
    // attempting to find the first (aka closest to the end) path that doesn't contain a collision
    // so we can remove redundant points & decrease the jagginess of the path.
    // 
    // Helper function for SmoothPath (basically one step of it)
    private static void AttemptToRemovePointsStartingFrom(
        int startIndex,
        List<Vector3> inputPath,
        Octree octree,
        HashSet<Vector3>? positionsToKeep = null
    ) {
        // inputPath.Size should be max ~50 (and in all likelihood, probs just ~15)
        Vector3 start = inputPath[startIndex];

        // Iterate from end to the point right after the startIndex
        // though we don't actually need to hit that point - we know we can draw a line between them
        for (int i = inputPath.Count - 1; i > startIndex; i--) {
            // Check if we can draw a line between the two points
            // If we can, remove all of the points between them (keeping them two)
            // then we're done

            Vector3 end = inputPath[i];

            if (positionsToKeep != null && positionsToKeep.Contains(end)) continue; // Don't remove ones we were told to keep!

            // Can we draw a line between start & end?
            bool hasCollisionBetweenStartAndEnd = octree.Raycast(start, end);
            if (!hasCollisionBetweenStartAndEnd) {
                // we can draw a line between the points!
                // Thus, remove all of the points between them
                int numberToRemove = i - startIndex - 1;
                inputPath.RemoveRange(startIndex + 1, numberToRemove);

                // Debug.Log($"Removed {numberToRemove} nodes in path at startIdx {startIndex}");

                // we're done
                return;
            }
        }

        // If we got here, then we couldn't simplify this path anymore
    }
}

public class OctreeNodeComparer : IComparer<OctreeNode> {
    public Dictionary<OctreeNode, float> gCosts, hCosts;

    public OctreeNodeComparer(
        Dictionary<OctreeNode, float> gCosts,
        Dictionary<OctreeNode, float> hCosts
    ) {
        this.gCosts = gCosts;
        this.hCosts = hCosts;
    }

    public int Compare(OctreeNode left, OctreeNode right) {
        float leftFCost = gCosts[left] + hCosts[left];
        float rightFCost = gCosts[right] + hCosts[right];

        float diff = leftFCost - rightFCost;

        if (diff > 0) {
            return 1;
        } else if (diff < 0) {
            return -1;
        } else {
            return 0;

            // Previously we just did return 0 here
            // but I figure comparing on h cost when f cost is the same is fine
            float hDiff = hCosts[left] - hCosts[right];

            if (hDiff > 0) {
                return 1;
            } else if (hDiff < 0) {
                return -1;
            } else {
                return 0;
            } 
        }
    }
}
