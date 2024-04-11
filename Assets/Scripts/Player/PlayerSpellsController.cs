using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

// Downside of modularization (or really, how I architectured this?
// we don't want this to dependon the UI module, so we can't define this in the UI module like we should
public interface AimDelegate {
    public Texture2D? CurrentAimTexture { get; }
}

// We might want this to live in the Player module
// [RequireComponent(typeof(PlayerController))] // Idk if we actually need this? Maybe PlayerController should require this? Regardless they should be attach on the same GameObject
[RequireComponent(typeof(InputHandler))]
public class PlayerSpellsController : MonoBehaviour, AimDelegate {
    [Header("References")]
    public InputHandler? inputHandler;

    [Tooltip("Where to spawn spell fire effects")]
    public Transform? SpellEffectsFireSpawnPoint;

    [Tooltip("Left arm for where the spell should be spawned / shot out of")] // Tooltip won't apply to lightning jump or ice wall
    public Transform? LeftArmSpellSpawnPoint;

    // SpellMuzzle was under Internal References, but idk what internal was supposed to mean in this case
    [Tooltip("Right arm for where the spell should be spawned / shot out of")] // Tooltip won't apply to lightning jump or ice wall
    public Transform? RightArmSpellSpawnPoint;

    // I still don't really understand this
    [Tooltip("Secondary camera used to avoid seeing weapon go through geometries?")]
    public Camera? SpellCamera;

    [Header("UI")]
    [Tooltip("The normal aim indicator image which displays by default (when the user can shoot")]
    public Texture2D? NormalAimTexture;

    [Header("Spells")]
    public Spell? FirstSpellPrefab; // These have to be MonoBehaviors to be able to be assigned in Unity btw
    public Spell? SecondSpellPrefab;

    public Spell[] spells = new Spell[2];
    
    private Entity? player;

    // Starts out at 12, increases by 2.4 every level
    // Setting this as constant for now, but it won't be later
    public const float baseDamage = 12.0f;

    public LayerMask playerLayerMask;
    public Texture2D? CurrentAimTexture { get; protected set; }

    public bool IsForcingAimLookForward { get; private set; }

    // Start is called before the first frame update
    void Awake() {
        playerLayerMask = LayerMask.GetMask("Player");

        // This is obviously a shit way of doing this
        // really we should be able to provide spells as an array in Unity
        // but that array/list should be a fixed size. Surely that's possible
        spells[0] = Instantiate(FirstSpellPrefab!, RightArmSpellSpawnPoint!);
        if (SecondSpellPrefab != null) { // probs just want to yell if this is null, idk
            spells[1] = Instantiate(SecondSpellPrefab, RightArmSpellSpawnPoint!);
        }
        // spells[2] = ThirdSpell;
        // spells[3] = FourthSpell;

		player = GetComponent<PlayerController>();
	    if (!player) {
		    Debug.LogError("Should have a PlayerController!");
		}

        foreach(var spell in spells) {
            spell.SpellEffectsSpawnPoint = SpellEffectsFireSpawnPoint;
        }

        IsForcingAimLookForward = false;
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
        CurrentAimTexture = DetermineCurrentAimTexture();

        IsForcingAimLookForward = DetermineIsForceLookingForward();
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
        if (inputHandler!.GetFirstAttackInputHeld()) {
            // print("first attack held");
            if (spells[0].CanShoot()) {
                spells[0].ShootSpell(
                    muzzlePositions: (LeftArmSpellSpawnPoint!.transform.position, RightArmSpellSpawnPoint!.transform.position),
                    owner: this.gameObject,
                    spellCamera: SpellCamera!,
				    currDamage: player!.CurrentBaseDamage,
                    layerToIgnore: playerLayerMask
                );
            } else {
                // print("can't shoot");
            }
        }

        if (inputHandler.GetSecondAttackInputHeld()) {
            if (spells[1].CanShoot()) {
                spells[1].ShootSpell(
                    muzzlePositions: (LeftArmSpellSpawnPoint!.transform.position, RightArmSpellSpawnPoint!.transform.position),
                    owner: this.gameObject,
                    spellCamera: SpellCamera!,
				    currDamage: player!.CurrentBaseDamage,
                    layerToIgnore: playerLayerMask
                );
            }
        } else if (inputHandler.GetSecondAttackInputReleased()) {
            spells[1].AttackButtonReleased();
        }
        
        // Maybe I should get the spells working first before preventing them from working at the same time?
        // I already know how to do fireball though so I don't *really* need to
        // but Ice Wall and Lightning Jump should be a good challenge
    }

    private Texture2D DetermineCurrentAimTexture() {
        Texture2D? aimTexture = null;
        foreach (var spell in spells) {
            if (spell.GetAimTexture() is Texture2D texture) {
                aimTexture = texture;
            }   
        }

        if (aimTexture != null) {
            return aimTexture;
        } else {
            return NormalAimTexture!;
        }
    }

    private bool DetermineIsForceLookingForward() {
        bool isForceLookingForward = false;

        foreach (var spell in spells) {
            if (spell.ShouldForceLookForward()) {
                isForceLookingForward = true;
            }
        }

        return isForceLookingForward;
    }


    // We probably don't want this
    void RestoreSpellCharges() {

    }
}
