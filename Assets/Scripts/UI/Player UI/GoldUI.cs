using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#nullable enable

public class GoldUI : MonoBehaviour {
    [Tooltip("Text used for displaying the player's current gold")]
    public TextMeshProUGUI? GoldText;

    private GoldWallet? goldWallet;

    private void Start() {
        goldWallet = PlayerController.instance!.goldWallet;
    }

    // Can we animate this somehow?
    // Or is it increased animated?
    // It's probably easier to do the "animation" of the gold in here then elsewhere
    // Should be that the more we increase the gold amount by, the faster that it "animates"
    private void Update() {
        GoldText!.text = $"{goldWallet!.GetGoldAmount()}";
    }
}
