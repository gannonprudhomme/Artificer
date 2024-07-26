using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;

#nullable enable

public class ItemPickup : MonoBehaviour {
    [Header("References")]
    public MeshRenderer? MeshRenderer;
    public MeshFilter? MeshFilter;
    public VisualEffect? OnSpawnVFX;

    [Tooltip("VFX which plays as soon as the actual item model appears")]
    public VisualEffect? ExplosionSpawnVFX;

    [Tooltip("VFX which plays when the item model is being displayed on the ground before the player picks it up")]
    public VisualEffect? IdleVFX;

    public Light? Light;

    [Header("Colors")]
    public Color CommonColor = Color.white;
    public Color UncommonColor = Color.green;

    // The item that is granted upon pickup
    // [HideInInspector] // Only commented out for testing
    public Item? item;

    [HideInInspector]
    public Vector3 startPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 endPosition = Vector3.zero;

    private float startAnimationTime = Mathf.NegativeInfinity;
    private readonly float animationDuration = 1f;

    // TODO: Give this a better name lol
    private bool hasDoneWrapUpAnimation = false;

    private BezierCurve curve;

    void Start() {
        startAnimationTime = Time.time;

        MeshRenderer!.enabled = false;
        Light!.enabled = false;

        float height = 20f;
        curve = new BezierCurve(
            p0: startPosition,
            p1: startPosition + (Vector3.up * height),
            p2: endPosition + (Vector3.up * height),
            p3: endPosition
        );

        if (item != null) {
            MeshFilter!.mesh = item.DropModelMesh!;
        } else {
            Debug.LogError("No Item for ItemPickup!");
        }

        Light!.color = ColorForRarity(item!.rarity);
        OnSpawnVFX!.SetVector4("Color", ColorForRarity(item!.rarity));
        // Also set it for the idle vfx
    }

    void Update() {
        // Sample the curve (until we're done animating)
        bool doneAnimating = Time.time - startAnimationTime >= animationDuration;
        if (doneAnimating) {
            if (!hasDoneWrapUpAnimation) {
                OnSpawnVFX!.Stop();
                OnSpawnVFX!.enabled = false;

                ExplosionSpawnVFX!.Play();
                IdleVFX!.Play();

                MeshRenderer!.enabled = true;
                Light!.enabled = true;

                hasDoneWrapUpAnimation = true;
            }

            float rotate = 50f;
            Vector3 rotationEuler = MeshFilter!.transform.rotation.eulerAngles;
            rotationEuler.y += rotate * Time.deltaTime;
            MeshFilter!.gameObject.transform.rotation = Quaternion.Euler(rotationEuler);
        };

        transform.position = CurveUtility.EvaluatePosition(curve, (Time.time - startAnimationTime) / animationDuration);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out ItemsDelegate itemsDelegate)) {
            itemsDelegate.PickupItem(item!);

            Destroy(gameObject);
        }
    }

    private Color ColorForRarity(Item.Rarity rarity) {
        return rarity switch {
            Item.Rarity.COMMON => CommonColor,
            Item.Rarity.UNCOMMON => UncommonColor,
            _ => throw new System.NotImplementedException()
        };
    }
}
