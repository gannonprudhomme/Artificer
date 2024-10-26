using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class NewGraphGenerator {
    private static readonly int3[] allFaceDirs = { new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1) };
    private static readonly int3[] allDiagonalDirs = { new(0, 1, 1), new(0, 1, -1), new(0, -1, 1), new(0, -1, -1), new(1, 0, 1), new(-1, 0, 1), new(1, 0, -1), new(-1, 0, -1), new(1, 1, 0), new(1, -1, 0), new(-1, 1, 0), new(-1, -1, 0) };

    public static Dictionary<int4, List<int4>> GenerateNeighbors(NewOctree octree) {
        Dictionary<int4, List<int4>> ret = new();

        List<NewOctreeNode> leaves = octree.GetAllNodes().FindAll(node => node.isLeaf && !node.containsCollision && node.inBounds);

        foreach(NewOctreeNode leaf in leaves) {
            if (leaf.index.x == 5 && leaf.index.y == 5 && leaf.index.z == 2) {
                Debug.Log("");
            }

            // For each face of the octLeaf, find all the nodes that should be connected to it
            for(int i = 0; i < 6; i++) { // 6 = num faces
                FindAndConnectNearestNodeInDirection(leaf, allFaceDirs[i], octree, ret);
            }

            // Do the same thing as above, but for the corners (diagonals)
            for(int i = 0; i < 12; i++) { // 12 = num corners
                FindAndConnectNearestNodeInDirection(leaf, allDiagonalDirs[i], octree, ret);
            }
        }

        return ret;
    }
 
    // Connect this node to the nearest node in dir of the same size or larger (same nodeLevel or "smaller")
    // Note that this is banking on the fact that we don't do this the other way (larger -> smaller connection), so it's "bottom-up" in a sense
    private static void FindAndConnectNearestNodeInDirection(
        NewOctreeNode leaf,
        int3 dir,
        NewOctree octree,
        Dictionary<int4, List<int4>> edges
    ) {
        NewOctreeNode? nearestLeafInDirection = FindLeafInDirectionOfSameSizeOrLarger(leaf, dir, octree);

        if (nearestLeafInDirection == null || nearestLeafInDirection.Value.containsCollision || !nearestLeafInDirection.Value.inBounds) {
            return;
        }

        if (leaf.nodeLevel == nearestLeafInDirection.Value.nodeLevel) {
            // If they're the same level only do it in one direction (curr -> nearest)
            // since when we iterate over nearest we'll do it in this direction
            // (though w/ the edge dictionary this doesn't actually matter)
            AddEdge(from: leaf, to: nearestLeafInDirection.Value, edges);
        } else {
            // Nodes are of different levels (only smaller size -> larger size actually), so add in both directions
            // since we won't do it in the opposite direction when we Connect for the nearest found node
            // (because we only find leaves of the same size or larger, not smaller)
            AddEdge(from: leaf, to: nearestLeafInDirection.Value, edges);
            AddEdge(from: nearestLeafInDirection.Value, to: leaf, edges);
        }
    }

    // Attempts to find a node in the given direction of the same size or larger (same nodeLevel or smaller)
    //
    // Note this doesn't find a "valid" leaf - it simply finds a leaf in the given direction that is the same size or larger
    // Validity checks need to be called elswhere
    private static NewOctreeNode? FindLeafInDirectionOfSameSizeOrLarger(
        NewOctreeNode leaf,
        int3 direction,
        NewOctree octree
    ) {
        int3 goalIndex = leaf.index + direction;

        // Index or something is 2^nodeLevel,
        // so if the nodeLevel is 4 the max index (for a node on level 4) is 16. (exclusive, so 15, hence the `- 1`)
        // but if current node level is 2, we can only check to index 4 (exclusive, so 3)
        int maxIndexPossible = (1 << leaf.nodeLevel) - 1; // 2^nodeLevel - 1

        // Using the above, ensure the goalIndex is within range
        if (goalIndex.x < 0 || goalIndex.x > maxIndexPossible ||
            goalIndex.y < 0 || goalIndex.y > maxIndexPossible ||
            goalIndex.z < 0 || goalIndex.z > maxIndexPossible
        ) {
            // Out of bounds
            return null;
        }

        // Starting at our current node level, attempt to find the node in the direction of the same size or larger (aka same level or smaller)
        //
        // The key for finding a larger node in the same direction is the currRelativeSize - more about this below.  
        for (int level = leaf.nodeLevel; level >= 0; level--) {
            int4 key = new(goalIndex, level);

            if (octree.nodes.ContainsKey(key)) {
                return octree.nodes[key];
            } else {
                // The node that we were looking for doesn't exist, i.e. it wasn't subdivided.
                // So we have to get the theoretical child's parent, i.e. go up a level
                //
                // Because, for every child index the equation is (parent.index * 2) + offset (0 or 1)
                // We need to 1) shave off the offset and 2) divide by 2 to get the parent index
                // Because integers don't have decimals, dividing by 2 makes that (0 or 1) offset disappear
                // so that's all we need to do!
                goalIndex = goalIndex >> 1; // divide by 2
            }
        }

        return null;
    }

    private static void AddEdge(NewOctreeNode from, NewOctreeNode to, Dictionary<int4, List<int4>> edges) {
        // Honestly might be better if we just did this "manually"
        List<int4> nodeEdges = edges.GetValueOrDefault(from.dictionaryKey, new List<int4>());
        nodeEdges.Add(to.dictionaryKey);
        edges[from.dictionaryKey] = nodeEdges;
    }
}
