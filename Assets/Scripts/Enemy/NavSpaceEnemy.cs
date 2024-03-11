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
public abstract class NavSpaceEnemy : Enemy {
    [Header("NavSpaceEnemy (Inherited)")]
    public SplineContainer? _SplineContainer;

    // The spline we are currently traversing, if any
    protected Spline? currentSplinePath = null;

    public abstract float Speed { get; }

    // Graph we use to navigate
    //
    // Should be set in the sub-class's Start()
    protected Graph? graph;

    private float currT = 0;
    private float pathLength = -1f;
    private float timeToTravel { get { return pathLength / Speed;  } }

    protected abstract bool ColliderCast(Vector3 position, out RaycastHit? hit);

    // Called on every frame in sub-classes of this (when we want to move)
    public void TraversePath() {
        if (graph == null) {
            Debug.LogError("Graph was null - it should be set in the sub-classes Start()!");
            return;
        }

        if (currentSplinePath != null) {
            currT += Time.deltaTime / timeToTravel; // TODO: This is not right lol
            transform.position = SplineUtility.EvaluatePosition(currentSplinePath, currT);
        }
    }

    // Call to generate a new path to traverse 
    // 
    // May skip traditional pathfinding if we can do a straight-shot to the given position
    public void CreatePathTo(Vector3 position) {
        if (graph == null) {
            Debug.LogError("Graph was null - it should be set in the sub-classes Start()!");
            return;
        }

        if (!ColliderCast(position, out RaycastHit? hit)) {
            // If it's a straight shot (nothing in our way)
            // go directly to it
            // I think this is probably expensive so hopefully we don't call CreatePathTo frequently
            currentSplinePath = ConvertToSpline(new List<Vector3> { transform.position, position });
        } else {
            List<Vector3> path = Pathfinder.GeneratePath(graph, this.transform.position, position);
            currentSplinePath = ConvertToSpline(path);
        }

        // TODO: Remove, only for debugging
        if (_SplineContainer != null) {
            if (_SplineContainer.Splines.Count > 0) {
                _SplineContainer.RemoveSplineAt(0);
            }

            _SplineContainer.AddSpline(currentSplinePath);
        }

        if (currentSplinePath != null) {
            pathLength = currentSplinePath.GetLength();
        }

        // Reset currT so we start at the beginning of the new path we just created
        currT = 0;
    }

    private static Spline? ConvertToSpline(List<Vector3> path) {
        if (path.Count == 0) return null;

        // Convert List<Vector3> to float[3] for Spline
        float3[] _path = new float3[path.Count];
        for (int i = 0; i < path.Count; i++) {
            _path[i] = path[i];
        }

        Spline spline = SplineFactory.CreateCatmullRom(_path, false);
        return spline;
    }
}

