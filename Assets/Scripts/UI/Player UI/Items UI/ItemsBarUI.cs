using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class ItemsBarUI : MonoBehaviour {
    [Tooltip("Reference to items controller so we can get all of the current items")]
    public PlayerItemsController? ItemsController;

    [Tooltip("Prefab we use for making more Item UIs when we pick up items")]
    public ItemUI? ItemUIPrefab;

    // Store a mapping to the UI elements so we can update their count when we pick up
    private Dictionary<string, ItemUI> itemUIs = new();

    private void Start() {
        ItemsController!.OnItemPickedUp += OnItemPickedUp;
    }

    private void OnItemPickedUp(Item item, int count) {
        if (count == 1) { // First one, create a new one
            CreateItemUI(item);
        } else { // Otherwise, update it
            ItemUI? itemUI = itemUIs[item.itemName];

            itemUI!.ItemCount = count;
        }
    }

    private void CreateItemUI(Item item) {
        ItemUI itemUI = Instantiate(ItemUIPrefab!, transform);
        itemUI.Item = item;
        itemUI.ItemCount = 1;

        itemUIs.Add(item.itemName, itemUI);
    }
}
