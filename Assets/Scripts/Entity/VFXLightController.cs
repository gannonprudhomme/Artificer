using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#nullable enable

[RequireComponent(typeof(VisualEffect))]
public class VFXLightController : MonoBehaviour {
    [Tooltip("The light we'll be controlling")]
    public Light? Light;

    public float BaseLightIntensity = 90.0f;

    [Tooltip("How long the explosion lasts for")]
    public float Duration = 1.0f;

    [Tooltip("The curve that determines the intensity of the light over time. Should be on a scale of [0, 1].")]
    public AnimationCurve? LightingMultiplierCurve;

    private bool isPlaying = false;

    private float timeOfPlay = Mathf.NegativeInfinity;

    private VisualEffect? vfx;

    void Start() {
        vfx = GetComponent<VisualEffect>();
    }

    void Update() {
        if (!isPlaying) {
            return;
        }

        float percent = (Time.time - timeOfPlay) / Duration;
        float multiplier = LightingMultiplierCurve!.Evaluate(percent);
        Light!.intensity = BaseLightIntensity * multiplier;
    }

    // Start the VFX graph + 
    public void Play() {
        isPlaying = true;

        // Start won't be called by the time Play is called, so we do it in here!
        vfx = GetComponent<VisualEffect>();

        if (vfx != null) {
            vfx.Play();
        } else {
            Debug.LogError("VFX was null somehow");
        }

        timeOfPlay = Time.time;
    }
    
    public void Stop() {
        isPlaying = false;
    }
}
