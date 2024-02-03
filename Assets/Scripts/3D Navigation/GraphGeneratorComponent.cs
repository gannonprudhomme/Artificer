using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

interface IGraphGenerator {
    public Graph Generate();
    public void Step();
    public void Reset();
}

public class GraphGenerator {
    private Octree octree;

    public static readonly int[,] allFaceDirs = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };

    private Dictionary<OctreeNode, GraphNode> octreeToGraphNodeDict = new();
    int currDictIndex = 0;

    public GraphNode? toProcessNext = null;
    public GraphNode? previouslyProcessed = null;
    private bool DebugLogs = false;

    public GraphGenerator(Octree octree) {
        this.octree = octree;
    }

    // Ideally this would be static
    public Graph Generate() {
        // Is there a benefit to doing this ahead of time? We'll see!
        Initialize();

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        // How can we do this progressively? Should we sort the leaves by their nodeLevel?
        // so the biggest leaves get processed first?
        // or maybe smallest leaves first?
        foreach (KeyValuePair<OctreeNode, GraphNode> keyPair in octreeToGraphNodeDict) {
            OctreeNode octLeaf = keyPair.Key;
            GraphNode currGraphNode = keyPair.Value;

            Step(octLeaf, currGraphNode);
        }

        Graph graph = new Graph(new List<GraphNode>(octreeToGraphNodeDict.Values));
        stopwatch.Stop();

        Debug.Log($"Generated graph with {graph.nodeCount} nodes and {graph.GetEdgeCount()} edges in {stopwatch.ElapsedMilliseconds} ms");

        return graph;
    }

    // What the GraphGeneratorComponent calls
    private bool resetCalledForStep = false;
    public Graph Step() {
        if(!resetCalledForStep) {
            resetCalledForStep = true;
            Initialize();
        }

        // TODO: Check currDictIndex bounds
        var (currOctLeaf, currGraphNode) = GetGraphNodeForIndex(currDictIndex);

        int previousCount = currGraphNode.edges.Count;

        Step(currOctLeaf, currGraphNode);

        Debug.Log($"Calculated {currOctLeaf.IndexToString()} and added {currGraphNode.edges.Count - previousCount} edges");

        currDictIndex++; // Onto the next!
        var (_, nextGraphNode) = GetGraphNodeForIndex(currDictIndex);
        toProcessNext = nextGraphNode;

        // Get it and set it as the next
        // Set curr as previous
        previouslyProcessed = currGraphNode;

        Graph graph = new Graph(new List<GraphNode>(octreeToGraphNodeDict.Values));
        return graph;
    }

    private void Step(OctreeNode octLeaf, GraphNode currGraphNode) {
        if (DebugLogs) Debug.Log($"Stepping for {octLeaf.IndexToString()}");

        // For each face, find the nodes that should be connected to it
        for(int i = 0; i < 6; i++) { // For all of the face directions
            // Connect this to the nearest node  of the same size or larger (same node level or smaller)
            int[] faceDir = { allFaceDirs[i, 0], allFaceDirs[i, 1], allFaceDirs[i, 2] }; // TODO: Surely this is bad since we're creating an array each time

            OctreeNode? nearestOctLeafInDirection = FindLeafInDirectionOfSameSizeOrLarger(octLeaf, faceDir);

            // Should we do the containsCollision checking in FindLeaf?
            if (nearestOctLeafInDirection == null || nearestOctLeafInDirection.containsCollision) {
                continue;
            }

            GraphNode nearestNode = octreeToGraphNodeDict[nearestOctLeafInDirection];

            if (octLeaf.nodeLevel == nearestOctLeafInDirection.nodeLevel) {
                // I think we should only add it in one direction since there will be duplicates
                // nearestNode.AddEdgeTo(currGraphNode);
                currGraphNode.AddEdgeTo(nearestNode);
            } else { // Different levels, add to both
                nearestNode.AddEdgeTo(currGraphNode);
                currGraphNode.AddEdgeTo(nearestNode);
            }
        }
    }

    public Graph Initialize() {
        octreeToGraphNodeDict = new();
        currDictIndex = 0;

        int count = 1;
        List<OctreeNode> octLeaves = octree.Leaves();
        foreach(OctreeNode octLeaf in octLeaves) {
            if (octLeaf.containsCollision) continue; // Don't make a node if this contains a collision

            GraphNode newNode = new(octLeaf.center, count++);

            if (octreeToGraphNodeDict.ContainsKey(octLeaf)) Debug.LogError("Wtf why are there duplicates");
            octreeToGraphNodeDict[octLeaf] = newNode;
        }

        var (_, next) = GetGraphNodeForIndex(0);
        toProcessNext = next;

        return new(new(octreeToGraphNodeDict.Values));
    }

    private (OctreeNode, GraphNode) GetGraphNodeForIndex(int index) {
        // This is obviously dumb to do this every time, but I'm only going to change if it's a problem
        List<OctreeNode> keys = new(octreeToGraphNodeDict.Keys);
        OctreeNode octLeaf = keys[index];
        return (octLeaf, octreeToGraphNodeDict[octLeaf]);
    }

    private OctreeNode? FindLeafInDirectionOfSameSizeOrLarger(OctreeNode currOctLeaf, int[] faceDir) {
        int[] goalIndex = { currOctLeaf.index[0] + faceDir[0], currOctLeaf.index[1] + faceDir[1], currOctLeaf.index[2] + faceDir[2] };

        int xIndex = goalIndex[0];
        int yIndex = goalIndex[1];
        int zIndex = goalIndex[2];

        int maxIndexPossible = (1 << currOctLeaf.nodeLevel) - 1;
        if (DebugLogs) Debug.Log($"\tAttempting to find nearest for {currOctLeaf.IndexToString()} with goal {threeArrToStr(goalIndex)} in dir {threeArrToStr(faceDir)} for nodeLevel {currOctLeaf.nodeLevel}");

        // 2^nodeLevel, so if nodeLevel is 4 the max index (for a node on level 4) is 16. (exclusive, so 15)
        // but if current node level is 2, we can only check to index 4 (exclusive, so 3)

        if (xIndex < 0 || yIndex < 0 || zIndex < 0 ||
            xIndex > maxIndexPossible || yIndex > maxIndexPossible || zIndex > maxIndexPossible
        ) {
            if (DebugLogs) Debug.Log($"\t\tDidn't find nearest leaf for {currOctLeaf.IndexToString()} in dir {threeArrToStr(faceDir)}, out of bounds");
            return null;
        }

        int currSize = 1 << currOctLeaf.nodeLevel; // I think this is size?

        // Starting at the root, find the leaf closest to faceDir
        // For now, lets try to get it only on the same level as the one we're looking for
        OctreeNode current = octree.root;
        for(int level = 0; level < currOctLeaf.nodeLevel; level++) { // I feel like we'd want to go the other way around but w/e
            if (current.children == null) { 
                // current is a leaf, we're done! This has got to be it (I guess?)
                if (DebugLogs) Debug.Log($"\t\tFound nearest leaf for ${currOctLeaf.IndexToString()} of {current.IndexToString()} in dir {threeArrToStr(faceDir)}");
                return current;
            }

            // Why do we do this first?
            // Go down a level and get the node which hopefully contains a node with goalIndex
            //currSize = currSize / 2; // I could divide by 2 but this might communicate better
            currSize = currSize >> 1;

            // For each coordinate position (x,y,z) we need to determine if it's a 0 or a 1 basically
            if(DebugLogs) Debug.Log($"\t\tChecking child index ({xIndex / currSize}, {yIndex / currSize}, {zIndex / currSize}) and indices ({xIndex}, {yIndex}, {zIndex}) for goal {threeArrToStr(goalIndex)} for node {current.IndexToString()} and curr level {level} with currSize {currSize}");
            current = current.children[xIndex / currSize, yIndex / currSize, zIndex / currSize];

            // offset xIndex, yIndex, zIndex accordingly i guess?
            xIndex %= currSize;
            yIndex %= currSize;
            zIndex %= currSize;

            // currSize = currSize / 2; // just going to check
        }

        // Can I just move the check in the for loop to the bottom of the loop? I think I can
        if (current.children == null) {
            if (DebugLogs) Debug.Log($"\t\tFound nearest leaf for ${currOctLeaf.IndexToString()} of {current.IndexToString()} in dir {threeArrToStr(faceDir)}");
            return current;
        }

        if (DebugLogs) Debug.Log($"\t\tDidn't find nearest leaf for {currOctLeaf.IndexToString()} in dir {threeArrToStr(faceDir)}");
        return null;
    }

    private string threeArrToStr(int[] arr) {
        return $"({arr[0]}, {arr[1]}, {arr[2]})";
    }
}


// Purely for debugging
// this functionality will just go on a entity in the future
public class GraphGeneratorComponent : MonoBehaviour {
    public Octree? Octree;

    public bool ShouldDisplayGraph = true;
    public bool ShouldDisplayEdges = true;

    public Graph? graph;

    private GraphGenerator? _generator = null;
    private GraphGenerator generator {
        get {
            _generator ??= new GraphGenerator(Octree!);

            return _generator;
        }
    }

    public void GenerateFullGraph() {
        if (Octree == null) {
            Debug.LogError("Octree was not set!");
            return;
        }

        var generator = new GraphGenerator(Octree);

        graph = generator.Generate();
    }

    public void Step() {
        graph = generator.Step();
    }

    public void ResetGenerator() {
        graph = generator.Initialize();
    }

    //public void OnDrawGizmosSelected()
    public void OnDrawGizmosSelected()
    {
        if (graph == null || !ShouldDisplayGraph) return;

        graph.DrawGraph(ShouldDisplayEdges);

        if (generator.previouslyProcessed != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(generator.previouslyProcessed.center, 5.0f);
        }

        if (generator.toProcessNext != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(generator.toProcessNext.center, 5.0f);
        }
    }
}
