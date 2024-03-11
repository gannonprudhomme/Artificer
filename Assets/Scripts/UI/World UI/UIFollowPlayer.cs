using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFollowPlayer : MonoBehaviour {
    // [Tooltip("The target we should rotate to look at")]
    public Transform Target { get; set; }
    void Update() {
        // Make this look at the target at all times
        transform.LookAt(Target.position);
    }
}
