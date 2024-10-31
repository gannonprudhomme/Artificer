using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Linq;

#nullable enable

public class OctreeNavigator {
    private readonly float speed;

    private readonly Octree octree;
    private readonly Transform ownerTransform;
    private readonly ColliderCastDelegate colliderCast;

    // The spline we are currently traversing, if any
    private Spline? currentSplinePath = null;

    // Where in the knotPositions there are positions we haven't traversed yet
    // we use this to replace positions
    private int futureKnotsIndex = -1;
    private List<float3> knotPositions = new();

    private float currT = 0;
    private float pathLength = -1f;
    private float timeToTravel { get { return pathLength / speed; } }

    public Vector3 CurrentSplineTangent {
        get {
            if (currentSplinePath == null) {
                Debug.LogError("OctreeNavigator: no current path");
                return Vector3.zero;
            }

            return SplineUtility.EvaluateTangent(currentSplinePath, currT);
        }
    }

    public delegate bool ColliderCastDelegate(Vector3 startPosition, Vector3 targetPosition, out RaycastHit? hit);

    public OctreeNavigator(
        Transform ownerTransform,
        Octree octree,
        float speed,
        ColliderCastDelegate colliderCast
    ) {
        this.ownerTransform = ownerTransform;
        this.octree = octree;
        this.speed = speed;
        this.colliderCast = colliderCast;
    }

    // Called on every frame in sub-classes of this (when we want to move)
    public void TraversePath() {
        if (currentSplinePath != null) {
            currT += Time.deltaTime / timeToTravel; // TODO: This is not right lol
            ownerTransform.position = currentSplinePath.EvaluatePosition(currT);
        }
    }

    // Pathfind from the pathStartPosition, while ensuring we keep the originalPosition and the pathStartPosition in the Path
    //
    // TODO: Combine this with the CreatePathTo fucntion
    // This function is pretty hyper focused on the AtG Missile's use case
    public void CreatePathGuaranteeingPositions(Vector3 goalPosition, Vector3 pathStartPosition, Vector3 originalPosition) {
        // Positions we don't want to be "smoothed"
        HashSet<Vector3> positionsToNotSmooth = new() { pathStartPosition, originalPosition };

        List<Vector3> path = GetNewPath(
            goalPosition: goalPosition,
            startPosition: pathStartPosition,
            positionsToKeep: positionsToNotSmooth
        );
        path.Insert(0, originalPosition);

        if (currentSplinePath != null) {
            path.Insert(0, GetPreviousKnot(currentSplinePath, currT)); 
        }

        // Insert the nearest previous knot to the current position into the beginning of the path
        // before we generating the new spline path so the curve maintains it's shape (i.e. so we don't have sharp & random turns)
        currentSplinePath = ConvertToSpline(path);

        if (currentSplinePath == null) return;

        pathLength = currentSplinePath.GetLength();

        // Reset currT so we start at the beginning of the new path we just created.
        // Basically migrates the previous currT to the new path, such that we end up in the same position.
        currT = GetTimeForPointOnPath(currentSplinePath, ownerTransform.position);
    }

    // Call to generate a new path to traverse 
    // 
    // May skip traditional pathfinding if we can do a straight-shot to the given position
    //
    // General idea is that because we use Catcull-Rom splines, we don't want to *entirely* replace the path (spline)
    // instead, we want to keep the previous position (knot), as Catcull-Rom splines/knots use their previous and next knot
    // positions to determine their shape/curve.
    // Thus, new paths will consist of [previousNearestKnot, currentPositionKnot, newPathKnot1, ..., goalPosition]
    public void CreatePathTo(Vector3 goalPosition) {
        List<Vector3> path = GetNewPath(goalPosition: goalPosition, startPosition: ownerTransform.transform.position);

        if (currentSplinePath != null) {
            path.Insert(0, GetPreviousKnot(currentSplinePath, currT));
        }

        // Insert the nearest previous knot to the current position into the beginningh of the path
        // before we generating the new spline path so the curve maintains it's shape (i.e. so we don't have sharp & random turns)

        currentSplinePath = ConvertToSpline(path);

        if (currentSplinePath == null) return;

        // Note: This is only for debugging to get the output
        /*
        if (splineContainer.Splines.Count > 0) {
            splineContainer.RemoveSplineAt(0);
        }
        splineContainer.AddSpline(currentSplinePath);
        */

        pathLength = currentSplinePath.GetLength();

        // Reset currT so we start at the beginning of the new path we just created.
        // Basically migrates the previous currT to the new path, such that we end up in the same position.
        currT = GetTimeForPointOnPath(currentSplinePath, ownerTransform.position);
    }

    private List<Vector3> GetNewPath(Vector3 goalPosition, Vector3 startPosition, HashSet<Vector3>? positionsToKeep = null) {
        if (!colliderCast(startPosition, goalPosition, out RaycastHit? hit)) {
            // If it's a straight shot (nothing in our way), go directly to it
            return new() { startPosition, goalPosition };
        } else {
            // No straight shot - use the Graph/Octree to pathfind!
            return Pathfinder.GenerateSmoothedPath(
                start: startPosition,
                end: goalPosition,
                octree: octree,
                positionsToKeep: positionsToKeep
            );
        }
    }

    private static float GetTimeForPointOnPath(Spline spline, Vector3 position) {
        SplineUtility.GetNearestPoint(
            spline: spline,
            point: position,
            out _,
            out float t
        );

        return t;
    }

    // Get the closest (previous) knot to the position we're at
    private static Vector3 GetPreviousKnot(Spline spline, float currTime) {
        // Find the closest knot to this position, behind it (from cast to int/floor)
        int knotIndex =  (int) spline.ConvertIndexUnit(currTime, PathIndexUnit.Knot);

        return spline.Knots.ToArray()[knotIndex].Position; // Get position of Knots[knotIndex]
    }

    private static Spline? ConvertToSpline(List<Vector3> path) {
        if (path.Count == 0) return null;

        // Convert List<Vector3> to float[3] for Spline
        float3[] _path = new float3[path.Count];
        for (int i = 0; i < path.Count; i++) {
            _path[i] = path[i];
        }

        Spline spline = SplineFactory.CreateCatmullRom(_path, false);
        for (int i = 0; i < spline.Count; i++) {
            spline.SetAutoSmoothTension(i, 1f);
        }

        return spline;
    }
}

