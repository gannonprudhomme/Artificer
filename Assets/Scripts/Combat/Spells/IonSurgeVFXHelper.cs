using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable
// We don't necessarily need this since it's just a Vector3
// but it's nice to have the naming in VFX graph
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct ExplosionPoint {
    public Vector3 position;

    public ExplosionPoint(Vector3 position) {
        this.position = position;
    }
}

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct ExplosionLine {
    public int startIndex;
    public int endIndex;
    public float timeStart;

    public ExplosionLine((int, int) indices) {
        startIndex = indices.Item1;
        endIndex = indices.Item2;
        timeStart = Time.time;
    }
}

public class IonSurgeVFXHelper {
    private readonly Transform playerTransform;

    private readonly VisualEffect leftHandVFXInstance;
    private readonly VisualEffect rightHandVFXInstance;

    // We create an instance as the world/local space conversion gets complicated
    // since we change the lines positions within the lifetime of the explosion VFX
    private readonly VisualEffect mainExplosionVFXPrefab;
    private VisualEffect? mainExplosionVFXInstance;

    private Light explosionLightInstance;
    private AnimationCurve explosionLightIntensityCurve;

    /** GENERAL CONSTANTS **/
    private readonly float lightLifetime = 0.75f;
    private readonly float baseLightIntensity = 1500f;
    private readonly float explosionVFXRadius = 20f; // TODO: Rename to explosionRadius (VFX is implied)

    /** POINTS & LINES CONSTANTS **/
    private readonly float lineAndPointsLifetime = 1.0f;
    private readonly int explosionVFXPointsCount = 16;
    // How many lines are on screen at a given time
    private int explosionVFXLinesCount => explosionVFXPointsCount - 1; // My line generating algorithm always ends up being 1 less than the points
    // How many lines are added per second
    // it should be *really* fast
    private readonly float lineAddRate = 200;
    private float totalLinesOverLifetime => lineAddRate * lineAndPointsLifetime + (explosionVFXLinesCount / 2); // Add the initial amount of lines to the total count
    // The radius of the points + lines
    private float pointsAndLinesRadius => explosionVFXRadius * 0.75f;
    // How long a single line should be on the screen
    // with the assumption that I want {explosionVFXLinesCount} lines on screen at all times
    private float individualLineLifetime => (lineAndPointsLifetime / totalLinesOverLifetime) * explosionVFXLinesCount; // TODO: I think this is right?

    /** POINTS & LINES PER-FRAME DATA **/
    private GraphicsBuffer explosionPointsBuffer;
    private GraphicsBuffer explosionLinesBuffer;

    private float lastTimeAddedLines = Mathf.NegativeInfinity;
    private int lastLineIndexAdded = 0;
    private List<ExplosionLine> currentLines = new();
    private Queue<ExplosionLine> linesQueue = new();

    public IonSurgeVFXHelper(
        Transform playerTransform,
        VisualEffect leftHandVFXInstance,
        VisualEffect rightHandVFXInstance,
        VisualEffect mainExplosionVFXPrefab,
        Light explosionLightInstance,
        AnimationCurve explosionLightIntensityCurve
    ) {
        this.playerTransform = playerTransform;
        this.leftHandVFXInstance = leftHandVFXInstance;
        this.rightHandVFXInstance = rightHandVFXInstance;
        this.mainExplosionVFXPrefab = mainExplosionVFXPrefab;
        this.explosionLightInstance = explosionLightInstance;
        this.explosionLightIntensityCurve = explosionLightIntensityCurve;

        // Initialize the GraphicsBuffers for the points and lines
        // This assumes that they're constant (which they should be)
        explosionLinesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count: explosionVFXLinesCount, stride: Marshal.SizeOf(typeof(ExplosionLine)));
        explosionPointsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count: explosionVFXPointsCount, stride: Marshal.SizeOf(typeof(ExplosionPoint)));
    }

    // Called in IonSurgeJumpSpell.Update()
    public void OnUpdate(float timeOfLastFire) {
        // Idk what should be doing this check - this or the spell?
        bool isExplosionAndLinesActive = (Time.time - timeOfLastFire) < lineAndPointsLifetime;
        if (isExplosionAndLinesActive) {
            RotateOrAddLines();
        }

        bool isLightActive = (Time.time - timeOfLastFire) <= (lightLifetime * 1.1f); // Give it a little buffer so it transitions to 0
        if (isLightActive) {
            float time = (Time.time - timeOfLastFire) / lightLifetime;
            explosionLightInstance.intensity = baseLightIntensity * explosionLightIntensityCurve.Evaluate(Mathf.Max(time, 1f));
        }
    }

    public void PlayVFX() {
        leftHandVFXInstance!.enabled = true;
        rightHandVFXInstance!.enabled = true;
        explosionLightInstance!.enabled = true;

        leftHandVFXInstance!.Play();
        rightHandVFXInstance!.Play();

        mainExplosionVFXInstance = Object.Instantiate(mainExplosionVFXPrefab!); // Spawn it in world space
        mainExplosionVFXInstance!.SetFloat("Explosion Radius", explosionVFXRadius);
        mainExplosionVFXInstance!.SetFloat("Line+Points Lifetime", lineAndPointsLifetime);
        mainExplosionVFXInstance!.SetFloat("Individual Line Lifetime", individualLineLifetime);
        mainExplosionVFXInstance!.transform.position = playerTransform.position;

        mainExplosionVFXInstance!.Play();

        CreateAndPopulatePointsBuffer(numToCreate: explosionVFXPointsCount);
        CreateAndPopulateLinesBuffer(numToCreate: explosionVFXPointsCount);

        // Initialize the light
        explosionLightInstance!.intensity = baseLightIntensity;
    }

    public void StopVFX() {
        leftHandVFXInstance!.enabled = false;
        rightHandVFXInstance!.enabled = false;
        explosionLightInstance!.enabled = false;

        leftHandVFXInstance!.Stop();
        rightHandVFXInstance!.Stop();

        // We won't really need to do this since lifetime will handle it but w/e
        if (mainExplosionVFXInstance != null) { // Called on Awake() so have to check this
            mainExplosionVFXInstance!.Stop();
            Object.Destroy(mainExplosionVFXInstance.gameObject);
            mainExplosionVFXInstance = null;
        }
    }

    private void CreateAndPopulateLinesBuffer(int numToCreate) {
        List<ExplosionLine> lines = GetLines(pointsCount: numToCreate);

        linesQueue = new(); // Recreate it so we have a clean slate (really just to catch my mistakes)

        int numToAdd = explosionVFXLinesCount / 2;

        // On start, add them to the queue
        // and we'll "prebake" half of them, but leave the other ~half
        currentLines = lines.GetRange(0, numToAdd).ToList();
        lastLineIndexAdded = numToAdd - 1;

        // Add all of the remaining lines to the queue
        for(int i = numToAdd; i < lines.Count; i++) {
            linesQueue.Enqueue(lines[i]);
        }

        // Populate initial buffer
        mainExplosionVFXInstance!.SetGraphicsBuffer("ExplosionLinesBuffer", explosionLinesBuffer);
        mainExplosionVFXInstance!.SetInt("Num Lines", lines.Count);
    }

    // TODO: Add a gradual points fadeout(?)
    // Maybe make them disappear towards the end of the lifetime? Like when their aren't any points connected to them idk
    private void CreateAndPopulatePointsBuffer(int numToCreate) {
        ExplosionPoint[] points = GetPoints(count: numToCreate, sphereRadius: pointsAndLinesRadius);

        explosionPointsBuffer!.SetData(points); // pass the data to the graphics buffer
        mainExplosionVFXInstance!.SetGraphicsBuffer("ExplosionPointsBuffer", explosionPointsBuffer);
        mainExplosionVFXInstance!.SetInt("Num Points", points.Length);
    }

    private static ExplosionPoint[] GetPoints(int count, float sphereRadius = 30f) {
        ExplosionPoint[] ret = new ExplosionPoint[count];

        List<Vector3> randomPoints = GeneratePointsOnSphere(count: count, radius: sphereRadius);

        // Select == Collection.map in C#
        return randomPoints.Select(point => new ExplosionPoint(point)).ToArray();
    }

    // Generates N evenly distributed points on a sphere, with a bit of randomness thrown in
    // This uses a Fibonacci lattice so the points are evenly distributed (though idk what that is lol)
    private static List<Vector3> GeneratePointsOnSphere(int count, float radius) {
        List<Vector3> points = new();
        float phi = (1 + Mathf.Sqrt(5)) / 2; // Golden ratio

        float randomnessFactor = 0.1f;

        for (int i = 0; i < count; i++) {
            float theta = 2 * Mathf.PI * (i / phi + randomnessFactor * (Random.value - 0.5f)); // Adds randomness
            float cosTheta = Mathf.Cos(theta);
            float sinTheta = Mathf.Sin(theta);

            // Latitude using adapted Fibonacci lattice for sphere

            // The 2* is how it changes between a sphere and a hemisphere (hemisphere doesn't have 2*)
            float latitude = Mathf.Acos(1 - 2f * (i + 0.5f) / count + randomnessFactor * (Random.value - 0.5f)); // Adds randomness (Sphere)
            float sinLatitude = Mathf.Sin(latitude);
            float cosLatitude = Mathf.Cos(latitude);

            // Convert spherical coordinates to Cartesian coordinates
            float x = sinLatitude * cosTheta;
            float y = cosLatitude; // Vertical axis
            float z = sinLatitude * sinTheta;

            points.Add(new Vector3(x, y, z) * radius); // Multiply by radius at the end
        }

        return points;
    }

    // Get a list of {pointsCount} lines that connect the points
    // with no two lines sharing the same start or end point such that each point only has one "outward" point
    // This may mean some points have more than 1 line coming *to* them, but not *from* them.
    private static List<ExplosionLine> GetLines(int pointsCount) {
        // Create a list containing integers from 0 to pointsCount - 1
        List<int> indices = new(pointsCount);
        for (int i = 0; i < pointsCount; i++) {
            indices.Add(i);
        }

        HashSet<(int, int)> pairs = new(pointsCount);

        // Iterate until we've connected all of the points (except the last one?)
        while(indices.Count > 1) {
            int start = indices[Random.Range(0, maxExclusive: indices.Count)];
            int end;

            do {
                end = indices[Random.Range(0, maxExclusive: indices.Count)];
            // if start == end or this pair already exists, try again
            } while (start == end || pairs.Contains((start, end)) || pairs.Contains((end, start)));

            pairs.Add((start, end));
            indices.Remove(start);
        }

        return pairs.Select(pair => new ExplosionLine(pair)).ToList();
    }

    // "Rotates" the lines by adding new ones and "removing" (replacing) old ones
    // by taking it from the Queue
    private void RotateOrAddLines() {
        // Should there be a delay from the start of the explosion vs when we start changing / adding the lines? urgh

        float timeBetweenLineAdds = 1f / lineAddRate;
        bool shouldAddLineThisFrame = Time.time - lastTimeAddedLines >= timeBetweenLineAdds;
        
        if (!shouldAddLineThisFrame) {
            return;
        }

        if (linesQueue.Count == 0) { // Populate the queue if it's empty
            List<ExplosionLine> newLines = GetLines(pointsCount: explosionVFXPointsCount);

            foreach(ExplosionLine newLine in newLines) {
                linesQueue.Enqueue(newLine);
            }
        }

        lastTimeAddedLines = Time.time;

        // take one from the top of the queue
        ExplosionLine lineToAdd = linesQueue.Dequeue();
        lineToAdd.timeStart = Time.time;

        int nextIndex = (lastLineIndexAdded + 1) % explosionVFXLinesCount;

        // Ensure it's big enough before we start replacing
        if (nextIndex == currentLines.Count) {
            currentLines.Add(lineToAdd);
        } else {
            // Overwrite
            currentLines[nextIndex] = lineToAdd;
        }

        lastLineIndexAdded = nextIndex;

        // finally, update the buffer
        explosionLinesBuffer!.SetData(currentLines.ToArray());
    }
}

