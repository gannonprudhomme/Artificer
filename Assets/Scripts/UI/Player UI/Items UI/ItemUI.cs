using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class ItemUI : MonoBehaviour {
    [Tooltip("Image that we set with the item's icon")]
    public RawImage? ItemImage;

    [Tooltip("Text reference we use to set how many items the player has of it")]
    public TextMeshProUGUI? ItemCountText;

    [HideInInspector]
    public Item? Item;

    [HideInInspector]
    public int ItemCount = 0;

    private void Start() {
        ItemImage!.texture = Item!.Icon!;
    }

    private void Update() {
        if (ItemCount == 0) {
            ItemCountText!.enabled = false;
        } else {
            ItemCountText!.enabled = true;
            ItemCountText!.text = $"x{ItemCount}";
        }
    }
}
