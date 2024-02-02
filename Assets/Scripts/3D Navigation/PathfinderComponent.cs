using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Codice.CM.Common.Tree;
using JetBrains.Annotations;
using UnityEngine;
// using C5;

#nullable enable

// Ok I'm not going to do this now but I need to
// Cause we're modifying the graph, which should be read-only
// Maybe I can just use dictionaries for the f/g/h/parent stuff that we need?
// and have the key be some index which we calculate in Graph.CalculateConnectivity or w/e
public class PathfinderNode: GraphNode {
    public float f, g, h;

    public PathfinderNode(GraphNode node) : base(node.center, node.index) {

    }
}

public class NodeComparer : IComparer<GraphNode> {
    int IComparer<GraphNode>.Compare(GraphNode x, GraphNode y) {
        float diff = x.f - y.f;

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
    public List<GraphNode> GeneratePath(Vector3 start, Vector3 end) {

        GraphNode startNode = graph.FindNearestToPosition(start);
        GraphNode endNode = graph.FindNearestToPosition(end);

        if (startNode == endNode) {
            Debug.LogError("Start node and end node are the same!");
        }

        var heap = new C5.IntervalHeap<GraphNode>(new NodeComparer()) {
            startNode // Add start node
        };

        GraphNode current = startNode; // just ensure it's never null. Redundant ofc
        while (!heap.IsEmpty) {
            // Pop the top of the heap, which is the minimum f cost node and mark it as the current node
            current = heap.DeleteMin(); // node with the lowest f cost

            current.closed = true;

            if (current == endNode) {
                // We're done!
                break;
            }

            // Display this node

            // populate all current node's neighbors
            foreach(GraphEdge edge in current.edges) {
                GraphNode neighbor = edge.to; // from is always going to be the current node

                if (neighbor.closed) continue;

                bool isNeighborOpen = heap.Contains(neighbor); // Can probably use a boolean

                // aka movementCostToNeighbor
                // float currG = 
                float movementCostToNeighbor = current.g + edge.distance;
                if (movementCostToNeighbor < neighbor.g || !isNeighborOpen) { // Hrm
                    neighbor.g = movementCostToNeighbor;
                    // Fuck how do we calculate distance to the end node ughh
                    neighbor.h = Vector3.Distance(neighbor.center, endNode.center);

                    // Set the parent so we can traverse this path at the end
                    neighbor.parent = current;

                    if (!isNeighborOpen) {
                        heap.Add(neighbor);
                    } else {
                        // I'm praying that DeleteMin will do the balancing when it's called or something
                        // though that defeats the purpose of the heap. I just don't know how to force it to rebalance
                    }
                }
            }
        }

        // We've found the path, now lets construct it by back tracking
        List<GraphNode> path = new();
        while(current.parent != null && current != startNode) {
            path.Add(current);
            // current.closed = false; // Do we really need to do this? Does it really matter?

            GraphNode nextCurrent = current.parent;
            current.parent = null; // Why do we need to do this?
            current = nextCurrent;
        }

        // Add the start node? Why? Won't the above get it?
        path.Add(startNode);

        path.Reverse(); // Surely there's a better way to do this

        return path;
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

    // public float agentSize = 1.0f;
    public Transform StartPosition;
    public Transform EndPosition;

    private Pathfinder? pathfinder;

    private List<GraphNode> path = new();

    public void GeneratePath() {
        ResetPathfinder();

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        path = pathfinder!.GeneratePath(StartPosition.position, EndPosition.position);
        stopwatch.Stop();

        Debug.Log($"Generated path with {path.Count} nodes in {stopwatch.ElapsedMilliseconds} ms");
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

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(StartPosition.position, 4.0f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(EndPosition.position, 4.0f);

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
