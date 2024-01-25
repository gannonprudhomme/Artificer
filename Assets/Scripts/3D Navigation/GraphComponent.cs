using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class GraphComponent: MonoBehaviour {
    public enum GraphType { CENTER, CORNER }

    [Tooltip("Reference to the octree we use to build the graph")]
    public Octree? Octree;
    public GraphType graphType = GraphType.CENTER;

    public bool ShouldDisplayGraph = true;

    private Graph? graph;

    public static readonly int[,] dir = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };

    public void GenerateGraph() {
        if(Octree == null) {
            Debug.LogError("Octree wasn't set! Can't continue");
            return;
        }

        switch (graphType) {
            case GraphType.CENTER:
                graph = GenerateCenterGraph(Octree);
                break;
            case GraphType.CORNER:
                graph = GenerateCornerGraph(Octree);
                break;
        }
    }

    private void GenerateCornerGraph() {

    }

    // Ok idk where to put this but it definitely shouldn't be in here
    // Maybe a GraphGenerator class/function? Maybe a static function on Graph that returns a Graph instance
    private static Graph GenerateCenterGraph(Octree octree) { // Doing a CenterGraph (for now at least)

        List<OctreeNode> octLeaves = octree.Leaves();
        Dictionary<OctreeNode, GraphNode> dict = new();

        List<GraphNode> allNodes = new();
        int nodeCount = 0;

        // Build the dictionary mapping between OctreeNode (leaves): Node
        foreach(OctreeNode octLeaf in octLeaves) {
            if (octLeaf.containsCollision) continue;

            GraphNode node = new(octLeaf.center, nodeCount);
            dict.Add(octLeaf, node);
            allNodes.Add(node);

            nodeCount += 1;
        }

        foreach(KeyValuePair<OctreeNode, GraphNode> keyPair in dict) {
            OctreeNode octLeaf = keyPair.Key;
            GraphNode currNode = keyPair.Value;

            if (octLeaf.containsCollision) continue; // Pointless check - we know it won't b/c of the above loop

            if (octLeaf.nodeLevel == 0) {
                Debug.Log("Ignoring nodeLevel 0 - how did this happen?");
                continue;
            }

            for(int i = 0; i < 6; i++) {
                int[] indices = new int[] {
                    octLeaf.index[0] + dir[i, 0],
                    octLeaf.index[1] + dir[i, 1],
                    octLeaf.index[2] + dir[i, 2]
                };

                OctreeNode? nearestOctLeaf = octree.FindNearestLeaf(gridIndex: indices, level: octLeaf.nodeLevel);
                if (nearestOctLeaf == null || nearestOctLeaf.containsCollision) continue;

                GraphNode nearestNode = dict[nearestOctLeaf]; // Get the corresponding GraphNode
                
                if (nearestOctLeaf.nodeLevel <= octLeaf.nodeLevel) {

                    // Add an edge
                    currNode.edges.Add(new GraphEdge(currNode, nearestNode));
                    // Add one the other way
                    nearestNode.edges.Add(new GraphEdge(nearestNode, currNode));
                } else if (nearestOctLeaf.children == null) { // WHICH IT ALWAYS FUCKING WILL
                    // Add one only one way? Wtf why? Maybe so you can only go down a level?
                    currNode.edges.Add(new GraphEdge(currNode, nearestNode));
                    nearestNode.edges.Add(new GraphEdge(nearestNode, currNode));
                }
            }
        }

        Graph graph = new Graph(allNodes);
        graph.CalculateConnectivity();

        return graph;
    }

    private void OnDrawGizmosSelected() {
        if (!ShouldDisplayGraph || graph == null) return;

        graph.DrawGraph();
    }
}
