using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

#nullable enable

// Helper class for writing and reading an Octree to a file.
//
// It does this by doing a post-order traversal of sorts.
// After writing the tree data, it starts from the root, writes that to data,
// then runs it on each child recursively, always writing the current node before its children
//
// We know whether a given node has a child as we write the 
//
// We assign each node an index (where root has 0) based on the order Octree.GetAllNodes() returns
// then when we serialize the nodes we also serialize their parent's index
// so when we deserlialize we can re-construct each OctreeNode.children correctly (i.e. figure out which parent it should connect to)
//
// Performance optimizations attempted:
// 1. Only saving leaves, then generating their parents upon load/deserialization
//    a. This was unfortunately slower than just saving & deserializing all nodes
public class OctreeSerializer {
    public static void Save(NavOctreeSpace space) {
        string filename = space.GetFileName();
        Octree? octree = space.octree;
        
        if (space.octree == null) {
            Debug.LogError("No octree to save!");
            return;
        }

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        using (var stream = File.Open(filename, FileMode.Create))
        using(var writer = new BinaryWriter(stream)) {
            OctreeSerializer.Serialize(octree, writer);
        }

        stopwatch.Stop();

        int nodeCount = octree.GetAllNodes().Count;

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;
        Debug.Log($"Wrote Octree with {nodeCount} nodes to '{filename}' in {seconds:F2} sec ({ms:F0} ms)");
    }
    
    // Writes the given Octree to a file in binary format using the given BinaryWriter
    public static void Serialize(Octree octree, BinaryWriter writer) {

        // Serialize octree properties
        writer.Write(octree.MaxDivisionLevel);
        writer.Write(octree.Size);

        float[] centerArr = VectorToArray(octree.Center);
        foreach (float val in centerArr) writer.Write(val);

        // Now start serializing the nodes

        // Get the child:parent map since parents aren't stored on the nodes
        // so we can set a given's node parent index for deserializing
        Dictionary<OctreeNode, OctreeNode> childToParentMap = new();
        List<OctreeNode> allNodes = GetAllNodesAndSetParentMap(octree.root!, null, childToParentMap);

        // Serialize the node count (so we know how many nodes there are when we iterate)
        // May not be 100% neccessary but only costs 4 bytes and makes our life easier
        writer.Write(allNodes.Count);

        // Populate nodeToIndexMap (what index in allNodes is a given node)
        Dictionary<OctreeNode, int> nodeToIndexMap = new();
        for (int i = 0; i < allNodes.Count; i++) {
            OctreeNode current = allNodes[i];
            nodeToIndexMap[current] = i;
        }

        // Serialize all of the nodes one by one
        foreach (OctreeNode current in allNodes) {
            OctreeNode? parent = childToParentMap!.GetValueOrDefault(current, null);
            int parentIndex = parent != null ? nodeToIndexMap[parent] : 1; // Handling root not having a parent

            SerializeNode(current, parentIndex, writer);
        }
    }

    // NOT recursive
    private static void SerializeNode(OctreeNode node, int parentIndex, BinaryWriter writer) {
        // float[] centerArr = VectorToArray(node.center);
        // foreach (float val in centerArr) writer.Write(val);

        foreach (int val in node.index) writer.Write(val);

        writer.Write(node.nodeLevel);
        writer.Write(parentIndex);
        writer.Write(node.containsCollision);
        writer.Write(false); // TODO: This was childrenContainsCollision, remove it when I have time to generate again
        writer.Write(node.isInBounds);
    }

    // Load the generated octree from a file into memory (put in this.octree)
    public static Octree Load(string filename) {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        Octree octree;

        using (Stream stream = File.Open(filename, FileMode.Open)) {
            // Read all of the data at once, rather than reading bytes at a time
            // Led to a ~27% speed up (2.2 sec -> 1.6 sec)
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            using (var memoryStream = new MemoryStream(buffer)) {
                using (var reader = new BinaryReader(memoryStream)) {
                    octree = OctreeSerializer.Deserialize(reader);
                }
            }
        }

        stopwatch.Stop();

        int nodeCount = octree.GetAllNodes().Count;

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;
        Debug.Log($"Read Octree from '{filename}' and got {nodeCount:N0} nodes in {seconds:F2} sec ({ms:F0} ms)");

        return octree;
    } 

    public static Octree Deserialize(BinaryReader reader) {
        int maxDivisionLevel = reader.ReadInt32();
        int size = reader.ReadInt32();

        Vector3 center = new();
        for (int i = 0; i < 3; i++) center[i] = reader.ReadSingle();

        Octree octree = new(size: size, maxDivisionLevel: maxDivisionLevel, center: center);

        int nodeCount = reader.ReadInt32();

        Dictionary<OctreeNode, int> childToParentIndexMap = new(nodeCount);

        // Get the first node, which is the root
        OctreeNode root = DeserializeNode(reader, octree, childToParentIndexMap);
        octree.root = root;

        // Deserialize all of the nodes (skipping the root since we just did that)
        List<OctreeNode> allNodes = new() { root }; // Add root
        for(int i = 1; i < nodeCount; i++) {
            OctreeNode node = DeserializeNode(reader, octree, childToParentIndexMap);

            allNodes.Add(node);
        }

        // Now that we have all of the nodes deserialized
        // set the children on the parents
        foreach(var node in allNodes) {
            int parentIndex = childToParentIndexMap[node];
            if (parentIndex == -1) continue; // root doesn't have a parent

            OctreeNode parent = allNodes[parentIndex];
            parent.children ??= new OctreeNode[8]; // Init children if it's null

            // Now figure out what index we are relative to the parent
            // which we can do using both of their indices

            int x = node.index[0] - (parent.index[0] * 2);
            int y = node.index[1] - (parent.index[1] * 2);
            int z = node.index[2] - (parent.index[2] * 2);

            parent.children[OctreeNode.Get1DIndex(x, y, z)] = node;
        }

        return octree;
    }

    private static OctreeNode DeserializeNode(
        BinaryReader reader,
        Octree octree,
        Dictionary<OctreeNode, int> childToParentIndexMap
    ) {
        // TODO: We don't actually have to store this - we can just calculate it!
        // Vector3 center = new();
        // for(int i = 0; i < 3; i++) center[i] = reader.ReadSingle();

        int[] index = new int[3];
        for(int i = 0; i < 3; i++) index[i] = reader.ReadInt32();

        int nodeLevel = reader.ReadInt32();
        int parentIndex = reader.ReadInt32();
        bool containsCollision = reader.ReadBoolean();
        bool isInBounds = reader.ReadBoolean();

        OctreeNode node = new(
            nodeLevel,
            index,
            octree,
            // center,
            containsCollision,
            isInBounds
        );

        childToParentIndexMap[node] = parentIndex;

        return node;
    }
    
    /** HELPERS **/

    private static float[] VectorToArray(Vector3 vector) {
        return new float[] { vector.x, vector.y, vector.z };
    }

    // Used for serializing the nodes
    private static List<OctreeNode> GetAllNodesAndSetParentMap(
        OctreeNode curr,
        OctreeNode? parent,
        Dictionary<OctreeNode, OctreeNode> childToParentMap
    ) {
        if (parent != null) { // Check since root won't have a parent
            childToParentMap[curr] = parent;
        }

        List<OctreeNode> nodes = new();
        nodes.Add(curr);

        if (curr.children == null) return nodes; // No children, return early

        // Calculate for children of curr
        for (int x = 0; x < 2; x++) {
            for (int y = 0; y < 2; y++) {
                for (int z = 0; z < 2; z++) {
                    OctreeNode child = curr.children[OctreeNode.Get1DIndex(x, y, z)];
                    if (child == null) continue;

                    nodes.AddRange(GetAllNodesAndSetParentMap(child, curr, childToParentMap));
                }
            }
        }

        return nodes;
    }
}
