using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is akin to a NavMeshSurface
//
// It shouldn't really do much when the game is being played,
// other than loading data from a file and sending it to some Graph to be used
public class Octree : MonoBehaviour { // idk if this should actually be a Monobehavior or not
    [Tooltip("How many levels we'll divide the Octree into")]
    public int MaxDivisionLevel = 3;

    public bool ShouldCalculateForChildren = true;

    [Header("Display (Debug)")]
    public bool ShouldDisplayVoxels = false;
    public bool DisplayOnlyBlocked = false;

    // Not sure what this actually means yet
    // Maybe the size of the entire thing?
    // Really hard to tell lol, lets just make it configurable for now and see how it goes
    public float Size = 1024.0f; 

    private OctreeNode root;

    // Should we make this configurable?
    // private Vector3 corner;
    // public Vector3 CornerOffset = Vector3.zero; // Rename this to actually mean somethin
    public Vector3 Corner = Vector3.zero;

    public Vector3 center = Vector3.zero;

    // public static readonly int[,] dir = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
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

    private void Awake() {
        root = new OctreeNode(0, new int[] { 0, 0, 0 }, null, this);
    }

    public void Bake() {
        float start = Time.time;

        root = new OctreeNode(0, new int[] { 0, 0, 0 }, null, this);

        if (this.TryGetComponent(out MeshFilter meshFilter)) {
            Vector3 _center = gameObject.transform.TransformPoint(meshFilter.sharedMesh.bounds.center);
            Corner = _center - (Vector3.one * Size / 2);
        } else {
            Debug.LogError("It's assumed that the Octree will have a MeshFilter attached as the 'Base level' (biggest mesh)");
        }

        // Bake for the parent
        BakeForGameObject(this.gameObject);

        Debug.Log($"Finished baking in {Time.time - start} sec, counting voxels!");

        // Calculate how many voxels there are and display it somewhere. Idk where
        start = Time.time;
        int voxelCount = root.CountVoxels();
        Debug.Log($"Generated {voxelCount} voxels (leaves?), counted in {Time.time - start} seconds");

    }

    private void BakeForGameObject(GameObject currGameObject) {
        if (currGameObject.TryGetComponent(out MeshFilter meshFilter)) {
            BakeForMesh(meshFilter.sharedMesh);
        }

        // Now run it on the children (recursively)
        if (ShouldCalculateForChildren) {
            for(int i = 0; i < gameObject.transform.childCount; i++) {
                GameObject childObj = gameObject.transform.GetChild(i).gameObject;
                if (childObj.activeInHierarchy) {
                    BakeForGameObject(childObj);
                }
            }
        }
    }

    public void Bake1() {
        // Not sure when we should actually set this, but this should work
        root = new OctreeNode(0, new int[] { 0, 0, 0 }, null, this);

        // Read all of the meshes on this component & below it
        List<Mesh> meshes = GetAllMeshes(this.gameObject);
        Debug.Log($"Got {meshes.Count} mesh(es)!");

        // Set the center based on the "base" game object
        // There's gotta be a better way to do this lol
        if (meshes.Count > 0) {
        }

        foreach(var mesh in meshes) {
            // Get all the triangles?
            BakeForMesh(mesh);
        }

        Debug.Log("Done baking, counting voxels!");

        // Calculate how many voxels there are and display it somewhere. Idk where
        int voxelCount = root.CountVoxels();
        Debug.Log($"Generated {voxelCount} voxels (leaves?)");
    }

    private void BakeForMesh(Mesh mesh) {
        int[] triangles = mesh.triangles;
        Vector3[] vertsLocalSpace = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] vertsWorldSpace = new Vector3[vertsLocalSpace.Length];

        for(int i = 0; i < vertsLocalSpace.Length; i++) {
            vertsWorldSpace[i] = gameObject.transform.TransformPoint(vertsLocalSpace[i]) + (gameObject.transform.TransformDirection(normals[i]) * 0);
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

    // NOTE: This assume only things that are considered in the level are on the GameObject that Octree is on
    // This needs to be recursive
    private static List<Mesh> GetAllMeshes(GameObject currGameObject) {
        List<Mesh> meshes = new();

        if (currGameObject.TryGetComponent(out MeshFilter thisMesh)) {
            // Debug.Log("Got mesh Mesh!");
            // Might need to instantiate the Mesh? idk
            meshes.Add(thisMesh.sharedMesh); // Do I need to do sharedMesh?

            // Need to MergeOverlappingPoints on the mesh? Then Recalculate normals?
        }

        // Get it from the children
        for(int i = 0; i < currGameObject.transform.childCount; i++) {
            GameObject child = currGameObject.transform.GetChild(i).gameObject;
            if (child.activeInHierarchy) {
                meshes.AddRange(GetAllMeshes(child));
            }
        }

        return meshes;
    }

    private void OnDrawGizmosSelected() {
        if (root == null || !ShouldDisplayVoxels) {
            // Debug.LogError("Why is root null");
            return;
        }

        root.DisplayVoxels();
    }
}
