using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AimUI : MonoBehaviour {
    public RawImage AimImage;

    public PlayerSpellsController aimDelegate;

    void Start() {
        if (aimDelegate == null) {
            Debug.LogError("AimUI was not passed an AimDelegate!");
        }

        AimImage.texture = aimDelegate.CurrentAimTexture;
    }

    // Update is called once per frame
    void Update() {
        AimImage.texture = aimDelegate.CurrentAimTexture;
    }
}
