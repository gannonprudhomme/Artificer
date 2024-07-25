using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public class PlayerItemsController : MonoBehaviour, ItemsDelegate {
    [HideInInspector]
    private float _modifiedSprintMultiplier = 0;
    [HideInInspector]
    private int _modifiedSecondarySpellCharges = 0;

    public Dictionary<string, List<Item>> items = new();

    public UnityAction<Item, int>? OnItemPickedUp;

    public float ModifiedSprintMultiplier {
        get => _modifiedSprintMultiplier;
        set => _modifiedSprintMultiplier = value;
    }

    public int ModifiedSecondarySpellCharges {
        get => _modifiedSecondarySpellCharges;
        set => _modifiedSecondarySpellCharges = value;
    }

    void Update() {
        ResetValues();

        foreach(var (itemName, itemList) in items) {
            int count = itemList.Count;

            Item item = itemList[0];

            item.OnUpdate(this, count: count);
        }
    }

    public void PickupItem(Item item) {
        List<Item> list = items.GetValueOrDefault(item.itemName, new List<Item>());
        list.Add(item);
        items[item.itemName] = list; // Why do we have to do this? I thought it was a reference

        Debug.Log($"Picked up {item.itemName}, now have {list.Count}");

        OnItemPickedUp?.Invoke(item, list.Count);
    }

    private void ResetValues() {
        _modifiedSecondarySpellCharges = 0;
        _modifiedSprintMultiplier = 0;
    }
}
