using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This collects _all_ of the ice wall spikes

public class IceWall : MonoBehaviour {
    // [Tooltip("Max lifetime of the ice wall")]
    // public float MaxLifeTime = 5f;

    [Tooltip("Layers this can collide with")]
    public LayerMask HittableLayers = -1;

    [Tooltip("How much damage each ice spike does on collision")]
    public float Damage = 10f;

    private IceWallSpike[] iceSpikesChildren;
    private GameObject[] iceSpikesParents;
    private List<Collider> ignoredColliders;

    void Start() {
        // This should actually be triggering the affect (figure it'll be Aoe? But maybe not)
        //Destroy(this.gameObject, MaxLifeTime);

        // Get all of the children
        iceSpikesChildren = GetComponentsInChildren<IceWallSpike>();

        // Get the parents of the children because we added a parent to them
        // so we can move the pivot (so they scale in one direction rather than in both directions for animations)
        // this is annoying and I regret this - I should have just fixed the pivot in Blender
        iceSpikesParents = new GameObject[iceSpikesChildren.Length];
        for (int i = 0; i < iceSpikesChildren.Length; i++) {
            iceSpikesParents[i] = iceSpikesChildren[i].transform.parent.gameObject;
        }

        // Set the vertical values of the ice spikes
        PlaceIceSpikes();
    }

    void Update() {
        // Check if lifetime has been hit and trigger all of the ice spikes to explode
    }

    // Determine Y-value for each ice spike
    // so they're not all in a line
    private void PlaceIceSpikes() {
        // I need to go up some amount from the ice spike
        // (maybe it's height?)
        // then raycast downwards, find the position it collides
        // and set that to be where the ice spike spawns
        for(int i = 0; i < iceSpikesParents.Length; i++) {
            // If this is false we really fucked up, but check anyways
            if (i >= iceSpikesChildren.Length) {
                Debug.LogError("There's not enough children in the iceSpikesChildren array, returning");
                return;
            }

            var parent = iceSpikesParents[i];
            IceWallSpike child = iceSpikesChildren[i];

            var size = child.boxCollider.size.y;

            // Raycast from the middle of the top face of the box collider
            // Vector3 rayCastPos = child.boxCollider.center + (Vector3.up * size / 2.0f);
            Vector3 rayCastPos = child.boxCollider.bounds.max;

            Debug.DrawRay(rayCastPos, Vector3.down, Color.red);

            // Raycast down
            if (Physics.Raycast(rayCastPos, Vector3.down, out RaycastHit hit)) {
                parent.transform.position = hit.point;
            } else {
                print("we didn't hit anything, idk what to do here");
                // maybe hide the child?
            }
        }
    }
}
