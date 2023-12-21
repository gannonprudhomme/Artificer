using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  This should probably share logic with HealthBarUI, but I decided against it

// Controls the full-screen shader (post effect)
// which displays a red vignette when the player gets damaged
public class DamagedVignette : MonoBehaviour {
    [Tooltip("Reference to the material which contains the damage vignette full screen shader (that's used as a URP renderer feature)")]
    public Material VignetteMaterial;

    [Tooltip("Health instance of the playerr")]
    public Health PlayerHealth;

    [Tooltip("The curve for how the vignette fades out over time")]
    public AnimationCurve FadeOutCurve;

    [Tooltip("The inner radius for the vignette shader")]
    public float VignetteInnerRadiusStart = 0.4f;
    
    [Tooltip("The outer radius for the vignette shader")]
    public float VignetteOuterRadius = 1.4f;

    public float StrengthStart = 1.4f;

    // public const float DamageTakenAnimationDuration = HealthBarUI.DamageTakenAnimationDuration;
    public const float DamageTakenAnimationDuration = 1f;

    private float lastDamageTakenTime = Mathf.NegativeInfinity;

    private const string SHADER_STRENGTH = "_Strength";

    void Start() {
        PlayerHealth.OnDamaged += OnDamaged;
        ResetShader();
    }

    void Update() {
        AnimateDamageTaken();
    }

    private void AnimateDamageTaken() { 
        // Calculate the time ([0, 1]) that has elapsed to feed into the animation curve
        float t = (Time.time - lastDamageTakenTime) / DamageTakenAnimationDuration;

        // Evalulate the animation curve to determine how much this should have faded out
        // Curve is 1 -> 0
        float percentFaded = FadeOutCurve.Evaluate(t);
        float strength = StrengthStart * percentFaded;

        VignetteMaterial.SetFloat(SHADER_STRENGTH, strength);
    }

    private void OnDamaged(float damage, Vector3 damagePosition, DamageType damageType) {
        lastDamageTakenTime = Time.time;
    }

    private void ResetShader() { 
        VignetteMaterial.SetFloat(SHADER_STRENGTH, 0.0f);
    }
}
