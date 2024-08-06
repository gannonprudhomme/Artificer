using UnityEngine;
using UnityEngine.VFX;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/Items/HopooFeather")]
public class HopooFeather : Item {
    [Header("Hopoo Feather")]
    [Tooltip("VFX prefab which is spawned when the user jumps")]
    public VisualEffect? OnJumpVFXPrefab;

    public override string itemName => "Hopoo Feather";
    public override ItemType itemType => ItemType.HOPOO_FEATHER;

    public override string description => "Gain +1 (+1 per stack) maximum jump count.";

    public override Rarity rarity => Rarity.UNCOMMON;

    public override void OnUpdate(ItemsDelegate itemsController, int count) {
        itemsController!.ModifiedNumberOfJumps += count;
    }

    public override void OnJump(bool wasGrounded, Transform vfxSpawnTransform) {
        if (wasGrounded) {
            Debug.Log("Was Grounded - not doing anything");
            return;
        }

        // Spawn the VFX (and destroy it after some time)
        VisualEffect onJumpVFX = Instantiate(
            OnJumpVFXPrefab!,
            position: vfxSpawnTransform.position,
            rotation: Quaternion.identity
        );

        float lifetime = 1.0f;
        Destroy(onJumpVFX.gameObject, lifetime);
    }
}
