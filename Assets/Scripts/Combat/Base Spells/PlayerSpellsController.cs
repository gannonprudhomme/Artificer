using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Downside of modularization (or really, how I architectured this?
// we don't want this to dependon the UI module, so we can't define this in the UI module like we should
public interface AimDelegate {
    public Texture2D CurrentAimTexture { get; }
}

// We might want this to live in the Player module
// [RequireComponent(typeof(PlayerController))] // Idk if we actually need this? Maybe PlayerController should require this? Regardless they should be attach on the same GameObject
[RequireComponent(typeof(InputHandler))]
public class PlayerSpellsController : MonoBehaviour, AimDelegate {
    [Header("References")]
    public InputHandler inputHandler;

    // SpellMuzzle was under Internal References, but idk what internal was supposed to mean in this case
    [Tooltip("Where the spell should be spawned / shot out of")] // Tooltip won't apply to lightning jump or ice wall
    public Transform SpellSpawnPoint;

    // I still don't really understand this
    [Tooltip("Secondary camera used to avoid seeing weapon go through geometries?")]
    public Camera SpellCamera;

    [Header("UI")]
    [Tooltip("The normal aim indicator image which displays by default (when the user can shoot")]
    public Texture2D NormalAimTexture;

    [Tooltip("The aim indicator image which displays when the user can't shoot at a target")]
    public Texture2D CantShootTexture;

    [Header("Spells")]
    public Spell FirstSpellPrefab; // These have to be MonoBehaviors to be able to be assigned in Unity btw
    public Spell SecondSpellPrefab;
    // public Spell ThirdSpell;
    // public Spell FourthSpell;

    public Spell[] spells = new Spell[2];

    // Spells have to be able to set this - how?
    private bool canShootWhereAiming = false;

    // Starts out at 12, increases by 2.4 every level
    // Setting this as constant for now, but it won't be later
    public const float baseDamage = 12.0f;

    public Texture2D CurrentAimTexture {
        get {
            if (canShootWhereAiming) {
                return NormalAimTexture;
            }

            return CantShootTexture;
        }
    }

    // Start is called before the first frame update
    void Awake() {

        // This is obviously a shit way of doing this
        // really we should be able to provide spells as an array in Unity
        // but that array/list should be a fixed size. Surely that's possible
        spells[0] = Instantiate(FirstSpellPrefab, SpellSpawnPoint);
        if (SecondSpellPrefab != null) { // probs just want to yell if this is null, idk
            spells[1] = Instantiate(SecondSpellPrefab, SpellSpawnPoint);
        }
        // spells[2] = ThirdSpell;
        // spells[3] = FourthSpell;
    }

    void Start() {
        // Can we do this in Awake()?
        inputHandler = GetComponent<InputHandler>();
    }

    // Update is called once per frame
    void Update() {
        HandleAttackInput();

        // Check all of the spells and see if it's okay to shoot where we're aiming
        // if it's not we se canShootWhereAiming to false so the aim indicator changes
        var didChange = false;
        foreach (var spell in spells) {
            if (!spell.CanShootWhereAiming(SpellSpawnPoint.transform.position, SpellCamera)) {
                canShootWhereAiming = false;
                didChange = true;
                break;
            }
        }

        if (!didChange) {
            canShootWhereAiming = true;
        }
    }

    void HandleAttackInput() {
        // Ideally there'd be an iterative way of doing this
        // but then we'd have to give InputHandler access to the Spells, which we probs shouldn't do
        // (even though it's available to the Base Spell assembly (and thus ISpell) at the moment
        //if (inputHandler.GetFirstAttackInputDown()) { // Honestly idk how to handle this so I'm just going to do held for now

        //}
        /*
        if (inputHandler.GetFirstAttackInputHeld()) {
            // Do we really want to call this every frame?

            FirstSpell.AttackButtonHeld();
        } else if (inputHandler.GetFirstAttackInputReleased()) {
            FirstSpell.AttackButtonReleased();
        }
        */

        // How I do this is going to have to change for the Ice Wall
        // but it'll work for now for getting the Fireball Spell working
        if (inputHandler.GetFirstAttackInputHeld()) {
            // print("first attack held");
            if (spells[0].CanShoot()) {
                spells[0].ShootSpell(
                    SpellSpawnPoint.transform.position,
                    this.gameObject,
                    SpellCamera
                );
            } else {
                // print("can't shoot");
            }
        }

        if (inputHandler.GetSecondAttackInputHeld()) {
            if (spells[1].CanShoot()) {
                spells[1].ShootSpell(
                    SpellSpawnPoint.transform.position,
                    this.gameObject,
                    SpellCamera
                );
            }
        } else if (inputHandler.GetSecondAttackInputReleased()) {
            spells[1].AttackButtonReleased();
        }
        
        // Maybe I should get the spells working first before preventing them from working at the same time?
        // I already know how to do fireball though so I don't *really* need to
        // but Ice Wall and Lightning Jump should be a good challenge
    }

    // We probably don't want this
    void RestoreSpellCharges() {

    }
}
