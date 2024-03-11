using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fast, Slow, and Teleporter directors
public abstract class ContinuousDirector: CombatDirector {
    protected override (float, float) minAndMaxSpawnDistanceFromPlayer => (10.0f, 70.0f);

    // Constant value. Is the same for Fast and Slow director
    private const float creditMultipler = 0.75f;

    // If we failed to spawn an enemy (i.e. not enough credits), we'll set this to know when we can spawn again
    private float timeOfNextSpawnAttempt = Mathf.NegativeInfinity;

    // 0.1f - 1f for both 
    // protected abstract (float, float) minAndMaxSuccessSpawnTime { get; }
    protected (float, float) minAndMaxSuccessSpawnTime => (0.1f, 1.0f);

    protected abstract (float, float) minAndMaxFailureSpawnTime { get; }

    // How many credits we generate per second
    private float creditsPerSecond {
        get {
            float playerCount = 1;
            return (creditMultipler * (1 + 0.4f * difficultyCoefficient) * (playerCount + 1) ) / 2f;
        }
    }

    protected override void Update() {
        base.Update();

        HandleSpawnLoop();

        // Only Continuous Directors generate credits;
        GenerateCredits();
    }

    protected void HandleSpawnLoop() {
        if (Time.time < timeOfNextSpawnAttempt) { // We haven't waited long enough to try again
            // We can't spawn anything; don't move forward
            return;
        }

        // Select a card if we don't have one selected right now
        if (selectedCard == null) {
            selectedCard = SelectRandomEnemyCard();
            // Debug.Log($"Selected card: {((EnemyCard) selectedCard).identifier} with {numCredits} credits");
        }

        EnemyCard _selectedCard = (EnemyCard) selectedCard; // Idk why it won't let me force unwrap
        if (CanSpawnSelectedCard(_selectedCard)) {
            // Debug.Log($"Attempting to spawn: {_selectedCard.identifier} for {_selectedCard.spawnCost} with {numCredits} credits");
            SpawnEnemy(_selectedCard, target: Target);
            // Spawn succeeded - keep this card (though above can technically fail ack)

            // numCredits -= _selectedCard.spawnCost;

            // Pick a time to spawn another monster??
            // This will be smaller interval than if we fail

            float minSuccesSpawnTime = minAndMaxSuccessSpawnTime.Item1;
            float maxSuccessSpawnTime = minAndMaxSuccessSpawnTime.Item2; 
            float randTimeToWait = minSuccesSpawnTime + (Random.value * (maxSuccessSpawnTime - minSuccesSpawnTime));

            timeOfNextSpawnAttempt = Time.time + (randTimeToWait);

            // Debug.Log($"Spawn succeeded! Waitng {randTimeToWait}s");

        } else { // Spawn failed
            // We only re-select a card next frame
            selectedCard = null;

            float minFailureSpawnTime = minAndMaxFailureSpawnTime.Item1;
            float maxFailureSpawnTime = minAndMaxFailureSpawnTime.Item2;

            float randTimeToWait = minFailureSpawnTime + (Random.value * (maxFailureSpawnTime - minFailureSpawnTime)); // Range of [minFailureSpawnTime, maxFailureSpawnTime]
            timeOfNextSpawnAttempt = Time.time + randTimeToWait;

            // Debug.Log($"Spawn failed! Waiting {randTimeToWait} seconds until spawning!");
        }
    }

    
    private void GenerateCredits() {
        numCredits += creditsPerSecond * Time.deltaTime;
    }
    private EnemyCard SelectRandomEnemyCard() {
        int randomIndex = new System.Random().Next(0, enemyCards!.Length);
        return enemyCards![randomIndex];
    }
}

