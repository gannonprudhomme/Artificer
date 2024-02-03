using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class GraphEdge {
    // We don't need from - the node that owns this is always the "from"
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
    public int id = -1;

    // Used for pathfinding
    // If this was generic this wouldn't have it, but I'm using A* so I'm putting these in here dammit
    // g is something we can (and do) calculate at any given step, and it's the distance between start and this node
    // h is something we don't know, and need to estimate - the distance from this node to the target
    // f is the sum of g + h
    // public float f, g, h;

    // Used for pathfinding
    // public bool closed = false;

    // public GraphNode? parent = null;

    // This could really be a dictionary mapping the node it connects to to a distance
    // we don't need the Edge object
    public List<GraphEdge> edges;

    // Ok idk if I should commit this so TODO for removal
    // just for debug displaying
    public bool ProcessingNext = false;
    public bool ProcessedLast = false;

    public GraphNode(Vector3 center, int id) {
        this.center = center;
        this.id = id;
        edges = new();
    }

    public void AddEdgeTo(GraphNode toNode) {
        edges.Add(new GraphEdge(this, toNode));
    }
}

public class Graph { // I don't think i want this to be a monobehavior?
    public List<GraphNode> nodes;

    public int nodeCount {  get { return nodes.Count; } }

    public Graph(List<GraphNode> nodes) {
        this.nodes = nodes;
    }

    // Ok obviously this sucks but we're going to deal with it for now
    // Really we should find the nearest voxel (using the Octree.FindNearestLeaf)
    // then get the GraphNode from the dictionary in GraphGenerator
    // but obviously I need some refactoring before that happens
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

            if (displayEdges) {
                foreach (GraphEdge edge in node.edges) {
                    edges.Add(edge);
                }
            }
        }

        if (displayEdges)
        {
            Vector3[] linesToDraw = new Vector3[edges.Count * 2];
            int currIndex = 0;

            foreach (GraphEdge edge in edges)
            {
                linesToDraw[currIndex] = edge.from.center;
                linesToDraw[currIndex + 1] = edge.to.center;

                // On to the next pair!
                currIndex += 2;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLineList(linesToDraw);
        }
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

