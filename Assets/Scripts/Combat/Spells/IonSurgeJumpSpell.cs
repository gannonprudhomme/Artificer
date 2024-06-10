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
    }
}

public class IonSurgeJumpSpell : Spell {
    public override float ChargeRate => 1f / 8f; // 8s cooldown...?
    public override int MaxNumberOfCharges => 1;
    public override Color SpellColor => Color.blue;
    public override bool DoesBlockOtherSpells => false;
    public override bool IsBlockedByOtherSpells => false;

    [Header("General (Ion Surge)")]
    public float SurgeJumpForce = 90f;

    [Tooltip("VFX instance for the left hand")]
    public VisualEffect? LeftHandVFX;

    [Tooltip("VFX instance for the right hand")]
    public VisualEffect? RightHandVFX;

    [Tooltip("VFX prefab for the explosion")]
    public VisualEffect? MainExplosionVFXPrefab;

    // We create an instance as the world/local space conversion gets complicated
    // since we change the lines positions within the lifetime of the explosion VFX
    private VisualEffect? mainExplosionVFXInstance;

    public float ExplosionVFXRadius = 15f;

    private GraphicsBuffer? explosionPointsBuffer;
    private GraphicsBuffer? explosionLinesBuffer;

    private float timeOfLastFire = Mathf.NegativeInfinity;

    // TODO: We should actually calculate this based on the velocity of the player
    // Or rather, how long we expect for it to take for the player to reach the peak of the ion surge jump
    private float animationDuration {
        get {
            float gravityDownForce = 75f;
            // about 1.64 sec (w/ 90 jump force) - SurgeJumpForce / GravityDownForce
            return SurgeJumpForce / gravityDownForce;
        }
    }

    // TODO: Make these private readonly when I've found a good value
    private readonly explosionVFXLineLifetime = 1.0f; 
    private readonly int numExplosionVFXLineCycles = 4; // Will "flash" 4 times within the VFX lifetime

    // Time between the lines changing in the explosion VFX
    // Determine the delay between each line refresh as a function of the lifetime & number of "cycles"
    private float delayBetweenExplosionVFXLineRefreshes {
        get {
            return explosionVFXLineLifetime / numExplosionVFXLineCycles;
        }
    }

    private float timeOfLastExplosionVFXLineRefresh = Mathf.Infinity; // Don't want to refresh until this is set?
    // How many "points" there are in the explosion VFX
    private readonly int explosionVFXPointsCount = 16;
    private int explosionVFXLinesCount => explosionVFXPointsCount - 1;

    private int currentLineMoveIndex = 0;
    private ExplosionLine[]? currentLines = null;
    private ExplosionLine[]? nextLines = null;

    private void Start() {
        StopVFX();

        // Initialize the GraphicsBuffers for the points and lines
        // This assumes that they're constant (which they should be)
        explosionLinesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, explosionVFXLinesCount, Marshal.SizeOf(typeof(ExplosionLine)));
        explosionPointsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, explosionVFXPointsCount, Marshal.SizeOf(typeof(ExplosionPoint)));
    }

    private void Update() {
        Recharge();

        bool isActive = Time.time - timeOfLastFire < animationDuration;
        PlayerAnimator!.SetBool("IsIonSurgeActive", isActive);
        if (!isActive) { // if not active, stop VFX
            // We need to do this a bit sooner b/c of the trails bleh
            StopVFX();
        }

        bool isExplosionLinesActive = (Time.time - timeOfLastFire) < explosionVFXLineLifetime;
        if (isExplosionLinesActive) {
            MigrateLinesToNextSet();
        }
    }

    // really don't know if I need to do this or not but w/e doesn't hurt
    private void OnDestroy() {
        if (explosionPointsBuffer != null) {
            explosionPointsBuffer!.Release();
        }

        if (explosionLinesBuffer != null) {
            explosionLinesBuffer!.Release();
        }
    }

    // Called in OnUpdate
    // TODO: Move to the end
    private void MigrateLinesToNextSet() {
        // TODO: I'm a fucking idiot just use a queue!
        // We should have a minimum amount of lines we should be displaying at once, namely at the beginning so its sort of pre-baked (and at the end?)
        // Though I still haven't decided 

        int prevLineMoveIndex = currentLineMoveIndex;

        float percentCurrentCycleCompleted = (Time.time - timeOfLastExplosionVFXLineRefresh) / delayBetweenExplosionVFXLineRefreshes;

        // Do we want to do this at the top or the bottom? I feel like the bottom? But idfk floats + frametime makes this weird cause we totally could miss some
        // We're out of lines - on to the next!
        bool isCycleCompleted = percentCurrentCycleCompleted >= 1.0f;
        if (isCycleCompleted) {
            currentLines = nextLines;
            nextLines = GetLines(pointsCount: explosionVFXPointsCount);

            timeOfLastExplosionVFXLineRefresh = Time.time;
            percentCurrentCycleCompleted = 0f; // Reset it back to 0 since that's what we just did anyways
        }

        currentLineMoveIndex = (int) (percentCurrentCycleCompleted * explosionVFXLinesCount);

        // Find the lines that were "newly added" this loop and update their time field
        if (prevLineMoveIndex != currentLineMoveIndex) { // Don't want to do it if we just did it!
            // We can basically always assume it's going to be in the next array
            // We should also get how many of them were added rather than just the one
            ExplosionLine copy = nextLines![currentLineMoveIndex];
            copy.timeStart = Time.time;
            nextLines![currentLineMoveIndex] = copy;
        }

        // If I was smart I wouldn't need this List - I could just pick & choose from each of them and combine
        // using an array I create to pass into explosionsLineBuffer but bleh
        List<ExplosionLine> lines = new(currentLines!.Length + nextLines!.Length);
        lines.AddRange(currentLines!);
        lines.AddRange(nextLines!);

        List<ExplosionLine> slidingWindow = lines.GetRange(currentLineMoveIndex, explosionVFXLinesCount);
        explosionLinesBuffer!.SetData(slidingWindow.ToArray()) ;
    }

    private void Recharge() {
        CurrentCharge += ChargeRate * Time.deltaTime;

        CurrentCharge = Mathf.Clamp(CurrentCharge, 0f, MaxNumberOfCharges);
    }

    public override void ShootSpell(
        (Vector3 leftArm, Vector3 rightArm) muzzlePositions,
        GameObject owner,
        Camera spellCamera,
        float currDamage,
        LayerMask layerToIgnore
    ) {
        CurrentCharge -= 1;

        // Launch the player in the air
        UpdatePlayerVelocity?.Invoke(Vector3.up * SurgeJumpForce);

        timeOfLastFire = Time.time;

        StopVFX(); // Temporary for testing! (since I'm spamming it);
        PlayVFX();
    }

    public override void AttackButtonPressed() { }
    public override void AttackButtonHeld() { }
    public override void AttackButtonReleased() { }

    public override bool CanShoot() { // This is basically AttackButtonHeld() lol
        return CurrentCharge >= MaxNumberOfCharges;
    }

    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera spellCamera) {
        return true;
    }

    public override Texture2D? GetAimTexture() {
        return null;
    }

    private void PlayVFX() {
        LeftHandVFX!.enabled = true;
        RightHandVFX!.enabled = true;

        LeftHandVFX!.Play();
        RightHandVFX!.Play();

        mainExplosionVFXInstance = Instantiate(MainExplosionVFXPrefab!); // Spawn it in world space
        mainExplosionVFXInstance!.SetFloat("Explosion Radius", ExplosionVFXRadius);
        mainExplosionVFXInstance!.SetFloat("Line+Points Lifetime", explosionVFXLineLifetime);
        mainExplosionVFXInstance!.transform.position = transform.position;

        mainExplosionVFXInstance!.Play();

        CreateAndPopulatePointsBuffer(numToCreate: explosionVFXPointsCount);
        CreateAndPopulateLinesBuffer(numToCreate: explosionVFXPointsCount);
    }

    private void CreateAndPopulateLinesBuffer(int numToCreate) {
        ExplosionLine[] lines = GetLines(pointsCount: numToCreate);

        currentLines = lines;
        nextLines = GetLines(pointsCount: numToCreate); // Get another set
        currentLineMoveIndex = 0; // Reset

        mainExplosionVFXInstance!.SetGraphicsBuffer("ExplosionLinesBuffer", explosionLinesBuffer);
        mainExplosionVFXInstance!.SetInt("Num Lines", lines.Length);

        timeOfLastExplosionVFXLineRefresh = Time.time;
    }

    private void CreateAndPopulatePointsBuffer(int numToCreate) {
        ExplosionPoint[] points = GetPoints(count: numToCreate, sphereRadius: ExplosionVFXRadius);

        explosionPointsBuffer!.SetData(points); // pass the data to the graphics buffer
        mainExplosionVFXInstance!.SetGraphicsBuffer("ExplosionPointsBuffer", explosionPointsBuffer);
        mainExplosionVFXInstance!.SetInt("Num Points", points.Length);
    }

    private void StopVFX() {
        LeftHandVFX!.enabled = false;
        RightHandVFX!.enabled = false;

        LeftHandVFX!.Stop();
        RightHandVFX!.Stop();

        // We won't really need to do this since lifetime will handle it but w/e
        if (mainExplosionVFXInstance != null) { // Called on Awake() so have to check this
            mainExplosionVFXInstance!.Stop();
            Destroy(mainExplosionVFXInstance.gameObject);
            mainExplosionVFXInstance = null;
        }
    }


    private static ExplosionPoint[] GetPoints(int count, float sphereRadius = 30f) {
        ExplosionPoint[] ret = new ExplosionPoint[count];

        List<Vector3> randomPoints = GeneratePointsOnHemisphere(count: count, radius: sphereRadius);

        // Select == Collection.map in C#
        return randomPoints.Select(point => new ExplosionPoint(point)).ToArray();
    }

    // Generates N evenly distributed points on a hemisphere, with a bit of randomness thrown in
    // This uses a Fibonacci lattice so the points are evenly distributed (though idk what that is lol)
    private static List<Vector3> GeneratePointsOnHemisphere(int count, float radius) {
        List<Vector3> points = new();
        float phi = (1 + Mathf.Sqrt(5)) / 2; // Golden ratio

        float randomnessFactor = 0.1f;

        for (int i = 0; i < count; i++) {
            // float theta = 2 * Mathf.PI * i / phi; // Longitude - normal
            float theta = 2 * Mathf.PI * (i / phi + randomnessFactor * (Random.value - 0.5f)); // Adds randomness
            float cosTheta = Mathf.Cos(theta);
            float sinTheta = Mathf.Sin(theta);

            // Latitude using adapted Fibonacci lattice for hemisphere

            // float latitude = Mathf.Acos(1f - (i + 0.5f) / N); // Normal (hemisphere) - no randomness
            // This latitude is how it changes for a hemipshere
            float latitude = Mathf.Acos(1 - (i + 0.5f) / count + randomnessFactor * (Random.value - 0.5f)); // Adds randomness
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
    private static ExplosionLine[] GetLines(int pointsCount) {
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

        return pairs.Select(pair => new ExplosionLine(pair)).ToArray();
    }
}
