using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

// TODO: Ensure IJobParallelFor is actually better - I'm not convinced the downsides
// f ParallelWriter - having to pre-allocate memory - is the best idea (because # of neighbors is uncalculatable ahead of time)
// I wouldn't be surprised if generating this parallelized would be faster than loading & deserializing the edges
[BurstCompile]
public struct GraphGenerationJob: IJobParallelFor {
    private static readonly int3[] allFaceDirs = { new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1) };
    private static readonly int3[] allDiagonalDirs = { new(0, 1, 1), new(0, 1, -1), new(0, -1, 1), new(0, -1, -1), new(1, 0, 1), new(-1, 0, 1), new(1, 0, -1), new(-1, 0, -1), new(1, 1, 0), new(1, -1, 0), new(-1, 1, 0), new(-1, -1, 0) };

    // We need this to do lookups so we can see if a node 1) exists and 2) is valid
    [ReadOnly]
    public NativeHashMap<int4, NewOctreeNode> nodes;

    // Should this be the indices or a copy of the nodes? Idk which would be faster
    // honestly wouldn't be surprised if it was compying
    [ReadOnly]
    public NativeArray<int4> allValidLeafKeys;

    [WriteOnly]
    public NativeParallelMultiHashMap<int4, int4>.ParallelWriter edges;

    public void Execute(int index) {
        int4 currentLeafKey = allValidLeafKeys[index];

        for (int face = 0; face < 6; face++) { // 6 = num faces
            FindAndConnectNearestNodeInDirection(currentLeafKey, allFaceDirs[face]);
        }

        for (int diagonal = 0; diagonal < 12; diagonal++) { // 12 = num corners (diagonals)
            FindAndConnectNearestNodeInDirection(currentLeafKey, allDiagonalDirs[diagonal]);
        }
    }
 
    // Connect this node to the nearest node in dir of the same size or larger (same nodeLevel or "smaller")
    // Note that this is banking on the fact that we don't do this the other way (larger -> smaller connection), so it's "bottom-up" in a sense
    private void FindAndConnectNearestNodeInDirection(
        int4 leafDictionaryKey,
        int3 dir
    ) {

        int3 leafIndex = new(leafDictionaryKey.x, leafDictionaryKey.y, leafDictionaryKey.z); // index is the first 3 digits of the key
        int leafNodeLevel = leafDictionaryKey.w;

        NewOctreeNode? nearestLeafInDirection = FindLeafInDirectionOfSameSizeOrLarger(leafIndex, leafNodeLevel, dir);

        if (nearestLeafInDirection == null || nearestLeafInDirection.Value.containsCollision || !nearestLeafInDirection.Value.inBounds) {
            return;
        }

        if (leafNodeLevel == nearestLeafInDirection.Value.nodeLevel) {
            // If they're the same level only do it in one direction (curr -> nearest)
            // since when we iterate over nearest we'll get the same result in the other way (nearest -> curr)
            // and we don't have to have to deal w/ duplicates
            edges.Add(key: leafDictionaryKey, item: nearestLeafInDirection.Value.dictionaryKey);
        } else {
            // Nodes are of different levels (only smaller size -> larger size actually), so add in both directions
            // since we won't do it in the opposite direction when we Connect for the nearest found node
            // (because we only find leaves of the same size or larger, not smaller)

            // This is like the main reason why I can't IJobParallelFor this bitch
            // which is incredibly unfortunate
            edges.Add(key: leafDictionaryKey, item: nearestLeafInDirection.Value.dictionaryKey);
            edges.Add(key: nearestLeafInDirection.Value.dictionaryKey, item: leafDictionaryKey);
        }
    }

    // Attempts to find a node in the given direction of the same size or larger (same nodeLevel or smaller)
    //
    // Note this doesn't find a "valid" leaf - it simply finds a leaf in the given direction that is the same size or larger
    // Validity checks need to be called elswhere
    private NewOctreeNode? FindLeafInDirectionOfSameSizeOrLarger(
        int3 leafIndex,
        int leafNodeLevel,
        int3 direction
    ) {
        int3 goalIndex = leafIndex + direction;

        // Index or something is 2^nodeLevel,
        // so if the nodeLevel is 4 the max index (for a node on level 4) is 16. (exclusive, so 15, hence the `- 1`)
        // but if current node level is 2, we can only check to index 4 (exclusive, so 3)
        int maxIndexPossible = (1 << leafNodeLevel) - 1; // 2^nodeLevel - 1

        // Using the above, ensure the goalIndex is within range
        if (goalIndex.x < 0 || goalIndex.x > maxIndexPossible ||
            goalIndex.y < 0 || goalIndex.y > maxIndexPossible ||
            goalIndex.z < 0 || goalIndex.z > maxIndexPossible
        ) {
            // Out of bounds
            return null;
        }

        // Starting at our current node level, attempt to find the node in the direction of the same size or larger (aka same level or smaller)
        // If we can't find it, go up a level (aka find what would be its parent), then continue
        //
        // The key for finding a larger node in the same direction is the currRelativeSize - more about this below.  
        for (int level = leafNodeLevel; level >= 0; level--) {
            int4 key = new(goalIndex, level);

            if (nodes.ContainsKey(key)) {
                return nodes[key];
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
}
