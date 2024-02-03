using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using C5;

#nullable enable

public class NodeComparer : IComparer<GraphNode> {
    public Dictionary<int, float> gCosts, hCosts;

    public NodeComparer(Dictionary<int, float> gCosts, Dictionary<int, float> hCosts) {
        this.gCosts = gCosts;
        this.hCosts = hCosts;
    }

    int IComparer<GraphNode>.Compare(GraphNode left, GraphNode right) {
        float leftFCost = gCosts[left.id] + hCosts[left.id];
        float rightFCost = gCosts[right.id] + hCosts[right.id];

        float diff = leftFCost - rightFCost;

        if (diff > 0) {
            return 1;
        } else if (diff < 0) {
            return -1;
        }

        // Shouldn't we care about the g or h value here? We gotta decide somehow
        // Presumably h (distance to end)
        return 0; // same value
    }
}

public class Pathfinder {
    public HashSet<GraphNode> displayNodes = new();
    public HashSet<GraphEdge> displayEdges = new();

    private Graph graph;

    public Pathfinder(Graph graph) {
        this.graph = graph;
    }

    // Ok this won't really work
    // we should probably return vectors instead
    // cause we need a path from the start position to the nearest node, and same for the end position
    // since agents won't be perfectly in the middle of a node
    public static (List<GraphNode>, HashSet<GraphNode>) GeneratePath(Graph graph, Vector3 start, Vector3 end) {
        // For debug displaying, not needed for algo
        HashSet<GraphNode> calculated = new();
        HashSet<GraphNode> visited = new();

        // TODO: I should find how long this takes
        // We also need to ensure we don't go _down_ as that leads to a lot of problems
        GraphNode startNode = graph.FindNearestToPosition(start);
        GraphNode endNode = graph.FindNearestToPosition(end);

        // This could also be an array holy shit.
        // ALL OF THESE COULD BE ARRAYS THAT'S WHAT THIS IS
        HashSet<int> openSet = new();
        HashSet<int> closedSet = new();

        Dictionary<int, float> gCosts = new();
        Dictionary<int, float> hCosts = new();
        Dictionary<int, GraphNode> parents = new(); // I could also do <int, int>

        if (startNode == endNode) {
            Debug.LogError("Start node and end node are the same!");
        }

        var heap = new C5.IntervalHeap<GraphNode>(new NodeComparer(gCosts, hCosts)) {
            startNode // Add start node
        };

        GraphNode current = startNode; // just ensure it's never null. Redundant ofc
        while (!heap.IsEmpty) {
            // Pop the top of the heap, which is the minimum f cost node and mark it as the current node
            current = heap.DeleteMin(); // node with the lowest f cost

            closedSet.Add(current.id);

            // visited.Add(current);

            if (current == endNode) {
                // We're done!
                break;
            }

            // Display this node

            // populate all current node's neighbors
            foreach(GraphEdge edge in current.edges) {
                GraphNode neighbor = edge.to; // from is always going to be the current node

                if (closedSet.Contains(neighbor.id)) continue;

                bool isNeighborOpen = heap.Contains(neighbor); // Can probably use a boolean

                // aka movementCostToNeighbor
                float currG = gCosts.GetValueOrDefault(neighbor.id, 0);
                float movementCostToNeighbor = currG + edge.distance;
                float neighborG = gCosts.GetValueOrDefault(neighbor.id, 0);
                if (movementCostToNeighbor < neighborG || !isNeighborOpen) {
                    gCosts[neighbor.id] = movementCostToNeighbor;

                    // Fuck how do we calculate distance to the end node ughh
                    hCosts[neighbor.id] = Vector3.Distance(neighbor.center, endNode.center);

                    // Set the parent so we can traverse this path at the end
                    parents[neighbor.id] = current;

                    calculated.Add(neighbor);

                    if (!isNeighborOpen) {
                        heap.Add(neighbor);
                    } else {
                        // I'm praying that DeleteMin will do the balancing when it's called or something
                        // though that defeats the purpose of the heap. I just don't know how to force it to rebalance
                    }
                }
            }
        }

        List<GraphNode> path = new();
        while (parents.TryGetValue(current.id, out GraphNode currParent) && current != startNode) {
            path.Add(current);

            parents.Remove(current.id);
            current = currParent;
        }

        // Add the start node? Why? Won't the above get it?
        path.Add(startNode);

        path.Reverse(); // Surely there's a better way to do this

        return (path, calculated);
    }

    public void Step() {

    }

    // Really just for a reset
    public void Initialize() {
    }
}

public class PathfinderComponent : MonoBehaviour {
    public GraphGeneratorComponent graphGenerator;
    public bool DebugDisplay = true;
    public bool DisplayVisited = true;

    // public float agentSize = 1.0f;
    public Transform StartPosition;
    public Transform EndPosition;

    private Pathfinder? pathfinder;

    private HashSet<GraphNode> visited = new();
    private List<GraphNode> path = new();

    public void GeneratePath() {
        ResetPathfinder();

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        (path, visited) = Pathfinder.GeneratePath(graphGenerator.graph!, StartPosition.position, EndPosition.position);
        stopwatch.Stop();


        double ticks = stopwatch.ElapsedTicks;
        double milliseconds = (ticks / System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Generated path with {path.Count} nodes in {milliseconds} ms");
    }

    public void Step() {

    }

    public void ResetPathfinder() {
        if (pathfinder == null) {
            if (graphGenerator.graph != null) {
                pathfinder = new(graphGenerator.graph);
            } else {
                Debug.LogError("Graph hasn't been generated yet!");
            }
        }

        pathfinder?.Initialize();
        
    }

    public void OnDrawGizmos() {
        if (!DebugDisplay) return;

        if (StartPosition != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(StartPosition.position, 4.0f);
        }

        if (EndPosition != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(EndPosition.position, 4.0f);
        }

        if (DisplayVisited) {
            Gizmos.color = Color.magenta;
            foreach(var node in visited) {
                Gizmos.DrawSphere(node.center, 2.0f);
            }
        }

        if (path.Count == 0) return;

        List<Vector3> edgesToDraw = new();

        Gizmos.color = Color.yellow;
        for(int i = 0; i < path.Count; i++) {
            var currNode = path[i];
            if (i != 0 && i != path.Count - 1) {
                // Don't draw start and end, we're already doing that above
                Gizmos.DrawSphere(currNode.center, 2.0f);
            }

            if (i != 0) {
                edgesToDraw.Add(path[i - 1].center);
                edgesToDraw.Add(currNode.center);
            }

        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLineList(edgesToDraw.ToArray());
    }
}
