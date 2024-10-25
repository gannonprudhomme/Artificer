using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class NewOctreeGenerator {
    public static IEnumerator GenerateOctree(
        NewNavOctreeSpace navOctreeSpace,
        int maxDivisionLevel,
        int numJobs
    ) {
        Debug.Log("Starting to generate!");

        var mainStopwatch = new System.Diagnostics.Stopwatch();
        mainStopwatch.Start();

        // Create ConvertToWorldSpace jobs

        // Each index in this corresponds to a gameObject, with 0 being the root
        List<NativeArray<Vector3>> allVertsLocalSpace = new();
        List<NativeArray<float3>> allVertsWorldSpace = new();
        List<NativeArray<int>> allTriangles = new();

        JobHandle convertAllToWorldSpaceJobHandle = CreateConvertToWorldSpaceJobs(
            rootGameObject: navOctreeSpace.gameObject,
            allVertsLocalSpaceOutput: allVertsLocalSpace,
            allVertsWorldSpaceOutput: allVertsWorldSpace,
            allTrianglesOutput: allTriangles
        );

        // Create generation jobs

        Bounds bounds = navOctreeSpace.GetBounds();
        long totalSize = NewNavOctreeSpace.CalculateSize(bounds.min, bounds.max);

        NewOctreeNode root = new(
            nodeLevel: 0,
            size: totalSize,
            index: new(0, 0, 0),
            center: bounds.center
        );

        List<NativeHashMap<int4, NewOctreeNode>> allNodeMaps = new();
        List<OctreeGenerationJob> allJobs = new();

        // Create the jobs for each game object (root or children)
        int numChildren = allVertsLocalSpace.Count;
        for (int i = 0; i < numChildren; i++) {
            List<OctreeGenerationJob> jobsForGameObject = CreateGenerateJobsForGameObject(
                totalSize: totalSize,
                octreeCenter: bounds.center,
                root: root,
                maxDivisionLevel: maxDivisionLevel,
                numJobs: numJobs,
                triangleVertices: allTriangles[i],
                vertsWorldSpace: allVertsWorldSpace[i],
                allNodeMapsOutput: allNodeMaps
            );

            allJobs.AddRange(jobsForGameObject);
        }

        // Combine all of the generation jobs into one JobHandle

        NativeArray<JobHandle> allJobHandles = new(allJobs.Count, Allocator.Temp);
        for (int i = 0; i < allJobs.Count; i++) {
            allJobHandles[i] = allJobs[i].Schedule(convertAllToWorldSpaceJobHandle);
        }

        JobHandle allGenerateJobsHandle = JobHandle.CombineDependencies(allJobHandles);
        allJobHandles.Dispose();
        
        yield return WaitForJobToComplete(allGenerateJobsHandle);

        // Combine all of the node maps into one

        Dictionary<int4, NewOctreeNode> combinedManaged = CombineAllNodeMaps(allNodeMaps);

        // Create the octree!
        NewOctree octree = new(totalSize, bounds.center, combinedManaged);
        navOctreeSpace.octree = octree;

        // Mark in-bound leaves
        yield return MarkInBoundLeaves(navOctreeSpace);

        foreach(NativeArray<Vector3> verts in allVertsLocalSpace) { verts.Dispose(); }
        foreach(NativeArray<float3> verts in allVertsWorldSpace) { verts.Dispose(); }
        foreach(NativeArray<int> triangles in allTriangles) { triangles.Dispose(); }
        foreach(NativeHashMap<int4, NewOctreeNode> nodesMap in allNodeMaps) { nodesMap.Dispose();}

    }

    // Creates all of the jobs to convert the vertices from local space to world space
    //
    // This is step 1
    private static JobHandle CreateConvertToWorldSpaceJobs(
        GameObject rootGameObject,
        List<NativeArray<Vector3>> allVertsLocalSpaceOutput, // output
        List<NativeArray<float3>> allVertsWorldSpaceOutput, // output
        List<NativeArray<int>> allTrianglesOutput // output
    ) {

        List<GameObject> rootAndChildren = new();
        GetAllChildrenRecursively(rootGameObject, rootAndChildren);

        int batchSize = 128;
        NativeList<JobHandle> allJobHandles = new(rootAndChildren.Count, Allocator.Temp);
        for(int i = 0; i < rootAndChildren.Count; i++) {
            GameObject currGameObject = rootAndChildren[i];

            if (!currGameObject.TryGetComponent(out MeshFilter meshFilter)) continue;
            if (meshFilter.sharedMesh == null) continue; // Shouldn't be needed - really just for the empty grash mesh

            Mesh mesh = meshFilter.sharedMesh;
            NativeArray<int> triangleVertices = new(mesh.triangles, Allocator.Persistent);
            allTrianglesOutput.Add(triangleVertices);

            NativeArray<Vector3> vertsLocalSpace = new(mesh.vertices, Allocator.Persistent);
            allVertsLocalSpaceOutput.Add(vertsLocalSpace); // We store them so we can dispose of it later

            NativeArray<float3> rootVertsWorldSpace = new(mesh.vertices.Length, Allocator.Persistent);
            allVertsWorldSpaceOutput.Add(rootVertsWorldSpace); // Stored so we can pass to Generate jobs

            float4x4 transform = currGameObject.transform.localToWorldMatrix; // Technically returns a Matrix4x4
            ConvertVertsToWorldSpaceJob convertForRoot = new(vertsLocalSpace, transform, rootVertsWorldSpace);

            JobHandle jobHandle = convertForRoot.Schedule(vertsLocalSpace.Length, batchSize);
            allJobHandles.Add(jobHandle);
        }

        JobHandle ret = JobHandle.CombineDependencies(allJobHandles.AsArray());
        allJobHandles.Dispose();

        return ret;
    }

    // Creates multiple jobs OctreeGenerationJob's for a single game object
    //
    // This splits the vertices (triangles) of the game object's mesh into multiple jobs
    // which will later be combined in step 3
    //
    // This is step 2, and really the "meat & bones" of the generation
    private static List<OctreeGenerationJob> CreateGenerateJobsForGameObject(
        long totalSize,
        float3 octreeCenter,
        NewOctreeNode root,
        int maxDivisionLevel,
        int numJobs,
        NativeArray<int> triangleVertices,
        NativeArray<float3> vertsWorldSpace,
        List<NativeHashMap<int4, NewOctreeNode>> allNodeMapsOutput
    ) {
        List<OctreeGenerationJob> jobs = new();

        int numTriangles = triangleVertices.Length / 3;
        int numTrianglesPerBatch = numTriangles / numJobs;

        int numBatches = numJobs;
        if (numTriangles % numTrianglesPerBatch != 0) {
            numBatches++;
        }

        for(int i = 0; i < numBatches; i++) {
            NativeHashMap<int4, NewOctreeNode> nodes = new(0, Allocator.Persistent);
            nodes[new int4(0)] = root;
            allNodeMapsOutput.Add(nodes);

            int startIndexInclusive = i * numTrianglesPerBatch;
            int endIndexExclusive = math.min((i + 1) * numTrianglesPerBatch, numTriangles); // Ensure's we don't overflow

            var job = new OctreeGenerationJob(
                startIndexInclusive: startIndexInclusive,
                endIndexExclusive: endIndexExclusive,
                meshTriangles: triangleVertices,
                meshVertsWorldSpace: vertsWorldSpace,
                nodes: nodes,
                octreeCenter: octreeCenter,
                totalOctreeSize: totalSize,
                maxDivisionLevel: maxDivisionLevel
            );

            jobs.Add(job);
        }

        return jobs;
    }

    // Combines all of the node maps from each OctreeGenerationJob into a final Dictionary
    // to be used to create the Octree
    //
    // This is step 3
    //
    // This absolutely should be a Job, but I couldn't figure out how to do it in a reasonable time
    // and this only takes a few seconds anyways
    //
    private static Dictionary<int4, NewOctreeNode> CombineAllNodeMaps(
        List<NativeHashMap<int4, NewOctreeNode>> allNodeMaps
    ) {
        Dictionary<int4, NewOctreeNode> combined = new();
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        // For every hashmap, iterate through all of the keys and add it to the "Combined" dictionary
        // if there's a collision, then prioritize the node in the order so that:
        // 1. hasChildren == true
        // 2. Then if hasChildren is the same, pick the one where containsCollision == true
        foreach (NativeHashMap<int4, NewOctreeNode> nodesMap in allNodeMaps) {
            NativeArray<NewOctreeNode> nodes = nodesMap.GetValueArray(Allocator.Temp); // This is a copy

            foreach(NewOctreeNode node in nodes) {
                int4 key = node.dictionaryKey;

                if (!combined.ContainsKey(key)) {
                    combined.Add(key, node);
                } else {
                    NewOctreeNode existingNode = combined[key];

                    if (node.hasChildren && !existingNode.hasChildren) {
                        combined[key] = node;
                    } else if (node.hasChildren == existingNode.hasChildren) {
                        if (node.containsCollision && !existingNode.containsCollision) {
                            combined[key] = node;
                        }
                    }
                }
            }
        }

        return combined;
    }

    // Checks whether a given leaf is "in bounds" by raycasting downwards & seeing if it hits anything
    //
    // This is step 4
    private static IEnumerator MarkInBoundLeaves(
        NewNavOctreeSpace navOctreeSpace
    ) {
        List<NewOctreeNode> leaves = navOctreeSpace.octree.GetAllNodes().FindAll((node) => node.isLeaf);

        NativeArray<RaycastCommand> commands = new(leaves.Count, Allocator.Persistent);

        for (int i = 0; i < leaves.Count; i++) {
            NewOctreeNode leaf = leaves[i];

            commands[i] = new RaycastCommand(from: leaf.center, direction: Vector3.down, QueryParameters.Default);
        }

        NativeArray<RaycastHit> raycastHitResults = new(leaves.Count, Allocator.Persistent);
        JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(
            commands: commands,
            results: raycastHitResults,
            minCommandsPerJob: 1,
            maxHits: 1 // We only need 1 to determine if this is "in bounds" or not
        );

        yield return WaitForJobToComplete(raycastJobHandle);

        // Go through all of the RaycastHit results
        // and mark the corresponding node as in or out of bounds depending on if it hit anything
        for(int i = 0; i < leaves.Count; i++) {
            NewOctreeNode leafCopy = leaves[i];
            RaycastHit hit = raycastHitResults[i];

            bool didHit = hit.collider != null;
            if (didHit) {
                leafCopy.inBounds = true;
            } else {
                leafCopy.inBounds = false; // Don't need to do this technically
            }

            leaves[i] = leafCopy;
        }

        // Because we're dealing with copies (as OctreeNode is a struct)
        // we need to re-update the list
        navOctreeSpace.octree.UpdateDictionaryWithNodes(leaves);
        commands.Dispose();
        raycastHitResults.Dispose();
    }

    public static IEnumerator GenerateNeighbors(NewNavOctreeSpace space) {
        if (space.octree == null) yield break;

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        List<NewOctreeNode> allValidLeaves = space.octree!.GetAllNodes().FindAll(
            (node) => node.isLeaf && !node.containsCollision && node.inBounds
        );

        NativeArray<int4> allValidLeafKeys = new(allValidLeaves.Count, Allocator.Persistent);
        for (int i = 0; i < allValidLeaves.Count; i++) {
            allValidLeafKeys[i] = allValidLeaves[i].dictionaryKey;
        }

        NativeHashMap<int4, NewOctreeNode> nodes = new(space.octree!.nodes.Count, Allocator.Persistent);
        foreach(KeyValuePair<int4, NewOctreeNode> kvp in space.octree!.nodes) {
            nodes.Add(kvp.Key, kvp.Value);
        }

        // Output
        // TODO: Idk what size this should be, and I honestly don't think we can confidently figure it out
        NativeParallelMultiHashMap<int4, int4> edges = new(allValidLeaves.Count * 32, Allocator.Persistent);

        var job = new GraphGenerationJob() {
            nodes = nodes,
            allValidLeafKeys = allValidLeafKeys,
            edges = edges.AsParallelWriter()
        };

        int batchSize = allValidLeaves.Count / space.batchThing;
        JobHandle jobHandle = job.Schedule(allValidLeaves.Count, batchSize);

        yield return WaitForJobToComplete(jobHandle);

        stopwatch.Stop();
        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;

        stopwatch.Reset();
        stopwatch.Start();

        // int count = edges.GetKeyArray(Allocator.Temp).Length;
        // Debug.Log($"neighbors job w/ {count:n0} (allocated {allValidLeaves.Count * 32:n0}) took {seconds:F2} sec ({ms:F0} ms) with batch size {batchSize}");
        Debug.Log($"neighbors job w/ took {seconds:F2} sec ({ms:F0} ms) with batch size {batchSize}");

        // We're done! Now put them back to a Dictionary<int4, List<NewOctreeNode>> edges so we can put it back to the NewOctree

        // Convert the NativeParallelMultiHashMap -> Dictionary<int4, List<int4>>
        // we can't do this - it's too slow
        /*
        Dictionary<int4, List<int4>> edgesDict = new();

        int count = 0;

        NativeArray<int4> nonUniqueKeys = edges.GetKeyArray(Allocator.Temp);
        for(int i = 0; i < nonUniqueKeys.Length; i++) {
            count++;
            int4 key = nonUniqueKeys[i];

            List<int4> valuesList = edgesDict.GetValueOrDefault(key, new());

            var values = edges.GetValuesForKey(key);
            foreach(var value in values) {
                valuesList.Add(value);
            }

            edgesDict[key] = valuesList;
        }

        space.octree!.edges = edgesDict;

        stopwatch.Stop();
        ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        seconds = ms / 1000d;

        Debug.Log($"Converting managed to unmanaged of with {allValidLeaves.Count} leaves & {count:n0} edges took {seconds:F2} sec ({ms:F0} ms)");
        */

        // Dispose of edges, nodes, leaves
        allValidLeafKeys.Dispose();
        edges.Dispose();
        nodes.Dispose();
    }

    private static void GetAllChildrenRecursively(
        GameObject gameObject,
        List<GameObject> ret
    ) {
        // Only do this for active game objects that have a MeshFilter component
        if (!gameObject.activeInHierarchy) return;

        ret.Add(gameObject);

        for(int i = 0; i < gameObject.transform.childCount; i++) {
            GetAllChildrenRecursively(gameObject.transform.GetChild(i).gameObject, ret);
        }
    }

    private static IEnumerator WaitForJobToComplete(
        JobHandle jobHandle
    ) {
        while (!jobHandle.IsCompleted) {
            // I wish I could wait 5 frames or something
            yield return null;
        }

        jobHandle.Complete();
    }
}
