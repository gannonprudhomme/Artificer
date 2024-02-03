using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using log4net.Core;
using UnityEngine;

#nullable enable

public class OldGraphComponent: MonoBehaviour {
    public enum GraphType { Center, Corner, Crossed }

    [Tooltip("Reference to the octree we use to build the graph")]
    public Octree? Octree;
    public GraphType graphType = GraphType.Center;

    public bool ShouldDisplayGraph = true;
    public bool DebugLogs = true;

    private Graph? graph;

    public static readonly int[,] dir = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
    public static int[,] edgeDir = { { 0, 1, 1 }, { 0, 1, -1 }, { 0, -1, 1 }, { 0, -1, -1 }, { 1, 0, 1 }, { -1, 0, 1 }, { 1, 0, -1 }, { -1, 0, -1 }, { 1, 1, 0 }, { 1, -1, 0 }, { -1, 1, 0 }, { -1, -1, 0 } };

    // ONly used for CrossGraph funnily enough
    public static int[,] cornerDir = { { 0, 0, 0 }, { 1, 0, 0 }, { 1, 1, 0 }, { 0, 1, 0 }, { 0, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 0, 1, 1 } };

    public void GenerateGraph() {
        if(Octree == null) {
            Debug.LogError("Octree wasn't set! Can't continue");
            return;
        }

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        switch (graphType) {
            case GraphType.Center:
                graph = GenerateEntireCenterGraph(Octree);
                break;
            case GraphType.Corner:
                graph = GenerateCornerGraph(Octree);
                break;
            case GraphType.Crossed:
                graph = GenerateCrossGraph(Octree);
                break;
        }

        stopwatch.Stop();

        Debug.Log($"Generated graph with {graph!.nodeCount} nodes and {graph!.GetEdgeCount()} edges in {stopwatch.ElapsedMilliseconds} ms");
    }

    private List<OctreeNode> octLeaves = new();
    private Dictionary<OctreeNode, GraphNode> dict = new();
    private List<KeyValuePair<OctreeNode, GraphNode>> keyPairs = new();

    private List<GraphNode> allNodes = new();
    private int nodeCount = 0;

    private bool hasDoneInitialSetup = false;

    private GraphNode? last = null;
    private GraphNode? next = null;

    private int currKeyPairIndex = 0;

    public void ResetSteps() {
        if (Octree == null) {
            Debug.LogError("Octree was null");
            return;
        }

        dict = new();
        keyPairs = new();
        allNodes = new();

        octLeaves = Octree.Leaves();
        currKeyPairIndex = 0;
        next = null;
        last = null;

        // Build the dictionary mapping between OctreeNode (leaves): Node
        foreach(OctreeNode octLeaf in octLeaves) {
            if (octLeaf.containsCollision) continue;

            GraphNode node = new(octLeaf.center, nodeCount);
            dict.Add(octLeaf, node);
            allNodes.Add(node);


            nodeCount += 1;
        }


        foreach (KeyValuePair<OctreeNode, GraphNode> keyPair in dict) {
            keyPairs.Add(keyPair);
        }
    }

    public void DoStep(int numSteps = 1) {
        if (Octree == null) {
            // Debug.LogError("Octree was null");
            return;
        }

        if (!hasDoneInitialSetup) {
            // Debug.Log("Doing initial setup!");
            hasDoneInitialSetup = true;
            ResetSteps();
        }

        // This won't work with multiple
        if (last != null) 
            last.ProcessedLast = false;

        if (next != null)
            next.ProcessingNext = false;

        //for (int i = 0; i < numSteps; i++) {
        if (currKeyPairIndex >= keyPairs.Count) {
            // Debug.Log("Done!");
            return;
        }

        var curr = keyPairs[currKeyPairIndex];
        DoCenterGraphStep(Octree, curr, dict);

        last = curr.Value;
        last.ProcessedLast = true;

        // Highlight the next one
        if (currKeyPairIndex + 1 < keyPairs.Count) {
            next = keyPairs[currKeyPairIndex + 1].Value;
            if (next != null) next.ProcessingNext = true;
        }

        currKeyPairIndex++;

        graph = new Graph(allNodes);
    }


    // Ok idk where to put this but it definitely shouldn't be in here
    // Maybe a GraphGenerator class/function? Maybe a static function on Graph that returns a Graph instance
    private static Graph GenerateEntireCenterGraph(Octree octree) { // Doing a CenterGraph (for now at least)

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
            DoCenterGraphStep(octree, keyPair, dict);
        }

        Graph graph = new(allNodes);

        return graph;
    }

    private static void DoCenterGraphStep(Octree octree, KeyValuePair<OctreeNode, GraphNode> keyPair, Dictionary<OctreeNode, GraphNode> dict) {
        OctreeNode octLeaf = keyPair.Key;
        GraphNode currNode = keyPair.Value;
        bool DebugLogs = false;

        if (octLeaf.containsCollision) return; // Pointless check - we know it won't b/c of the above loop

        if (octLeaf.nodeLevel == 0) {
            Debug.Log("Ignoring nodeLevel 0 - how did this happen?");
            return;
        }

        for (int i = 0; i < 6; i++) {
            int[] indices = new int[] {
                    octLeaf.index[0] + dir[i, 0],
                    octLeaf.index[1] + dir[i, 1],
                    octLeaf.index[2] + dir[i, 2]
                };

            // OctreeNode? nearestOctLeaf = octree.FindNearestLeaf(gridIndex: indices, level: octree.MaxDivisionLevel);
            if (DebugLogs) Debug.Log($"Grph: Trying to find nearest leaf for {octLeaf.IndexToString()} in dir {IndexToStr(indices)}");
            OctreeNode? nearestOctLeaf = octree.FindNearestLeaf(gridIndex: indices, level: octLeaf.nodeLevel);
            if (nearestOctLeaf == null || nearestOctLeaf.containsCollision) {
                if (DebugLogs) Debug.Log($"Node found for {octLeaf.IndexToString()} in dir {IndexToStr(indices)} was null ({nearestOctLeaf == null}) or contained collision?");
                continue;
            }

            GraphNode nearestNode = dict[nearestOctLeaf]; // Get the corresponding GraphNode

            // Add an edge
            // currNode.edges.Add(new GraphEdge(currNode, nearestNode));
            // Add one the other way
            // nearestNode.edges.Add(new GraphEdge(nearestNode, currNode));

            if (nearestOctLeaf.nodeLevel < octLeaf.nodeLevel) {
                if (DebugLogs) Debug.Log($"Creating edge between curr {octLeaf.index} and nearest {nearestOctLeaf.index}");

                // Add an edge
                currNode.edges.Add(new GraphEdge(currNode, nearestNode));
                // Add one the other way
                nearestNode.edges.Add(new GraphEdge(nearestNode, currNode));
            }
            else if (nearestOctLeaf.children == null) { // WHICH IT ALWAYS FUCKING WILL
                if (DebugLogs) Debug.Log($"Creating edge between curr {octLeaf.index} and leaf {nearestOctLeaf.index}");
              // Add one only one way? Wtf why? Maybe so you can only go down a level?
                currNode.edges.Add(new GraphEdge(currNode, nearestNode));
                nearestNode.edges.Add(new GraphEdge(nearestNode, currNode));
            }
        }
    }

    public static string IndexToStr(int[] arr)
    {
        return $"[{arr[0]}, {arr[1]}, {arr[2]}]";
    }

    private static Graph GenerateCornerGraph(Octree octree) {
        List<GraphNode> allNodes = new();

        Dictionary<long, GraphNode> dict = new();
        HashSet<long> edges = new();

        List<OctreeNode> leaves = octree.Leaves();
        foreach(OctreeNode octLeaf in leaves) {
            for(int i = 0; i < 12; i++) {
                int[] dirs = { edgeDir[i, 0], edgeDir[i, 1], edgeDir[i, 2] };

                // Still don't know what this is
                int[][] threeNeighborDir = GetThreeNeighborDir(dirs);

                bool shouldCreateEdge = false;

                if (octLeaf.containsCollision) {
                    shouldCreateEdge = false;

                    // Can we turn this into a function?
                    for(int j = 0; j < 3; j++) {
                        // If any of these don't contain a collision, then we make an edge?
                        shouldCreateEdge = !DoesContainCollision(
                            new int[] {
                                octLeaf.index[0] + threeNeighborDir[j][0],
                                octLeaf.index[1] + threeNeighborDir[j][1],
                                octLeaf.index[2] + threeNeighborDir[j][2],
                            },
                            octree,
                            octree.MaxDivisionLevel
                        );

                        if (shouldCreateEdge) {
                            break;
                        }
                    }
                } else {
                    shouldCreateEdge = true;
                    for (int j = 0; j < 3; j++) {
                        OctreeNode found = octree.FindNearestLeaf(
                            new int[] {
                                octLeaf.index[0] + threeNeighborDir[j][0],
                                octLeaf.index[1] + threeNeighborDir[j][1],
                                octLeaf.index[2] + threeNeighborDir[j][2]
                            },
                            octLeaf.nodeLevel
                        );

                        if (found != null && found.nodeLevel == octLeaf.nodeLevel && found.children != null) {
                            shouldCreateEdge = false;
                            break;
                        }
                    }
                }

                if(shouldCreateEdge) {
                    int[][] arcVertexCoord = GetEdgeVertexDir(new int[] { edgeDir[i, 0], edgeDir[i, 1], edgeDir[i, 2] });
                    for (int j = 0; j < 2; j++) {
                        for (int k = 0; k < 3; k++) {
                            arcVertexCoord[j][k] = (octLeaf.index[k] * 2 + 1 + arcVertexCoord[j][k]) / 2 * (1 << (octree.MaxDivisionLevel - octLeaf.nodeLevel));
                        }
                    }

                    long edgeID = GetEdgeKey(arcVertexCoord[0], arcVertexCoord[1], octree.MaxDivisionLevel);

                    if (!edges.Contains(edgeID)) {
                        edges.Add(edgeID);

                        GraphNode node1 = GetFromOrCreateAndAddNodeToDict(arcVertexCoord[0], dict, allNodes, octree.MaxDivisionLevel, octree.Size, octLeaf);
                        GraphNode node2 = GetFromOrCreateAndAddNodeToDict(arcVertexCoord[1], dict, allNodes, octree.MaxDivisionLevel, octree.Size, octLeaf);

                        node1.edges.Add(new GraphEdge(node1, node2));
                        node2.edges.Add(new GraphEdge(node2, node1));
                    }
                }
            }
        }

        Graph graph = new(allNodes);
        return graph;
    }

    public static Graph GenerateCrossGraph(Octree octree) {
        List<OctreeNode> leaves = octree.Leaves();
        Dictionary<long, GraphNode> dict = new();
        Dictionary<string, bool> arcAdded = new();
        List<GraphNode> nodes = new();
        List<int[]> coords = new();

        // Populuate dictNodes
        Dictionary<OctreeNode, HashSet<GraphNode>> dictNodes = new();
        foreach (OctreeNode o in leaves)
        {
            if (!o.containsCollision)
            {
                for (int i = 0; i < 8; i++)
                {
                    int[] index = o.cornerIndex(i, octree);
                    long key = GetNodeKey(index, octree.MaxDivisionLevel); // just the hash wtf

                    if (!dict.ContainsKey(key))
                    {
                        GraphNode node = GetFromOrCreateAndAddNodeToDict(index, dict, nodes, octree.MaxDivisionLevel, octree.Size, o);
                        coords.Add(index);

                        for (int j = 0; j < 8; j++)
                        {
                            int[] gridIndex = new int[] {
                                index[0] - 1 + cornerDir[j,0],
                                index[1] - 1 + cornerDir[j, 1],
                                index[2] - 1 + cornerDir[j, 2]
                            };

                            OctreeNode? voxel = octree.FindNearestLeaf(gridIndex, octree.MaxDivisionLevel);
                            if (voxel != null && !voxel.containsCollision)
                            {
                                if (!dictNodes.ContainsKey(voxel))
                                {
                                    // Why would there be multiple nodes within a voxel? interesting
                                    dictNodes.Add(voxel, new HashSet<GraphNode> { node });
                                }
                                else
                                {
                                    dictNodes[voxel].Add(node);
                                }
                            }
                        }
                    }
                }
            }
        }
        foreach (OctreeNode voxel in dictNodes.Keys) {
            List<GraphNode> enclosingNodes = new(dictNodes[voxel]); // Convert HashSet into a List

            // For every node i, iterate through every other node that's in front of it
            for (int i = 0; i < enclosingNodes.Count - 1; i++) {
                for (int j = i + 1; j < enclosingNodes.Count; j++) {
                    GraphNode n1 = enclosingNodes[i];
                    GraphNode n2 = enclosingNodes[j];
                    int[] coord1 = coords[n1.id];
                    int[] coord2 = coords[n2.id];

                    if ((coord1[0] == coord2[0] || coord1[1] == coord2[1] || coord1[2] == coord2[2]) &&
                    // Why do we care if their index is divisible by 2?
                        (coord1[0] - coord2[0]) % 2 == 0 &&
                        (coord1[1] - coord2[1]) % 2 == 0 &&
                        (coord1[2] - coord2[2]) % 2 == 0
                    )
                    {
                        int[] coordM = new int[] {
                             (coord1[0] + coord2[0]) / 2,
                             (coord1[1] + coord2[1]) / 2,
                             (coord1[2] + coord2[2]) / 2
                        };

                        // why do we skip making a node [j] in this case?
                        if (dict.ContainsKey(GetNodeKey(coordM, octree.MaxDivisionLevel)))
                        {
                            continue;
                        }
                    }

                    string arcKey = GetArcKeyS(coord1, coord2);
                    bool temp;
                    if (!arcAdded.TryGetValue(arcKey, out temp))
                    {
                        arcAdded[arcKey] = true;
                        n1.edges.Add(new GraphEdge(n1, n2));
                        n2.edges.Add(new GraphEdge(n2, n1));
                    }
                }
            }
        }
        Graph g = new Graph(nodes);
        return g;
    }

    // For cross graph
    private static string GetArcKeyS(int[] index1, int[] index2) {
        return "" + (char)index1[0] + (char)index1[1] + (char)index1[2] + (char)index2[0] + (char)index2[1] + (char)index2[2];
    }

    // CORNER GRAPH FUNCTIONS

    // Used for GetThreeNeighborDir and GetEdgeVertexDir
    private static (int[][], int) GetBaseDir(int[] edgeDir) {
        // Find the edge in the edgeDir param with edge = 0
        int zeroIndex = -1;
        for (int i = 0; i < 3; i++) {
            if (edgeDir[i] == 0) {
                zeroIndex = i;
                break;
            }
        }

        // make result equal to
        // { edgeDir }, { edgeDir }, { edgeDir}, so a 3x3 grid
        int[][] result = new int[3][];
        for (int i = 0; i < 3; i++) {
            result[i] = new int[3];
            for (int j = 0; j < 3; j++) {
                result[i][j] = edgeDir[j];
            }
        }

        return (result, zeroIndex);
    }


    private static int[][] GetThreeNeighborDir(int[] edgeDir) {
        var (result, zeroIndex) = GetBaseDir(edgeDir);

        // make first & third equal to 0
        // I guess to match edgeDir or something idk, get the output of this
        result[0][(zeroIndex + 1) % 3] = 0;
        result[2][(zeroIndex + 2) % 3] = 0;

        // Debug.Log($"ThreeEdgeDir for {edgeDir} was {result}");

        return result;
    }

    private static int[][] GetEdgeVertexDir(int[] edgeDir) {
        var (result, zeroIndex) = GetBaseDir(edgeDir);

        result[0][zeroIndex] = 1;
        result[1][zeroIndex] = -1;

        // Debug.Log($"GetEdgeVertexDir for {edgeDir} was {result}");

        return result;
    }

    private static GraphNode GetFromOrCreateAndAddNodeToDict(int[] index, Dictionary<long, GraphNode> dict, List<GraphNode> nodes, int maxLevel, int totalSize, OctreeNode currOctLeaf) {
        long key = GetNodeKey(index, maxLevel);
        GraphNode? result = null;

        int cellSize = totalSize / (1 << maxLevel); // is this the minimum cell size?

        if (!dict.TryGetValue(key, out result) && nodes != null) {
            // This calculates the position wrong
            Vector3 positionFromIndex = new( 
                index[0] * cellSize, // x 
                index[1] * cellSize, // y
                index[2] * cellSize // z
            );

            result = new GraphNode(positionFromIndex, nodes.Count);
            dict.Add(key, result);
            nodes.Add(result);
        }
        return result;
    }

    private static long GetNodeKey(int[] index, int maxLevel) {
        long rowCount = 1 << maxLevel + 1;
        return (index[0] * rowCount + index[1]) * rowCount + index[2];
    }

    private static long GetEdgeKey(int[] index1, int[] index2, int maxLevel) {
        long rowCount = 1 << (maxLevel + 1) + 1;
        return ((index1[0] + index2[0]) * rowCount + index1[1] + index2[1]) * rowCount + index1[2] + index2[2];
    }

    private static bool DoesContainCollision(int[] gridIndex, Octree octree, int maxLevel) {
        bool outsideIsBlocked = false;
        bool doublePrecision = false;

        int xIndex = gridIndex[0];
        int yIndex = gridIndex[1];
        int zIndex = gridIndex[2];
        if (doublePrecision) {
            xIndex /= 2;
            yIndex /= 2;
            zIndex /= 2;
        }
        int t = 1 << maxLevel; // This is just 2^maxLevel

        // I think this is just checking if it's the lowest level, since this is the smallest index something can have yeah?
        // Actually no I don't think so, but maybe?
        if (xIndex >= t || xIndex < 0 || yIndex >= t || yIndex < 0 || zIndex >= t || zIndex < 0) return outsideIsBlocked;

        OctreeNode current = octree.root;

        for (int l = 0; l < maxLevel; l++) {
            t >>= 1; // I think this is just dividing by 2

            if (!current.doesChildrenContainCollision) return false;
            if (current.children == null) return current.containsCollision;


            current = current.children[xIndex / t, yIndex / t, zIndex / t]; // doesn't each of these values have to be between [0, 2]?
            xIndex %= t;
            yIndex %= t;
            zIndex %= t;
        }
        return current.containsCollision;
    }

    private void OnDrawGizmosSelected() {
        if (!ShouldDisplayGraph || graph == null) return;

        graph.DrawGraph(false);
    }
}
