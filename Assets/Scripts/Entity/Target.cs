using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// TODO: There's probably a better way to handle this
// Contains a reference to the transform to aim at
// and the collider that's attached to it
// Used so the Enemies can aim at the correct point on the player and know if they have line of sight (collider)
// without having a direct reference to the Player component
[RequireComponent(typeof(Entity))]
public class Target : MonoBehaviour {
    [Tooltip("Where the enemies will aim at")]
    public Transform? AimPoint;

    // Must be a CapsuleCollider otherwise it gets set to the CharacterController collider on the player
    // there's gotta be a better way to do this
    [Tooltip("Colliders enemies will use to determine line of sight")]
    public CapsuleCollider? TargetCollider;

    // Reference to the player camera.
    // Used for the UIFollowPlayer so we can rotate it to look at the camera at all times
    public Camera? PlayerCamera { get; private set; }

    private void Start() {
        PlayerCamera = Camera.main; // Hope this works
    }
}
