using UnityEngine;

#nullable enable

// This collects _all_ of the ice wall spikes
public class IceWall : MonoBehaviour {
    [Tooltip("Prefab of the ice wall spike we spawn")]
    public IceWallSpike? IceWallSpikePrefab;

    [Tooltip("Layers this can collide with")]
    public LayerMask HittableLayers = -1;

    [Tooltip("The layer mask of the level - used for raycasting to place the ice spikes correctly")]
    public LayerMask levelMask;

    // Going to need to sync w/ the decal somehow
    private float totalIceWallWidth = 12f * 4.5f;
    // How long we wait between spawning each ice wall spike
    private float delayBetweenIceWallSpawns = 0.05f;

    public float DamagePerSpike { get; set; }

    private IceWallSpike[]? iceSpikes;
    private readonly int totalIceSpikes = 12;

    private int numIceSpikesSpawned = 0;
    private float timeOfLastSpikeSpawn = Mathf.NegativeInfinity;

    void Start() {
        iceSpikes = new IceWallSpike[totalIceSpikes];
    }

    void Update() {
        // Check if we should destroy this instance (all of the ice wall spikes have detonated)

        // Check if we need to spawn another ice wall spike pair
        if (numIceSpikesSpawned < totalIceSpikes && Time.time - timeOfLastSpikeSpawn > delayBetweenIceWallSpawns) {
            SpawnIceSpikePair();
        }
    }

    private void SpawnIceSpikePair() {
        int spawnIndex = numIceSpikesSpawned / 2;

        Debug.Log($"Spawning ice spikes at index {spawnIndex}");


        // Spawn one in negative
        IceWallSpike negativeDirSpike = CreateAndPlaceIceSpike(pairSpawnIndex: spawnIndex, isNegative: true);
        iceSpikes![numIceSpikesSpawned] = negativeDirSpike;

        // Spawn one in positive
        IceWallSpike positiveDirSpike = CreateAndPlaceIceSpike(pairSpawnIndex: spawnIndex, isNegative: false);
        iceSpikes![numIceSpikesSpawned + 1] = negativeDirSpike;

        numIceSpikesSpawned += 2;
        timeOfLastSpikeSpawn = Time.time;
    }

    private IceWallSpike CreateAndPlaceIceSpike(int pairSpawnIndex, bool isNegative) {
        IceWallSpike iceSpike = Instantiate(IceWallSpikePrefab!, parent: transform);
        iceSpike.damage = DamagePerSpike;

        // 0.5f is b/c we have an even amount - so the first pair spawns correctly & not on top of each other
        float spawnPosition = (totalIceWallWidth / totalIceSpikes) * (pairSpawnIndex + 0.5f);

        // First, determine horizontal position

        // Note we should be using Vector3.left/.right, but the IceWall instance is spawned rotated 90 degrees
        // than what it should be, and I'm too lazy to fix it b/c this works.
        Vector3 direction = isNegative ? Vector3.back : Vector3.forward;
        iceSpike.transform.localPosition = direction * spawnPosition;

        // Determine vertical position

        // Raycast from the middle of the top face of the box collider
        Vector3 rayCastPos = iceSpike.transform.position + (Vector3.up * 10f);
        Debug.DrawRay(rayCastPos, Vector3.down, Color.blue, 2.0f);

        // Raycast down
        if (Physics.Raycast(
            origin: rayCastPos,
            direction: Vector3.down,
            out RaycastHit hit,
            maxDistance: 100f,
            layerMask: levelMask
        )) {
            Debug.DrawLine(rayCastPos, hit.point, Color.green, 2.0f);
            iceSpike.transform.position = hit.point - (Vector3.up * 0.2f);
        } else {
            Debug.LogError($"ice spike raycast didn't hit anything for index: {pairSpawnIndex + (isNegative ? 0 : 1)}");
        }

        // Randomize the scale
        float scale = Random.Range(0.9f, 1.1f);
        iceSpike.transform.localScale = new Vector3(scale, scale, scale);

        // Randomly rotate it a bit by a few degrees only forward/left right, not up/down
        iceSpike.transform.Rotate(Vector3.right, Random.Range(-4f, 4f));
        iceSpike.transform.Rotate(Vector3.forward, Random.Range(-4f, 4f));

        return iceSpike;
    }
}
