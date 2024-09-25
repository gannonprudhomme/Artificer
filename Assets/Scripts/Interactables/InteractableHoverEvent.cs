using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

[CreateAssetMenu(menuName = "ScriptableObjects/InteractableHoverEvent")]
public class InteractableHoverEvent : ScriptableObject {
    public UnityAction<string, int?>? OnHover;

    public UnityAction? OnNoHover;
}
