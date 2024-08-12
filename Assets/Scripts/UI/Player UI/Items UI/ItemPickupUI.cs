using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

[RequireComponent(typeof(Canvas))]
public class ItemPickupUI : MonoBehaviour {
    public RawImage? ItemImage;
    public TextMeshProUGUI? ItemTitleText;
    public TextMeshProUGUI? ItemSubtitleText;
    public PlayerItemsController? itemsController;

    private Canvas? canvas;

    private Queue<Item> itemsToDisplay = new();

    // TODO: I don't think we need this?
    private Item? displayingItem = null;

    private readonly float displayDuration = 2.0f;
    private float timeOfDisplayStart = Mathf.NegativeInfinity;
    private bool isDisplaying = false;

    void Start() {
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;

        itemsController!.OnItemPickedUp += OnItemPickedUp;
    }

    void Update() {
        if (itemsToDisplay.Count > 0 && !isDisplaying) {
            displayingItem = itemsToDisplay.Dequeue();
            DisplayItem(displayingItem);
        }

        // Check if we should hide it
        bool hasReachedDisplayDuration = Time.time - timeOfDisplayStart > displayDuration;
        if (isDisplaying && hasReachedDisplayDuration) {
            canvas!.enabled = false;
            isDisplaying = false;
            displayingItem = null;
        }
    }
    private void OnItemPickedUp(Item item, int _) {
        // Add it to the queue to be displayed
        itemsToDisplay.Enqueue(item);
    }

    private void DisplayItem(Item item) {
        isDisplaying = true;
        canvas!.enabled = true;
        timeOfDisplayStart = Time.time;

        ItemImage!.texture = item.Icon;
        ItemTitleText!.text = item.itemName;
        ItemSubtitleText!.text = item.description;
    }
}
