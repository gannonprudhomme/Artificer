using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

// We might want this to live in the Player module
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(InputHandler), typeof(Animator))]
public class PlayerSpellsController : MonoBehaviour {
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

    [Header("Spells")]
    public FireballSpell? FireballSpell;

    public IceWallSpell? SecondSpellPrefab;

    public IonSurgeJumpSpell? IonSurgeJumpSpell;

    public NanoSpearSpell? NanoSpearSpell;

    public Spell[] spells { get; } = new Spell[4];

    private PlayerController? player;
    private Animator? animator;

    // Starts out at 12, increases by 2.4 every level
    // Setting this as constant for now, but it won't be later
    public const float baseDamage = 12.0f;

    public LayerMask playerLayerMask;

    public bool IsForcingAimLookForward { get; private set; }

    // Start is called before the first frame update
    void Awake() {
        playerLayerMask = LayerMask.GetMask("Player");

        // This is obviously a shit way of doing this
        // really we should be able to provide spells as an array in Unity
        // but that array/list should be a fixed size. Surely that's possible

        // We could do this way better
        spells[0] = FireballSpell!;
        spells[2] = IonSurgeJumpSpell!;
        spells[3] = NanoSpearSpell!;
        
        if (SecondSpellPrefab != null) { // probs just want to yell if this is null, idk
            spells[1] = Instantiate(SecondSpellPrefab, RightArmSpellSpawnPoint!);
        }

		player = GetComponent<PlayerController>();
	    if (!player) {
		    Debug.LogError("Should have a PlayerController!");
		}

        animator = GetComponent<Animator>();

        foreach(var spell in spells) {
            spell.SpellEffectsSpawnPoint = SpellEffectsFireSpawnPoint;
            spell.PlayerAnimator = animator;
            spell.UpdatePlayerVelocity += UpdatePlayerVelocity;
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

        if (inputHandler.GetThirdAttackInputHeld()) {
            if (spells[2].CanShoot()) {
                spells[2].ShootSpell(
                    muzzlePositions: (LeftArmSpellSpawnPoint!.transform.position, RightArmSpellSpawnPoint!.transform.position),
                    owner: gameObject,
                    spellCamera: SpellCamera!,
				    currDamage: player!.CurrentBaseDamage,
                    layerToIgnore: playerLayerMask
                );
            }
        } else if (inputHandler.GetThirdAttackInputReleased()) {
            spells[2].AttackButtonReleased();
        }

        if (inputHandler.GetFourthAttackInputHeld()) {
            if (spells[3].CanShoot()) {
                spells[3].ShootSpell(
                    muzzlePositions: (LeftArmSpellSpawnPoint!.transform.position, RightArmSpellSpawnPoint!.transform.position),
                    owner: gameObject,
                    spellCamera: SpellCamera!,
				    currDamage: player!.CurrentBaseDamage,
                    layerToIgnore: playerLayerMask
                );
            }
        } else if (inputHandler.GetFourthAttackInputReleased()) {
            spells[3].AttackButtonReleased();
        }

        // Maybe I should get the spells working first before preventing them from working at the same time?
        // I already know how to do fireball though so I don't *really* need to
        // but Ice Wall and Lightning Jump should be a good challenge
    }

    public int GetPrimarySpellChargesCount() {
        return (int) spells[0].CurrentCharge;
    }

    public CrosshairReplacementImage? DetermineCurrentAimTexture() {
        foreach (var spell in spells) {
            // We're just going to return the first one; I don't forsee this being a problem
            if (spell.GetAimTexture() is CrosshairReplacementImage image) {
                return image;
            }   
        }

        return null;
    }

    public float? DetermineCurrentReticleOffsetMultipler() {
        foreach(var spell in spells) {
            // Return the first one which gives a value (they should never intersect)
            if (spell.GetInnerReticleMultiplier() is float multiplier) {
                return multiplier;
            }
            // TODO: Add error checking in case we're trying to animate both at once?
        }

        return null;
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

    private void UpdatePlayerVelocity(Vector3 addedVelocity) {
        // We reset the velocity - rather than add to it - as otherwise Ion Surge's boost won't do much when we're e.g. falling
        player!.CharacterVelocity.y = addedVelocity.y;

        // TODO: We should *really* have a better way to do this
        // Without this the player will get stuck to the ground w/ the ground check
        player!.lastTimeJumped = Time.time;
    }
}
