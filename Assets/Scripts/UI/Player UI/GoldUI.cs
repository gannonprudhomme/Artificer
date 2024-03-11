using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour {
    [Tooltip("Text used for displaying the player's current gold")]
    public TextMeshProUGUI GoldText;

    [Tooltip("Reference to the player's GoldWallet")]
    public GoldWallet GoldWallet;

    // Update is called once per frame
    void Update() {
        GoldText.text = $"{GoldWallet.GetGoldAmount()}";
    }
}
