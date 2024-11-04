using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

#nullable enable

// Increases the FOV when the player is sprinting
public sealed class PlayerCameraController : MonoBehaviour {

    [Tooltip("The default FOV when the player isn't sprinting")]
    public int DefaultFOV;

    // TODO: Should this change by speed?
    [Tooltip("The FOV when the player is sprinting")]
    public float MaxSprintFOV;

    // When we're between these two floats we Lerp
    public float MaxPlayerWalkSpeed = 15;
    public float MaxPlayerSprintSpeed = 22;

    public bool IsPlayerSprinting { get; set; }

    private CinemachineVirtualCamera? VirtualCamera;

    private readonly float smoothTime = 0.2f;
    private float currentDampVelocity = 0f;

    private void Start() {
        VirtualCamera = CameraObjects.instance!.VirtualCamera;
    }

    // Start is called before the first frame update
    // Update is called once per frame
    private void Update() {
        if (VirtualCamera == null) { return; }

        float goalFOV =  IsPlayerSprinting ? MaxSprintFOV : DefaultFOV;

        float fov = Mathf.SmoothDamp(VirtualCamera.m_Lens.FieldOfView, goalFOV, ref currentDampVelocity, smoothTime);

        VirtualCamera.m_Lens.FieldOfView = fov;
    }
}
