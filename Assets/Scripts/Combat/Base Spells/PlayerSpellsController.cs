using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerSpellsController {
    // What is needed externally?

    // Should the UI go through this to get the spells? I figure probably

    // Needed for the UI to get the spells
    // Honestly it might be worth to split these out into GetPrimarySpell, ..., GetQuaternarySpell
    // but that fixes us to 4, so whatever
    public ISpell FirstSpell { get; protected set; } // I think if I do this I can't set it in the Unity UI, which I def want to do
    public ISpell SecondSpell { get; protected set; }
    public ISpell ThirdSpell { get; protected set; }
    public ISpell FourthSpell { get; protected set; }
}

// We might want this to live in the Player module
// [RequireComponent(typeof(PlayerController))] // Idk if we actually need this? Maybe PlayerController should require this? Regardless they should be attach on the same GameObject
[RequireComponent(typeof(InputHandler))]
public class PlayerSpellsController : MonoBehaviour {
    [Header("References")]
    public InputHandler inputHandler;

    // SpellMuzzle was under Internal References, but idk what internal was supposed to mean in this case
    [Tooltip("Where the spell should be spawned / shot out of")] // Tooltip won't apply to lightning jump or ice wall
    public Transform SpellSpawnPoint;

    // I still don't really understand this
    [Tooltip("Secondary camera used to avoid seeing weapon go through geometries?")]
    public Camera SpellCamera;

    [Header("Spells")]
    public Spell FirstSpellPrefab; // These have to be MonoBehaviors to be able to be assigned in Unity btw
    public Spell SecondSpellPrefab;
    // public Spell ThirdSpell;
    // public Spell FourthSpell;

    // This is proof that we really shouldn't have an interface _and_ an abstract class
    private Spell[] spells = new Spell[2];

    private bool IsBlockingSpellActive = false;

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
                print("can shoot");
                spells[0].ShootSpell(
                    SpellSpawnPoint.transform.position,
                    this.gameObject,
                    SpellCamera
                );
            } else {
                // print("can't shoot");
            }
        }

        if (inputHandler.GetFirstAttackInputDown()) {
            print("first attack down");
        }
        
        // Maybe I should get the spells working first before preventing them from working at the same time?
        // I already know how to do fireball though so I don't *really* need to
        // but Ice Wall and Lightning Jump should be a good challenge
    }

    // We probably don't want this
    void RestoreSpellCharges() {

    }
}
