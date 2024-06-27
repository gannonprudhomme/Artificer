using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class AimUI : MonoBehaviour {
    [Header("General")]
    [Tooltip("The parent of the crosshair images. Used so we can hide it when sprinting / etc")]
    public GameObject? CrosshairParent;

    [Tooltip("Reference to the Player Controller")]
    public PlayerController? aimDelegate;

    [Header("Replacement Images")]
    [Tooltip("The image that replaces the crosshair when we're sprinting / aiming the ice wall / etc")]
    public RawImage? SprintingImage;

    [Tooltip("Image which displays when we're aiming [the ice wall]")]
    public RawImage? AimingImage;

    [Tooltip("Image which displays when we can't fire [the ice wall]")]
    public RawImage? CantFireImage;

    [Header("Reticle Image References")]
    public RectTransform? TopInnerReticle;
    public RectTransform? LeftInnerReticle;
    public RectTransform? RightInnerReticle;
    public RectTransform? BottomInnerReticle;

    [Tooltip("The dots on the right side which represent primary spell charge")]
    public GameObject?[] primarySpellChargeImages = new GameObject[4];

    public readonly float startPosition = 22.5f;

    private void Start() {
        if (aimDelegate == null) {
            Debug.LogError("AimUI was not passed an AimDelegate!");
        }

        UpdateInnerReticlePositions(multiplier: 1f);
    }

    private void Update() {
        ShowReplacementImageIfAvailable(aimDelegate!.GetCurrentAimTexture());

        // We shouldn't do these if we're showing a replacement texture but w/e
        UpdateInnerReticlePositions(multiplier: aimDelegate!.GetCurrentReticleOffsetMultiplier() ?? 1.0f);
        SetShowingPrimarySpellChargeImages(amount: aimDelegate!.GetPrimarySpellChargesCount());
    }

    private void ShowReplacementImageIfAvailable(CrosshairReplacementImage? replacementImage) {
        // hide all of the replacement iamges at first (just unduplicates some code)
        SprintingImage!.enabled = false;
        AimingImage!.enabled = false;
        CantFireImage!.enabled = false;

        if (replacementImage != null) { // we were actually given a texture
            // Hide the normal crosshair
            CrosshairParent!.SetActive(false);

            // Determine which replacement to show
            switch (replacementImage) {
                case CrosshairReplacementImage.Sprinting:
                    SprintingImage!.enabled = true;
                    break;
                case CrosshairReplacementImage.Aiming:
                    AimingImage!.enabled = true;
                    break;
                case CrosshairReplacementImage.CantFire:
                    CantFireImage!.enabled = true;
                    break;
            }

        } else {
            CrosshairParent!.SetActive(true);
            // We already hide all of the replacements at the start so no need to do anything
        }
    }

    public void UpdateInnerReticlePositions(float multiplier) {
        TopInnerReticle!.localPosition = new(
            x: TopInnerReticle!.localPosition.x,
            y: startPosition * multiplier,
            z: TopInnerReticle!.localPosition.z
        );

        BottomInnerReticle!.localPosition = new(
            x: BottomInnerReticle!.localPosition.x,
            y: -startPosition * multiplier,
            z: BottomInnerReticle!.localPosition.z
        );

        LeftInnerReticle!.localPosition = new(
            x: -startPosition * multiplier,
            y: LeftInnerReticle!.localPosition.y,
            z: LeftInnerReticle!.localPosition.z
        );

        RightInnerReticle!.localPosition = new(
            x: startPosition * multiplier,
            y: RightInnerReticle!.localPosition.y,
            z: RightInnerReticle!.localPosition.z
        );
    }

    public void SetShowingPrimarySpellChargeImages(int amount) {
        if (amount > primarySpellChargeImages.Length) {
            Debug.LogError("Tried to set more primary spell charge images than we have!");
            return;
        }

        for(int i = 0; i < primarySpellChargeImages.Length; i++) {
            primarySpellChargeImages[i]!.SetActive(i < amount);
        }
    }
}
