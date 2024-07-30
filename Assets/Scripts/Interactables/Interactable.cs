using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// If we wanted to be really efficient we could store this in the Octree somehow
// and we'd use an Octree "query" to even check if the player is close enough to anyt interactable
// to check if we're aiming at it
// but we'll just brute-force all of them for now

// Anything that can be interacted with (by pressing E) and has e.g. an outline
// Note this contains both spawned interactables like Chests & Barells,
// but also stuff like ItemPickup
public abstract class Interactable : MonoBehaviour {
    public InteractableHoverEvent? HoverEvent;

    protected bool hasBeenInteractedWith = false;

    // We should probably rename this since you don't "purchase" everything
    // more of like a CanInteract or something

    // For chests this is "Can Afford"
    // For item pickups this is "Is Hovering"
    protected const string SHADER_OUTLINE_COLOR_FLIP = "_ColorFlip";
    protected const string SHADER_OUTLINE_IS_ENABLED = "_IsEnabled";
    protected const string SHADER_OUTLINE_TRUE_COLOR = "_TrueColor";
    protected const string SHADER_OUTLINE_FALSE_COLOR = "_FalseColor";

    // The user was hovering/aiming at it and pressed E to interact
    public abstract void OnSelected(GoldWallet goldWallet, ItemsDelegate itemsDelegate);


    protected virtual void Start() {
        // Hide it at the start
        foreach (Material mat in GetMaterials()) {
            mat.SetInt(SHADER_OUTLINE_IS_ENABLED, 0);
        }
    }

    // Not abstract since not everything is going to do something for this
    public virtual void OnNearby() { }

    public virtual void OnNotNearby() { }

    // The player is "hovering" over it (aiming at it)
    public virtual void OnHover() {
        if (hasBeenInteractedWith) return;

        // Show the outline of it
        foreach (Material mat in GetMaterials()) {
            mat.SetInt(SHADER_OUTLINE_IS_ENABLED, 1);
        }
    }

    public virtual void OnNotHovering() {
        if (hasBeenInteractedWith) return;

        // hide it
        foreach (Material mat in GetMaterials()) {
            mat.SetInt(SHADER_OUTLINE_IS_ENABLED, 0);
        }
    }

    // This should just require one material not a list
    // but I'll update this when I actually make the mesh + texture
    protected List<Material> GetMaterials() {
        List<Material> ret = new();

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer renderer in renderers) {
            Material[] materials = renderer.materials;

            foreach(Material material in materials) {
                ret.Add(material);
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(SkinnedMeshRenderer renderer in skinnedMeshRenderers) {
            Material[] materials = renderer.materials;

            foreach(Material material in materials) {
                ret.Add(material);
            }
        }

        return ret;
    }
}
