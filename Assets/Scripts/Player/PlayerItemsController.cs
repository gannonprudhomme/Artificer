using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public class PlayerItemsController : MonoBehaviour, ItemsDelegate {
    public List<ItemDisplayer> ItemDisplayers = new();

    // TODO: Should I just do a abstract class so I don't have to do this BS?
    [HideInInspector]
    private float _modifiedSprintMultiplier = 0;
    [HideInInspector]
    private int _modifiedSecondarySpellCharges = 0;

    // We need to build one of these but w/ Transforms
    public Dictionary<ItemType, List<Item>> items = new();

    // Maps ItemTypes to their displayer.
    // Used to quickly lookup the corresponding ItemDisplayer for an item whenever we pick one up.
    public Dictionary<ItemType, ItemDisplayer> itemDisplayerDict = new(); 

    public UnityAction<Item, int>? OnItemPickedUp;

    public float ModifiedSprintMultiplier {
        get => _modifiedSprintMultiplier;
        set => _modifiedSprintMultiplier = value;
    }

    public int ModifiedSecondarySpellCharges {
        get => _modifiedSecondarySpellCharges;
        set => _modifiedSecondarySpellCharges = value;
    }

    private void Awake() {
        CheckAllItemDisplayersPresentAndNotDuplicated();
        HideAllItemDisplayers();

        itemDisplayerDict = BuildItemDisplayersDict();
    }

    private void Update() {
        ResetModifiers();

        foreach(var (itemName, itemList) in items) {
            int count = itemList.Count;

            Item item = itemList[0];

            item.OnUpdate(this, count: count);
        }
    }

    public void PickupItem(Item item) {
        List<Item> list = items.GetValueOrDefault(item.itemType, new List<Item>());
        list.Add(item);
        items[item.itemType] = list; // Why do we have to do this? I thought it was a reference

        OnItemPickedUp?.Invoke(item, list.Count);

        itemDisplayerDict[item.itemType].gameObject.SetActive(true);
    }

    private void ResetModifiers() {
        _modifiedSecondarySpellCharges = 0;
        _modifiedSprintMultiplier = 0;
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
}
