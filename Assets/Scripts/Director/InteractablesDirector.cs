using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#nullable enable

class InteractableCard {
    public readonly string identifier;
    public readonly int spawnCost;
    public readonly int weight;
    public readonly int? baseGoldCost;
    public readonly Spawnable prefab;
    private readonly int spawnLimit;

    // Turned it into a class for this; I figured this was simpler than storing it in e.g. a dictionary
    public int numberSpawned = 0;

    public InteractableCard(string identifier, int spawnCost, int spawnLimit, int weight, int? baseGoldCost, Spawnable prefab) {
        this.identifier = identifier;
        this.spawnCost = spawnCost;
        this.weight = weight;
        this.prefab = prefab;
        this.baseGoldCost = baseGoldCost;
        this.spawnLimit = spawnLimit;
    }

    public bool HasReachedSpawnLimit() {
        return numberSpawned >= spawnLimit;
    }
}

public sealed class InteractablesDirector : MonoBehaviour {

    [Header("Player/Level references")]
    public GameObject? Level;

    [Header("Interactables")]
    public ItemChest? ItemChestPrefab;
    public Barrel? BarrelPrefab;
    public MultishopTerminal? MultishopTerminalPrefab;

    private Target? Target;
    private GoldWallet? GoldWallet;
    private InteractableCard[]? interactableCards;

    // TODO: Pass this in
    private const float difficultyCoefficient = 1f; // Difficulty coefficient at the start.

    private int numCredits = 0;

    private void Awake() {
        interactableCards = new InteractableCard[] {
            new(
                identifier: "ItemChest",
                spawnCost: 15,
                spawnLimit: 45, // TODO: I'm not even sure if spawn limit is a thing
                weight: 24,
                baseGoldCost: 25,
                prefab: ItemChestPrefab!
            ), // 15 is for a small chest
            new(
                identifier: "Barrel",
                spawnCost: 1,
                spawnLimit: 10,
                weight: 10,
                baseGoldCost: null,
                BarrelPrefab!
            ),
            new(
                identifier: "MultishopTerminal",
                spawnCost: 20, 
                spawnLimit: 10,
                weight: 8,
                baseGoldCost: 25,
                MultishopTerminalPrefab!
            )
        };
    }

    private IEnumerator Start() {
        while (PlayerController.instance == null) {
            yield return null;
        }
        Target = PlayerController.instance!.target;
        GoldWallet = PlayerController.instance!.goldWallet;
        
        numCredits = 220; // Depends on level, but we'll set it for Titanic Planes

        InteractableCard? selectedCard = GetRandomAffordableAndSpawnableCard();
        while (numCredits > 0 && selectedCard != null) {
            SpawnInteractable(selectedCard);

            selectedCard.numberSpawned++;
            numCredits -= selectedCard.spawnCost;

            selectedCard = GetRandomAffordableAndSpawnableCard();
        }

        // Done spawning
    }

    private void SpawnInteractable(InteractableCard interactableCard) {
        // I guess we can query the NavMesh?

        if (!TryFindRandomSpawnPosition(Level!, out Vector3 spawnPosition, out Vector3 spawnNormal)) {
            Debug.LogError($"Couldn't spawn {interactableCard.identifier}");
            return;
        }


        // Align the interactable w/ the normal of the surface we hit + add a random rotation
        Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);

        MonoBehaviour spawnable = Instantiate(interactableCard.prefab.Prefab, position: Vector3.zero, rotation: Quaternion.identity);

        if (spawnable is Spawnable _spawnable) { // It always will be
            spawnable.transform.position = spawnPosition + _spawnable.GetSpawnPositionOffset();
            spawnable.transform.rotation = normalRotation * _spawnable.GetSpawnRotationOffset();
        }

        int baseGoldCost = -1;
        if (interactableCard.baseGoldCost != null) {
            baseGoldCost = interactableCard.baseGoldCost.Value;
        }

        int costToPurchase = (int)(baseGoldCost * Mathf.Pow(difficultyCoefficient, 1.25f));

        // Booo reflection - code smell!
        if (spawnable is ItemChest itemChest) {
            itemChest.SetUp(
                costToPurchase: costToPurchase,
                target: Target!,
                goldWallet: GoldWallet!
            );
        } else if (spawnable is MultishopTerminal multishopTerminal) {
            multishopTerminal.SetUp(
                costToPurchase: costToPurchase,
                target: Target!,
                goldWallet: GoldWallet!
            );
        }
    }

    private bool TryFindRandomSpawnPosition(GameObject level, out Vector3 position, out Vector3 normal) {
        Bounds bounds = GetBoundsFrom(obj: level);

        for(int i = 0; i < 300; i++) { // It frequently takes 100 tries to get this right
            Vector3 randPosition = new(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            float agentHeight = 3.0f;

            if (NavMesh.SamplePosition(randPosition, out NavMeshHit hit, agentHeight * 2f, NavMesh.AllAreas)) {
                position = hit.position;

                if (GetNormalFromNavMeshHit(navMeshHit: hit, out Vector3 surfaceNormal)) {
                    normal = surfaceNormal;

                    return true;
                } else {
                    Debug.LogError($"Couldn't get surface normal for successful NavMesh SamplePosition");
                }
            } 
        }

        position = Vector3.negativeInfinity;
        normal = Vector3.negativeInfinity;
        return false;
    }

    private static Bounds GetBoundsFrom(GameObject obj) {
        // This gets renderers from this GameObject(Component), as well as it's children recursively
        Renderer[] renderers = obj.transform.GetComponentsInChildren<Renderer>();
        // if (renderers.Count == 0) return new Bounds
        Bounds bounds = renderers[0].bounds;
        // Debug.Log($"Bounds: {bounds}");
        foreach(var renderer in renderers) {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;

    }
    
    private InteractableCard? GetRandomAffordableAndSpawnableCard() {
        List<InteractableCard> affordableAndSpawnable = new();
        foreach(InteractableCard card in interactableCards!) {
            if (numCredits >= card.spawnCost && !card.HasReachedSpawnLimit()) {
                affordableAndSpawnable.Add(card);
            }
        }

        if (affordableAndSpawnable.Count == 0) {
            return null;
        }

        return GetRandomWeightedCard(affordableAndSpawnable);
    }

    // Gets a random card from the list of cards, weighted by the weight of each card
    //
    // In actuality I should be using the "Alias Method", but I have max of 3 cards so I'm just doing whatever is easiest
    //
    // Source: https://softwareengineering.stackexchange.com/a/15064
    private static InteractableCard GetRandomWeightedCard(List<InteractableCard> cards) {
        int totalWeight = 0;
        InteractableCard selectedCard = cards[0];

        foreach(InteractableCard card in cards) {
            int weight = card.weight;
            int rand = Random.Range(0, totalWeight + weight);

            if (rand >= totalWeight) { // probability of this is weight/(totalWeight+weight)
                selectedCard = card; // it is the probability of discarding last selected element and selecting current one instead
            }

            totalWeight += weight;
        }

        return selectedCard;
    }

    private bool GetNormalFromNavMeshHit(NavMeshHit navMeshHit, out Vector3 normal) {
        if (Physics.Raycast(
            origin: navMeshHit.position + (Vector3.up * 1f),
            direction: Vector3.down,
            out RaycastHit hit
        )) {
            normal = hit.normal;
            return true;
        }

        normal = Vector3.negativeInfinity;
        return false;
    }
}
