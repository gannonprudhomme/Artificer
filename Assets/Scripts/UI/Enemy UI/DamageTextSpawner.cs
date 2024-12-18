using UnityEngine;

#nullable enable

// TODO: Should we have one of these per Entity? Or should there just be one global one? Probably the former

// Spawns the DamageText instances for indicating how much damage was just applied to a given entity
public class DamageTextSpawner : MonoBehaviour {
    [Tooltip("Health instance we use to know when we should display a damage indicator")]
    public Health? health;

    [Tooltip("Reference to the prefab of an individual damage text")]
    public DamageText? DamageTextPrefab;

    [Tooltip("Transform of the Canvas that the damage text will be spawned under")]
    public Transform? CanvasTransform;

    // This is a shit way of doing this - it's not extendable to other colors really
    // and won't transfer between diff instances on diff prefabs (like golem vs lemurian)
    // unless we set default values here.
    // But I need to configure it in the UI somehow!
    [Tooltip("Color used for the default damage effect (e.g. not from a status effect)")]
    public Color NormalDamageTextColor;

    [Tooltip("Color used for the burn status effect")]
    public Color BurnDamageTextColor;

    [Tooltip("Color used for the bleed status effect")]
    public Color BleedDamageTextColor;

    // Start is called before the first frame update
    void Start() {
        health!.OnDamaged += SpawnDamageText;
    }

    // We should really pass an int cause we don't do decimals
    // Where do we want to handle this?
    public void SpawnDamageText(float damage, Vector3? spawnPosition, DamageType damageType) {
        // We shouldn't have to do this - it should be done automatically
        // we might want to do this for stuff like the Fire Status Effect damage over time
        if (spawnPosition == null) {
            spawnPosition = CanvasTransform!.position + (Vector3.right * GetRandomSpawnOffset());
        }

        // DamageText newInst = Instantiate(DamageTextPrefab, this.transform);
        DamageText newInst = Instantiate(DamageTextPrefab!, CanvasTransform);
        newInst.transform.position = (Vector3) spawnPosition;
        // Cast it to an int since we don't want to show decimals
        newInst.Damage = (int) damage;
        newInst.SetTextColor(GetColorFromDamageType(damageType));
    }

    private Color GetColorFromDamageType(DamageType damageType) {
        return damageType switch {
            DamageType.Normal => NormalDamageTextColor,
            DamageType.Burn   => BurnDamageTextColor,
            DamageType.Bleed  => BleedDamageTextColor,
                            _ => Color.green,
        };
    }

    private float GetRandomSpawnOffset() {
        float offset = Random.Range(-1.5f, 1.5f);
        return offset;
    }
}
