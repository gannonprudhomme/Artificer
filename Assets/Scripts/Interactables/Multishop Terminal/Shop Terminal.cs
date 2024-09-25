using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

// A singular "shop" in the Multishop Terminal
[RequireComponent(typeof(Animator))]
public class ShopTerminal : Interactable {
    [Header("Shop Terminal")]
    public SkinnedMeshRenderer? TerminalMeshRenderer;

    [Tooltip("Transform for where the item should be spawned from")]
    public Transform? ItemSpawnTransform;

    [Tooltip("The VFX instance that plays when the terminal is opened")]
    public VisualEffect? OpenVFX;

    [Header("Item")]
    [Tooltip("Reference to the Item MeshRenderer so we can change the texture")]
    public MeshRenderer? ItemMeshRenderer;

    [Tooltip("Reference to the Item MeshFilter so we can change the mesh")]
    public MeshFilter? ItemMeshFilter;

    [Tooltip("All items we can pick from")]
    public AllItems? AllItems;

    [Tooltip("Prefab for the ItemPickup, which we assign the item's mesh & texture to to display it.")]
    public ItemPickup? ItemPickupPrefab;

    [Tooltip("Layer mask for the level; used to determine where to spawn the item")]
    public LayerMask LevelLayerMask;

    [Header("Question Mark")]
    [Tooltip("Mesh for the question mark we use when the item isn't visible")]
    public Mesh? QuestionMarkMesh;

    [Tooltip("Texture for the question mark we use when the item isn't visible")]
    public Texture? QuestionMarkTexture;

    private Animator? animator;
    private Item? item; // If this is null we'll show a question mark
    private int costToPurchase = 25;
    private GoldWallet? playerGoldWallet;

    private const string ANIM_HAS_OPENED = "HasOpened";

    public bool isPlayerNearby { get; private set; } = false;

    public delegate void OnOpened();
    private OnOpened? onOpenedCallback;

    public void SetUp(
        Item? item,
        int costToPurchase,
        GoldWallet goldWallet,
        OnOpened onOpen
    ) {
        this.item = item;
        this.costToPurchase = costToPurchase;
        playerGoldWallet = goldWallet;
        this.onOpenedCallback = onOpen;
    }

    protected override void Start() {
        base.Start();
        
        animator = GetComponent<Animator>();
        animator.SetBool(ANIM_HAS_OPENED, false);


        Mesh mesh;
        Texture texture;
        if (item != null) {
            mesh = item.DropModelMesh!;
            texture = item.MeshTexture!;
        } else {
            // Assign it to the question mark if the item isn't picked
            mesh = QuestionMarkMesh!;
            texture = QuestionMarkTexture!;
        }

        ItemMeshFilter!.mesh = mesh;
        ItemMeshRenderer!.material.SetTexture("_Texture", texture); // For the ItemPickup material
    }
    public override void OnSelected(
        GoldWallet goldWallet,
        Experience experience,
        Transform targetTransform,
        ItemsDelegate itemsDelegate
    ) {
        if (hasBeenInteractedWith) return;

        hasBeenInteractedWith = true;

        // Hide the item displayer
        ItemMeshFilter!.gameObject.SetActive(false);

        OpenVFX!.Play();

        // Spawn the item (same "drop" logic as the chest)
        SpawnItem();

        // Close the other ones (send signal to MultishopTerminal)
        // Note this will also call OnOtherShopTerminalSelected() on this object
        // which is intended / what we want.
        onOpenedCallback?.Invoke();
    }

    // Called whenever a different shop terminal in this Multishop Terminal is selected
    public void OnOtherShopTerminalSelected() { // TODO: Rename this to something like close or something
        foreach (Material material in GetMaterials()) {
            material.SetInt(SHADER_OUTLINE_IS_ENABLED, 0);
        }

        hasBeenInteractedWith = true;

        // enable the "gates"
        animator!.SetBool(ANIM_HAS_OPENED, true);
    }

    public override void OnHover() {
        base.OnHover();

        if (hasBeenInteractedWith) return; // Don't show the text if it's already been opened

        HoverEvent!.OnHover!("Open terminal", costToPurchase);
    }

    public override void OnNearby() {
        if (hasBeenInteractedWith) return;

        isPlayerNearby = true;
    }

    public override void OnNotNearby() {
        isPlayerNearby = false;
    }

    private void SpawnItem() {
        ItemPickup itemPickup = Instantiate(ItemPickupPrefab!, ItemSpawnTransform!.position, Quaternion.identity);
        itemPickup.startPosition = ItemSpawnTransform!.position;
        itemPickup.endPosition = DetermineItemSpawnPosition();

        if (item == null) { // If it wasn't pre-selected (we were showing a question mark), pick an item
            item = AllItems!.PickItem();
        }

        itemPickup.item = item;
    }

    private Vector3 DetermineItemSpawnPosition() {
        if (Physics.Raycast(
            // TODO: I should really just pass in a Transform
            origin: transform.position + (transform.forward * 5f) + (Vector3.up * 10),
            direction: Vector3.down,
            out RaycastHit hit,
            maxDistance: 20f,
            layerMask: LevelLayerMask
        )) {
            return hit.point;
        } else {
            Debug.LogError("Couldn't find position for item!");
            return transform.position + (transform.forward * 5f);
        }
    }

    protected override IEnumerable<Material> GetMaterials() {
        return TerminalMeshRenderer!.materials;
    }
}

