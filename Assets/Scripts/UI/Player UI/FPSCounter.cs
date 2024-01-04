using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Ok this sucks I probably shouldn't use this - it's not smooth
// (but apparently it's supposed to be?)
// It's from this: https://forum.unity.com/threads/fps-counter.505495/
public class FPSCounter : MonoBehaviour {
    public TextMeshProUGUI FPSText;

    // void Update() {
    /*(
    void OnGUI() {
        // print("OnGUI");
        float newFPS = 1.0f / Time.smoothDeltaTime;
        // idk what this lerp is about, it's never going to get there
        // fps = Mathf.Lerp(fps, newFPS, 0.0005f);
        // fps = newFPS;
        // GUI.Label(new Rect(0, 0, 300, 300), "FPS: " + ((int)fps).ToString());
        // GUI.Label(new Rect(0, 0, 300, 300), "Something", GUISty)
    }
    */

    /*
    void Update() {
        float newFPS = 1.0f / Time.smoothDeltaTime;
        // idk what this lerp is about, it's never going to get there
        // fps = Mathf.Lerp(fps, newFPS, 0.0005f);

        // fps = newFPS;
        fps = Mathf.Lerp(fps, newFPS, 0.05f);

        FPSText.text = $"FPS: {(int)fps}";
    }
    */

    // The decay needs to be above 1.0.
    // The larger this value is, the faster the influence from the past will decay.
    const float decayVal = 1.5f;

    float accumWt = 0.0f;
    float accum = 0.0f;

    private void Start() {
        // Don't think this did anything lol
        QualitySettings.vSyncCount = 1;
    }

    void OnGUI() {
        float wtAvgDT = accum / accumWt;

        // GUIStyle style = new();
        // style.fontSize = 24;
        int final = (int) (1.0f / wtAvgDT);

        // GUILayout.Label(((int) (1.0f / wtAvgDT)).ToString(), style);
        FPSText.text = $"{final}";
    }

    private void Update() {
        // Decay the accumulations.
        // The ratio of this.accum/this.accumWt doesn't change from this - but the
        // values are now smaller compared to the new values we're going to add
        // in - and thus have a smaller weighting.
        this.accum /= decayVal;
        this.accumWt /= decayVal;

        this.accum += Time.unscaledDeltaTime;
        this.accumWt += 1.0f;
    }
}
