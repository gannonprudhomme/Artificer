using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Scene Director and Teleporter Boss
public class InstantDirector: CombatDirector {
    protected override float experienceMultipler => 1f / 15f; // 0.06666 repeating;
    protected override (float, float) minAndMaxSpawnDistanceFromPlayer => (50.0f, 300.0f);

    protected override void Start() {
        base.Start();

        int monsterLimit = 30;
        int monstersSpawned = 0;

        float mapBaseCredits = 100.0f;
        // Calculate the initial number of credits
        numCredits = mapBaseCredits * difficultyCoefficient;

        // Spawn the monsters!!!

        // While we haven't run out of credits
        selectedCard = SelectRandomCardWeCanAfford();
        while (selectedCard != null && monstersSpawned <= monsterLimit) {
            Debug.Log($"Instant: Spawning {selectedCard.Value.identifier}");
            // Try to spawn it I guess?
            SpawnEnemy(selectedCard.Value, Target);

            // Select the next one
            selectedCard = SelectRandomCardWeCanAfford();
            monstersSpawned++;
        }
    }

    private EnemyCard? SelectRandomCardWeCanAfford() {
        // First filter out of the cards we can't afford

        List<EnemyCard> cardsWeCanAfford = new();
        foreach (EnemyCard card in enemyCards!) {
            if (numCredits >= card.spawnCost) {
                cardsWeCanAfford.Add(card);
            }
        }

        if (cardsWeCanAfford.Count == 0) {
            return null;
        }

        int randomIndex = new System.Random().Next(0, cardsWeCanAfford.Count);
        return cardsWeCanAfford[randomIndex];
    }
}
