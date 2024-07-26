using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class UIFollowPlayer : MonoBehaviour {
    public Transform? Target { get; set; }

    private void Start() {
        if (Target == null) {
            Debug.LogError("Target is null in UIFollowPlayer");
        }
    }

    void Update() {
        if (Target == null) return;

        // Make this look at the target at all times
        transform.LookAt(Target.position);
    }
}
