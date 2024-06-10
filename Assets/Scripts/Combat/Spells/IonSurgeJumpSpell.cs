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

    private readonly float explosionVFXLineLifetime = 1.0f; 
    private readonly int numExplosionVFXLineCycles = 4; // Will "flash" 4 times within the VFX lifetime

    // Time between the lines changing in the explosion VFX
    // Determine the delay between each line refresh as a function of the lifetime & number of "cycles"
    private float delayBetweenExplosionVFXLineRefreshes {
        get {
            return explosionVFXLineLifetime / numExplosionVFXLineCycles;
        }
    }

    private float timeOfLastExplosionVFXLineRefresh = Mathf.Infinity; // Don't want to refresh until this is set?
    private readonly int pointsCount = 16;

    // TODO: We should actually calculate this based on the velocity of the player
    // Or rather, how long we expect for it to take for the player to reach the peak of the ion surge jump
    private float animationDuration {
        get {
            float gravityDownForce = 75f;
            // about 1.64 sec (w/ 90 jump force) - SurgeJumpForce / GravityDownForce
            return SurgeJumpForce / gravityDownForce; 
        }
    }

    private void Awake() {
        StopVFX();
    }

    void Update() {
        Recharge();

        bool isActive = Time.time - timeOfLastFire < animationDuration;
        PlayerAnimator!.SetBool("IsIonSurgeActive", isActive);

        if (isActive) {
            bool shouldRegenerateLines = (Time.time - timeOfLastExplosionVFXLineRefresh) > delayBetweenExplosionVFXLineRefreshes;
            if (shouldRegenerateLines) {
                CreateAndPopulateLinesBuffer(numToCreate: pointsCount);
            }
        } else { // if not active, stop VFX
            // We need to do this a bit sooner b/c of the trails bleh
            StopVFX();
        }
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

        CreateAndPopulatePointsBuffer(numToCreate: pointsCount);
        CreateAndPopulateLinesBuffer(numToCreate: pointsCount);
    }

    private void CreateAndPopulateLinesBuffer(int numToCreate) {
        ExplosionLine[] lines = GetLines(pointsCount: numToCreate);

        explosionLinesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lines.Length, Marshal.SizeOf(typeof(ExplosionLine)));
        explosionLinesBuffer.SetData(lines); // pass the data to the graphics buffer
        mainExplosionVFXInstance!.SetGraphicsBuffer("ExplosionLinesBuffer", explosionLinesBuffer);
        mainExplosionVFXInstance!.SetInt("Num Lines", lines.Length);

        timeOfLastExplosionVFXLineRefresh = Time.time;
    }

    private void CreateAndPopulatePointsBuffer(int numToCreate) {
        ExplosionPoint[] points = GetPoints(count: numToCreate, sphereRadius: ExplosionVFXRadius);

        explosionPointsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, points.Length, Marshal.SizeOf(typeof(ExplosionPoint)));
        explosionPointsBuffer.SetData(points); // pass the data to the graphics buffer
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

        if (explosionPointsBuffer != null) {
        // This is problematic cause sometimes all of the pairs are already in there
            explosionPointsBuffer!.Release();
            explosionPointsBuffer = null;
        }

        if (explosionLinesBuffer != null) {
            explosionLinesBuffer!.Release();
            explosionLinesBuffer = null;
        }
    }


    // TODO: https://stackoverflow.com/a/44164075 Might want to do this so it's more of an even distribution?
    // Or this https://www.mathworks.com/matlabcentral/answers/457965-how-to-generate-random-points-located-on-the-surface-of-a-hemisphere-with-its-center-at-2-1-3-an
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
