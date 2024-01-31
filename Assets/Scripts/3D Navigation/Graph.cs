using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class GraphEdge {
    public GraphNode from;
    public GraphNode to;

    public float distance;

    public GraphEdge(GraphNode from, GraphNode to) {
        this.from = from;
        this.to = to;

        this.distance = (from.center - to.center).magnitude;
    }
}

public class GraphNode {
    public Vector3 center;
    public int index;
    public int connectIndex = 0;

    // This could really be a dictionary mapping the node it connects to to a distance
    // we don't need the Edge object
    public List<GraphEdge> edges;

    // Ok idk if I should commit this so TODO for removal
    // just for debug displaying
    public bool ProcessingNext = false;
    public bool ProcessedLast = false;

    public GraphNode(Vector3 center, int index) {
        this.center = center;
        this.index = index;
        edges = new();
    }
}

public class Graph { // I don't think i want this to be a monobehavior?
    public List<GraphNode> nodes;
    // private List<Node> temporaryNodes;

    public int nodeCount {  get { return nodes.Count; } }

    public Graph(List<GraphNode> nodes) {
        this.nodes = nodes;
    }

    // Calculate connectIndex on each node, which is used for pathfinding
    // Why? I'm not sure yet
    public void CalculateConnectivity() {
        foreach(GraphNode node in nodes) {
            // Idk if this is necessary but w/e
            node.connectIndex = 0;
        }

        int current = 0 - 1;
        foreach (GraphNode node in nodes) {

        }
    }

    public void DrawGraph() {
        List<GraphEdge> edges = new();
        foreach(GraphNode node in nodes) {
            if (node.ProcessingNext) {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(node.center, 5f);
            } else if (node.ProcessedLast) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(node.center, 5f);
            } else {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(node.center, 3f);
            }

            foreach(GraphEdge edge in node.edges) {
                // Gizmos.DrawLine(edge.from.center, edge.to.center);
                edges.Add(edge);
            }
        }

        // List<Vector3> linesToDraw = new();
        Vector3[] linesToDraw = new Vector3[edges.Count * 2];
        int currIndex = 0;

        foreach(GraphEdge edge in edges) {
            linesToDraw[currIndex] = edge.from.center;
            linesToDraw[currIndex + 1] = edge.to.center;

            // On to the next pair!
            currIndex += 2;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLineList(linesToDraw);
    }

    // For debug counting, probs don't actually want
    public int GetEdgeCount() {
        HashSet<(GraphNode, GraphNode)> edges = new();

        foreach(var node in nodes) {
            foreach(GraphEdge edge in node.edges) {
                var pair1 = (edge.from, edge.to);
                var pair2 = (edge.to, edge.from);

                if (!edges.Contains(pair1) && !(edges.Contains(pair2)))
                    edges.Add(pair1);
            }
        }

        return edges.Count;
    }
}

