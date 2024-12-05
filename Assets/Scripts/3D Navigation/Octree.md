# Octrees
This project uses an octree, specifically a **sparse-voxel octree**, for 3D pathfinding.
For instance, the AtG Missile item and Wisp enemy use octrees to pathfind in 3D space.

It uses two types / implementations of octrees:
1. Flat-based (`struct`), where nodes store a boolean on whether they have children or not, and we identify them using a Dictionary where the (`int4`) key is their index + node level

   a. We use the node level as our node indices are not unique - a parent & a child may have the same index (e.g. node of index `(1, 3, 5), level 2` may (_will_) have a child of `(1, 3, 5`, level 3))

   b. This can probably be improved (there's tons of articles on this), but it worked for me.

   c. This _might_ be a "linear octree", but I don't think it is - we don't store any identifiers for the children of a given node, because the each of the node's children's dictionaryKey (index + node level/depth) is determinstic.
3. Pointers-based (`class`), where nodes store 8 pointers to their children (if they are not a leaf)

We use the flat-based for generation, as we can significantly parallelize this operation. (98% speed-up from the single-threaded / pointers-based version)
Once generation has completed, we convert it to pointers-based and use the pointer-based operation during actual runtime
as its much better for actual usage; it's both easier to reason with,
and traversing the tree is seemingly faster than having to get each child from the dictionary each time (like we do for flat-based)

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

> Note: after https://github.com/gannonprudhomme/Artificer/pull/1, I converted the generation from a single-threaded approach to a parallelized one, though the same overarching process still applies.

General, single-threaded approach:
1. Calculate the bounds of the entire level (starting from whatever `NavOctreeSpace` is on), and use that to determine a power-of-2 size that contains all of the level

   a. Theoretically `size` could be a non-power-of-2, but making it a power-of-2 means we can cleanly store `OctreeNode.size` as an integer. (which also might not matter depending on usecases)
3. Retrieve all of the vertices for the mesh and convert them to world coordinates

   a. Most likely not necessary, but made operations easier to reason about.
5. For each triangle, starting from the root, see if the triangle intersects with the current node
6. If it does, subdivide the node (if it hasn't already) by creating 8 children, then repeat until we've reached the max depth

   a. If we reached the max depth, mark the current node as containing a collision
8. Once all of this is done, we mark in bound leaves so we can don't try to pathfind on nodes that we wouldn't consider in bounds (e.g. below the map)

   a. This is done by Raycasting downwards from each leaf & seeing if we hit anything - if we don't then this node is out of bounds!
9. Save to file!

After #1, the overarching process is mostly the same, but requires some extra steps:
1. First, we need to batch the vertices so we can pass each batch to a job. Each of these batches will basically create their own octree (stored as a `NativeHashMap<int4, FlatOctreeNode>`) using the same steps as above.
2. Once these jobs are done, we combine all of the nodes into one big Octree (still a hashmap), prioritizing nodes that have `containsChildren = true`.
3. We then use Jobs to mark in bound leaves in parallel
4. Convert from the flat-based OctreeNode to the pointer-based OctreeNodes
5. Save to file!

### Neighbor Calculating

At (game) runtime we calculate each node's nearest valid neighbors - valid meaning in bounds & doesn't contain a collision.
We do so by iterating over every leaf then over every direction (all face-directions + diagonals/corner directions) and seeing if there is a node of the same size or larger (aka same depth or "smaller").
If it's the same size we only connect it one way (as when we iterate on the other node we'll connect it to this)
and if the node is larger we connect it both ways (as we don't search for big -> small, only small -> big, or small -> small)

Note that we still connect from nodes that contain a collision to nodes that don't.
However we do not connect from nodes that _don't_ contain a collision to ones that do - i.e. it's a one-way edge.
This is done so we can easily search for the closest valid `OctreeNode` to a given `Vector3`.

This still isn't as fast as I'd like it to be though - it takes ~0.3 sec for 170k leaves & ~1 sec for 370k leaves.

### Pathfinding

- Standard A*
- "Path smoothing" - aka removing of redundant points


## Future Optimizations / Improvements

1. Generate the Octree bottom-up, by first identifying what leaves need to be greater then recursively making their parents

   a. This is the approach described in [this video](https://www.gdcvault.com/play/1022016/Getting-off-the-NavMesh-Navigating), [this paper](https://michael-schwarz.com/research/publ/files/vox-siga10.pdf), and [this repository](https://github.com/Forceflow/cuda_voxelizer)

   b. I'd have done this approach if I realized what I was doing was different, but at this point my solution works & 1.5 seconds (depth of 8, ~715k triangles) for generation is pretty damn good.
3. Memory usage - `OctreeNode` uses much more memory than it needs to. 
   There are optimizations in [Advanced Octrees 2: node representations](https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations)
   that I could surely do to reduce this, but I'm not releasing this so I don't mind too much.
4. Using the digital differential analyzer algorithm for more efficiently stepping through nodes during the `Octree.Raycast` operation

   a. I currently use a fixed step length, and didn't optimize further as it was fast enough for my purposes
5. Use Lazy* / Lazy Theta* for pathfinding
6. Morton code usages? I could never figure out how they'd be applied
7. Perform loading & neighbor generation asynchronously / on a background thread

   a. Ideally this would be fast enough to be on the main thread

## Resources

- Initial implementation heavily based off of [supercontact/PathFindingEnhanced](https://github.com/supercontact/PathFindingEnhanced)
  - [Report](https://ascane.github.io/assets/portfolio/pathfinding3d-report.pdf)
  - [Slides](https://ascane.github.io/assets/portfolio/pathfinding3d-poster.pdf)
- [GDC: Getting off the NavMesh (Warframe devs)](https://www.gdcvault.com/play/1022016/Getting-off-the-NavMesh-Navigating)
   - This was my jumping off point for using Octrees for 3D pathfinding!
- [Advanced Octrees 2: node representations](https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations)
- [Fast Parallel Surface And Solid Voxelization on GPUs](https://michael-schwarz.com/research/publ/files/vox-siga10.pdf)
   - I can't confidently say how much of my implementation uses this, though it is a good resource regardless.

## What this is not:

1. The octree is not updated constantly - it is generated once, as I only care about collisions with static objects.
2. Per 1, does not store the objects within an octree node (since it'd just be mesh triangles)

## Videos

You can see this in action in the videos in the README. 
