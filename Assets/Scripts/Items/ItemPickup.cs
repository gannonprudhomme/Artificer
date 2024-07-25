using UnityEngine;
using UnityEngine.Splines;

#nullable enable

public class ItemPickup : MonoBehaviour {
    // We could probably just GetComponent this
    public MeshFilter? MeshFilter;

    // The item that is granted upon pickup
    public Item? item;

    [HideInInspector]
    public Vector3 startPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 endPosition = Vector3.zero;

    private float startAnimationTime = Mathf.NegativeInfinity;
    private readonly float animationDuration = 0.5f;

    private BezierCurve curve;

    void Start() {
        startAnimationTime = Time.time;

        curve = new BezierCurve(
            p0: startPosition,
            p1: startPosition + (Vector3.up * 5.0f),
            p2: endPosition + (Vector3.up * 5.0f),
            p3: endPosition
        );

        if (item != null) {
            MeshFilter!.mesh = item.DropModelMesh!;
        } else {
            Debug.LogError("No Item for ItemPickup!");
        }
    }

    void Update() {
        // Sample the curve (until we're done animating)
        bool doneAnimating = Time.time - startAnimationTime >= animationDuration;
        if (doneAnimating) return;

        transform.position = CurveUtility.EvaluatePosition(curve, (Time.time - startAnimationTime) / animationDuration);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out ItemsDelegate itemsDelegate)) {
            itemsDelegate.PickupItem(item!);

            Destroy(gameObject);
        }
    }
}
