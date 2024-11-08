using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Items/EnergyDrink")]
public class EnergyDrinkItem: Item {
    public override ItemType itemType => ItemType.ENERGY_DRINK;
    public override string itemName => "Energy Drink";
    public override string description => "Increase sprint speed by +25%.";
    public override string longDescription => "Sprint speed is improved by 25%. (+25% per stack)";
    public override Rarity rarity => Rarity.COMMON;

    public override void OnUpdate(ItemsDelegate itemsController, int count) {
        // TODO: Apparently it's not actually 25% but ~17.5%
        itemsController.ModifiedSprintMultiplier = count * 0.25f;
    }
}

