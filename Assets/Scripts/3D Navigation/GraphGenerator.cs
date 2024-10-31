using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Helper class for generating a Graph from an Octree
public static class GraphGenerator {
    // TODO: Convert to 1D array?
    private static readonly int[,] allFaceDirs = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
    private static readonly int[,] allDiagonalDirs = { { 0, 1, 1 }, { 0, 1, -1 }, { 0, -1, 1 }, { 0, -1, -1 }, { 1, 0, 1 }, { -1, 0, 1 }, { 1, 0, -1 }, { -1, 0, -1 }, { 1, 1, 0 }, { 1, -1, 0 }, { -1, 1, 0 }, { -1, -1, 0 } };


    private static OctreeNode? FindLeafInDirectionOfSameSizeOrLarger(OctreeNode currOctLeaf, int[] dir, Octree octree) {
        // TODO: I really feel like using an int3 here would be better
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

        // Attempted to optimize this using a map (like I initially had this written w/ the flat-based version of the Octree)
        // but this is faster, which I suppose isn't that surprising (but it's a little surprising)
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
             // Skip out of bounds leaves (but we're fine w/ leaves w/ collisions)
             // in fact, we *want* to connect nodes with collisions (invalid nodes) -> valid nodes
             // as it assists us in finding the nearest valid node to a given position
             // we just don't want to connect valid nodes -> invalid nodes
            if (!leaf.isInBounds) continue;

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
