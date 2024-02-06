using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

public class OldNodeComparer : IComparer<OldGraphNode> {
    public Dictionary<int, float> gCosts, hCosts;

    public OldNodeComparer(Dictionary<int, float> gCosts, Dictionary<int, float> hCosts) {
        this.gCosts = gCosts;
        this.hCosts = hCosts;
    }

    int IComparer<OldGraphNode>.Compare(OldGraphNode left, OldGraphNode right) {
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

public class OldPathfinder {
    public HashSet<OldGraphNode> displayNodes = new();
    public HashSet<GraphEdge> displayEdges = new();

    private OldGraph graph;

    public OldPathfinder(OldGraph graph) {
        this.graph = graph;
    }

    // Ok this won't really work
    // we should probably return vectors instead
    // cause we need a path from the start position to the nearest node, and same for the end position
    // since agents won't be perfectly in the middle of a node
    public static (List<Vector3>, HashSet<OldGraphNode>) GeneratePath(OldGraph graph, Vector3 start, Vector3 end) {
        // For debug displaying, not needed for algo
        HashSet<OldGraphNode> calculated = new();
        HashSet<OldGraphNode> visited = new();

        // TODO: I should find how long this takes
        // We also need to ensure we don't go _down_ as that leads to a lot of problems
        OldGraphNode startNode = graph.FindNearestToPosition(start);
        OldGraphNode endNode = graph.FindNearestToPosition(end);

        // This could also be an array holy shit.
        // ALL OF THESE COULD BE ARRAYS THAT'S WHAT THIS IS
        HashSet<int> openSet = new();
        HashSet<int> closedSet = new();

        Dictionary<int, float> gCosts = new();
        Dictionary<int, float> hCosts = new();
        Dictionary<int, OldGraphNode> parents = new(); // I could also do <int, int>

        if (startNode == endNode) {
            Debug.LogError("Start node and end node are the same!");
            return (new(), new());
        }

        var heap = new C5.IntervalHeap<OldGraphNode>(new OldNodeComparer(gCosts, hCosts));
        heap.Add(ref startNode.handle, startNode);


        OldGraphNode current = startNode; // just ensure it's never null. Redundant ofc
        while (!heap.IsEmpty) {
            // Pop the top of the heap, which is the minimum f cost node and mark it as the current node
            current = heap.DeleteMin(); // node with the lowest f cost

            closedSet.Add(current.id);

            visited.Add(current);

            if (current == endNode) {
                // We're done!
                break;
            }

            // Display this node

            // populate all current node's neighbors
            foreach(GraphEdge edge in current.edges) {
                OldGraphNode neighbor = edge.to; // from is always going to be the current node

                if (closedSet.Contains(neighbor.id)) continue;

                bool isNeighborInHeap = heap.Contains(neighbor); // Can probably use a boolean

                // aka movementCostToNeighbor
                float currG = gCosts.GetValueOrDefault(neighbor.id, 0);
                float movementCostToNeighbor = currG + edge.distance;
                float neighborG = gCosts.GetValueOrDefault(neighbor.id, 0);

                // Wondering if I should update this to be closer to what PathfindingEnhanced does
                // to see if it gives a different outcome

                if (movementCostToNeighbor < neighborG || !isNeighborInHeap) {
                    gCosts[neighbor.id] = movementCostToNeighbor;

                    // Fuck how do we calculate distance to the end node ughh
                    hCosts[neighbor.id] = Vector3.Distance(neighbor.center, endNode.center);

                    // Set the parent so we can traverse this path at the end
                    parents[neighbor.id] = current;

                    calculated.Add(neighbor);

                    if (!isNeighborInHeap) {
                        heap.Add(ref neighbor.handle, neighbor);
                    } else {
                        Debug.Log($"Updating the Priority of {neighbor.center}");
                        heap.Replace(neighbor.handle, neighbor);
                        // I'm praying that DeleteMin will do the balancing when it's called or something
                        // though that defeats the purpose of the heap. I just don't know how to force it to rebalance
                    }
                }
            }
        }

        foreach(var node in calculated) {
            node.handle = null;
        }

        foreach(var node in visited) {
            node.handle = null;
        }

        List<Vector3> posPath = new();
        posPath.Add(end); // Note we're adding the *end*, not the end node

        List<OldGraphNode> path = new();
        while (parents.TryGetValue(current.id, out OldGraphNode currParent) && current != startNode) {
            path.Add(current);
            posPath.Add(current.center);

            parents.Remove(current.id);
            current = currParent;

        }

        // Add the start node? Why? Won't the above get it?
        path.Add(startNode);

        posPath.Add(startNode.center);
        posPath.Add(start);

        path.Reverse(); // Surely there's a better way to do this
        posPath.Reverse();

        return (posPath, calculated);
    }

    public void Step() {

    }

    // Really just for a reset
    public void Initialize() {
    }
}

public class OldPathfinderComponent : MonoBehaviour {
    public OldGraphGeneratorComponent? graphGenerator;
    public SplineContainer? container;
    public bool DebugDisplay = true;
    public bool DisplayVisited = true;

    // public float agentSize = 1.0f;
    public Transform? StartPosition;
    public Transform? EndPosition;

    private OldPathfinder? pathfinder;

    private HashSet<OldGraphNode> visited = new();
    // private List<GraphNode> path = new();
    private List<Vector3> path = new();
    private Spline? spline = null;

    public void GeneratePath() {
        ResetPathfinder();

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        (path, visited) = OldPathfinder.GeneratePath(graphGenerator!.graph!, StartPosition!.position, EndPosition!.position);
        stopwatch.Stop();

        List<float3> floats = new();
        foreach(var node in path)
        {
            var inLocal = gameObject.transform.InverseTransformPoint(node);
            floats.Add(new float3(inLocal));
        }

        spline = SplineFactory.CreateCatmullRom(floats, false);
        if (container != null) {
            foreach(var spline in container.Splines) { container.RemoveSpline(spline);  }
            container.AddSpline(spline);
        }

        double ticks = stopwatch.ElapsedTicks;
        double milliseconds = (ticks / System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Generated path with {path.Count} nodes in {milliseconds} ms");
    }

    public void Step() {

    }

    public void ResetPathfinder() {
        if (pathfinder == null) {
            if (graphGenerator?.graph != null) {
                pathfinder = new(graphGenerator.graph);
            } else {
                Debug.LogError("Graph hasn't been generated yet!");
            }
        }

        pathfinder?.Initialize();
        
    }

    public void OnDrawGizmos() {
        if (spline != null && container != null)
        {
            // SplineContainer container = new();
            // container.AddSpline(spline);
            SplineGizmoUtility.DrawGizmos(container);
        }


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
            //if (i != 0 && i != path.Count - 1) {
                // Don't draw start and end, we're already doing that above
                Gizmos.DrawSphere(currNode, 2.0f);
            //}

            if (i != 0) {
                edgesToDraw.Add(path[i - 1]);
                edgesToDraw.Add(currNode);
            }

        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLineList(edgesToDraw.ToArray());

    }
}
