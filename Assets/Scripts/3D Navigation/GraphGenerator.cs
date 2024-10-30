using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Helper class for generating a Graph from an Octree
public static class GraphGenerator {
    // TODO: Convert to 1D array?
    private static readonly int[,] allFaceDirs = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
    private static readonly int[,] allDiagonalDirs = { { 0, 1, 1 }, { 0, 1, -1 }, { 0, -1, 1 }, { 0, -1, -1 }, { 1, 0, 1 }, { -1, 0, 1 }, { 1, 0, -1 }, { -1, 0, -1 }, { 1, 1, 0 }, { 1, -1, 0 }, { -1, 1, 0 }, { -1, -1, 0 } };

    public static Graph GenerateGraph(Octree octree, bool shouldBuildDiagonals = true) {
        // Also creates all of the GraphNodes we need
        Dictionary<OctreeNode, GraphNode> octreeNodeToGraphNodeDict = GetOctreeNodeToGraphNodeDict(octree);

        foreach(KeyValuePair<OctreeNode, GraphNode> keyPair in octreeNodeToGraphNodeDict) {
            var (octLeaf, currGraphNode) = keyPair;

            // For each face of the octLeaf, find all the nodes that should be connected to it
            for(int i = 0; i < 6; i++) { // 6 = num faces
                int[] faceDir = { allFaceDirs[i, 0], allFaceDirs[i, 1], allFaceDirs[i, 2] };
                
                FindAndConnectNearestNodeInDirection(octLeaf, currGraphNode, faceDir, octree, octreeNodeToGraphNodeDict);
            }

            if (!shouldBuildDiagonals) continue;

            // Do the same thing as above, but for the corners (diagonals)
            for(int i = 0; i < 12; i++) { // 12 = num corners
                int[] diagDir = { allDiagonalDirs[i, 0], allDiagonalDirs[i, 1], allDiagonalDirs[i, 2] };

                FindAndConnectNearestNodeInDirection(octLeaf, currGraphNode, diagDir, octree, octreeNodeToGraphNodeDict);
            }
        }
        return new Graph(octreeNodeToGraphNodeDict, octree);
    }

    // Connect this node to the nearest node in dir of the same size or larger (same nodeLevel or "smaller")
    // Note that this is banking on the fact that we don't do this the other way (larger -> smaller connection), so it's "bottom-up" in a sense
    private static void FindAndConnectNearestNodeInDirection(
        OctreeNode octLeaf,
        GraphNode currGraphNode,
        int[] dir,
        Octree octree,
        Dictionary<OctreeNode, GraphNode> octreeNodeToGraphNodeDict
    ) {
        OctreeNode? nearestOctLeafInDirection = FindLeafInDirectionOfSameSizeOrLarger(octLeaf, dir, octree);

        if (nearestOctLeafInDirection == null || nearestOctLeafInDirection.containsCollision || !nearestOctLeafInDirection.isInBounds) {
            return;
        }

        GraphNode nearestGraphNode = octreeNodeToGraphNodeDict[nearestOctLeafInDirection];

        if (octLeaf.nodeLevel == nearestOctLeafInDirection.nodeLevel) {
            // If they're the same level only do it in one direction (curr -> nearest)
            // since when we iterate over nearest we'll do it in this direction
            // (though w/ the edge dictionary this doesn't actually matter)
            currGraphNode.AddEdgeTo(nearestGraphNode);
        } else {
            // Nodes are of different levels (only smaller size -> larger size actually), so add in both directions
            // since we won't do it in the opposite direction when we Connect for the nearest found node
            // (because we only find leaves of the same size or larger, not smaller)
            currGraphNode.AddEdgeTo(nearestGraphNode);
            nearestGraphNode.AddEdgeTo(currGraphNode);
        }
    }

    // Find all of the leaves in the Octree that don't contain a collision and are in bounds
    // create an according GraphNode for it, then puts them as the key & value respectively into the returned dictionary.
    private static Dictionary<OctreeNode, GraphNode> GetOctreeNodeToGraphNodeDict(Octree octree) {
        Dictionary<OctreeNode, GraphNode> ret = new();

        int count = 0;
        List<OctreeNode> octLeaves = octree.GetAllNodes(onlyLeaves: true);
        foreach(OctreeNode octLeaf in octLeaves) {
            // We don't want to make a graph node if it contains a collision or is out of bounds
            if (octLeaf.containsCollision || !octLeaf.isInBounds) continue;

            GraphNode newNode = new(octLeaf.center);
            count++;

            if (ret.ContainsKey(octLeaf)) Debug.LogError("There are duplicates but there shouldn't be!");
            ret[octLeaf] = newNode;
        }

        if (count == 0) {
            Debug.LogError("GraphGenerator: Input Octree didn't have any leaves in-bounds or without collisions!");
        }

        return ret;
    }

    private static OctreeNode? FindLeafInDirectionOfSameSizeOrLarger(OctreeNode currOctLeaf, int[] dir, Octree octree) {
        int[] goalIndex = { currOctLeaf.index[0] + dir[0], currOctLeaf.index[1] + dir[1], currOctLeaf.index[2] + dir[2] };

        int xIndex = goalIndex[0], yIndex = goalIndex[1], zIndex = goalIndex[2];

        // Index or something is 2^nodeLevel,
        // so if the nodeLevel is 4 the max index (for a node on level 4) is 16. (exclusive, so 15, hence the ' - 1')
        // but if current node level is 2, we can only check to index 4 (exclusive, so 3)
        int maxIndexPossible = (1 << currOctLeaf.nodeLevel) - 1;

        // Using the above, ensure the goalIndex is within range
        if (xIndex < 0 || xIndex > maxIndexPossible ||
            yIndex < 0 || yIndex > maxIndexPossible ||
            zIndex < 0 || zIndex > maxIndexPossible
        ) {
            // Out of bounds
            return null;
        }

        // This is more like currRelativeSize
        // since the actual size is (octree.Size / (1 << nodeLevel))
        int currSize = 1 << currOctLeaf.nodeLevel; // 2^nodeLevel

        // Starting at the root, find the leaf closest to dir
        OctreeNode current = octree.root!;
        for(int level = 0; level < currOctLeaf.nodeLevel; level++) {
            if (current.IsLeaf) { // If we reached a leaf then we got it! Return
                return current;
            }

            // Go down a level and get the node which should hopefully contains a node (leaf) with goalIndex
            currSize = currSize >> 1; // Divide by 2 lol

            // For each coordinate poxition (x, y, z), we need to determine if it's a 0 or 1 basically
            current = current.children![OctreeNode.Get1DIndex(xIndex / currSize, yIndex / currSize, zIndex / currSize)];

            // Offset xIndex, yIndex, and zIndex accordingly...?
            xIndex %= currSize;
            yIndex %= currSize;
            zIndex %= currSize;
        }

        // I think I could move this check in the for-loop to the bottom of it but eh w/e this is fine
        if (current.IsLeaf) {
            return current;
        }

        return null;
    }

    /** Octree "Graph" generation **/

    // Basically the same thing as GenerateGraph
    public static void PopulateOctreeNeighbors(Octree octree, bool shouldBuildDiagonals = true) {
        List<OctreeNode> leaves = octree.GetAllNodes(onlyLeaves: true);

        foreach(OctreeNode leaf in leaves) {
            if (!leaf.isInBounds) continue; // Skip out of bounds leaves (but we're fine w/ leaves w/ collisions)

            // For each face of the octLeaf, find all the nodes that should be connected to it
            for (int i = 0; i < 6; i ++) { // 6 = num faces
                int[] faceDir = { allFaceDirs[i, 0], allFaceDirs[i, 1], allFaceDirs[i, 2] };

                FindAndConnectNearestOctreeNodeInDirection(leaf, faceDir, octree);
            }

            if (!shouldBuildDiagonals) continue;

            // Do the same thing as above, but for the corners (diagonals)
            for(int i = 0; i < 12; i++) { // 12 = num corners
                int[] diagDir = { allDiagonalDirs[i, 0], allDiagonalDirs[i, 1], allDiagonalDirs[i, 2] };

                FindAndConnectNearestOctreeNodeInDirection(leaf, diagDir, octree);
            }
        }
    }
    
    private static void FindAndConnectNearestOctreeNodeInDirection(
        OctreeNode leaf,
        int[] dir,
        Octree octree
    ) {
        OctreeNode? nearestOctLeafInDirection = FindLeafInDirectionOfSameSizeOrLarger(leaf, dir, octree);

        // We only want to draw edges to "valid" nodes (but we can draw edges *from* nodes w/ collisions, just not to them)
        if (nearestOctLeafInDirection == null || !nearestOctLeafInDirection.isInBounds || nearestOctLeafInDirection.containsCollision) {
            return;
        }
        
        if (leaf.nodeLevel == nearestOctLeafInDirection.nodeLevel) {
            // If they're the same level only do it in one direction (curr -> nearest)
            // since when we iterate over nearest we'll do it in this direction
            // (though w/ the edge dictionary this doesn't actually matter)
            leaf.AddEdgeTo(nearestOctLeafInDirection);
        } else {
            // Nodes are of different levels (only smaller size -> larger size actually), so add in both directions
            // since we won't do it in the opposite direction when we Connect for the nearest found node
            // (because we only find leaves of the same size or larger, not smaller)
            leaf.AddEdgeTo(nearestOctLeafInDirection);
            nearestOctLeafInDirection.AddEdgeTo(leaf);
        }
    }
}
