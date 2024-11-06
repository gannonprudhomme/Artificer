using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#nullable enable

public class MultishopTerminal : MonoBehaviour, Spawnable {
    [Tooltip("Reference to the 3 ShopTerminals this is the parent of")]
    public ShopTerminal[] shopTerminals = new ShopTerminal[3];

    [Tooltip("UIFollowPlayer for the cost text")]
    public UIFollowPlayer? UIFollowPlayer;

    [Tooltip("Text used for setting the cost of the shop terminal")]
    public TextMeshProUGUI? CostText;

    [Tooltip("Reference to the AllItems ScriptableObject so we can pick a random item")]
    public AllItems? AllItems;

    public MonoBehaviour Prefab => this;

    private int costToPurchase = 25; // Will get overriden
    private GoldWallet? goldWallet;

    private bool isPurchased = false;

    public void SetUp(int costToPurchase, Target target, GoldWallet goldWallet) {
        this.costToPurchase = costToPurchase;
        UIFollowPlayer!.Target = target.AimPoint;
        CostText!.text = $"${costToPurchase}";
        this.goldWallet = goldWallet;
    }

    void Awake() {
        // Should be temporary, but I'm going to leave it cause it should be fine
        if (goldWallet == null) {
            goldWallet = FindObjectOfType<GoldWallet>();
            // Honestly idek if I care about this
            // Debug.LogError("Having to manually find GoldWallet!");
        }

        // Pick items for the individual ShopTerminals
        // One item is guaranteed to to be visible, the others have a 20% chance to be replaced by a question mark
        // Need to do this in Awake since we'll be setting up the ShopTerminals in their Start()
        for(int i = 0; i < 3; i++) {
            Item? item = ItemToSpawn(guaranteedToBeVisible: i == 0); // Only do it for the first one
            shopTerminals[i]!.SetUp(
                item: item,
                costToPurchase: costToPurchase,
                goldWallet: goldWallet!,
                onOpen: OnTerminalOpened
            );
        }
    }

    private void Update() {
        bool anyNearby = false;
        foreach(var terminal in shopTerminals) {
            if (terminal!.isPlayerNearby) {
                anyNearby = true;
                break;
            }
        }

        if (anyNearby && !isPurchased) {
            CostText!.enabled = true;
        } else {
            CostText!.enabled = false;
        }
    }

    private void OnTerminalOpened() {
        isPurchased = true;
        CostText!.enabled = false;

        // Close the other terminals
        foreach(var terminal in shopTerminals) {
            terminal!.OnOtherShopTerminalSelected();
        }
    }

    public Vector3 GetSpawnPositionOffset() {
        return Vector3.zero;
    }

    public Quaternion GetSpawnRotationOffset() {
        return Quaternion.identity;
    }

    private Item? ItemToSpawn(bool guaranteedToBeVisible) {
        bool chanceToSpawnQuestionMark = Random.value < 0.2f; // 20% chance to be replaced by a question mark

        if (chanceToSpawnQuestionMark && !guaranteedToBeVisible) {
            return null;
        } else {
            return AllItems!.PickItem(Item.Rarity.COMMON);
        }
    }
}
