using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class GraphNode {
    public Vector3 center;
    public int id = -1;

    // {edge, distance} map
    public Dictionary<GraphNode, float> edges;

    public GraphNode(Vector3 center, int id) {
        this.center = center;
        this.id = id;

        edges = new();
    }

    public void AddEdgeTo(GraphNode toNode) {
        float distance = Vector3.Distance(center, toNode.center);
        edges[toNode] = distance;
    }
}

public class Graph {
    public List<GraphNode> nodes;

    public Graph(List<GraphNode> nodes) {
        this.nodes = nodes;
    }
    // Find the nearest GraphNode to the position
    // we just brute-force it rn
    public GraphNode FindNearestToPosition(Vector3 position) {
        GraphNode nearest = nodes[0];
        float minDist = Mathf.Infinity;

        foreach(var node in nodes) {
            float distance = (position - node.center).magnitude;
            if (distance < minDist) {
                minDist = distance;
                nearest = node;
            }
        }

        return nearest;
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
