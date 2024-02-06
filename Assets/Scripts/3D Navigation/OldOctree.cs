using System;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Video;
// using OdinSerializer;

#nullable enable

// This is akin to a NavMeshSurface
//
// It shouldn't really do much when the game is being played,
// other than loading data from a file and sending it to some Graph to be used
//
// This is *heavily* based off of: https://github.com/supercontact/PathFindingEnhanced
public class OldOctree : MonoBehaviour { // idk if this should actually be a Monobehavior or not
    [Tooltip("How many levels we'll divide the Octree into")]
    public int MaxDivisionLevel = 3;

    public bool CalculateForChildren = true;

    public bool MarkInBounds = true;
    public BoxCollider? LevelBounds;

    [Header("Display (Debug)")]
    public bool DisplayVoxels = false;
    public bool DisplayOnlyBlocked = false;
    public bool DisplayNonLeaves = false;
    public bool DisplayText = false;

    // We really need this to be a factor of 2 (I think?) Otherwise it gets really weird and the nodes aren't even
    // TODO: Can I dynamically calculate this? E.g. get the smallest size (that's a power of 2) that fits all of the meshes
    public int Size = 1024; 

    public OldOctreeNode root;

    // Should we make this configurable?
    // private Vector3 corner;
    // public Vector3 CornerOffset = Vector3.zero; // Rename this to actually mean somethin
    public Vector3 Corner = Vector3.zero;

    public Vector3 center = Vector3.zero;

    public static readonly int[,] cornerDir = { // Interesting that there aren't any negatives in here
        { 0, 0, 0 },
        { 1, 0, 0 },
        { 1, 1, 0 },
        { 0, 1, 0 },
        { 0, 0, 1 },
        { 1, 0, 1 },
        { 1, 1, 1 },
        { 0, 1, 1 }
    };

    public static readonly string fileName = "./octree.bin";

    private void Awake() {
        root = new OldOctreeNode(0, new int[] { 0, 0, 0 }, null, this);
    }

    public void Save() {
        Debug.Log($"Saving to {fileName}");
        using (var stream = File.Open(fileName, FileMode.Create)) {
            using (var writer = new BinaryWriter(stream))
            {
                OctreeSerializer.Serialize(this, writer);
            }
        }
    }

    public void Load() {
        Debug.Log($"Loading from {fileName}");
        using (var stream = File.Open(fileName, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream))
            {
                OctreeSerializer.Deserialize(this, reader);
            }
        }
    }

    public List<OldOctreeNode> Leaves() {
        return root.GetLeaves();
    }

    public List<OldOctreeNode> GetAllNodesAndSetParentMap(Dictionary<OldOctreeNode, OldOctreeNode> childToParentMap) {
        return GetAllNodesAndSetParentMap(root, null, childToParentMap);
    }

    private static List<OldOctreeNode> GetAllNodesAndSetParentMap(
        OldOctreeNode curr,
        OldOctreeNode parent,
        Dictionary<OldOctreeNode, OldOctreeNode> childToParentMap
    ) {
        if (parent != null) {
            childToParentMap[curr] = parent;
        }

        List<OldOctreeNode> nodes = new();
        nodes.Add(curr);

        // Debug.Log($"Has {curr.children?.Length ?? 0} children");

        if (curr.children == null) {
            return nodes;
        }

        for(int x = 0; x < 2; x++) {
            for(int y = 0; y < 2; y++) {
                for(int z = 0; z < 2; z++) {
                    OldOctreeNode child = curr.children[x, y, z];
                    // if (child == null || child.children == null) continue;
                    if (child == null) continue;

                    nodes.AddRange(GetAllNodesAndSetParentMap(child, curr, childToParentMap));
                }
            }
        }

        return nodes;
    }

    private static List<OldOctreeNode> GetAllNodes(OldOctreeNode curr) {
        List<OldOctreeNode> nodes = new();

        if (curr.children == null) {
            return nodes;
        }

        for(int x = 0; x < 2; x++) {
            for(int y = 0; y < 2; y++) {
                for(int z = 0; z < 2; z++) {
                    OldOctreeNode child = curr.children[x, y, z];
                    if (child == null || child.children == null) continue;

                    nodes.Add(child);
                }
            }
        }

        return nodes;
    }

/*
    public void Load() {
        
    }

    public void Save() {
        // string json = JsonUtility.ToJson(root);
        JsonSerializer serializer = new JsonSerializer();
        serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

        // OctreeNode reRead = JsonUtility.FromJson<OctreeNode>(json);
        // Debug.Log(reRead);

        string docPath = Environment.CurrentDirectory; // Environment.GetFolderPath(Environment.CurrentDirectory.);

        using (StreamWriter outputFile = new(Path.Combine(docPath, "octree.json")))
        using (JsonWriter writer = new JsonTextWriter(outputFile)) {
            serializer.Serialize(writer, root);
        }
    }

    public void Load() {
        string docPath = Environment.CurrentDirectory; // Environment.GetFolderPath(Environment.CurrentDirectory.);

        JsonSerializer serializer = new();
        serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;


        using (StreamReader outputFile = new(Path.Combine(docPath, "octree.json")))
        using (JsonReader reader = new JsonTextReader(outputFile)) {
            OctreeNode? newRoot = serializer.Deserialize<OctreeNode>(reader);
            if (newRoot != null) {
                root = newRoot;
            } else
            {
                Debug.LogError("Failed to deserialize octree");
            }
        }

        /*
        using (StreamReader reader = new(Path.Combine(docPath, "octree.json"))) {
            string thing = reader.ReadToEnd();
            OctreeNode newRoot = JsonUtility.FromJson<OctreeNode>(thing);
        } 

        if (root != null) Debug.Log($"Loaded {root.GetLeaves().Count} leaves");
    }
    */


    public void Bake() {
        var stopwatch = new System.Diagnostics.Stopwatch();

        root = new OldOctreeNode(0, new int[] { 0, 0, 0 }, null, this);

        if (this.TryGetComponent(out MeshFilter meshFilter)) {
            // NOTE: If we end up modifying this (like reducing overlapping verts), DON'T use sharedMesh it'll actually modify the mesh
            Vector3 _center = gameObject.transform.TransformPoint(meshFilter.sharedMesh.bounds.center);
            Corner = _center - (Vector3.one * Size / 2);
        } else {
            Debug.LogError("It's assumed that the Octree will have a MeshFilter attached as the 'Base level' (biggest mesh)");
        }

        // Bake for the parent, and thus the children (if enabled)
        stopwatch.Start();
        BakeForGameObject(this.gameObject);
        stopwatch.Stop();

        // After we've backed, go through all of the OctreeNode (leaves) and see which ones are in bounds
        if (MarkInBounds && LevelBounds != null) {
            List<OldOctreeNode> leaves = Leaves();

            foreach(var leaf in leaves) {
                leaf.isInBounds = LevelBounds.bounds.Contains(leaf.center);
            }
        }

        Debug.Log($"Generated octree with {root.CountVoxels()} leaves in {stopwatch.ElapsedMilliseconds} ms");
    }

    private void BakeForGameObject(GameObject currGameObject) {
        if (currGameObject.TryGetComponent(out MeshFilter meshFilter)) {
            // NOTE: If we end up modifying the mesh, DON'T use sharedMesh it'll actually modify the mesh
            VoxelizeForMesh(meshFilter.sharedMesh, currGameObject);
        }

        /*
        if (currGameObject.TryGetComponent(out SkinnedMeshRenderer meshRenderer)) {
            VoxelizeForMesh(meshRenderer.sharedMesh, currGameObject);
        }
        */

        // Now run it on the children (recursively)
        if (CalculateForChildren) {
            for(int i = 0; i < currGameObject.transform.childCount; i++) {
                GameObject childObj = currGameObject.transform.GetChild(i).gameObject;
                if (childObj.activeInHierarchy) {
                    BakeForGameObject(childObj);
                }

            }
        }
    }

    private void VoxelizeForMesh(Mesh mesh, GameObject currGameObject) {
        int[] triangles = mesh.triangles;
        Vector3[] vertsLocalSpace = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] vertsWorldSpace = new Vector3[vertsLocalSpace.Length];

        for(int i = 0; i < vertsLocalSpace.Length; i++) {
            vertsWorldSpace[i] = currGameObject.transform.TransformPoint(vertsLocalSpace[i]) + (currGameObject.transform.TransformDirection(normals[i]) * 0);
        }

        for(int i = 0; i < triangles.Length / 3; i++) {
            Vector3 point1 = vertsWorldSpace[triangles[3 * i]];
            Vector3 point2 = vertsWorldSpace[triangles[3 * i + 1]];
            Vector3 point3 = vertsWorldSpace[triangles[3 * i + 2]];

            // We should probably assume the first mesh in the list will be the center
            // though I don't actually think we can assume that
            // Vector3 centerOfMesh = mesh.bounds.center;
            // center = mesh.bounds.center + transform.position;

            root.DivideTriangleUntilLevel(point1, point2, point3, MaxDivisionLevel);
        }
    }

    // Remove this?
    public OldOctreeNode? FindNearestLeaf(int[] gridIndex, int level) {
        int xIndex = gridIndex[0];
        int yIndex = gridIndex[1];
        int zIndex = gridIndex[2];

        int t = 1 << level; // again why do we do this

        // Check bounds i guess?
        if (
            xIndex >= t || xIndex < 0 ||
            yIndex >= t || yIndex < 0 ||
            zIndex >= t || zIndex < 0
        ) {
            return null;
        }

        OldOctreeNode current = root;
        for (int l = 0; l < level; l++) {
            if (current.children == null) {
                return current; // Found a leaf
            }

            t = t >> 1; // is this just dividing by 2 or am I dumb

            // What if we miss? How do we know something is going to be here? It's not uniform
            current = current.children[xIndex / t, yIndex / t, zIndex / t]; // WHAT

            xIndex %= t;
            yIndex %= t;
            zIndex %= t;
        }

        return null;
    }

    private int[] WorldPositionToIndex(Vector3 position) {
        // We need to like normalize the position to the octree

        return new int[] { 0 };
    }

    //private void OnDrawGizmosSelected() {
    private void OnDrawGizmos() {
        if (root == null || !(DisplayVoxels || DisplayText || DisplayNonLeaves)) {
            return;
        }

        List<OldOctreeNode> allNodes = root.GetAllNodes();
        List<OldOctreeNode> allLeaves = allNodes.FindAll(thing => (thing.children == null));
        List<OldOctreeNode> notLeaves = allNodes.FindAll(thing => (thing.children != null));
        List<OldOctreeNode> collisionLeaves = allLeaves.FindAll(thing => thing.containsCollision);
        List<OldOctreeNode> noCollisionLeaves = allLeaves.FindAll(thing => !thing.containsCollision);

        if (allLeaves.Count <= 1) return; // Ignore the beginning when only root is set


        if (!DisplayOnlyBlocked) {
            Gizmos.color = Color.red;
            foreach (var noCollisionLeaf in noCollisionLeaves) {
                noCollisionLeaf.DrawGizmos(DisplayVoxels, DisplayText, Color.white);
            }
        }


        Gizmos.color = Color.green;
        foreach(var collisionLeaf in collisionLeaves) {
            collisionLeaf.DrawGizmos(DisplayVoxels, DisplayText, Color.white);
        }


        if (DisplayNonLeaves) {
            Gizmos.color = Color.yellow;
            foreach(var node in notLeaves) {
                node.DrawGizmos(DisplayVoxels, DisplayText, Color.yellow);
            }
        }
    }
}
