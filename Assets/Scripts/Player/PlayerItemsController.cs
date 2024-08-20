using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public class PlayerItemsController : MonoBehaviour, ItemsDelegate {
    [Tooltip("ScriptableObject which we use to subscribe to OnEnemyKilled events.")]
    public OnEnemyKilledEvent? OnEnemyKilledEvent;

    [Tooltip("Reference to all of the Item Displayers. Will output errors if it's misconfigured.")]
    public List<ItemDisplayer> ItemDisplayers = new();

    // TODO: Should I just do a abstract class so I don't have to do this BS?
    [HideInInspector]
    private float _modifiedSprintMultiplier = 0;
    [HideInInspector]
    private int _modifiedSecondarySpellCharges = 0;
    [HideInInspector]
    private int _modifiedNumberOfJumps = 0;

    // We have to store the count here since Item is a ScriptableObject
    // and increasing instance values on it will increase permanently - downside to ScriptableObjects!
    // And we don't want to do a List as the value since there's literally no point (it'd be a bunch of the same reference anyways)
    private Dictionary<ItemType, (Item, int)> items = new();

    // Maps ItemTypes to their displayer.
    // Used to quickly lookup the corresponding ItemDisplayer for an item whenever we pick one up.
    private Dictionary<ItemType, ItemDisplayer> itemDisplayerDict = new(); 

    // For ItemPickupUI
    public UnityAction<Item, int>? OnItemPickedUp;

    private PlayerController? playerController;

    public float ModifiedSprintMultiplier {
        get => _modifiedSprintMultiplier;
        set => _modifiedSprintMultiplier = value;
    }

    public int ModifiedSecondarySpellCharges {
        get => _modifiedSecondarySpellCharges;
        set => _modifiedSecondarySpellCharges = value;
    }

    public int ModifiedNumberOfJumps {
        get => _modifiedNumberOfJumps;
        set => _modifiedNumberOfJumps = value;
    }

    private void Awake() {
        CheckAllItemDisplayersPresentAndNotDuplicated();
        HideAllItemDisplayers();

        itemDisplayerDict = BuildItemDisplayersDict();

        playerController = GetComponent<PlayerController>();
        playerController.OnJumped += OnPlayerJumped;

        OnEnemyKilledEvent!.Event += OnEnemyKilled;
    }

    private void Update() {
        ResetModifiers();

        foreach(var (itemName, itemCountTuple) in items) {
            var (item, count) = itemCountTuple;

            item.OnUpdate(this, itemCount: count);
        }
    }

    public void PickupItem(Item item) {
        var (currItem, currCount) = items.GetValueOrDefault(item.itemType, (item, 0));
        currCount += 1;
        items[item.itemType] = (currItem, currCount);

        OnItemPickedUp?.Invoke(item, currCount);

        itemDisplayerDict[item.itemType].gameObject.SetActive(true);
    }

    private void ResetModifiers() {
        _modifiedSecondarySpellCharges = 0;
        _modifiedSprintMultiplier = 0;
        _modifiedNumberOfJumps = 0;
    }

    private Dictionary<ItemType, ItemDisplayer> BuildItemDisplayersDict() {
        Dictionary<ItemType, ItemDisplayer> dict = new();

        foreach(ItemDisplayer itemDisplayer in ItemDisplayers) {
            dict[itemDisplayer.CorrespondingItemType] = itemDisplayer;
        }

        return dict;
    }

    private void CheckAllItemDisplayersPresentAndNotDuplicated() {
        HashSet<ItemType> remainingItemTypes = new();
        // Populate it w/ all item types to start
        ItemType[] allCases = (ItemType[]) Enum.GetValues(typeof(ItemType)); // lmfao, thanks C#.
        foreach(ItemType itemType in allCases) {
            remainingItemTypes.Add(itemType);
        }

        string duplicatedTypes = "";
        foreach(ItemDisplayer itemDisplayer in ItemDisplayers) {
            // Removing also works as a way to check for missing ones
            bool wasFound = remainingItemTypes.Remove(itemDisplayer.CorrespondingItemType);

            if (!wasFound) {
                duplicatedTypes += itemDisplayer.CorrespondingItemType.ToString() + ", ";
            }
        }

        if (!duplicatedTypes.Equals("")) {
            Debug.LogError($"ItemDisplayers has duplicated: {duplicatedTypes}");
        }

        // Check for missing
        if (remainingItemTypes.Count > 0) {
            string missingTypes = "";
            foreach(ItemType itemType in remainingItemTypes) {
                missingTypes += itemType.ToString() + ", ";
            }

            Debug.LogError($"ItemDisplayers is missing: {missingTypes}");
        }
    }

    private void HideAllItemDisplayers() {
        foreach(ItemDisplayer itemDisplayer in ItemDisplayers) {
            itemDisplayer.gameObject.SetActive(false);
        }
    }

    private void OnPlayerJumped(bool wasGrounded, Transform spawnTransform) {
        foreach(var (item, _) in items.Values) {
            // We just have to assume this won't fail
            item.OnJump(
                wasGrounded: wasGrounded,
                spawnTransform: spawnTransform
            );
        }
    }

    public void OnAttackHitEntity(float playerBaseDamage, Entity entityHit) {
        foreach(var (item, count) in items.Values) {
            item.OnEnemyHit(playerBaseDamage, entityHit, itemCount: count);
        }
    }

    private void OnEnemyKilled(Vector3 killedEnemyPosition) {
        foreach(var (item, count) in items.Values) {
            item.OnEnemyKilled(
                killedEnemyPosition,
                playerBaseDamage: playerController!.CurrentBaseDamage,
                itemCount: count
            );
        }
    }
}
