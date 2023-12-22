using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Health bar for the player
//
// Ideally we'd pass in a curve to this (start fast then slow down) for the damage fill, but this is good enough for now
public class HealthBarUI : MonoBehaviour {
    [Tooltip("The image used to show how much health the player has.")]
    public Image HealthFillImage;

    [Tooltip("The image used to show how much damage was just taken.")]
    public Image DamageFillImage;

    [Tooltip("Optional health text. Will display like {CurrHealth}/{MaxHealth}")]
    public TextMeshProUGUI CurrentHealthText;

    [Tooltip("Reference to the Player's health component")]
    public Health health;

    // TODO: Should this increase with damage taken? Yes!
    // How long in seconds we want to animate the damage taken fill image
    // public const float DamageTakenAnimationDuration = 0.5f;
    private float durationToAnimate = 0.0f;

    // When the last time we took damage was
    // Does this reset every time we take damage? Or just the first time we (recently) took damage
    private float lastDamageTakenTime = Mathf.Infinity;

    // How much damage is left to animate
    // Stacks with each damage we take
    private float damageToAnimate = 0f;

    void Start() {
        health.OnDamaged += OnDamaged;
    }

    void Update() {
        if (health == null) {
            Debug.LogError($"{this.name}'s health was not passed, returning");
            return;
        }

        SetHealthFillAndColor();

        AnimateDamageTaken();

        if (CurrentHealthText != null ) {
            CurrentHealthText.text = $"{(int)health.CurrentHealth} / {(int) health.MaxHealth}";
        }
    }

    private void SetHealthFillAndColor() {
        // Do I really want this to be casted to an int? What does it matter if it's a float?
        float healthFillAmount = ((int) health.CurrentHealth) / health.MaxHealth;
        HealthFillImage.fillAmount = healthFillAmount;

        // TODO: If we have < 25% health we should change the HealthFillImage color to red
        // (otherwise set it to green). Need to pass in the colors.
    }

    // Animate the damaged-taken fill image, to provide extra feedback to the player on how much health they recently lost
    private void AnimateDamageTaken() {
        float healthFillAmount = ((int) health.CurrentHealth) / health.MaxHealth;

        // We should remove a certain amount of damage in one frame (seconds * Time.deltaTime)
        // And that remaining damage is how big the thing bar should be. No Lerp'ing!
        // (and that amount should be change based on a curve, but that's for later)

        // Remove 30% of the max health per second from the damageToAnimate
        float ratePerSec = health.MaxHealth * 0.3f;

        damageToAnimate -= (ratePerSec * Time.deltaTime); // Convert to per-frame value
        damageToAnimate = Mathf.Max(0.0f, damageToAnimate); // Prevent it from being negative

        float curr = (damageToAnimate / health.MaxHealth) + healthFillAmount;
        DamageFillImage.fillAmount = curr;
    }

    // Every time we get damaged 
    private void OnDamaged(float damage, Vector3 damagedPos, DamageType damageType) {
        damageToAnimate += damage;
    } 
}
