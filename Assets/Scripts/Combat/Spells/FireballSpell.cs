using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The only reason this has to be a MonoBehavior in the first place
// is so we can assign it a projectile
// Is there really no other way? Ugh I want this to just be input/output
public class FireballSpell : Spell {
    [Header("Idk")]
    public Color SpellColorForUI;

    [Tooltip("The Fireball projectile we shoot out")]
    public Projectile Projectile; // Projectile will determine it's damage? Or should this? How dumb should the Projectile be?

    /** Abstract Spell Properties **/

    public override float ChargeRate => 0.5f;
    public override int MaxNumberOfCharges => 5;
    public override bool DoesBlockOtherSpells => true;
    public override bool IsBlockedByOtherSpells => true;
    // There's gotta be some way to configure this
    // maybe make this retrieve a value set from Unity?
    public override Color SpellColor => SpellColorForUI;

    /** Local variables **/

    private bool isAttackButtonHeld = false;

    void Start() {
        // Start off with max charges
        CurrentCharge = MaxNumberOfCharges;
    }

    void Update() {
        if (isAttackButtonHeld) {
            // Try to fire
        }
    }

    public override void AttackButtonHeld() {
        isAttackButtonHeld = true;
    }

    public override void AttackButtonReleased() {
        isAttackButtonHeld = false;
    }
    public override void AttackButtonPressed() {
        // I'm really not sure if we care about this
    }

    private void TriggerSpell() {
        // Spawn a projectile
    }
}
