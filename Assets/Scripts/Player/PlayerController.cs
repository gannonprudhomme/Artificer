using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(InputHandler))]
public class PlayerController : Entity {
    /** PROPERTIES **/

    [Header("References")]
    [Tooltip("Reference to the main camera used for the player")]
    public Camera PlayerCamera;

    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    // Why would this be on the PlayerController? Should there be some like World / Game object we get this from?
    public float GravityDownForce = 20f;

    [Tooltip("Distance from the bottom of the character controller capsule to test for grounded")]
    // I really need to see how other things to this, b/c in the past I ran into weird things w/ this and capsule size
    public float GroundCheckDistance = 0.05f;

    [Header("Movement")]
    [Tooltip("Max movement speed when grounded (when not sprinting)")]
    public float MaxSpeedOnGround = 10f;

    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float MovementSharpnessOnGround = 15; // TODO: Rename to speed?

    [Tooltip("Max movement speed when in the air")]
    public float MaxSpeedInAir = 10f;

    [Tooltip("Sharpness for the movement when in the air, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float MovementSharpnessInAir = 15f;

    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
    public float SprintSpeedModifier = 2f;

    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    public float RotationSpeed = 200f;

    //[Tooltip("Rotation speed multiplier when aiming")]
    //[Range(0.1f, 1f)]
    //public float AimingRotationMultiplier = 0.4f;

    [Header("Jump")]
    [Tooltip("Force applied upward when jumping")]
    public float JumpForce = 20f;

    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
    public float CameraHeightRatio = 0.9f;

    [Tooltip("Height of character when standing")]
    public float CapsuleHeightStanding = 4f;

    /** LOCAL VARIABLES **/
    private Vector3 CharacterVelocity; // may need to be public, as enemies will need this to predict for aiming

    private InputHandler inputHandler;
    private CharacterController characterController;
    private Vector3 groundNormal;
    private float cameraVerticalAngle = 0f;

    private float lastTimeJumped = Mathf.NegativeInfinity;

    // So I think this has to be public? But set it to private for now
    private bool IsGrounded = true;

    /** CONSTANTS **/

    // TODO: Describe this
    private const float JUMP_GROUNDING_PREVENTION_TIME = 0.2f;
    // TODO: Describe this
    private const float GROUND_CHECK_DISTANCE_IN_AIR = 0.15f;

    /** COMPUTED PROPERTIES **/

    private Vector3 capsuleBottomHemisphere {
        get {
            return transform.position + (transform.up * characterController.radius);
        }
    }

    private float RotationMultiplier {
        get {
            // if we're aiming, return AimingRotationMultipler (not implemented)
            // otherwise, return normal one:
            return 1f;
        }
    }

    /** FUNCTIONS **/

    void Awake() {
        // add this as an actor I guess?
        // I find it a little odd that we have to do that
    }


    protected override void Start() {
        base.Start();

        characterController = GetComponent<CharacterController>();
        // Handle null

        inputHandler = GetComponent<InputHandler>();

        characterController.enableOverlapRecovery = true;

        // UpdateCharacterHeight(true);
    }

    // Update is called once per frame
    protected override void Update() {
        base.Update();

        (bool newIsGrounded, Vector3 newGroundNormal) = GroundCheck(
            IsGrounded,
            characterController,
            GroundCheckDistance,
            lastTimeJumped,
            transform.up,
            GetCapsuleTopHemisphere(characterController.height),
            GetCapsuleBottomHemisphere()
        );
        IsGrounded = newIsGrounded;
        groundNormal = newGroundNormal;

        HandleLooking();

        HandleCharacterMovement();

        // UpdateCharacterHeight(false) ???
    }

    // Future self: does making this static make this better or worse? (Especially since we call characterController.Move in here since we're passing a reference - blehhh)
    // this used to be an instance method for context
    // Returns (IsGrounded, groundNormal)
    private static (bool, Vector3) GroundCheck(
        bool wasGrounded,                  // this.IsGrounded
        CharacterController characterController,
        float groundCheckDistance,        // this.GroundCheckDistance
        float lastTimeJumped,             // this.lastTimeJumped
        Vector3 transformUp,              // this.transform.up
        Vector3 capsuleTopHemisphere,     // GetCapsuleTopHemisphere(characterController.height),
        Vector3 capsuleBottomHemisphere   // GetCapsuleBottomHemisphere()
    ) {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = wasGrounded ? (characterController.skinWidth + groundCheckDistance) : GROUND_CHECK_DISTANCE_IN_AIR;

        // Reset values before the ground check
        bool retIsGrounded = false;
        Vector3 groundNormal = Vector3.up;

        // only try to detect ground if it's been a short time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= lastTimeJumped + JUMP_GROUNDING_PREVENTION_TIME) {
            if (Physics.CapsuleCast(
                capsuleBottomHemisphere,
                capsuleTopHemisphere,
                characterController.radius,
                Vector3.down,
                out RaycastHit hit,
                chosenGroundCheckDistance,
                -1, // Used to be GroundCheckLayers, will probably need to change in the future
                QueryTriggerInteraction.Ignore
            )) {
                // storing the upward direction for the surface found
                groundNormal = hit.normal;

                // only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transformUp) > 0f
                    && IsNormalUnderSlopeLimit(groundNormal, transformUp, characterController)
                ) {
                    retIsGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > characterController.skinWidth) {
                        characterController.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }

        return (retIsGrounded, groundNormal);
    }

    // Called by Update()
    private void HandleCharacterMovement() {
        // character movement handling
        bool isSprinting = inputHandler.GetSprintInputHeld();
        float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

        // converts move input to a worldspace vector based on our character's transform orientation
        Vector3 worldSpaceMoveInput = transform.TransformVector(inputHandler.GetMoveInput());

        // handle grounded movement
        if (IsGrounded) {
            // calculate the desired velocity from inputs, max speed, and current slope
            Vector3 targetVelocity = worldSpaceMoveInput * MaxSpeedOnGround * speedModifier;

            targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, groundNormal, transform.up) * targetVelocity.magnitude;

            // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
            CharacterVelocity = Vector3.Lerp(
                CharacterVelocity,
                targetVelocity,
                MovementSharpnessOnGround * Time.unscaledDeltaTime
            );
        } else {
            // handle air movement

            // We do the same horizontal movement as ground movement (Lerp + movement sharpness)
            // except for vertical (as its affected by gravity), which is handled separately
            Vector3 targetVelocity = worldSpaceMoveInput * MaxSpeedInAir * speedModifier;

            // Treat only horizontal movement like we do with ground movement
            float x = Mathf.Lerp(
                CharacterVelocity.x,
                targetVelocity.x,
                MovementSharpnessInAir * Time.unscaledDeltaTime
            );

            float z = Mathf.Lerp(
                CharacterVelocity.z,
                targetVelocity.z,
                MovementSharpnessInAir * Time.unscaledDeltaTime
            );

            // Handle vertical velocity (isn't limited like horizontal is)

            float verticalVelocity = CharacterVelocity.y;
            // Note this is negative as it's a downward force
            float downForce = -GravityDownForce * Time.unscaledDeltaTime;
            float y = verticalVelocity + downForce; // note this is actually subtracting

            // Should we add a terminal velocity?

            CharacterVelocity = new Vector3(x, y, z);
        }

        // Handle jumping

        (bool didJump, Vector3 newCharacterVelocity) = GetCanJumpAndVelocity(inputHandler, CharacterVelocity, IsGrounded, JumpForce);
        if (didJump) {
            // Set character velocity to new one
            CharacterVelocity = newCharacterVelocity;

            // remember last time we jumped because we need to prevent snapping to ground for a short time
            lastTimeJumped = Time.time;
            // HasJumpedThisFrame = true; // I don't think we ever use this

            // Force grounding to false
            IsGrounded = false;
            groundNormal = Vector3.up;
        }

        // Apply the velocity and deal with obstructions

        // Apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(characterController.height);

        characterController.Move(CharacterVelocity * Time.unscaledDeltaTime);

        // detect obstructions to adjust velocity accordingly
        if (Physics.CapsuleCast(
            capsuleBottomBeforeMove,
            capsuleTopBeforeMove,
            characterController.radius,
            CharacterVelocity.normalized,
            out RaycastHit hit,
            CharacterVelocity.magnitude * Time.unscaledDeltaTime,
            -1,
            QueryTriggerInteraction.Ignore
        )) {
            CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
        }
    }

    // Called by Update()
    private void HandleLooking() {
        // Handle horizontal camera rotation

        // Rotate the transform with the input speed around it's local Y axis
        transform.Rotate(
            new Vector3(
                0f,
                inputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier,
                0f
            ),
            Space.Self
        );

        // Handle vertical camera rotation

        // add vertical inputs to the camera's vertical angle
        cameraVerticalAngle += -inputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;

        // limit the camera's vertical angle to min/max
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -89f, 89f);

        // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
    }

    // Returns modified (canJump, newCharacterVelocity)
    private static (bool, Vector3) GetCanJumpAndVelocity(
        InputHandler inputHandler, // we could pass in GetJumpInputDown(), but this is probably better (since we're not testing it)
        Vector3 previousCharacterVelocity,
        bool isGrounded,
        float jumpForce
    ) {
        bool shouldJump = inputHandler.GetJumpInputDown() && isGrounded;
        if (shouldJump) {
            // start by cancelling out the vertical component of our velocity
            // Note that we also want to do this when we're in the
            // air (aka falling) - I assume for double jumping (this is probably an old comment)
            Vector3 retCharacterVelocity = new Vector3(
                previousCharacterVelocity.x,
                0f, // I'm not sure if we want to cancel this out? But hey maybe we do
                previousCharacterVelocity.z
            );

            // then, add the jumpSpeed value upwards
            retCharacterVelocity.y = jumpForce;

            // play sound
            // AudioSource.PlayOneShot(JumpSfx);

            return (true, retCharacterVelocity);
        }

        // Not jumping, returning existing CharacterVelocity
        return (false, previousCharacterVelocity);
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule
    private Vector3 GetCapsuleBottomHemisphere() {
        return transform.position + (transform.up * characterController.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule
    private Vector3 GetCapsuleTopHemisphere(float atHeight) {
        return transform.position + (transform.up * (atHeight - characterController.radius));
    }


    // Used by GroundedCheck()
    private static bool IsNormalUnderSlopeLimit(
        Vector3 normal, // actual input
        Vector3 transformUp, // this.transform.up
        CharacterController characterController // this.characterController
    ) {
        return Vector3.Angle(transformUp, normal) <= characterController.slopeLimit;
    }

    // Gets a reoriented direction that is tanget to a given slope
    // Used by HandleCharacterMovement()
    public static Vector3 GetDirectionReorientedOnSlope(
        Vector3 direction,
        Vector3 slopeNormal,
        Vector3 transformUp // = this.transform.up
    ) {
        Vector3 directionRight = Vector3.Cross(direction, transformUp);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    public override Material GetMaterial() {
        throw new NotImplementedException();
    }

    public override Vector3 GetMiddleOfMesh() {
        throw new NotImplementedException();
    }
}

