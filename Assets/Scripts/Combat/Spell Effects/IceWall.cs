using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This collects _all_ of the ice wall spikes

public class IceWall : MonoBehaviour {
    // [Tooltip("Max lifetime of the ice wall")]
    // public float MaxLifeTime = 5f;

    [Tooltip("Sfx which plays when something runs into this")]
    public AudioClip CollisionSfx;

    [Tooltip("Layers this can collide with")]
    public LayerMask HittableLayers = -1;

    [Tooltip("How much damage each ice spike does on collision")]
    public float Damage = 10f;

    private List<GameObject> iceSpikes;
    private List<Collider> ignoredColliders;

    void OnEnable() {
        // This should actually be triggering the affect (figure it'll be Aoe? But maybe not)
        //Destroy(this.gameObject, MaxLifeTime);
    }

    void Update() {
        // Check if lifetime has been hit and trigger all of the ice spikes to explode
    }

    // Maybe this can control the animation for the ice wall?
}
