using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

public class NewOctree {
    public readonly Dictionary<int4, NewOctreeNode> nodes;
    public readonly int size;
    public readonly float3 center;

    public NewOctree(
        long size, // TODO: This should be an int - size^3 needs to be a long, but size itself doesn't
        float3 center, // TODO: I'm not even sure if we need this. Also might want to make it a Vector3
        Dictionary<int4, NewOctreeNode> nodes
    ) {
        this.size = (int) size; // it's not a long anyways, I need to fix this and make it back (it's size^3 that needs to be a long)
        this.center = center;
        this.nodes = nodes;
    }
    
    // TODO: Ideally I wouldn't have to do this
    public void UpdateDictionaryWithNodes(List<NewOctreeNode> newNodes) {
        foreach(NewOctreeNode node in newNodes) {
            if (!nodes.ContainsKey(node.dictionaryKey)) {
                Debug.LogError("Node isn't in the dictionary!");
                continue;
            }

            // Override it with the new node
            nodes[node.dictionaryKey] = node;
        }
    }

    public List<NewOctreeNode> GetAllNodes() {
        // List<NewOctreeNode> ret = new(nodes.Count);
        return nodes.Values.ToList(); // Do I even want to do ToList?
    }
}

