using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Animations;

#nullable enable

// Exists solely to store & get references to/from the Player
// as a singleton
public sealed class CameraObjects : MonoBehaviour {
    public static CameraObjects? instance { get; private set; }
    
    [Tooltip("The main camera of the game")]
    public Camera? MainCamera;

    // This is the same GameObject as the PositionConstraint
    [Tooltip("The transform we use to rotate (on the PlayerController) so the camera rotates with the player/mouse")]
    public Transform? CameraAimPoint;
    
    [Tooltip("Position constraint we set to the Player's 'Main Camera Look At' transform")]
    public PositionConstraint? CameraLookAtPositionConstraint;

    [Tooltip("Reference to the (3rd Person Follow) Virtual Camera. Used to change the FOV when the player is sprinting.")]
    public CinemachineVirtualCamera? VirtualCamera;
    
    private void Awake() {
        instance = this;
    }

    private IEnumerator Start() {
        while (PlayerController.instance == null) {
            yield return null;
        }
        
        SetCameraLookAt();
    }

    private void SetCameraLookAt() {
        ConstraintSource constraintSource = CameraLookAtPositionConstraint!.GetSource(0); // This is a copy, as ConstraintSource is a struct
        constraintSource.sourceTransform = PlayerController.instance!.MainCameraLookAt;
        constraintSource.weight = 1; // It should be set to 1, but make sure it is just in case
        CameraLookAtPositionConstraint.SetSource(0, constraintSource);
    }
}
