using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable


// Component to go on the (parent-most) Level game object
// Intended to be the equivalent of NavMeshSurface
public class NavOctreeSpace : MonoBehaviour {


    [Header("Debug")]
    public bool DisplayBlocked = false;
    public bool DisplayNonLeaves = false;
    public bool DisplayIndices = false;

    private Octree? octree = null;
    
    public void GenerateOctree() {
        Bounds bounds = GetBounds();
        Debug.Log($"Got bounds of {bounds.min} {bounds.max} and center {bounds.center}");

        octree = new Octree(
            min: bounds.min,
            max: bounds.max,
            smallestActorDimension: Vector3.one * 2f,
            center: bounds.center
        );

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        octree.Generate();
        stopwatch.Stop();

        Debug.Log($"Finished generating octree in {stopwatch.ElapsedMilliseconds} ms");
    }

    // TODO: Get the bounds of the children or w/e
    // Save the generated octree to a file
    public void Save() {

    }

    // Load the generated octree from a file into memory (put in this.octree)
    public void Load() {

    }

    void Start() {
        // We don't need to do anything at runtime I don't think?
        // if anything this class will just be for during the Editor
    }

    // DO NOT modify the meshes in this - we use sharedMesh!
    /*
    private List<Mesh> GetAllMeshes(GameObject currGameObj) {
        List<Mesh> ret = new();
        if (currGameObj.TryGetComponent(out MeshFilter meshFilter)) {
            ret.Add(meshFilter.sharedMesh);
        }

        for(int i = 0; i < currGameObj.transform.childCount; i++) {
            GameObject child = currGameObj.transform.GetChild(i).gameObject;
            if (child.activeInHierarchy) {
                ret.AddRange(GetAllMeshes(child));
            }
        }

        return ret;
    }
    */
    private Bounds GetBounds() {
        // This gets renderers from this GameObject(Component), as well as it's children recursively
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        // if (renderers.Count == 0) return new Bounds
        Bounds bounds = renderers[0].bounds;
        foreach(var renderer in renderers) {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    // Retrieves the filename 
    private string GetFileName() {
        return "octree.bin";
    }
}
