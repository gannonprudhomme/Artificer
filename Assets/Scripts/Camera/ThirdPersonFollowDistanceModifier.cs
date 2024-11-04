using System.Collections;
using Cinemachine;
using UnityEngine;

#nullable enable

// TODO: I need to figure out if this should be on the player or the virtual camera
// namely since it depends on the PlayerController to get the min & max angles
// 

// Modifies the camera distance as a function of the vertical angle to closer represent
// the functionality of the FreeLook camera.
//
// Basically entirely copied from the Cinemachine AimRigging sample 
[RequireComponent(typeof(CinemachineVirtualCamera))]
public sealed class ThirdPersonFollowDistanceModifier : MonoBehaviour {

    [Tooltip("Defines how the camera distance scales a fucntion of vertical Camera angle." + 
             "X axis of graph is from [0, 1]; Y-Axis is the multipler applied to base dist")]
    public AnimationCurve? DistanceScale;

    // Retrieved from the VirtualCamera
    private Cinemachine3rdPersonFollow? thirdPersonFollow = null;

    private PlayerController? playerController;
    private Transform? followTarget = null;
    private float baseDistance = Mathf.Infinity;

    private IEnumerator Start() {
        while (PlayerController.instance == null) {
            yield return null;
        }
        
        playerController = PlayerController.instance;
        
        if (!TryGetComponent(out CinemachineVirtualCamera virtualCamera)) {
            Debug.LogError("Couldn't find vcam in child!");
            yield break;
        }

        thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        // Store the base camera distance as it's what'll be used to scale the camera distance (max distance == BaseDistance * DistanceScale[1.0f])
        if (thirdPersonFollow != null) {
            baseDistance = thirdPersonFollow.CameraDistance;
            followTarget = virtualCamera.Follow;
        } else {
            Debug.LogError("ThirdPersonFollow is null!");
        }
    }

    private void Update() {
        if (thirdPersonFollow == null || followTarget == null) {
            Debug.LogError("ThirdPersonFollow/FollowTarget are null!");
            return;
        };

        // Scale the third person camera distance based on how much the camera is looking up (i.e. closer to the ground / min angle)
        // Makes it so the closer we are to the ground, the closer the camera is to the player (well, assuming the AnimationCurve is 0)
        // with maxAngle corresponding to full base distance, and minAngle corresponding to DistanceScale[0f] * BaseDistance (usually 0.5)

        float verticalAngle = followTarget.rotation.eulerAngles.x; // Rotation along x-axis is the "vertical" rotation (aiming up/down)
        if (verticalAngle > 180) { // Keep vertical angle in the range [-180, 180]
            verticalAngle -= 360;
        }

        float max = playerController!.VerticalRotationMax;
        float min = playerController!.VerticalRotationMin;
        float percent = (verticalAngle - min) / (max - min); // Normalize it to [0, 1] where 0 is MinAngle, and 1 is MaxAngle

        thirdPersonFollow.CameraDistance = baseDistance * DistanceScale!.Evaluate(percent);
    }
}
