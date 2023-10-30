using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningJumpSpell : Spell {
    [Header("Idk")]
    public Color ExternalSpellColor;

    public override float ChargeRate => 5.0f;
    public override int MaxNumberOfCharges => 1;
    public override bool DoesBlockOtherSpells => false;
    public override bool IsBlockedByOtherSpells => false;
    public override Color SpellColor => ExternalSpellColor;


    void Start() {
        CurrentCharge = MaxNumberOfCharges;
    }

    void Update() {
        // How tf should I apply affects on the character from here?
        // There's no way this should have direct access to PlayerController, should it?
        // I suppose I could get it from PlayerSpellsController, but even still it feels a little odd
        // though the PlayerController does (eventually) own this tbf
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

    public override bool CanShootWhereAiming(Vector3 muzzlePosition, Camera SpellCamera) { return true; }
}
