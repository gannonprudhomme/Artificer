using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellChargesUI : MonoBehaviour {
    [Tooltip("Image components displaying the spell charges or w/e")]
    public SpellChargeUI[] SpellChargeUIs;

    [Tooltip("Reference to the PlayerSpellsController so we can get the spellsj")]
    public PlayerSpellsController SpellsController;

    void Start() {
        for(int i = 0; i < SpellChargeUIs.Length; i++) {
            SpellChargeUIs[i].spell = SpellsController.spells[i];
        }
    }
}
