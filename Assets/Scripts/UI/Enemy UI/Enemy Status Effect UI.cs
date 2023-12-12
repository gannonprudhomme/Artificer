using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Controls an individual Enemy status effect UI entry
// Contains the icon and text indicating how many stacks this status effect has
public class EnemyStatusEffectUI : MonoBehaviour {
    [Tooltip("The Image component we use to set the status effect on")]
    public Image StatusEffectImage;

    [Tooltip("The Text component we use to set the # of stacks")]
    public TextMeshProUGUI StackCountText;

    public BaseStatusEffect StatusEffect { get; set; }

    void Start() {
        if (StatusEffect == null) {
            Debug.LogError("Status effect was not passed to StatusEffectUI");
            return;
        }

        // This isn't the best way to do this, but eh what're you gonna do
        StatusEffectImage.sprite = Resources.Load<Sprite>($"Status Effects/{StatusEffect.ImageName}");
    }

    // Update is called once per frame
    void Update() {
        int currentStacks = StatusEffect.CurrentStacks;

        // If we're at 0, then just hide this
        // I need to update when Health.OnStatusEffectRemoved gets called cause currently there's a bug 
        // where this will display for a second (between Update() and the next FixedUpdate() call)
        if (currentStacks <= 0) {
            // So for now we're just going to make this not be active,
            // and on the next FixedUpdate() Health.OnStatusEffectRemoved will be called and actually remove / destroy this
            // (in EnemyStatusEffectBarUI)
            this.gameObject.SetActive(false);
        }

        StackCountText.text = $"x{StatusEffect.CurrentStacks}";
    }
}
