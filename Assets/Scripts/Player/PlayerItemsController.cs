using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

public class PlayerItemsController : MonoBehaviour, ItemsDelegate {
    public Dictionary<string, List<Item>> items = new();

    public UnityAction<Item, int> OnItemPickedUp;

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
        items[item.itemName] = list;

        Debug.Log($"Picked up {item.itemName}, now have {list.Count}");

        OnItemPickedUp?.Invoke(item, list.Count);
    }

    private void ResetValues() {
    }
}
