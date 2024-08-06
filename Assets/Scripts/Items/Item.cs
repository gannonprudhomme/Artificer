using UnityEngine;

#nullable enable

// Enum for all possible items
//
// Namely needed so we can map ItemDisplayers to their coresponding Items and vice-versa.
public enum ItemType {
    BACKUP_MAGAZINE,
    ENERGY_DRINK,
    HOPOO_FEATHER
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

    public virtual void OnJump(bool wasGrounded, Transform spawnTransform) { }

    public enum Rarity {
        COMMON, UNCOMMON
    }
}

public interface ItemsDelegate {
    public float ModifiedSprintMultiplier { get; set; }

    public int ModifiedSecondarySpellCharges { get; set; }

    public int ModifiedNumberOfJumps { get; set; }

    public void PickupItem(Item item);
}

