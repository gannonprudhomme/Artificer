using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#nullable enable

[RequireComponent(typeof(Canvas))]
public class InteractableHoverUI : MonoBehaviour {
    public TextMeshProUGUI? Text;

    public InteractableHoverEvent? HoverEvent;

    private Canvas? canvas;

    private void Start() {
        canvas = GetComponent<Canvas>();

        HoverEvent!.OnHover += OnHoverEvent;
        HoverEvent!.OnNoHover += OnNoHoverEvent;
    }

    private string MakeString(string name, int? cost) {
        string parenthesis = "";
        if (cost != null) {
            parenthesis = $" (<color=#FFF75B>${cost}</color>)";
        }

        return $"<b><color=#FFF75B>E</color></b> {name}{parenthesis}";
    }

    private void OnHoverEvent(string name, int? cost) {
        canvas!.enabled = true;
        Text!.SetText(MakeString(name, cost));
    }

    private void OnNoHoverEvent() {
        canvas!.enabled = false;
    }
}
