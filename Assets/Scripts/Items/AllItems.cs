using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Container ScriptableObject so we can find all of the objects we have
[CreateAssetMenu(menuName = "ScriptableObjects/AllItems")]
public class AllItems : ScriptableObject {
    // We shouldn't even need to do this - we could just filter this by Priority
    public List<Item> CommonItems = new();

    public List<Item> UncommonItems = new();

    [Tooltip("When set, only drops this")]
    public Item? OnlyItem = null;

    public Item PickItem() {
        if (OnlyItem != null) {
            return OnlyItem;
        }

        Item.Rarity rarity = PickRandomRarity();
        List<Item> rarityList = rarity switch {
            Item.Rarity.COMMON => CommonItems,
            Item.Rarity.UNCOMMON => UncommonItems,
            _ => throw new System.Exception("Default case hit!"),
        };

        return rarityList[Random.Range(0, rarityList.Count)];
    }

    // Technically this isn't how RoR2 does drops - but it should be equivalent (and it's easier)
    private Item.Rarity PickRandomRarity() {
        float randomNumber = Random.Range(0.0f, 1.0f);

        // [0, 79.2) -> Common
        // [79.2, 99) -> Uncommon
        // [99, 100] -> Rare
        Debug.Log($"Item drop was {randomNumber}");

        if (randomNumber < 0.792f) {
            return Item.Rarity.COMMON;
        } else if (randomNumber < 0.99f) {
            return Item.Rarity.UNCOMMON;
        } else {
            // TODO: Return rare
            Debug.Log("Should have dropped a rare!");
            return Item.Rarity.UNCOMMON;
        }
    }
}
