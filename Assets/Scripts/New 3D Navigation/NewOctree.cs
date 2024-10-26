using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

// TODO: Do even want this to be a class? I assume so to avoid copies
// but using classes bit us in the ass last time so idk
public class NewOctree {
    public readonly Dictionary<int4, NewOctreeNode> nodes;
    public readonly float3 center; // TODO: Make private?

    // TODO: name
    // TODO: This *really* should be private - just did it to get a quick debug output
    public Dictionary<int4, List<int4>> edges = new(); 

    public NewOctree(
        long size,
        float3 center, // TODO: I'm not even sure if we need this. Also might want to make it a Vector3
        Dictionary<int4, NewOctreeNode> nodes
    ) {
        this.center = center;
        this.nodes = nodes;
    }

    public NewOctreeNode[]? GetChildrenForNode(NewOctreeNode node) {
        // Precondition: Should only do this if it has children / isn't a leaf?
        if (!node.hasChildren) return null;

        NewOctreeNode[] children = new NewOctreeNode[8];
        // TODO
        return children;
    }


    // Replacement for GraphNode.edges
    public List<int4>? GetNeighborsForNode(NewOctreeNode node) {
        if (!edges.ContainsKey(node.dictionaryKey)) return null;

        // We could have a version of this which returns List<NewOctreeNode>
        // it just depends when we want to make copies

        return edges[node.dictionaryKey];
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

    public NewOctreeNode GetRoot() {
        return nodes[new int4(0)];
    }
}

