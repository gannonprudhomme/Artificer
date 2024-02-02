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
public class Octree : MonoBehaviour { // idk if this should actually be a Monobehavior or not
    [Tooltip("How many levels we'll divide the Octree into")]
    public int MaxDivisionLevel = 3;

    public bool ShouldCalculateForChildren = true;

    [Header("Display (Debug)")]
    public bool ShouldDisplayVoxels = false;
    public bool DisplayOnlyBlocked = false;
    public bool DisplayNonLeaves = false;
    public bool DisplayText = false;

    // Not sure what this actually means yet
    // Maybe the size of the entire thing?
    // Really hard to tell lol, lets just make it configurable for now and see how it goes
    // We really need this to be a factor of 2 (I think?) Otherwise it gets really weird and the nodes aren't even
    public int Size = 1024; 

    public OctreeNode root;

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
    // public static readonly int[,] edgeDir = { { 0, 1, 1 }, { 0, 1, -1 }, { 0, -1, 1 }, { 0, -1, -1 }, { 1, 0, 1 }, { -1, 0, 1 }, { 1, 0, -1 }, { -1, 0, -1 }, { 1, 1, 0 }, { 1, -1, 0 }, { -1, 1, 0 }, { -1, -1, 0 } };

    // Size of what exactly? The smallest cell?
    /*
    private float cellSize { 
        get { return Size / (1 << MaxDivisionLevel); }
    }
    */

    string fileName = "./octree.json";

    private void Awake() {
        root = new OctreeNode(0, new int[] { 0, 0, 0 }, null, this);
    }

    public List<OctreeNode> Leaves() {
        return root.GetLeaves();
    }

    /*
    public void Save() {
        List<OctreeNode> nodes = GetAllNodes(root);

        byte[] json = SerializationUtility.SerializeValue(nodes, DataFormat.JSON);
        string path = Path.Combine(Environment.CurrentDirectory, "octree.json");

        File.WriteAllBytes(path, json);
    }
    */

    private static List<OctreeNode> GetAllNodes(OctreeNode curr) {
        List<OctreeNode> nodes = new();

        if (curr.children == null) {
            return nodes;
        }

        for(int x = 0; x < 2; x++) {
            for(int y = 0; y < 2; y++) {
                for(int z = 0; z < 2; z++) {
                    OctreeNode child = curr.children[x, y, z];
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

        root = new OctreeNode(0, new int[] { 0, 0, 0 }, null, this);

        if (this.TryGetComponent(out MeshFilter meshFilter)) {
            // NOTE: If we end up modifying this, DON'T use sharedMesh it'll actually modify the mesh
            Vector3 _center = gameObject.transform.TransformPoint(meshFilter.sharedMesh.bounds.center);
            Corner = _center - (Vector3.one * Size / 2);
        } else {
            Debug.LogError("It's assumed that the Octree will have a MeshFilter attached as the 'Base level' (biggest mesh)");
        }

        // Bake for the parent, and thus the children (if enabled)
        stopwatch.Start();
        BakeForGameObject(this.gameObject);
        stopwatch.Stop();


        Debug.Log($"Finished baking in {(int) stopwatch.Elapsed.TotalSeconds} sec, counting voxels!");

        // Calculate how many voxels there are and display it somewhere. Idk where
        stopwatch.Reset();
        stopwatch.Start();
        int voxelCount = root.CountVoxels();
        stopwatch.Stop();
        Debug.Log($"Generated {voxelCount} voxels (leaves?), counted in {(int)stopwatch.Elapsed.TotalMilliseconds} ms");
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
        if (ShouldCalculateForChildren) {
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
    public OctreeNode? FindNearestLeaf(int[] gridIndex, int level) {
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

        OctreeNode current = root;
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
        if (root == null || !(ShouldDisplayVoxels || DisplayText || DisplayNonLeaves)) {
            return;
        }

        List<OctreeNode> allNodes = root.GetAllNodes();
        List<OctreeNode> allLeaves = allNodes.FindAll(thing => (thing.children == null));
        List<OctreeNode> notLeaves = allNodes.FindAll(thing => (thing.children != null));
        List<OctreeNode> collisionLeaves = allLeaves.FindAll(thing => thing.containsCollision);
        List<OctreeNode> noCollisionLeaves = allLeaves.FindAll(thing => !thing.containsCollision);

        if (allLeaves.Count <= 1) return; // Ignore the beginning when only root is set


        if (!DisplayOnlyBlocked) {
            Gizmos.color = Color.red;
            foreach (var noCollisionLeaf in noCollisionLeaves) {
                noCollisionLeaf.DrawGizmos(ShouldDisplayVoxels, DisplayText, Color.white);
            }
        }


        Gizmos.color = Color.green;
        foreach(var collisionLeaf in collisionLeaves) {
            collisionLeaf.DrawGizmos(ShouldDisplayVoxels, DisplayText, Color.white);
        }


        if (DisplayNonLeaves) {
            Gizmos.color = Color.yellow;
            foreach(var node in notLeaves) {
                node.DrawGizmos(ShouldDisplayVoxels, DisplayText, Color.yellow);
            }
        }
    }
}