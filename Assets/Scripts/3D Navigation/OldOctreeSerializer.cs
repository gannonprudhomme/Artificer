using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#nullable enable

public class OctreeSerializer {
    // I'm going to have to deal with local vs world data aren't I
    // fuck
    public static void Serialize(OldOctree octree, BinaryWriter writer) {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        // Serialize Octree properties
        writer.Write(octree.MaxDivisionLevel);
        writer.Write(octree.Size);

        Vector3 corner = octree.Corner;
        float[] cornerArr = new float[3] { corner.x, corner.y, corner.z };
        for(int i = 0; i < cornerArr.Length; i++) {
            writer.Write(cornerArr[i]);
        }

        // Ok, now start serializing the nodes

        // Populate childToParentMap
        Dictionary<OldOctreeNode, OldOctreeNode> childToParentMap = new();
        List<OldOctreeNode> allNodes = octree.GetAllNodesAndSetParentMap(childToParentMap);

        // Serialize the node count
        writer.Write(allNodes.Count);

        // Populate nodeToIndexMap
        Dictionary<OldOctreeNode, int> nodeToIndexMap = new();
        for(int i = 0; i < allNodes.Count; i++) {
            OldOctreeNode current = allNodes[i];
            nodeToIndexMap[current] = i;
        }

        // Serialize all of the nodes one by one
        byte[][] allNodesData = new byte[allNodes.Count][];
        for(int i = 0; i < allNodes.Count; i++) {
            OldOctreeNode current = allNodes[i];
            OldOctreeNode? parent = childToParentMap!.GetValueOrDefault(current, null);
            int parentIndex = parent != null ? nodeToIndexMap[parent] : -1; // Handling root not having a parent

            SerializeNode(current, parentIndex, writer);
        }

        stopwatch.Stop();

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;

        Debug.Log($"Finished serializing/writing {allNodes.Count} nodes in {ms} ms");
    }

    const int numBytesForOctreeNode = sizeof(float) * 3 + // center's x, y, z
                                      sizeof(int) * (3 + 1 + 1) + // 3 indexs + 1 nodeLevel + parentIndexBytes
                                      sizeof(bool) * 2; // containsCollision + isInBounds


    private static void SerializeNode(OldOctreeNode node, int parentIndex, BinaryWriter writer) {
        float[] centerArr = new float[] { node.center.x, node.center.y, node.center.z };

        for(int i = 0; i < centerArr.Length; i++) {
            writer.Write(centerArr[i]);
        }

        for (int i = 0; i < node.index.Length; i++) {
            writer.Write(node.index[i]);
        }

        writer.Write(node.nodeLevel);
        writer.Write(parentIndex);
        writer.Write(node.containsCollision);
        writer.Write(node.isInBounds);
    }

    private static byte[] SerializeNode(OldOctreeNode node, int parentIndex) {
        byte[] bytesForThisNode = new byte[numBytesForOctreeNode];
        int position = 0;

        Array.Copy(sourceArray: node.index, sourceIndex: 0, destinationArray: bytesForThisNode, destinationIndex: position, length: 3);
        position += sizeof(int) * 3;

        // TODO: probably turn this into a generic function
        byte[] nodeLevel = BitConverter.GetBytes(node.nodeLevel);
        Array.Copy(sourceArray: nodeLevel, sourceIndex: 0, destinationArray: bytesForThisNode, destinationIndex: position, length: nodeLevel.Length);
        position += sizeof(int);

        byte[] parentIndexBytes = BitConverter.GetBytes(parentIndex);
        Array.Copy(parentIndexBytes, sourceIndex: 0, destinationArray: bytesForThisNode, destinationIndex: position, length: parentIndexBytes.Length);
        position += parentIndexBytes.Length;

        byte[] containsCollision = BitConverter.GetBytes(node.containsCollision);
        Array.Copy(containsCollision, sourceIndex: 0, destinationArray: bytesForThisNode, destinationIndex: position, length: containsCollision.Length);
        position += sizeof(bool);

        byte[] isInBounds = BitConverter.GetBytes(node.isInBounds);
        Array.Copy(isInBounds, sourceIndex: 0, destinationArray: bytesForThisNode, destinationIndex: position, length: isInBounds.Length);
        // position += sizeof(bool);

        return bytesForThisNode;
    }

    public static void Deserialize(OldOctree octree, BinaryReader reader) {
        // First, we assume root is the first node
        // Octree octree = new();
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        int maxDivisionLevel = reader.ReadInt32();
        int size = reader.ReadInt32();

        Vector3 corner = new();
        for(int i = 0; i < 3; i++) {
            corner[i] = reader.ReadSingle();
        }

        octree.MaxDivisionLevel = maxDivisionLevel;
        octree.Size = size;
        octree.Corner = corner;

        int nodeCount = reader.ReadInt32();

        // Deserialize the properties of the octree
        Dictionary<OldOctreeNode, int> childToParentMap = new();

        OldOctreeNode root = DeserializeNode(reader, octree, childToParentMap);
        octree.root = root;

        List<OldOctreeNode> nodes = new() { root }; // Add root
        for(int i = 1; i < nodeCount; i++) {
            OldOctreeNode node = DeserializeNode(reader, octree, childToParentMap);

            nodes.Add(node);
        }

        // Now iterate through the nodes and set the children (on the parent)
        foreach(var node in nodes) {
            int parentIndex = childToParentMap[node];

            if (parentIndex == -1) continue; // root doesn't have parent

            OldOctreeNode parent = nodes[parentIndex];
            if (parent.children == null) {
                parent.children = new OldOctreeNode[2, 2 ,2];
            }

            // Now we have to figure out what fucking index we are relative to the parent
            // we can get it that using both of their indices
            int parentX = parent.index[0], parentY = parent.index[1], parentZ = parent.index[2];
            int childX = node.index[0], childY = node.index[1], childZ = node.index[2];

            int x = childX - (parentX * 2);
            int y = childY - (parentY * 2);
            int z = childZ - (parentZ * 2);

            parent.children[x, y, z] = node;
        }

        stopwatch.Stop();

        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;

        int leaves = octree.Leaves().Count;
        Debug.Log($"Finished reading {nodes.Count} nodes and {leaves} leaves in {ms} ms");
        // return octree;
    }

    private static OldOctreeNode DeserializeNode(BinaryReader reader, OldOctree octree, Dictionary<OldOctreeNode, int> childToParentMap) {
        float[] centerArr = new float[3];
        for(int i = 0; i < centerArr.Length; i++) {
            centerArr[i] = reader.ReadSingle();
        }
        int[] index = new int[3];
        for(int i = 0; i < index.Length; i++) {
            index[i] = reader.ReadInt32();
        }

        int nodeLevel = reader.ReadInt32();
        int parentIndex = reader.ReadInt32();
        bool containsCollision = reader.ReadBoolean();
        bool isInBounds = reader.ReadBoolean();

        OldOctreeNode node = new OldOctreeNode(nodeLevel, index, containsCollision, isInBounds, octree);
        childToParentMap[node] = parentIndex;

        return node;
    }

    private static string threeArrToStr(int[] arr)
    {
        return $"({arr[0]}, {arr[1]}, {arr[2]})";
    }
}
