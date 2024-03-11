using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The "Container" for the gold / money system for the player
public class GoldWallet : MonoBehaviour {
    private int goldAmount = 0;

    private void Start() {
        // Player starts out with 15 gold
        goldAmount = 15;
    }

    public void GainGold(int amountToGain) {
        goldAmount += amountToGain;
    }

    // Attempts to spend gold if we have enough, but returns false if we don't have enoguh
    public bool SpendGoldIfPossible(int amountToSpend) {
        if (goldAmount < amountToSpend) {
            return false;
        }

        goldAmount -= amountToSpend;
        return true;
    }

    public int GetGoldAmount() {
        return goldAmount;
    }

    public bool CanAfford(int amount) {
        return goldAmount >= amount;
    }
}
