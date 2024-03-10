using UnityEngine;
using UnityEngine.AI;

#nullable enable

// Each level will have its own instance of a combat director
// Will probably want to make this conform to some abstract class
// 
// Not sure if we want this to be a MonoBehavior, but I suppose it might as well
// (well if we have an array of directors something else will need to be)
//
// This is considered a Continuous Director
public class CombatDirector: MonoBehaviour { 
    public StoneGolem? StoneGolemPrefab;
    public Lemurian? LemurianPrefab;
    public Wisp? WispPrefab;

    public Target Target;

    private const float minSpawnDistanceFromPlayer = 5.0f;
    private const float maxSpawnDistanceFromPlayer = 70.0f;

    public enum Enemies {
        StoneGolem, Lemurian, Wisp
    }

    public readonly struct EnemyCard { // I assume we'll want readonly
        public readonly float cost;
        // public readonly float weight; // don't think we need, they're all 1
        // public readonly string category; // probs don't need

        // Needs to match up with the NavMeshAgent name
        // (for grounded enemeis ofc)
        public readonly string identifier;

        public readonly bool isFlyingEnemy;

        public readonly Enemy prefab;

        // public readonly Enemies enemyType;

        // somehow we have to define a way to spawn these things

        public EnemyCard(string identifier, float cost, bool isFlyingEnemy, Enemy prefab) {
            this.cost = cost;
            this.identifier = identifier;
            this.isFlyingEnemy = isFlyingEnemy;
            this.prefab = prefab;
        }
    }

    // Should this a property on the Enemy? Probably
    // But it would be nice to just have a single place to change these values, so I'll do this for now 
    private EnemyCard[]? enemyCards;
    // EnemyManager is where the list of enemies will live.
    // We probably don't need to do this, but could serve as a dependency injection entrypoint?
    private EnemyManager enemyManager = EnemyManager.shared; 

    // How many credits currently have to spawn something
    private float numCredits = 0.0f;
    private void Awake() {
        if (StoneGolemPrefab != null && LemurianPrefab != null) {
        enemyCards = new EnemyCard[] {
                new(
                    identifier: "Stone Golem",
                    cost: 3.0f,
                    isFlyingEnemy: false,
                    prefab: StoneGolemPrefab
                ),
                new(
                    identifier: "Lemurian",
                    cost: 1.0f,
                    isFlyingEnemy: false,
                    prefab: LemurianPrefab
                ),
                new(
                    identifier: "Wisp",
                    cost: 1.0f,
                    isFlyingEnemy: true,
                    prefab : WispPrefab!
                )
            };
        }
    }

    private void Start() {
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) {
            Debug.LogError("CombatDirector couldn't find EnemyManager!");
        }
    }

    float timeOfLastSpawn = Mathf.NegativeInfinity;
    const float timeBetweenSpawns = 2.5f;
    private void Update() {
        // For designing this out for now I'm just going to spawn a random one every 5? seconds

        if (Time.time - timeOfLastSpawn >= timeBetweenSpawns) {
            // Spawn something
            // We're assuming these names match up with their NavMesh agent names
            int numEnemies = enemyCards!.Length;
            int randomIndex = new System.Random().Next(0, numEnemies);

            if (randomIndex >= 0 && randomIndex < numEnemies) {
                EnemyCard enemyCard = enemyCards[randomIndex];
                Debug.Log($"Picked {enemyCard.identifier} ({randomIndex})");

                SpawnEnemy(enemyCard, target: Target);
                
            } else {
                Debug.LogError($"out of bounds {randomIndex}");
            }
            timeOfLastSpawn = Time.time;
        }

        GenerateCredits();
    }

    private void SpawnEnemy(EnemyCard enemyCard, Target target) {
        Vector3 spawnPosition;

        if (enemyCard.isFlyingEnemy) {
            // Ok but I like actually need a reference to the graph for this enemy
            // for this
            // I guess for now I'll just get it
            Graph? enemyGraph = enemyManager.WispGraph;
            if (enemyGraph != null && FindFlyingSpawnPosition(playerPosition: target.AimPoint.position, enemyGraph, out Vector3 result)) {
                spawnPosition = result;
            } else {
                return;
            }
            
        } else { // It's grounded
            if (FindGroundedSpawnPosition(playerPosition: target.AimPoint.position, enemyCard.identifier, out Vector3 result)) {
                spawnPosition = result;
            } else {
                return;
            } 
        }

        Debug.Log($"Spawning {enemyCard.identifier} at {spawnPosition} from player pos {target.AimPoint.position}");

        // find the corresponding nav mesh
        // Enemy enemy;
        // if (enemyCard.identifier == "Lemurian") {
            // Apparently I can't spawn a lemurian the normal way but this still seems to work
        // } else {
            // enemy = Instantiate(enemyCard.prefab, spawnPosition, Quaternion.identity);
        // }

        Enemy enemy = Instantiate(enemyCard.prefab, spawnPosition, Quaternion.identity);
        enemy.transform.position = spawnPosition;
        enemy.Target = Target;
    }

    // Pick a point a random distance from the player on the Nav Mesh
    //
    // Really not sure how it knows which NavMesh to check from
    private bool FindGroundedSpawnPosition(Vector3 playerPosition, string enemyIdentifier, out Vector3 result) {
        // NavMeshQueryFilter filter = new();
        // filter.agentTypeID = GetAgentTypeIDByName(agentName: enemyIdentifier);

        int i; // just for debugging
        for (i = 0; i < 10; i++) {
            Vector3 randomPoint = playerPosition + Random.insideUnitSphere * maxSpawnDistanceFromPlayer;
            float agentHeight = 3.0f;

            // wait which fucking nav mesh is this search I'm so confused
            float distanceToPlayer = Vector3.Distance(playerPosition, randomPoint);

            if (distanceToPlayer >= minSpawnDistanceFromPlayer &&
                NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, agentHeight * 2.0f, NavMesh.AllAreas)
            ) {
                Debug.Log($"Found a position for {enemyIdentifier} {distanceToPlayer}m away!");
                result = hit.position;
                return true;
            }

            Debug.DrawLine(randomPoint, playerPosition, Color.red);

            Debug.Log($"Try {i+1} for {enemyIdentifier} didn't work w/ pos {randomPoint} and player pos {playerPosition}, trying again");
        }

        Debug.LogError($"Failed to find a position after {i+1} iterations for {enemyIdentifier}");

        result = Vector3.negativeInfinity;
        return false;
    }

    private bool FindFlyingSpawnPosition(Vector3 playerPosition, Graph graph, out Vector3 result) {
        int i;
        for(i = 0; i < 5; i++) {
            Vector3 randomPosition = playerPosition + Random.insideUnitSphere * maxSpawnDistanceFromPlayer;

            // Honestly we should be checking if:
            // 1. We're inside of an existing OctreeNode / the one we're in isn't marked out of bounds
            // 2. If the (smallest) Octreenode we're in contains a collision
            // but we don't have access to the Octree :( so brute forcing it is!
            GraphNode nearestNode = graph.FindNearestToPosition(randomPosition);

            float distToNodeFromPlayer = Vector3.Distance(playerPosition, nearestNode.center);
            if (distToNodeFromPlayer >= minSpawnDistanceFromPlayer && distToNodeFromPlayer <= maxSpawnDistanceFromPlayer) {
                Debug.Log($"Found a position for the wisp {distToNodeFromPlayer}m away!");
                result = nearestNode.center;
                return true;
            }

            Debug.Log($"Try {i + 1} for Wisp didn't work w/ pos {randomPosition} and nearest node {nearestNode.center} and player pos {playerPosition}, trying again");
        }

        Debug.LogError("Failed to find a position after {i+1} iterations for Wisp");

        result = Vector3.negativeInfinity;
        return false;
    }

    private void GenerateCredits() {

    }
}
