using UnityEngine;
using UnityEngine.UI;

#nullable enable

[RequireComponent(typeof(RawImage))]
public class PrimarySpellChargeUI : MonoBehaviour {
    [Tooltip("Animation curve used to determine ")]
    public AnimationCurve? OnShowSizeCurve;

    private readonly float onShowAnimationDuration = 0.075f; // in seconds

    private bool isShowing = false;
    private float timeOfShowAnimationStart = Mathf.NegativeInfinity;

    private RawImage? image;

    private void Start() {
        image = GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update() {
        if (isShowing) {
            // Animate it
            float time = (Time.time - timeOfShowAnimationStart) / onShowAnimationDuration;
            float scale = OnShowSizeCurve!.Evaluate(time);
            image!.transform.localScale = new Vector3(scale, scale, 1.0f);
        }
    }

    public void Show() {
        if (isShowing) {
            return;
        }

        isShowing = true;
        timeOfShowAnimationStart = Time.time;
        image!.enabled = true;
    }

    public void Hide() {
        isShowing = false;
        image!.enabled = false;
    }
}
