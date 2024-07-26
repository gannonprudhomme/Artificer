using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#nullable enable

// This should really be like "Purchasable" or something
[RequireComponent(typeof(Animator))]
public class ItemChest : Interactable {
    [Header("Item Chest")]
    public UIFollowPlayer? UIFollowPlayer;

    [Tooltip("Text used for setting the cost of the chest")]
    public TextMeshProUGUI? CostText;

    [Tooltip("Animator for the chest")]
    public Animator? Animator; // Animator for the chest

    // TODO: We need a better way of passing this. Maybe in SetUp()?
    // Set by the Player, but ideally it'd be set by the Director when we spawn this
    public GoldWallet? GoldWallet { get; set; }

    [Tooltip("All items we can pick frmo")]
    public AllItems? AllItems;

    [Tooltip("Prefab we spawn when the chest is opened")]
    public ItemPickup? ItemPickupPrefab;

    [Tooltip("Layer mask for the level. Used to determine where to spawn the item")]
    public LayerMask LevelLayerMask;

    // I think the Scene Director sets this?
    private int costToPurchase = 5; // Temp default value

    private const string ANIM_IS_OPEN = "IsOpen";
    // Called when the Director spawns this
    public void SetUp(int costToPurchase, Target target, GoldWallet goldWallet) { // We could just pass in a Transform
        this.costToPurchase = costToPurchase;
        UIFollowPlayer!.Target = target.AimPoint;
        GoldWallet = goldWallet;
    }

    // Start is called before the first frame update
    protected override void Start() {
        base.Start();

        // we need to get the Player / Target somehow and set it for UIFollowPlayer
        CostText!.text = $"${costToPurchase}";

        Animator!.SetBool(ANIM_IS_OPEN, false);
    }

    // Update is called once per frame
    void Update() {
        // Handle animation when it's opened probably

        // Once animation is done, drop the item
        if (GoldWallet != null) {
            // Set the color of the text depending on if we can afford this or not
            foreach (Material material in GetMaterials()) {
                material.SetInt(SHADER_OUTLINE_COLOR_FLIP, GoldWallet.CanAfford(costToPurchase) == true ? 1 : 0);
            }
        }
    }

    public override void OnSelected(GoldWallet _, ItemsDelegate itemsDelegate) {
        if (hasBeenInteractedWith) return; // Don't do anything if it's already been opened

        // Note below spends the gold when it returns true
        if (!GoldWallet!.SpendGoldIfPossible(costToPurchase)) {
            // Don't do anything?
            return;
        }
        
        foreach(Material material in GetMaterials()) {
            material.SetInt(SHADER_OUTLINE_IS_ENABLED, 0);
        }

        hasBeenInteractedWith = true;

        // Hide the text (and never show it again)
        CostText!.enabled = false;

        Animator!.SetBool(ANIM_IS_OPEN, true);

        ItemPickup itemPickup = Instantiate(ItemPickupPrefab!, transform.position, Quaternion.identity);
        itemPickup.startPosition = transform.position;
        itemPickup.endPosition = DetermineItemSpawnPosition();
        itemPickup.item = PickItemToDrop();

        // We spent it, do the other shit

        // Start the animation

        // Play VFX

        // Play audio

        // For now, drop the item
    }

    public override void OnHover() {
        base.OnHover();

        HoverEvent!.OnHover("Open chest", costToPurchase);
    }

    public override void OnNearby() {
        if (hasBeenInteractedWith) return; // Don't show the text if it's already been opened

        // Show the price if relevant 
        CostText!.enabled = true;
    }

    public override void OnNotNearby() {
        CostText!.enabled = false;
    }

    private Vector3 DetermineItemSpawnPosition() {
        // TODO: Add retry logic
        if (Physics.Raycast(
            origin: transform.position + (transform.forward * 5f) + (Vector3.up * 10),
            direction:  Vector3.down,
            out RaycastHit hit,
            maxDistance: 10f
            // TODO: level mask should just be the level
        )) {
            return hit.point;
        } else {
            Debug.LogError("Couldn't find position for item!");
            return transform.position + (transform.forward * 5f);
        }
    }

    private Item PickItemToDrop() {
        return AllItems!.CommonItems[Random.Range(0, AllItems!.CommonItems.Count)];
    }
}
