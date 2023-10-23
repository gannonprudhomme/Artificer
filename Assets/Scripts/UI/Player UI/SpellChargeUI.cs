using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellChargeUI : MonoBehaviour {
    public Image SpellFillImage;
    public TMPro.TextMeshProUGUI SpellChargeText;

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

        print(fillAmount);

        SpellFillImage.fillAmount = fillAmount;
        SpellChargeText.text = $"{(int) spell.CurrentCharge}";
    }
}