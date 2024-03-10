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
// This is considered a Continuous Director
public abstract class CombatDirector: MonoBehaviour { 
    public StoneGolem? StoneGolemPrefab;
    public Lemurian? LemurianPrefab;
    public Wisp? WispPrefab;
    public Target Target;

    // EnemyManager is where the list of enemies will live.
    // We probably don't need to do this, but could serve as a dependency injection entrypoint?
    private EnemyManager enemyManager = EnemyManager.shared; 

    // But it would be nice to just have a single place to change these values, so I'll do this for now 
    private EnemyCard[]? enemyCards;

    // Constant value. Is the same for Fast and Slow director
    private const float creditMultipler = 0.75f;

    // Idk where we're going to get this from
    // We need some form of (global?) (readonly) level state

    // Probably move this into a GameManager class?
    private float difficultyCoefficient {
        get {
            int playerCount = 1;
            float playerFactor = 1 + 0.3f * (playerCount - 1);

            float difficultyValue = 2.0f; // 1 for Drizzle, 2 for Rainstorm, 3 for Monsoon
            float timeFactor = 0.0506f * difficultyValue * (float) Mathf.Pow(playerCount, 0.2f);

            int stagesCompleted = 4;
            float stageFactor = (float)Mathf.Pow(1.15f, stagesCompleted);

            float tempTestingTimePlayed = 5.0f; // add 5 extra minutes
            float timeInMinutes = (Time.time / 60.0f) + tempTestingTimePlayed;

            return (playerFactor + timeInMinutes * timeFactor) * stageFactor;
        }
    }

    // How many credits we generate per second
    private float creditsPerSecond {
        get {
            float playerCount = 1;
            return (creditMultipler * (1 + 0.4f * difficultyCoefficient) * (playerCount + 1) ) / 2f;
        }
    }

    private const float minSpawnDistanceFromPlayer = 5.0f;
    private const float maxSpawnDistanceFromPlayer = 70.0f;

    // How many credits currently have to spawn something
    private float numCredits = 0.0f;

    // This WILL be null when we don't have a card selected
    private EnemyCard? selectedCard = null;

    // If we failed to spawn an enemy (i.e. not enough credits), we'll set this to know when we can spawn again
    private float timeOfNextSpawnAttempt = Mathf.NegativeInfinity;

    /*** Abstract properties ***/

    // 0.75f for Continuous, 1.0f for Instanteous
    protected abstract float creditMultiplers { get; }

    // 0.1f - 1f for both 
    // protected abstract (float, float) minAndMaxSuccessSpawnTime { get; }
    protected (float, float) minAndMaxSuccessSpawnTime => (0.1f, 1.0f);

    protected abstract (float, float) minAndMaxFailureSpawnTime { get; }
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
                    spawnCost: 10f,
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

    protected virtual void Update() {
        HandleSpawnLoop();
    }

    private void HandleSpawnLoop() {
        if (Time.time < timeOfNextSpawnAttempt) { // We haven't waited long enough to try again
            // We can't spawn anything; don't move forward
            return;
        }

        // Select a card if we don't have one selected right now
        if (selectedCard == null) {
            selectedCard = SelectRandomEnemyCard();
            Debug.Log($"Selected card: {((EnemyCard) selectedCard).identifier} with {numCredits} credits");
        }

        EnemyCard _selectedCard = (EnemyCard) selectedCard; // Idk why it won't let me force unwrap
        if (CanSpawnSelectedCard(_selectedCard)) {
            Debug.Log($"Attempting to spawn: {_selectedCard.identifier} for {_selectedCard.spawnCost} with {numCredits} credits");
            SpawnEnemy(_selectedCard, target: Target);
            // Spawn succeeded - keep this card (though above can technically fail ack)

            numCredits -= _selectedCard.spawnCost;

            // Pick a time to spawn another monster??
            // This will be smaller interval than if we fail

            float minSuccesSpawnTime = minAndMaxSuccessSpawnTime.Item1;
            float maxSuccessSpawnTime = minAndMaxSuccessSpawnTime.Item2; 
            float randTimeToWait = minSuccesSpawnTime + (Random.value * (maxSuccessSpawnTime - minSuccesSpawnTime));
            timeOfNextSpawnAttempt = Time.time + (randTimeToWait);

            Debug.Log($"Spawn succeeded! Waitng {randTimeToWait}s");

        } else { // Spawn failed
            didLastSpawnFail = true;
            // We only re-select a card next frame
            selectedCard = null;

            float minFailureSpawnTime = minAndMaxFailureSpawnTime.Item1;
            float maxFailureSpawnTime = minAndMaxFailureSpawnTime.Item2;

            float randTimeToWait = minFailureSpawnTime + (Random.value * (maxFailureSpawnTime - minFailureSpawnTime)); // Range of [minFailureSpawnTime, maxFailureSpawnTime]
            timeOfNextSpawnAttempt = Time.time + randTimeToWait;

            Debug.Log($"Spawn failed! Waiting {randTimeToWait} seconds until spawning!");
        }
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
                Debug.LogError("Not spawning a flying enemy!!");
                return;
            }
            
        } else { // It's grounded
            if (FindGroundedSpawnPosition(playerPosition: target.AimPoint.position, enemyCard.identifier, out Vector3 result)) {
                spawnPosition = result;
            } else {
                Debug.LogError("Not spawning a grounded enemy!!");
                return;
            } 
        }

        Debug.Log($"Spawning {enemyCard.identifier} at {spawnPosition} from player pos {target.AimPoint.position}");

        Enemy enemy = Instantiate(enemyCard.prefab, spawnPosition, Quaternion.identity);
        enemy.transform.position = spawnPosition;
        enemy.Target = Target;
        // Calculate HP based on Tier? We don't need to do this (yet)
        // Give it "Boost" items to apply HP & damage multiplers???
        // Set the xp reward for killing this monster
        // set the gold reward for killing this monster
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
        numCredits += creditsPerSecond * Time.deltaTime;
    }

    private bool CanSpawnSelectedCard(EnemyCard card) {
        return numCredits >= card.spawnCost;
    }

    private EnemyCard SelectRandomEnemyCard() {
        int randomIndex = new System.Random().Next(0, enemyCards!.Length);
        return enemyCards![randomIndex];
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
    public float GetCreditsPerSecond() { return creditsPerSecond; }
}
