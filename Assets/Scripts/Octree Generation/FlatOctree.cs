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
        int size,
        float3 center,
        Dictionary<int4, FlatOctreeNode> nodes
    ) {
        this.size = size;
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

