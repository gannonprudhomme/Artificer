using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  This should probably share logic with HealthBarUI, but I decided against it

// Controls the full-screen shader (post effect)
// which displays a red vignette when the player gets damaged
public class DamagedVignette : MonoBehaviour {
    [Tooltip("Reference to the material which contains the damage vignette full screen shader (that's used as a URP renderer feature)")]
    public Material VignetteMaterial;


    [Tooltip("The curve for how the vignette fades out over time")]
    public AnimationCurve FadeOutCurve;

    [Tooltip("What the strength for the shader starts at")]
    public float MaxStrength = 1;

    // The minimum strength this can be
    // Set depending on if the player is at "critical health"
    // private float MinStrength = 0.0f;
    
    private Health playerHealth;

    // TODO: This should scale with the damage taken (just like the starting strength)
    // public const float DamageTakenAnimationDuration = HealthBarUI.DamageTakenAnimationDuration;
    public const float DamageTakenAnimationDuration = 0.5f;

    private float damageLeftToAnimate = 0.0f;

    private const string SHADER_STRENGTH = "_Strength";

    void Start() {
        playerHealth = PlayerController.instance.health;
        
        playerHealth.OnDamaged += OnDamaged;
        ResetShader();
    }

    void Update() {
        AnimateDamageTaken();
    }

    private void OnDestroy() {
        // Clean up at the end (so we don't get the red vignette when we exit play mode if we were just damaged)
        VignetteMaterial.SetFloat(SHADER_STRENGTH, 0.0f);
    }

    private void AnimateDamageTaken() { 
        // Calculate the time ([0, 1]) that has elapsed to feed into the animation curve
	    float ratePerSec = playerHealth.MaxHealth * 0.3f;

	    damageLeftToAnimate -= (ratePerSec * Time.deltaTime);
	    damageLeftToAnimate = Mathf.Max(0.0f, damageLeftToAnimate); // Prevent it from being negative

	    const float multiplier = 12.0f; // I could probably do this more logically
        float percent = (damageLeftToAnimate * multiplier) / (playerHealth.MaxHealth);
	    percent = Mathf.Clamp(percent, 0, 1);

        float strength = MaxStrength * percent;
        // Evalulate the animation curve to determine how much this should have faded out
        // Curve is 1 -> 0
        // float percentFaded = FadeOutCurve.Evaluate(t);
        // This is [0, 1] (since StrengthStart is 1.0f)
        // This is [0, MaxStrength]
        // We need to move it to [MinimumStrength, StrengthStart]
        // float strength = MaxStrength * percentFaded;

        VignetteMaterial.SetFloat(SHADER_STRENGTH, strength);
    }

    private void OnDamaged(float damage, Vector3? _, DamageType __) {
	    damageLeftToAnimate += damage;
    }

    private void ResetShader() { 
        VignetteMaterial.SetFloat(SHADER_STRENGTH, 0.0f);
    }
}
