using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.VFX;
using UnityEngine.Splines;

#nullable enable

// This should really be like "Purchasable" or something
[RequireComponent(typeof(Animator))]
public class ItemChest : SpawnableInteractable {
    [Header("Item Chest")]
    public UIFollowPlayer? UIFollowPlayer;

    [Tooltip("Text used for setting the cost of the chest")]
    public TextMeshProUGUI? CostText;

    [Tooltip("Animator for the chest")]
    public Animator? Animator; // Animator for the chest

    [Header("VFX")]
    [Tooltip("VFX which plays when we actually open the chest")]
    public VisualEffect? OpenChestVFXInstance;

    [Tooltip("Left VFX which plays when we first interact with the chest")]
    public VisualEffect? LeftInteractionVFXInstance;
    [Tooltip("Right VFX which plays when we first interact with the chest")]
    public VisualEffect? RightInteractionVFXInstance;

    [Tooltip("Contains the path for the left interaction vfx")]
    public SplineContainer? LeftInteractionVFXSplineContainer;
    [Tooltip("Contains the path for the right interaction vfx")]
    public SplineContainer? RightInteractionVFXSplineContainer;

    [Header("Item")]
    [Tooltip("All items we can pick frmo")]
    public AllItems? AllItems;

    [Tooltip("Prefab we spawn when the chest is opened")]
    public ItemPickup? ItemPickupPrefab;

    [Tooltip("Layer mask for the level. Used to determine where to spawn the item")]
    public LayerMask LevelLayerMask;

    // TODO: We need a better way of passing this. Maybe in SetUp()?
    // Set by the Player, but ideally it'd be set by the Director when we spawn this
    public GoldWallet? GoldWallet { get; set; }

    // I think the Scene Director sets this?
    private int costToPurchase = 5; // Temp default value

    private float timeOfInteract = Mathf.NegativeInfinity;
    private readonly float interactVFXDuration = 0.5f;
    // Whether we've finished this interact VFX & have played the open VFX (and spawned the item)
    private bool hasOpened = false;

    private const string ANIM_HAS_INTERACTED = "HasInteracted";
    private const string ANIM_HAS_OPENED = "HasOpened";

    // Called when the Director spawns this
    public void SetUp(int costToPurchase, Target target, GoldWallet goldWallet) { // We could just pass in a Transform
        this.costToPurchase = costToPurchase;
        UIFollowPlayer!.Target = target.AimPoint;
        GoldWallet = goldWallet;
    }

    protected override void Start() {
        base.Start();

        // we need to get the Player / Target somehow and set it for UIFollowPlayer
        CostText!.text = $"${costToPurchase}";

        Animator!.SetBool(ANIM_HAS_OPENED, false);
        Animator!.SetBool(ANIM_HAS_INTERACTED, false);

        LeftInteractionVFXInstance!.SetFloat("Lifetime", interactVFXDuration);
        RightInteractionVFXInstance!.SetFloat("Lifetime", interactVFXDuration);
    }

    void Update() {
        // Handle animation when it's opened probably

        // Once animation is done, drop the item
        if (GoldWallet != null) {
            // Set the color of the text depending on if we can afford this or not
            foreach (Material material in GetMaterials()) {
                material.SetInt(SHADER_OUTLINE_COLOR_FLIP, GoldWallet.CanAfford(costToPurchase) == true ? 1 : 0);
            }
        }

        // If we've interacted with it, check if we should play the actual open animation & spawn the item
        if (hasBeenInteractedWith && !hasOpened) {
            bool isDoneWithInteractVFX = Time.time - timeOfInteract > interactVFXDuration;
            if (isDoneWithInteractVFX) {
                DoOpen();
            } else {
                HandleInteractionVFX();
            }
        }
    }

    public override void OnSelected(GoldWallet _, Experience __, Transform ___, ItemsDelegate itemsDelegate) {
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
        timeOfInteract = Time.time;
        hasOpened = false;

        LeftInteractionVFXInstance!.Play();
        RightInteractionVFXInstance!.Play();

        // Hide the text (and never show it again)
        CostText!.enabled = false;

        // Start the animation
        Animator!.SetBool(ANIM_HAS_INTERACTED, true);

        // Play audio
    }

    public override void OnHover() {
        base.OnHover();

        if (!hasBeenInteractedWith) {
            HoverEvent!.OnHover("Open chest", costToPurchase);
        }
    }

    public override void OnNearby() {
        if (hasBeenInteractedWith) return; // Don't show the text if it's already been opened

        // Show the price if relevant 
        CostText!.enabled = true;
    }

    public override void OnNotNearby() {
        CostText!.enabled = false;
    }

    public override Vector3 GetSpawnPositionOffset() {
        return Vector3.up * -0.5f;
    }

    public override Quaternion GetSpawnRotationOffset() {
        return Quaternion.Euler(x: 0, y: Random.Range(0, 360), z: 0);
    }

    // Called after the user interacts with the chest and we're playing the interaction VFX (before it's actually opened)
    private void HandleInteractionVFX() {
        float time = (Time.time - timeOfInteract) / interactVFXDuration;

        Vector3 leftLocalPos = SplineUtility.EvaluatePosition(LeftInteractionVFXSplineContainer!.Splines[0]!, time);
        Vector3 rightLocalPos = SplineUtility.EvaluatePosition(RightInteractionVFXSplineContainer!.Splines[0]!, time);

        Vector3 leftWorldPos = LeftInteractionVFXSplineContainer!.transform.localToWorldMatrix.MultiplyPoint(leftLocalPos);
        Vector3 rightWorldPos = RightInteractionVFXSplineContainer!.transform.localToWorldMatrix.MultiplyPoint(rightLocalPos);

        LeftInteractionVFXInstance!.transform.position = leftWorldPos;
        RightInteractionVFXInstance!.transform.position = rightWorldPos;
    }

    // Start the open animation & actually spawn the item
    private void DoOpen() {
        hasOpened = true;

        OpenChestVFXInstance!.Play();
        Animator!.SetBool(ANIM_HAS_OPENED, true);
        SpawnItem();
    }

    // We wait to call this until the interaction VFX is done playing
    private void SpawnItem() {
        ItemPickup itemPickup = Instantiate(ItemPickupPrefab!, transform.position, Quaternion.identity);
        itemPickup.startPosition = transform.position;
        itemPickup.endPosition = DetermineItemSpawnPosition();
        itemPickup.item = AllItems!.PickItem();
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
}
