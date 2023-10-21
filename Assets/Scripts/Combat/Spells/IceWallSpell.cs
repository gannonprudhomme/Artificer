using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceWallSpell : Spell {
    [Header("Idk")]
    public Color ExternalSpellColor;

    [Tooltip("The prefab to project on the ground so the user knows where they're aiming")]
    public GameObject AimAreaPrefab;

    public GameObject IceWallProjectile; // We need a better name than Projectile (it's not a projectile really?)

    /** Abstract Spell Properties **/

    public override float ChargeRate => 8.0f;
    public override int MaxNumberOfCharges => 1;
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;
    public override Color SpellColor => ExternalSpellColor;

    void Start() {
        CurrentCharge = MaxNumberOfCharges;
    }

    void Update() {
        
    }

    public override void AttackButtonHeld() {
    }

    public override void AttackButtonReleased() {
    }

    public override void AttackButtonPressed() {
    }

    public override bool CanShoot()
    {
        throw new System.NotImplementedException();
        return false;
    }

    public override void ShootSpell(Vector3 muzzlePosition, GameObject owner, Camera spellCamera)
    {
        throw new System.NotImplementedException();
    }
}
