using UnityEngine;
using UnityEngine.Animations;

#nullable enable

[RequireComponent(
    // Should I just use MultiPositionConstraint/etc from Rigging?
    typeof(PositionConstraint),
    typeof(RotationConstraint)
)]
public class ItemDisplayer : MonoBehaviour {
    public ItemType CorrespondingItemType;
}

