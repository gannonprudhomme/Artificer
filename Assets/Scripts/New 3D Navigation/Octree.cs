using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class Octree  {
    // Should be calculated based on the smallest agent's size
    public int MaxDivisionLevel { get; private set; }

    // Should be calculated based on the max size of the mesh
    // Must be a power of 2 so the octree divides evenly
    // Size^3 is the volume of the Octree
    public int Size { get; private set; }
    public Vector3 Center { get; private set; }
    public Vector3 Corner {
        get {
            return Center - (Vector3.one * Size / 2);
        }
    }

    private OctreeNode? root;

    public Octree(
        Vector3 min, // Used for size calculation
        Vector3 max,  // Used for size calculation
        Vector3 smallestActorDimension, // Used for calculating MaxDivisionLevel,
        Vector3 center // The center of the Octree
    ) {
        Center = center;

        Size = CalculateSize(min, max);

        MaxDivisionLevel = CalculateMaxDivisionLevel(smallestActorDimension, Size);
        Debug.Log($"Calculated max division level: {MaxDivisionLevel} and size: {Size}");
    }

    // Aka bake
    public void Generate() {
        // Shit I should probably read that research paper
        // Cause they create the Octree differently

        // I also need to figure out how to prevent the "hollow mesh" problem, where if the mesh is big enough (relative to the octree nodes)
        // it will say it's empty space inside of the mesh
        // I could just remove all of the disjoint graphs (other than the biggest one) since it shouldn't be navigatable anyways

        // I'd also like to figure out how to better mark stuff that's out of bounds
        // as if we generate stuff below the level there's a chance the nearest graph node to the point we want to get to
        // is below the level. (we might be able to do some math to get around this, but I'd just like for it to be impossible to begin with)

        // I'm still not sure if we want to have mulitple octrees or not
        // or if we can just filter (with OctreeNode.doChildrenContainCollision) out the smallest nodes based on the agent
        // to generate the graph for an agent

        // Rein in a bit here though - this really doesn't need to be that complex/perfect for what I want to do
    }

    private static int CalculateSize(Vector3 min, Vector3 max) {
        float length = max.x - min.x;
        float height = max.y - min.y;
        float width = max.z - min.z;
        float volume = length * height * width;

        int currMinSize = 1;
        while ((currMinSize * currMinSize * currMinSize) < volume) {
            currMinSize *= 2; // Power of 2's!
        }

        Debug.Log($"With dimensions of {length}, {height}, {width} and volume {volume} got min size of {currMinSize}");
        return currMinSize;
    }

    // Determine the smallest number of divisions we need in order to have the smallest OctreeNode
    // that is still just bigger (exclusive) than the smallest actor's volume
    private static int CalculateMaxDivisionLevel(Vector3 smallestActorDimension, int octreeSize) {
        float smallestActorVolume = smallestActorDimension.x * smallestActorDimension.y * smallestActorDimension.z;

        int currMinDivisionLevel = 0;
        int currMinLevelSize = octreeSize;

        // Keep increasing the number of division (division leveL) until we have a division level that is smaller than the smallest actor's volume
        // The goal is to have the least number of divisions (smallest div level) that is still bigger than the smallest actor's volume
        // Note that we can't have an OctreeNode that is the same size or smaller than the smallest actor/enmy
        while ((currMinLevelSize * currMinLevelSize * currMinLevelSize) > smallestActorVolume) { // Keep going until the volume is smaller than actor volume
            Debug.Log($"At {currMinLevelSize * currMinLevelSize * currMinLevelSize} with level of {currMinDivisionLevel}");
            // currMinDivisionLevel *= 2;
            currMinDivisionLevel++;
            currMinLevelSize = octreeSize / (1 << currMinDivisionLevel); // size / (2^currMinDivisionLevel)
        }

        // Now that we've gotten a division level that makes the smallest node that is smaller than the smallestActorVolume
        // decrease it by 1 (increase size of node) since we know that'll be bigger than it
        currMinDivisionLevel--;
        // currMinLevelSize = octreeSize / (1 << currMinDivisionLevel); // Just for debug output

        // int volume = currMinLevelSize * currMinLevelSize * currMinLevelSize;
        // Debug.Log($"Calculated min division level of {currMinDivisionLevel} which has a volume of {volume} to encapsulate an actor volume of {smallestActorVolume}");

        return currMinDivisionLevel;
    }
}
