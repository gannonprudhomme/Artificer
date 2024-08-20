using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/Items/BackupMagazine")]
public class BackupMagazineItem : Item {
    public override ItemType itemType => ItemType.BACKUP_MAGAZINE;
    public override string itemName => "Backup Magazine";
    public override string description => "Add an extra charge of your Secondary skill.";
    public override string longDescription => "Add +1 (+1 per stack) charge of your Secondary skill";

    public override Rarity rarity => Rarity.COMMON;

    public override void OnUpdate(ItemsDelegate itemsController, int count) {
        itemsController.ModifiedSecondarySpellCharges += count;
    }
}

