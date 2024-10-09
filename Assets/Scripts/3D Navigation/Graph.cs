using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

// Honestly at this point we might as well combine this with the OctreeNode
public class GraphNode {
    public Vector3 center;

    // {edge, distance} map
    public Dictionary<GraphNode, float> edges;

    public GraphNode(Vector3 center) {
        this.center = center;

        edges = new();
    }

    public void AddEdgeTo(GraphNode toNode) {
        float distance = Vector3.Distance(center, toNode.center);
        edges[toNode] = distance;
    }
}

// Generated by GraphGenerator
public class Graph {
    // Need both of these so we can do FindNearestToPosition (which when doing brute force took a lot of CPU time)
    public Dictionary<OctreeNode, GraphNode> octreeToNodesDict;
    private Octree octree;

    public IEnumerable<GraphNode> nodes {
        get {
            return octreeToNodesDict.Values;
        }
    }

    public Graph(Dictionary<OctreeNode, GraphNode> octreeToNodesDict, Octree octree) {
        this.octreeToNodesDict = octreeToNodesDict;
        this.octree = octree;
    }

    // Find the nearest GraphNode to the position
    public GraphNode? FindNearestToPosition(Vector3 position) {
        OctreeNode? octreeNode = octree.FindNodeForPosition(position);

        if (octreeNode == null) {
            Debug.LogError($"Couldn't find an OctreeNode for position {position}");
            return null;
        }

        // This doesn't quite work because not all OctreeNodes that contain the position
        // will be in this dict, as this dict only contains nodes that *don't* contain a collision (and are in bounds)
        // Thus we do FindClosestValidToPosition
        if (octreeToNodesDict.TryGetValue(octreeNode!.Value, out GraphNode? node)) {
            return node;
        } else if (FindClosestValidToPosition(position, octreeNode!.Value, out GraphNode? closestNode)) {
            return closestNode;
        } else {
            // Don't actually comment this - I have to fix this
            // Debug.LogError($"Couldn't find a node closest to {position}");
            return null;
        }
    }

    // Called when octreeNode has a collision (doesn't have a correspoinding node)
    private bool FindClosestValidToPosition(Vector3 position, OctreeNode octreeNode, out GraphNode? node) {
        // Go through all of the nearest ones

        // TODO: Make this use NativeList, probably?
        // honestly while we're here we _might_ be able to move this to jobs (unlikely w/o ECS though)
        List<OctreeNode>? inBoundsNeighborsWithoutCollisions = octreeNode.inBoundsNeighborsWithoutCollisions.ToList();

        if (inBoundsNeighborsWithoutCollisions == null || inBoundsNeighborsWithoutCollisions.Count == 0) {
            node = null;
            return false;
        }

        OctreeNode closest = inBoundsNeighborsWithoutCollisions[0];
        float minDist = Mathf.Infinity;
        foreach (OctreeNode neighbor in inBoundsNeighborsWithoutCollisions) {
            float distance = (position = neighbor.center).sqrMagnitude; // faster than magnitude - only care about relative values
            if (distance < minDist) {
                closest = neighbor;
                minDist = distance;
            }
        }

        // This should never fail, since we're only look at in bounds nodes w/o collisions, which there should always be
        // a corresponding GraphNode for.
        if (octreeToNodesDict.TryGetValue(closest, out GraphNode closestGraphNode)) {
            node = closestGraphNode;
            return true;
        } else {
            node = null;
            return false;
        }
    }

    public void DrawGraph(bool displayEdges) {
        List<(GraphNode, GraphNode)> edges = new();
        Gizmos.color = Color.red;
        foreach(GraphNode node in nodes) {
            Gizmos.DrawSphere(node.center, 3f);

            if (displayEdges) {
                foreach(GraphNode otherNode in node.edges.Keys) {
                    edges.Add((node, otherNode));
                }
            }
        }

        if (!displayEdges) return;

        int currIndex = 0;
        Vector3[] linesToDraw = new Vector3[edges.Count * 2];
        foreach((GraphNode, GraphNode) pair in edges) {
            linesToDraw[currIndex] = pair.Item1.center;
            linesToDraw[currIndex + 1] = pair.Item2.center;

            currIndex += 2;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLineList(linesToDraw);
    }
}

