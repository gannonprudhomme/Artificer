using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

[RequireComponent(typeof(SplineContainer))]
public class PathfindObject : MonoBehaviour {
    public NavOctreeSpace? octreeSpace;
    public SplineContainer? SplineContainer;
    
    public Transform? start = null;
    public Transform? end = null;

    public bool DrawPathPoints = true;
    public bool DrawLines = false;
    public bool DrawSpline = false;

    [HideInInspector]
    public List<Vector3> output = new();
    
    [HideInInspector]
    public Spline? spline = null;

    public void GenerateRawPath() {
        if (start == null || end == null) return;
        
        octreeSpace!.LoadIfNeeded();
        GraphGenerator.PopulateOctreeNeighbors(octreeSpace!.octree!, shouldBuildDiagonals: true);
        
        output = Pathfinder.GeneratePath(octreeSpace!.octree!, start.position, end.position);
    }

    public void GenerateSmoothedPath() {
        if (start == null || end == null) return;
        
        octreeSpace!.LoadIfNeeded();
        GraphGenerator.PopulateOctreeNeighbors(octreeSpace!.octree!, shouldBuildDiagonals: true);
        
        output = Pathfinder.GenerateSmoothedPath(start.position, end.position, octreeSpace!.octree!, null);
    }
    
    public void RemoveSplines() {
        if (SplineContainer!.Splines.Count > 0) {
            var splines = SplineContainer.Splines;

            foreach(var spline in splines) {
                SplineContainer.RemoveSpline(spline);
            }
        }
    }

    public void GenerateSpline() {
        if (start == null || end == null) return;
        
        // octreeSpace!.LoadIfNeeded();
        // GraphGenerator.PopulateOctreeNeighbors(octreeSpace!.octree!, shouldBuildDiagonals: true);
        
        // List<Vector3> smoothedPath = Pathfinder.GenerateSmoothedPath(start.position, end.position, octreeSpace!.octree!, null);
        if (output.Count == 0) {
            Debug.LogError("No path to make a spline for ");
            return;
        }

        spline = OctreeNavigator.ConvertToSpline(output);
        if (spline == null) {
            Debug.LogError("Couldn't make spline");
            return;
        }
        
        Debug.Log("Adding spline");

        RemoveSplines();

        // SplineContainer!.Spline = spline;
        SplineContainer!.AddSpline(spline);
    }

    public void OnDrawGizmos() {
        if (!(DrawPathPoints || DrawLines || !DrawSpline)) return;

        if (DrawPathPoints) {
            // Draw the points as spheres
            Gizmos.color = Color.green;
            foreach(Vector3 point in output) {
                Gizmos.DrawSphere(point, 3f);
            }
        }

        if (DrawLines) {
            // Draw the lines between the points
            Vector3[]? linePoints = GetNeighborLines(output);
            if (linePoints == null) {
                return;
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLineList(linePoints);
        }
    }
    
    private static Vector3[]? GetNeighborLines(List<Vector3> pathPoints) {
        if (pathPoints.Count <= 1) return null;

        List<(Vector3, Vector3)> linesPoints = new();

        for (int i = 1; i < pathPoints.Count; i++) { // skip first point
            linesPoints.Add((pathPoints[i - 1], pathPoints[i]));
        }

        int currIndex = 0;
        Vector3[] linesToDraw = new Vector3[linesPoints.Count * 2];
        foreach((Vector3, Vector3) pair in linesPoints) {
            linesToDraw[currIndex] = pair.Item1;
            linesToDraw[currIndex + 1] = pair.Item2;

            currIndex += 2;
        }

        return linesToDraw;
    }
}
