using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

// The octree that's used for generation.
//
// "Flat" as in it's a flat dictionary of nodes, not a tree of nodes (where nodes pointer to their children)
//
// Gets converted to the pointer-based Octree during/after generation
public class FlatOctree {
    public readonly Dictionary<int4, FlatOctreeNode> nodes;
    public readonly int size;
    public readonly float3 center;

    public FlatOctree(
        long size, // TODO: This should be an int - size^3 needs to be a long, but size itself doesn't
        float3 center, // TODO: I'm not even sure if we need this. Also might want to make it a Vector3
        Dictionary<int4, FlatOctreeNode> nodes
    ) {
        this.size = (int) size; // it's not a long anyways, I need to fix this and make it back (it's size^3 that needs to be a long)
        this.center = center;
        this.nodes = nodes;
    }
    
    // TODO: Ideally I wouldn't have to do this
    public void UpdateDictionaryWithNodes(List<FlatOctreeNode> newNodes) {
        foreach(FlatOctreeNode node in newNodes) {
            if (!nodes.ContainsKey(node.dictionaryKey)) {
                Debug.LogError("Node isn't in the dictionary!");
                continue;
            }

            // Override it with the new node
            nodes[node.dictionaryKey] = node;
        }
    }

    public List<FlatOctreeNode> GetAllNodes() {
        // List<NewOctreeNode> ret = new(nodes.Count);
        return nodes.Values.ToList(); // Do I even want to do ToList?
    }
}

