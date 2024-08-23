using UnityEngine;
using UnityEngine.Animations;

#nullable enable

// Don't require them cause sometimes we'll use ParentConstraints
// (honestly should probably use them all of the time?)
/*
[RequireComponent(
    // Should I just use MultiPositionConstraint/etc from Rigging?
    typeof(PositionConstraint),
    typeof(RotationConstraint)
)]
*/
public class ItemDisplayer : MonoBehaviour {
    public ItemType CorrespondingItemType;
}

