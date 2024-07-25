using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#nullable enable

public abstract class Item: ScriptableObject {
    [Header("Item (Base)")]
    [Tooltip("Icon which displays in the user's top bar")]
    public Texture2D? Icon;

    [Tooltip("Mesh prefab which displays when it's dropped")]
    public Mesh? DropModelMesh;

    // Name of the item, e.g. "Backup Magazine"
    public abstract string itemName { get; }

    // This really needs to be an AttributedString equivalent
    public abstract string description { get; }

    public abstract Rarity rarity { get; }

    public virtual void OnUpdate(ItemsDelegate itemsController, int count) { }

    public enum Rarity {
        COMMON, UNCOMMON
    }
}

public interface ItemsDelegate {
    public void PickupItem(Item item);
}

