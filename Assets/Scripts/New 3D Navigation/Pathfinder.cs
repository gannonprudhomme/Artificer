using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Helper class for generating a path using the A* algoritm
//
// Runs pretty damn fast as-is so didn't look into theta* or lazy theta*
public static class Pathfinder {
    public static List<Vector3> GeneratePath(Graph graph, Vector3 start, Vector3 end) {
        GraphNode startNode = graph.FindNearestToPosition(start);
        GraphNode endNode = graph.FindNearestToPosition(end);

        HashSet<GraphNode> closedSet = new();

        // F cost = G cost + H cost, and it's the main thing we sort on for the min-heap
        // G cost is our distance from the node to the start (or end?), and increased over time as we calculate stuff
        Dictionary<GraphNode, float> gCosts = new();
        // H cost is the distance from the node to the end (or end?)
        Dictionary<GraphNode, float> hCosts = new();
        Dictionary<GraphNode, GraphNode> parents = new();
        Dictionary<GraphNode, C5.IPriorityQueueHandle<GraphNode>> handlesDict = new();

        if (startNode == endNode) {
            return new List<Vector3>() {
                start,
                end
            };
        }

        var heap = new C5.IntervalHeap<GraphNode>(new GraphNodeComparer(gCosts, hCosts));
        C5.IPriorityQueueHandle<GraphNode> currHandle = null;
        heap.Add(ref currHandle, startNode); // Do we want to do the handle stuff?
        handlesDict[startNode] = currHandle;

        GraphNode current = startNode; // Redundant (we're about to pop it), just ensures it's never null
        while(!heap.IsEmpty) {
            // Pop the top of the heap, which is the minimum f-cost node (f = g + h) and mark it as the current node
            current = heap.DeleteMin();

            closedSet.Add(current);

            if (current == endNode) { // we're done!
                break;
            }

            // Populate all current node's neighbors
            foreach(KeyValuePair<GraphNode, float> keyValuePair in current.edges) {
                GraphNode neighbor = keyValuePair.Key;
                float edgeDistance = keyValuePair.Value;

                if (closedSet.Contains(neighbor)) continue; // Skip this if neighbor we've already visited it

                // I think this means the open set
                bool isNeighborInHeap = heap.Contains(neighbor);

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
                        // var neighborHandle = handlesDict!.GetValueOrDefault(neighbor, null);
                        C5.IPriorityQueueHandle<GraphNode> neighborHandle = null;
                        heap.Add(ref neighborHandle, neighbor);
                        handlesDict[neighbor] = neighborHandle;
                    } else {
                        C5.IPriorityQueueHandle<GraphNode> neighborHandle = handlesDict[neighbor];
                        heap.Replace(neighborHandle, neighbor); // Update it with the new values we set above
                    }
                }
            }
        }

        List<Vector3> path = new();
        path.Add(end); // note we're adding the end position, not the end (nearest) GraphNode

        while (parents.TryGetValue(current, out GraphNode currParent) && current != startNode) {
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
}

public class GraphNodeComparer : IComparer<GraphNode> {
    public Dictionary<GraphNode, float> gCosts, hCosts;

    public GraphNodeComparer(
        Dictionary<GraphNode, float> gCosts,
        Dictionary<GraphNode, float> hCosts
    ) {
        this.gCosts = gCosts;
        this.hCosts = hCosts;
    }

    public int Compare(GraphNode left, GraphNode right) {
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
