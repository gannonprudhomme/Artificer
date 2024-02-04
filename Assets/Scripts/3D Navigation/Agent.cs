using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

public class Agent : MonoBehaviour {
    public Octree? octree;

    // Player
    public Target Target; 

    public Graph? graph { get; set; }

    // Just for displaying
    public SplineContainer? SplineContainer;

    private void Awake() {
        if (octree == null) {
            Debug.LogError("Octree wasn't passed!");
            return;
        }

        Debug.Log("Init'ing agent");


        octree.Load(); // Load it  from the file baby!

        // TODO: Agents of the same type should (probably) share the same graph
        // ack if they share the same graph they're going to follow an identical path, and probably literally overlap w/ each other
        var generator = new GraphGenerator(octree);
        graph = generator.Generate(shouldBuildDiagonals: true);

        Debug.Log("Done init'ing agent");
    }

    private Spline? currSpline = null;
    private int frameCount = -1;
    private float currT = 0;
    public float pathLength = -1f;

    // How fast we should move this frame or something
    // The modifier is a product of the speed and how long the path is
    public float speedModifier {
        get { return 0.0f; }
    }

    // The speed should be constant
    public float speed = 5.0f; // meters per sec

    // Update is called once per frame
    void Update() {
        frameCount++;
        SplineContainer!.transform.position = Vector3.zero;

        if (currSpline != null) {
            currT += (Time.deltaTime * speedModifier);

            transform.position = SplineUtility.EvaluatePosition(currSpline, currT);
        }

        if (graph == null || SplineContainer == null)
        {
            Debug.LogError("Graph or spline container is null");
            return;
        }

        if (frameCount % 600 != 0) return;

        currSpline = GetSplinePath();
        currT = 0;
        transform.position = SplineUtility.EvaluatePosition(currSpline, 0);
        
        // float3 pos = currSpline.
    }

    // Assumes we check for
    private Spline GetSplinePath() {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var (path, _) = Pathfinder.GeneratePath(graph!, transform.position, Target.AimPoint.position + (Vector3.up * 30.0f));
        float3[] _path = new float3[path.Count];
        for (int i = 0; i < path.Count; i++) {
            // path[i] = gameObject.transform.InverseTransformPoint(path[i]);
            // path[i] = SplineContainer!.transform.InverseTransformPoint(path[i]);
            _path[i] = path[i];
        }
        stopwatch.Stop();
        double pathMS = ((double)stopwatch.ElapsedTicks / (double) System.Diagnostics.Stopwatch.Frequency) * 1000d;
        stopwatch.Reset();
        stopwatch.Start();

        Spline spline = SplineFactory.CreateCatmullRom(_path, false);
        pathLength = spline.GetLength();

        var splines = SplineContainer!.Splines;
        foreach(var s in splines)
            SplineContainer.RemoveSpline(s);
        SplineContainer.AddSpline(spline);
        stopwatch.Stop();

        double splineMS = ((double)stopwatch.ElapsedTicks / (double) System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Calculated path of {path.Count} in {pathMS} ms and spline in {splineMS}ms, pos is {transform.position}");

        return spline;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 2.0f);
    }
}
