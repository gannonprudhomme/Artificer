using UnityEngine;

#nullable enable

// We might want this to live in the Player module
[RequireComponent(
    typeof(PlayerController),
    typeof(PlayerItemsController)
)]
[RequireComponent(typeof(InputHandler), typeof(Animator))]
public sealed class PlayerSpellsController : MonoBehaviour {
    [Header("References")]
    public InputHandler? inputHandler;

    [Tooltip("Where to spawn spell fire effects")]
    public Transform? SpellEffectsFireSpawnPoint;

    [Tooltip("Left arm for where the spell should be spawned / shot out of")] // Tooltip won't apply to lightning jump or ice wall
    public Transform? LeftArmSpellSpawnPoint;

    // SpellMuzzle was under Internal References, but idk what internal was supposed to mean in this case
    [Tooltip("Right arm for where the spell should be spawned / shot out of")] // Tooltip won't apply to lightning jump or ice wall
    public Transform? RightArmSpellSpawnPoint;

    [Header("Spells")]
    public FireballSpell? FireballSpell;

    public IceWallSpell? IceWallSpell;

    public IonSurgeJumpSpell? IonSurgeJumpSpell;

    public NanoSpearSpell? NanoSpearSpell;

    public Spell[] spells { get; } = new Spell[4];

    // I still don't really understand this
    // camera used to avoid seeing weapon go through geometries?
    private Camera? spellCamera;
    private PlayerController? player;
    private PlayerItemsController? itemsController;
    private Animator? animator;

    public LayerMask playerLayerMask;

    public bool IsForcingAimLookForward { get; private set; }

    // Start is called before the first frame update
    private void Awake() {
        playerLayerMask = LayerMask.GetMask("Player");

        // This is obviously a shit way of doing this
        // really we should be able to provide spells as an array in Unity
        // but that array/list should be a fixed size. Surely that's possible

        // We could do this way better
        spells[0] = FireballSpell!;
        spells[1] = NanoSpearSpell!;
        spells[2] = IceWallSpell!;
        spells[3] = IonSurgeJumpSpell!;

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

    private void Start() {
        inputHandler = GetComponent<InputHandler>();
        itemsController = GetComponent<PlayerItemsController>();

        spellCamera = CameraObjects.instance!.MainCamera;
    }

    private void Update() {
        HandleAttackInput();

        IsForcingAimLookForward = DetermineIsForceLookingForward();

        spells[1].MaxNumberOfCharges = spells[1].InitialMaxCharges + itemsController!.ModifiedSecondarySpellCharges;

        HandleSpellBlocking();
        HandleSpellCancelling();
    }

    private void HandleAttackInput() {
        for(int i = 0; i < spells.Length; i++) {
            if (GetAttackInputHeld(index: i)) {
                spells[i].AttackButtonHeld(
                    muzzlePositions: (LeftArmSpellSpawnPoint!.transform.position, RightArmSpellSpawnPoint!.transform.position),
                    owner: player!,
                    spellCamera: spellCamera!,
                    currDamage: player!.CurrentBaseDamage,
                    layerToIgnore: playerLayerMask
                );
            } else if (GetAttackInputReleased(index: i)) {
                spells[i].AttackButtonReleased();
            }
        }
    }

    private void HandleSpellBlocking() {
        bool isBlockingSpellActive = false;
        int blockingSpell = -1;

        for (int i = 0; i < spells.Length; i++) {
            if (spells[i].ShouldBlockOtherSpells()) {
                isBlockingSpellActive = true;
                blockingSpell = i;
                break;
            }
        }

        for (int i = 0; i < spells.Length; i++) {
            // Don't mark itself as being blocked
            if (i == blockingSpell) {
                continue;
            }

            spells[i].IsBlockingSpellActive = isBlockingSpellActive;
        }
    }

    private void HandleSpellCancelling() {
        // Handle cancelling
        int cancelSpell = -1;
        bool shouldCancel = false;
        for(int i = 0; i < spells.Length; i++) {
            if (spells[i].ShouldCancelOtherSpells()) {
                shouldCancel = true;
                cancelSpell = i;
                break;
            }
        }

        if (shouldCancel) {
            for(int i = 0; i < spells.Length; i++) {
                // Don't cancel itself
                if (i == cancelSpell) {
                    continue;
                }

                spells[i].Cancel();
            }
        }
    }

    public int GetPrimarySpellChargesCount() {
        return (int) spells[0].CurrentCharge;
    }

    public bool ShouldCancelSprinting() {
        foreach(var spell in spells) {
            if (spell.ShouldCancelSprinting()) {
                return true;
            }
        }

        return false;
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
        player!.SetVerticalVelocity(addedVelocity.y);
    }

    private bool GetAttackInputHeld(int index) {
        switch (index) {
            case 0:
                return inputHandler!.GetFirstAttackInputHeld();
            case 1:
                return inputHandler!.GetSecondAttackInputHeld();
            case 2:
                return inputHandler!.GetThirdAttackInputHeld();
            case 3:
                return inputHandler!.GetFourthAttackInputHeld();
            default:
                Debug.LogError($"Invalid index of {index} for GetAttackInputHeld");
                return false;
        }
    }

    private bool GetAttackInputReleased(int index) {
        switch (index) {
            case 0:
                return inputHandler!.GetFirstAttackInputReleased();
            case 1:
                return inputHandler!.GetSecondAttackInputReleased();
            case 2:
                return inputHandler!.GetThirdAttackInputReleased();
            case 3:
                return inputHandler!.GetFourthAttackInputReleased();
            default:
                Debug.LogError($"Invalid index of {index} for GetAttackInputReleased");
                return false;
        }
    }
}
