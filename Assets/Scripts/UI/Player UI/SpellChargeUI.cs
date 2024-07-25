using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class SpellChargeUI : MonoBehaviour {
    public Image? SpellFillImage;
    public TextMeshProUGUI? SpellChargeText;
    public TextMeshProUGUI? CountdownText;

    public Spell? spell { get; set; } = null;
    public Spell spell { get; set; }

    void Update() {
        if (spell == null) {
            Debug.LogError($"{this.name}'s spell was not passed, returning");
            return;
        }

        float remainder = spell.CurrentCharge - ((int) spell.CurrentCharge);
        float fillAmount = remainder / Spell.CHARGE_PER_SHOT;

        if (spell.CurrentCharge == spell.MaxNumberOfCharges) {
            fillAmount = 1;
        }

        if (spell.MaxNumberOfCharges > 1) {
            SpellChargeText!.enabled = true;
        } else {
            SpellChargeText!.enabled = false;
        }

        SpellFillImage!.fillAmount = fillAmount;
        SpellChargeText!.text = $"{(int) spell.CurrentCharge}";

        if (spell!.isOnCooldown) {
            CountdownText!.enabled = true;
            float chargePercent = (spell.CurrentCharge / Spell.CHARGE_PER_SHOT);
            int secondsRemaining = Mathf.CeilToInt((1 - chargePercent) / spell.ChargeRate);

            CountdownText!.text = $"{secondsRemaining}";
        } else {
            CountdownText!.enabled = false;
        }
    }
}
