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
    private float pathLength = -1f;

    private Vector3 currTargetPos = Vector3.zero;

    // How fast we should move this frame or something
    // The modifier is a product of the speed and how long the path is
    public float speedModifier {
        get {
            return 0.3f;
        }
    }

    private float timeToTravel { get { return pathLength / speed; } } // meters / (meters/sec) = seconds

    // The speed should be constant
    public float speed = 5.0f; // meters per sec

    // Update is called once per frame
    void Update() {
        frameCount++;
        // SplineContainer!.transform.position = Vector3.zero;

        if (currSpline != null) {
            currT += (Time.deltaTime / timeToTravel); // TODO: This is not right
            transform.position = SplineUtility.EvaluatePosition(currSpline, currT);
        }

        if (graph == null || SplineContainer == null)
        {
            Debug.LogError("Graph or spline container is null");
            return;
        }

        if (frameCount % 60 == 0) {
            float distMoved = (Target.AimPoint.position - lastTargetPos).sqrMagnitude;
            float minDist = 3.0f;
            if (distMoved < minDist * minDist) {
                return;
            }

            lastTargetPos = Target.AimPoint.position;
            currSpline = GetSplinePath();
            currT = 0;

            // Well this is pointless cause 0 is just going to be where we are?
            if (currSpline != null)
                transform.position = SplineUtility.EvaluatePosition(currSpline, 0);
        }
    }

    private Vector3 lastTargetPos = Vector3.zero;

    // Assumes we check for
    private Spline? GetSplinePath() {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();


        currTargetPos = GetPointNearTarget(transform.position, Target.AimPoint.position);
        if (!Physics.Linecast(transform.position, currTargetPos)) { // If no collisions
            // Make a spline that's just a straight line
            float3 start = new(transform.position.x, transform.position.y, transform.position.z);
            float3 end = new(currTargetPos.x, currTargetPos.y, currTargetPos.z);

            Spline straightSpline = SplineFactory.CreateLinear(new float3[] { start, end }, false);
            pathLength = straightSpline.GetLength();
            // InsertSplineIntoContainer(straightSpline);

            return straightSpline;
        }

        Debug.Log("Can't do a straight line! Pathfinding");

        var (path, _) = Pathfinder.GeneratePath(graph!, transform.position, currTargetPos);

        if (path.Count == 0)
            return null;

        // Convert List<Vector3> to float3[] for Spline
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
        // InsertSplineIntoContainer(spline);
        stopwatch.Stop();

        double splineMS = ((double)stopwatch.ElapsedTicks / (double) System.Diagnostics.Stopwatch.Frequency) * 1000d;
        Debug.Log($"Calculated path of {path.Count} in {pathMS} ms and spline in {splineMS} ms, total of {pathMS + splineMS} ms");

        return spline;
    }

    public void InsertSplineIntoContainer(Spline spline) {
        var splines = SplineContainer!.Splines;
        foreach(var s in splines)
            SplineContainer.RemoveSpline(s);
        SplineContainer.AddSpline(spline);
    }

    // In reality the Wisp should be going to some point in a circle around the player (and any closer)
    // that way the player can actually shoot the wisp
    public static Vector3 GetPointNearTarget(Vector3 agentPos, Vector3 targetPos) { // should be static tbh
        // Should be above it somewhat, but probably have a logic to it

        float upAmount = 30.0f;
        Vector3 targetPosMovedUp = targetPos + (Vector3.up * upAmount);

        float circleRadius = 30.0f;

        // Shit this isn't a "flat" circle though (X & Z rotation=0), I need to act like they're in the same height?
        Vector3 modifiedAgentPos = new(agentPos.x, targetPosMovedUp.y, agentPos.z);
        // Vector3 modifiedAgentPos = agentPos;

        // direction vector from altered target pos to modifiedAgentPos(same height as targetPosMovedUp)
        Vector3 direction = (modifiedAgentPos - targetPosMovedUp).normalized;

        return targetPosMovedUp + (direction * circleRadius);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 2.0f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currTargetPos, 2.0f);
    }
}
