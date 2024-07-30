using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#nullable enable

class InteractableCard {
    public readonly string identifier;
    public readonly int spawnCost;
    public readonly SpawnableInteractable prefab;
    private readonly int spawnLimit;

    // Turned it into a class for this; I figured this was simpler than storing it in e.g. a dictionary
    public int numberSpawned = 0;

    public InteractableCard(string identifier, int spawnCost, int spawnLimit, SpawnableInteractable prefab) {
        this.identifier = identifier;
        this.spawnCost = spawnCost;
        this.prefab = prefab;
        this.spawnLimit = spawnLimit;
    }

    public bool HasReachedSpawnLimit() {
        return numberSpawned >= spawnLimit;
    }
}

public class InteractablesDirector : MonoBehaviour {

    [Header("Player/Level references")]
    public Target? Target;
    public GoldWallet? GoldWallet;
    public GameObject? Level;

    [Header("Interactables")]
    public ItemChest? ItemChestPrefab;
    public Barrel? BarrelPrefab;

    private InteractableCard[]? interactableCards;

    private int numCredits = 0;

    private void Awake() {
        interactableCards = new InteractableCard[] {
            new(
                identifier: "ItemChest",
                spawnCost: 15,
                spawnLimit: 45,
                prefab: ItemChestPrefab!
            ), // 15 is for a small chest
            new(
                identifier: "Barrel",
                spawnCost: 1,
                spawnLimit: 10,
                BarrelPrefab!
            )
        };
    }

    void Start() {
        numCredits = 220; // Depends on level, but we'll set it for Titanic Planes

        InteractableCard? selectedCard = GetRandomAffordableAndSpawnableCard();
        while (numCredits > 0 && selectedCard != null) {
            SpawnInteratable(selectedCard);

            selectedCard.numberSpawned++;
            numCredits -= selectedCard.spawnCost;

            selectedCard = GetRandomAffordableAndSpawnableCard();
        }

        // Done spawning
    }

    private void SpawnInteratable(InteractableCard interactableCard) {
        // I guess we can query the NavMesh?

        if (!TryFindRandomSpawnPosition(Level!, out Vector3 spawnPosition, out Vector3 spawnNormal)) {
            Debug.LogError($"Couldn't spawn {interactableCard.identifier}");
            return;
        }


        // Align the interactable w/ the normal of the surface we hit + add a random rotation
        Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);

        SpawnableInteractable spawnable = Instantiate(interactableCard.prefab, position: Vector3.zero, rotation: Quaternion.identity);
        spawnable.transform.position = spawnPosition + spawnable.GetSpawnPositionOffset();
        spawnable.transform.rotation = normalRotation * spawnable.GetSpawnRotationOffset();

        if (spawnable is ItemChest itemChest) {
            itemChest.SetUp(
                costToPurchase: interactableCard.spawnCost,
                target: Target!,
                goldWallet: FindObjectOfType<GoldWallet>() // This is awful but fuck it whatever
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

        return affordableAndSpawnable[Random.Range(0, affordableAndSpawnable.Count)];
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
