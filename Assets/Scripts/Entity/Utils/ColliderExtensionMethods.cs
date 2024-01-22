using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public static class ColliderExtensionMethod {
    public static bool TryGetEntityFromCollider(this Collider collider, out Entity outEntity) {
        if (collider.TryGetComponent<ColliderParentPointer>(out var colliderParentPointer)) {
            outEntity = colliderParentPointer.entity;
            return true;
        }
        
        if (collider.TryGetComponent<Entity>(out var entity)) {
            outEntity = entity;
            return true;
        }

        // This won't ever matter b/c of the nature of TryGet
        // outEntity will never be null when we're accessing this in the body of the if-statement
        // i.e. this makes it function like if-let at the callsite (otherwise we'd have to treat outEntity as optional when it never will be)
        #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        outEntity = null;
        #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        return false;
    }
}
