using UnityEngine;
using UnityEngine.Animations;

#nullable enable

[RequireComponent(
    typeof(MeshRenderer), // Idk if I want to do this, it's restrictive
    // Should I just use MultiPositionConstraint/etc from Rigging?
    typeof(PositionConstraint),
    typeof(RotationConstraint)
)]
public class ItemDisplayer : MonoBehaviour {
    public ItemType CorrespondingItemType;
}

