using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

// An enemy that navigates in 3 dimensions (aka flies), rather than moving along the ground.
//
// This actually does the moving of the Enemy so the Wisp doesn't need to think about how it should move
//
// I should really get rid of this - it's basically entirely been replaced by OctreeNavigator
public abstract class NavSpaceEnemy : Enemy {
    [Header("NavSpaceEnemy (Inherited)")]
    public SplineContainer? _SplineContainer; // Don't actually need, only for debuggin

    // The spline we are currently traversing, if any
    protected Spline? currentSplinePath = null;

    public abstract float Speed { get; }

    private OctreeNavigator? navigator;

    protected abstract bool ColliderCast(Vector3 position, Vector3 startPosition, out RaycastHit? hit);

    protected override void Start() {
        base.Start();

        navigator = new OctreeNavigator(
            ownerTransform: transform,
            graph: OctreeManager.shared!.Graph!,
            octree: OctreeManager.shared!.Octree!,
            speed: Speed,
            colliderCast: ColliderCast
        );
    }

    // Called on every frame in sub-classes of this (when we want to move)
    public void TraversePath() {
        navigator!.TraversePath();
    }

    // Call to generate a new path to traverse 
    // 
    // May skip traditional pathfinding if we can do a straight-shot to the given position
    public void CreatePathTo(Vector3 position) {
        navigator!.CreatePathTo(position);
    }
}

