using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Only exists so we can have a Mesh Collider be on the child of the Enemy component
// This is set on an Enemy's mesh
public class ColliderParentPointer : MonoBehaviour {
    [Tooltip("Points to the parent entity's Entity component")]
    public Entity entity;
}
