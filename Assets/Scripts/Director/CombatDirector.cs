using UnityEngine;
using UnityEngine.AI;

#nullable enable

// Only for this file really
public readonly struct EnemyCard {
    public readonly float spawnCost;
    // public readonly float weight; // don't think we need, they're all 1
    // public readonly string category; // probs don't need

    // Needs to match up with the NavMeshAgent name
    // (for grounded enemeis ofc)
    public readonly string identifier;

    public readonly bool isFlyingEnemy;

    public readonly Enemy prefab;

    private const float minimumSpawnDistanceFromPlayer = 0.0f;
    private const float maximumSpawnDistanceFromPlayer = 1.0f;

    // public readonly Enemies enemyType;

    // somehow we have to define a way to spawn these things

    public EnemyCard(
        string identifier,
        float spawnCost,
        bool isFlyingEnemy,
        Enemy prefab
    ) {
        this.spawnCost = spawnCost;
        this.identifier = identifier;
        this.isFlyingEnemy = isFlyingEnemy;
        this.prefab = prefab;
    }
}


// Each level will have its own instance of a combat director
// Will probably want to make this conform to some abstract class
// 
// Not sure if we want this to be a MonoBehavior, but I suppose it might as well
// (well if we have an array of directors something else will need to be)
//
// This is considered a Continuous Directorprivate
public abstract class CombatDirector: MonoBehaviour { 
    public StoneGolem? StoneGolemPrefab;
    public Lemurian? LemurianPrefab;
    public Wisp? WispPrefab;
    public Target? Target;

    // EnemyManager is where the list of enemies will live.
    // We probably don't need to do this, but could serve as a dependency injection entrypoint?
    private EnemyManager enemyManager = EnemyManager.shared!; 

    // But it would be nice to just have a single place to change these values, so I'll do this for now 
    protected EnemyCard[]? enemyCards;

    private const int playerCount = 1;

    private float playerFactor {
        get {
            return 0.7f + (0.3f * playerCount);
        }
    }

    // Idk where we're going to get this from
    // We need some form of (global?) (readonly) level state
    // Probably move this into a GameManager class?
    protected float difficultyCoefficient {
        get {
            float difficultyValue = 2.0f; // 1 for Drizzle, 2 for Rainstorm, 3 for Monsoon
            float timeFactor = 0.0506f * difficultyValue * (float) Mathf.Pow(playerCount, 0.2f);

            int stagesCompleted = 0; // TODO: Get this from some GameManager
            float stageFactor = Mathf.Pow(1.15f, stagesCompleted);

            float tempTestingTimePlayed = 0.0f; // add 5 extra minutes
            float timeInMinutes = (Time.time / 60.0f) + tempTestingTimePlayed;

            return (playerFactor + timeInMinutes * timeFactor) * stageFactor;
        }
    }

    private float enemyLevel {
        get {
            return 1 + ((difficultyCoefficient - playerFactor) / 0.33f);
        }
    }

    protected abstract float experienceMultipler { get; }
    protected abstract (float, float) minAndMaxSpawnDistanceFromPlayer { get; }

    // How many credits currently have to spawn something
    // Director State
    protected float numCredits = 0.0f;

    // This WILL be null when we don't have a card selected
    protected EnemyCard? selectedCard = null;


    /*** Abstract properties ***/

    // 0.75f for Continuous, 1.0f for Instanteous
    // protected abstract float creditMultiplers { get; }
    
    private void Awake() {
        if (StoneGolemPrefab != null && LemurianPrefab != null) {
            enemyCards = new EnemyCard[] {
                new(
                    identifier: "Stone Golem",
                    spawnCost: 40.0f,
                    isFlyingEnemy: false,
                    prefab: StoneGolemPrefab
                ),
                new(
                    identifier: "Lemurian",
                    spawnCost: 11f,
                    isFlyingEnemy: false,
                    prefab: LemurianPrefab
                ),
                new(
                    identifier: "Wisp",
                    spawnCost: 10f * 2f,
                    isFlyingEnemy: true,
                    prefab : WispPrefab!
                )
            };
        }
    }

    protected virtual void Start() {
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null) {
            Debug.LogError("CombatDirector couldn't find EnemyManager!");
        }
    }

    protected virtual void Update() {
        // HandleSpawnLoop();
    }


    protected void SpawnEnemy(EnemyCard enemyCard, Target target) {
        Vector3 spawnPosition;

        if (enemyCard.isFlyingEnemy) {
            // Ok but I like actually need a reference to the graph for this enemy
            // for this
            // I guess for now I'll just get it
            Graph? enemyGraph = OctreeManager.shared!.Graph;
            if (enemyGraph != null && FindFlyingSpawnPosition(playerPosition: target.AimPoint!.position, enemyGraph, out Vector3 result)) {
                spawnPosition = result;
            } else {
                // Debug.LogError("Not spawning a flying enemy!!");
                return;
            }
            
        } else { // It's grounded
            if (FindGroundedSpawnPosition(playerPosition: target.AimPoint!.position, enemyCard.identifier, out Vector3 result)) {
                spawnPosition = result;
            } else {
                // Debug.LogError("Not spawning a grounded enemy!!");
                return;
            } 
        }


        Enemy enemy = Instantiate(enemyCard.prefab, spawnPosition, Quaternion.identity);
        enemy.transform.position = spawnPosition;
        enemy.Target = Target;
        enemy.Level = enemyLevel;
        // Set the XP reward for killing this monster
        // This also sets gold granted on death (which is 2x experience granted on death)
        enemy.ExperienceGrantedOnDeath = (int) (difficultyCoefficient * enemyCard.spawnCost * experienceMultipler);

        // Debug.Log($"Spawning {enemyCard.identifier} with experience to be granted of {enemy.ExperienceGrantedOnDeath}");

        numCredits -= enemyCard.spawnCost;

        // Give it "Boost" items to apply HP & damage multiplers???
    }

    // Pick a point a random distance from the player on the Nav Mesh
    //
    // Really not sure how it knows which NavMesh to check from
    private bool FindGroundedSpawnPosition(Vector3 playerPosition, string enemyIdentifier, out Vector3 result) {
        // NavMeshQueryFilter filter = new();
        // filter.agentTypeID = GetAgentTypeIDByName(agentName: enemyIdentifier);

        int i; // just for debugging
        for (i = 0; i < 10; i++) {
            float maxSpawnDistanceFromPlayer = minAndMaxSpawnDistanceFromPlayer.Item2;

            // we don't want to go too high; the vertical axis should be limited
            Vector3 randomPoint = playerPosition + Random.insideUnitSphere * maxSpawnDistanceFromPlayer;
            float agentHeight = 3.0f;

            // wait which fucking nav mesh is this search I'm so confused
            float distanceToPlayer = Vector3.Distance(playerPosition, randomPoint);

            float minSpawnDistanceFromPlayer = minAndMaxSpawnDistanceFromPlayer.Item1;
            if (distanceToPlayer >= minSpawnDistanceFromPlayer &&
                NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, agentHeight * 2.0f, NavMesh.AllAreas)
            ) {
                // Debug.Log($"Found a position for {enemyIdentifier} {distanceToPlayer}m away!");
                result = hit.position;
                return true;
            }

            Debug.DrawLine(randomPoint, playerPosition, Color.red, 2.0f);

            // Debug.Log($"Try {i+1} for {enemyIdentifier} didn't work w/ pos {randomPoint} and player pos {playerPosition}, trying again");
        }

        // Debug.LogError($"Failed to find a position after {i+1} iterations for {enemyIdentifier}");

        result = Vector3.negativeInfinity;
        return false;
    }

    private bool FindFlyingSpawnPosition(Vector3 playerPosition, Graph graph, out Vector3 result) {
        int i;
        for(i = 0; i < 5; i++) {
            // TODO: This should only be for horizontal
            // we shouldn't spawn the Wisp too high in the air - it should be a only a bit above the ground
            float maxSpawnDistanceFromPlayer = minAndMaxSpawnDistanceFromPlayer.Item2;
            Vector3 randomPosition = playerPosition + Random.insideUnitSphere * maxSpawnDistanceFromPlayer;

            // Honestly we should be checking if:
            // 1. We're inside of an existing OctreeNode / the one we're in isn't marked out of bounds
            // 2. If the (smallest) Octreenode we're in contains a collision
            // but we don't have access to the Octree :( so brute forcing it is!
            GraphNode? nearestNode = graph.FindNearestToPosition(randomPosition);

            if (nearestNode == null) {
                Debug.LogError($"Couldn't find nearest node to position {randomPosition}");
                continue;
            }

            float minSpawnDistanceFromPlayer = minAndMaxSpawnDistanceFromPlayer.Item1;
            float distToNodeFromPlayer = Vector3.Distance(playerPosition, nearestNode.center);
            if (distToNodeFromPlayer >= minSpawnDistanceFromPlayer && distToNodeFromPlayer <= maxSpawnDistanceFromPlayer) {
                // Debug.Log($"Found a position for the wisp {distToNodeFromPlayer}m away!");
                result = nearestNode.center;
                return true;
            }

            //. Debug.Log($"Try {i + 1} for Wisp didn't work w/ pos {randomPosition} and nearest node {nearestNode.center} and player pos {playerPosition}, trying again");
        }

        // Debug.LogError("Failed to find a position after {i+1} iterations for Wisp");

        result = Vector3.negativeInfinity;
        return false;
    }


    // Instanteous doesn't use this currently
    protected bool CanSpawnSelectedCard(EnemyCard card) {
        return numCredits >= card.spawnCost;
    }

    private bool CanSpawnAnything() {
        foreach (EnemyCard card in enemyCards!) {
            if (numCredits >= card.spawnCost) {
                return true;
            }
        }

        return false;
    }
    
    // Public getters just for CombatDirectorEdtitor
    public EnemyCard? GetSelectedCard() { return selectedCard; }
    public float GetNumCredits() { return numCredits; }
    public float GetDifficultyCoefficient() { return difficultyCoefficient; }
    // public float GetCreditsPerSecond() { return creditsPerSecond; }
}
