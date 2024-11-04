using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class SpellChargesUI : MonoBehaviour {
    [Tooltip("Image components displaying the spell charges or w/e")]
    public SpellChargeUI[]? SpellChargeUIs;

    private PlayerSpellsController? spellsController;

    private void Start() {
        spellsController = PlayerController.instance!.spellsController;   
        
        for(int i = 0; i < SpellChargeUIs!.Length; i++) {
            SpellChargeUIs[i].spell = spellsController!.spells[i];
        }
    }
}
