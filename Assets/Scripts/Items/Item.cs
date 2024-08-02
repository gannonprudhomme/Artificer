using UnityEngine;

#nullable enable

// Enum for all possible items
//
// Namely needed so we can map ItemDisplayers to their coresponding Items and vice-versa.
public enum ItemType {
    BACKUP_MAGAZINE,
    ENERGY_DRINK
}

public abstract class Item: ScriptableObject {
    [Header("Item (Base)")]
    [Tooltip("Icon which displays in the user's top bar")]
    public Texture2D? Icon;

    [Tooltip("Mesh prefab which displays when it's dropped")]
    public Mesh? DropModelMesh;

    [Tooltip("Texture which displays on the mesh & is passed to the ItemPickup shader")]
    public Texture2D? MeshTexture;

    // Name of the item, e.g. "Backup Magazine"
    public abstract string itemName { get; }

    // This really needs to be an AttributedString equivalent
    public abstract string description { get; }

    public abstract Rarity rarity { get; }

    public abstract ItemType itemType { get; }

    public virtual void OnUpdate(ItemsDelegate itemsController, int count) { }

    public enum Rarity {
        COMMON, UNCOMMON
    }
}

public interface ItemsDelegate {
    public float ModifiedSprintMultiplier { get; set; }

    public int ModifiedSecondarySpellCharges { get; set; }

    public void PickupItem(Item item);
}


/*
// Will only want one [controlling] instance of this presumably
public class MedkitItem : Item{
    public override string itemName => "Medkit";
    public override string description => "2 seconds after getting hurt, heal for 20 plus an additional 5% (+5% per stack) of maximum health.";
    public override Rarity rarity => Rarity.COMMON;

    // We're going to need an Update() implementation here
    // since we need to heal the player
    // well shit maybe we won't idk
}


// Each instance could increment the jump counts itself
public class HopooFeatherItem: Item {
    public override string itemName => "Hopoo Feather";
    public override string description => "Gain an extra jump. _(+1 per stack)_";
    public override Rarity rarity => Rarity.UNCOMMON;
}

// We'd probably only want one [controlling] instance of this
// and just pass it in the count
public class UkeleleItem: Item { 
    public override string itemName => "Ukelele";
    // Occurs when the player hits an enemy
    public override string description => "25% chance to fire chain lightning for 80% TOTAL damage on up to 3 _(+2 per stack)_ targets within 20m _(+2m per stack)._";
    public override Rarity rarity => Rarity.UNCOMMON;
}
*/