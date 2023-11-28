using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Displays a damage indicator when any enemy takes damage
public class CrosshairDamageIndicatorUI : MonoBehaviour {
    [Tooltip("The damage indicator image")]
    public RawImage DamageIndicatorImage;

    // Needed so we can subscribe to all of the Health.OnDamageTaken of the active enemies
    private EnemyManager enemyManager;

    private float lastTimeDamageDealt = Mathf.NegativeInfinity;

    // How long the damage indicator will be on screen, in seconds
    private const float DisplayDuration = 12f / 60f; // show for 8 frames
    // Must be less than DisplayDuration
    private const float ScaleDuration = 6f / 60f;

    private const float StartScale = 1.2f;
    private const float EndScale = 1.0f;

    // Need to do this on Awake as the Enemys add themselves to EnemyManager in Start
    void Awake() {
        enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager == null) {
            Debug.LogError("Couldn't find EnemyManager! Returning");
            return;
        }

        enemyManager.OnEnemyAdded += OnEnemyAdded;
    }

    // We might want this to be run on LateUpdate, test the difference
    void Update() {
        // Animate the displaying of the crosshair
        // If lastTimeDamageDealt == 0 it should be bright, then fade out over time

        // How long it's been on screen for
        float timeDisplayed = Time.time - lastTimeDamageDealt; // Basically [0, displayDuration] (given how it's handled below)

        // If it's been on screen long enough (without another damage triggered), hide it
        if (timeDisplayed > DisplayDuration) {
            SetImageAlpha(0f); // Hide it

            // Reset the scale to 1, but I don't really think we need to do this
            DamageIndicatorImage.rectTransform.localScale = new Vector3(StartScale, StartScale, 1f);
            return;
        } else { // It's being shown
            float alpha = Mathf.Lerp(
                1f,
                0f,
                timeDisplayed / DisplayDuration
            );

            SetImageAlpha(alpha);

            // Handle the scaling of it - this should be faster than the alpha
            float newScale = Mathf.Lerp(
                StartScale,
                EndScale,
                timeDisplayed / ScaleDuration
            );

            DamageIndicatorImage.rectTransform.localScale = new Vector3(newScale, newScale, 1f);
        }
    }

    private void OnEnemyAdded(GameObject enemy) {
        Health health = enemy.GetComponent<Health>();

        if (health == null) {
            Debug.LogError($"Enemy {enemy} didn't have Health, returning");
            return;
        }

        health.OnDamaged += OnEnemyDamaged;
    }

    // We don't care about these parameters
    private void OnEnemyDamaged(float damage, Vector3 damagePosition) {
        lastTimeDamageDealt = Time.time;
    }

    // Helper function to set the alpha of an image since we can't set it directly
    private void SetImageAlpha(float alpha) {
        var newImageColor = DamageIndicatorImage.color;
        newImageColor.a = alpha;
        DamageIndicatorImage.color = newImageColor;
    }
}
