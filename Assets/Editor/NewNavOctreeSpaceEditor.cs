using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.EditorCoroutines.Editor;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NewNavOctreeSpace))]
public class NewNavOctreeSpaceEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        NewNavOctreeSpace navOctreeSpace = (NewNavOctreeSpace) target;

        // Display the octree's size & maxDivisionLevel if it's available in memory
        // maybe we could precalculate it and display it before we call Generate()?
        // so we know how long it's going to take

        GUI.skin.label.fontStyle = FontStyle.Bold;

        GUILayout.Space(16);
        GUILayout.Label("Information");

        GUI.skin.label.fontStyle = FontStyle.Normal;

        string isAvailable = navOctreeSpace.octree != null ? "YES" : "NO";
        GUILayout.Label($"Is Loaded\t\t    {isAvailable}");

        string size = "-";
        string maxDivisionLevel = "-";

        /*
        if (navOctreeSpace.octree != null) {
            size = $"{navOctreeSpace.octree.Size}";
            maxDivisionLevel = $"{navOctreeSpace.octree.MaxDivisionLevel}";
        }
        */

        GUILayout.Label($"Size\t\t\t    {size}");
        GUILayout.Label($"Max Division Level \t    {maxDivisionLevel}");

        GUILayout.Space(16);

        // Display buttons

        if (GUILayout.Button("Generate Octree")) {
            GameObject gameObject = navOctreeSpace.gameObject;
            Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

            NativeArray<Vector3> vertsLocalSpace = new(mesh.vertices, Allocator.Persistent);

            float4x4 transform = gameObject.transform.localToWorldMatrix; // Technically returns a Matrix4x4

            NativeArray<float3> vertsWorldSpaceOutput = new(vertsLocalSpace.Length, Allocator.Persistent);

            ConvertVertsToWorldSpaceJob convertJob = new(vertsLocalSpace, transform, vertsWorldSpaceOutput);
            JobHandle convertJobHandle = convertJob.Schedule(vertsLocalSpace.Length, 128); // Idk what batch size should be

            NativeArray<int> triangleVertices = new(mesh.triangles, Allocator.Persistent);

            // CREATE THE GENERATION JOBS

            List<NativeHashMap<int4, NewOctreeNode>> allNodeMaps = new();
            List<OctreeGenerationJob> allJobs = CreateAllGenerationJobs(
                navOctreeSpace,
                triangleVertices,
                vertsWorldSpaceOutput,
                allNodeMaps
            );

            int numTriangles = triangleVertices.Length / 3;
            NativeArray<JobHandle> allJobHandles = new(numTriangles, Allocator.Persistent);
            for(int i = 0; i < numTriangles; i++) {
                allJobHandles[i] = allJobs[i].Schedule(convertJobHandle);
            }

            JobHandle allGenerateJobsHandle = JobHandle.CombineDependencies(allJobHandles);

            Bounds bounds = navOctreeSpace.GetBounds();
            int totalOctreeSize = NewNavOctreeSpace.CalculateSize(bounds.min, bounds.max);

            var coroutine = ReportGenerationProgress(
                allGenerateJobsHandle,
                allNodeMaps,
                navOctreeSpace,
                totalOctreeSize: totalOctreeSize,
                octreeCenter: bounds.center
            );

            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        if (GUILayout.Button("Convert local to world")) {
            ConvertVertsToWorldSpaceJob job = ConvertVertsToWorldSpaceJob.CreateJob(navOctreeSpace.gameObject);

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            JobHandle handle = job.Schedule(navOctreeSpace.gameObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length, 64);

            handle.Complete();
            stopwatch.Stop();
            double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
            Debug.Log($"Converted {navOctreeSpace.gameObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length} verts in {ms} ms");
        }

        if (GUILayout.Button("Mark In-Bounds leaves")) {
            if (navOctreeSpace.octree == null) {
                Debug.LogError("No octree to mark in bounds leaves on");
                return;
            }

            List<NewOctreeNode> leaves = navOctreeSpace.octree.GetAllNodes().FindAll(node => node.isLeaf);
            NativeArray<RaycastHit> results = new(leaves.Count, Allocator.Persistent);
            JobHandle markJob = navOctreeSpace.CreateRaycastInBoundCommands(leaves, results);

            var coroutine = CheckMarkInBoundLeaves(markJob, navOctreeSpace, leaves, results);
            EditorCoroutineUtility.StartCoroutine(coroutine, this);
        }

        /*
        if (GUILayout.Button("Save")) {
            navOctreeSpace.Save();
        }

        if (GUILayout.Button("Load")) {
            navOctreeSpace.Load();
        }

        if (GUILayout.Button("Build Neighbors")) {
            navOctreeSpace.BuildNeighbors();
        }
        */
    }

    private IEnumerator ReportGenerationProgress(
        JobHandle jobHandle,
        List<NativeHashMap<int4, NewOctreeNode>> allNodeMaps,
        NewNavOctreeSpace space,
        int totalOctreeSize,
        float3 octreeCenter
    ) {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        int progressId = Progress.Start("Generate octree");

        int totalJobs = allNodeMaps.Count;
        while (!jobHandle.IsCompleted) {
            yield return null;
        }

        jobHandle.Complete();

        Dictionary<int4, NewOctreeNode> combinedNodes = CombineAllNodeMaps(allNodeMaps);

        // TODO: Probably best if we are the ones who initialized nodes in the first place

        // TODO: idk if I want a function for this or not
        NewOctree octree = new(totalOctreeSize, octreeCenter, combinedNodes);
        space.octree = octree;

        stopwatch.Stop();
        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Finish generating octree with {combinedNodes.Count} nodes in {ms} ms");

        Progress.Remove(progressId);
    }

    private IEnumerator CheckMarkInBoundLeaves(
        JobHandle jobHandle,
        NewNavOctreeSpace space,
        List<NewOctreeNode> leaves,
        NativeArray<RaycastHit> raycastHits
    ) {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        while (!jobHandle.IsCompleted) {
            yield return null;
        }
        jobHandle.Complete();


        if (raycastHits.Length != leaves.Count) {
            Debug.LogError("Raycast hits length doesn't match leaves length");
            yield break;
        }

        // Update the leaves list with the inBounds result
        for(int i = 0; i < leaves.Count; i++) {
            NewOctreeNode leafCopy = leaves[i];
            RaycastHit hit = raycastHits[i];

            bool didHit = hit.collider != null;
            if (didHit) {
                leafCopy.inBounds = true;
            } else {
                leafCopy.inBounds = false; // Don't need to do this technically
            }

            leaves[i] = leafCopy;
        }

        raycastHits.Dispose();

        // Update the octree with the new leaves
        space.octree.UpdateDictionaryWithNodes(leaves);
        stopwatch.Stop();
        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Marking in bound leaves took {ms} ms");
    }

    public List<OctreeGenerationJob> CreateAllGenerationJobs(
        NewNavOctreeSpace space,
        NativeArray<int> triangleVertices,
        NativeArray<float3> vertsWorldSpace,
        List<NativeHashMap<int4, NewOctreeNode>> allNodeMapsOutput // this is also something we "return"
    ) {
        Bounds bounds = space.GetBounds();

        int size = NewNavOctreeSpace.CalculateSize(bounds.min, bounds.max);

         // Create root node
        NewOctreeNode root = new(
            nodeLevel: 0,
            size: size,
            index: new(0, 0, 0),
            center: bounds.center
        );

        int numTriangles = triangleVertices.Length / 3;
        List<OctreeGenerationJob> jobs = new();

        for (int i = 0; i < numTriangles; i++) {
            NativeHashMap<int4, NewOctreeNode> nodes = new(0, Allocator.Persistent); // TODO: Ensure we're disposing of this
            nodes[new int4(0)] = root;

            allNodeMapsOutput.Add(nodes);

            var job = new OctreeGenerationJob(
                trianglesIndex: i,
                meshTriangles: triangleVertices,
                meshVertsWorldSpace: vertsWorldSpace,
                nodes: nodes,
                octreeCenter: bounds.center,
                totalOctreeSize: size,
                maxDivisionLevel: 8
            );

            jobs.Add(job);
        }

        return jobs;
    }

    // This is O(N*M), where N is the number of jobs (triangles),
    // and M is the number of total nodes (upper limit, assuming each job does the max number of divisions, which is practically impossible)
    private Dictionary<int4, NewOctreeNode> CombineAllNodeMaps(
        List<NativeHashMap<int4, NewOctreeNode>> allNodeMaps
    ) {
        Dictionary<int4, NewOctreeNode> combined = new();

        // For every hashmap, iterate through all of the keys and add it to the "Combined" dictionary
        // if there's a collision, then prioritize the node in the order so that:
        // 1. hasChildren == true
        // 2. Then if hasChildren is the same, pick the one where containsCollision == true
        foreach (NativeHashMap<int4, NewOctreeNode> nodesMap in allNodeMaps) {
            NativeArray<NewOctreeNode> nodes = nodesMap.GetValueArray(Allocator.Temp);

            foreach(NewOctreeNode node in nodes) {
                int4 key = node.dictionaryKey;

                if (!combined.ContainsKey(key)) {
                    combined.Add(key, node);
                } else {
                    NewOctreeNode existingNode = combined[key];

                    // TODO: Try to make this more readable bleh
                    if (node.hasChildren && !existingNode.hasChildren) {
                        combined[key] = node;
                    } else if (node.hasChildren == existingNode.hasChildren) {
                        if (node.containsCollision && !existingNode.containsCollision) {
                            combined[key] = node;
                        }
                    }
                }
            }

            nodes.Dispose();
            nodesMap.Dispose(); // I think we want to do this here? Cause we're now done w/ them
        }


        return combined;
    }

    /*
    private IEnumerator ReportVertsWorldspaceProgress(JobHandle jobHandle, ConvertVertsToWorldSpaceJob job) {
        yield return null;
    }
    */

    /*
    private Dictionary CreateOctreeFromMap(
        NativeHashMap<int4, NewOctreeNode> nodes
    ) {
        // Convert it to a managed version
        Dictionary<int4, NewOctreeNode> managedNodes = new();
        foreach (var node in nodes) {
            managedNodes.Add(node.Key, node.Value);
        }

        nodes.Dispose();

        /*
        return new NewOctree(
            size: size,
            center: octreeCenter,
            managedNodes
        );
    }
        */
}
