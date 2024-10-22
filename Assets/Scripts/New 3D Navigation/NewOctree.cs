using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

// TODO: Do even want this to be a class? I assume so to avoid copies
// but using classes bit us in the ass last time so idk
public class NewOctree {
    // Can we do an array instead of a hashmap?
    private Dictionary<int4, NewOctreeNode> nodes;
    /*private*/ public float3 center;

    public NewOctree(
        long size,
        float3 center, // TODO: I'm not even sure if we need this. Also might want to make it a Vector3
        Dictionary<int4, NewOctreeNode> nodes
    ) {
        this.center = center;
        this.nodes = nodes;
    }

    public NewOctreeNode[]? GetChildrenForNode(NewOctreeNode node) {
        if (!node.hasChildren) return null;

        NewOctreeNode[] children = new NewOctreeNode[8];
        // Precondition: Should only do this if it has children / isn't a leaf?

        // TODO
        return children;
    }

    // TODO: Ideally I wouldn't have to do this
    public void UpdateDictionaryWithNodes(List<NewOctreeNode> newNodes) {
        foreach(var node in newNodes) {
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

