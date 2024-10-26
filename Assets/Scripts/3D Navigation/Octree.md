# Octrees

This project uses an octree, specifically a **sparse-voxel octree**, for 3D pathfinding.
For instance, the AtG Missile item and Wisp enemy use octrees to pathfind in 3D space.

## Other features

- Raycasting (for collision detection for path "smoothing")
- Relatively basic A* pathfinding
    - "Path smoothing"
      - I don't think this is actually _smoothing_, it's more so reducing redundant points so the path is a straight-shot when in empty space,
        rather than having a bunch of points which makes the resulting path feel jagged and unnatural
- Neighbors generation
- Constant-time lookup for finding the according octree leaf for a given poin
  - This is just what octrees do though

## How?

We generate the octree from the static objects of my level - specifically their meshes - and writes the results to a file to be loaded during the game launch/runtime, while the neighbor nodes (needed for pathfinding) are generated at runtime.

This save-to-file approach was initially necessary because the generation took ~1.5 minutes for the mesh I was generating for (size of 1024^3, depth of 8, 415k vertices).
However after I converted to using Jobs & Burst and paralellized it (among other things), I got this down to ~1.5 seconds for the same mesh (~95% reduction!), thus I could probably do it during a loading screen within a reasonable amount of time in order to save download size. 

### Octree Generation

> Note: after {commit-SHA}, I converted the generation from a single-threaded approach to a parallelized one, though the same overarching process still applies.



### Neighbor Calculating

### Pathfinding

## Evolution

Initially this was single-threaded and used a pointer-based approach for generating & storing the octree, which was mostly fine for my original purposes.
However, 

### Generating the neighbors using Unity Jobs & Burst

When I was converting from a single-threaded octree generation 
Converting from a `NativeParallelMultiHashMap` -> `Dictionary<Key, List<Key>>` took a ridiculous amount of time.
Thus, I was stuck with two options:
1. Make the new Octree

## Files

TODO

Pointer-based (for game runtime usage):
- `Octree.cs`
    - Contains reference to the root `OctreeNode`
    - Performs raycasting
- `OctreeNode.cs`
- `NavOctreeSpace.cs`
- `NavOctreeSpaceEditor.cs`
- `GraphGenerator.cs`
- `OctreeNavigator.cs`
- `Pathfinder.cs`
- `OctreeSerializer.cs`




## Future Optimizations / Improvements

1. Memory usage - `OctreeNode` uses much more memory than it needs to. 
   There are optimizations in [Advanced Octrees 2: node representations](https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations)
   that I could surely do to reduce this, but I'm not releasing this so I don't mind too much.
2. Using the digital differential analyzer algorithm for more efficiently stepping through nodes during the `Octree.Raycast` operation

    a. I currently use a fixed step lengt, and didn't optimize further as it was fast enough for my purposes
3. Using Lazy* / Lazy Theta* for pathfinding
4. Sort vertices by morton codes?? Idk if this would actually help, but it might
5. Using

## Resources

- Initial implementation heavily based off of [supercontact/PathFindingEnhanced](https://github.com/supercontact/PathFindingEnhanced)
- [GDC: Getting off the NavMesh (Warframe devs)](https://www.gdcvault.com/play/1022016/Getting-off-the-NavMesh-Navigating)
   - This was my jumping off point for using Octrees for 3D pathfinding!
- [Advanced Octrees 2: node representations](https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations)
- [Fast Parallel Surface And Solid Voxelization on GPUs](https://michael-schwarz.com/research/publ/files/vox-siga10.pdf)
   - I can't confidently say how much of my implementation uses this, though iti s a good resource regardless.

## What this is not:

1. The octree is not updated constantly - it is generated once, as I only care about collisions with static objects.
2. Per 1, does not store the objects within an octree node (since it'd just be mesh triangles)

## Videos

- Show AtG Missile
- Show Wisp