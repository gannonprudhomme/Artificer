using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

#nullable enable

public class PlayerCameraController : MonoBehaviour {
    public CinemachineVirtualCamera? VirtualCamera;

    [Tooltip("The default FOV when the player isn't sprinting")]
    public int DefaultFOV;

    // TODO: Should this change by speed?
    [Tooltip("The FOV when the player is sprinting")]
    public float MaxSprintFOV;

    // When we're between these two floats we Lerp
    public float MaxPlayerWalkSpeed = 15;
    public float MaxPlayerSprintSpeed = 22;

    public bool IsPlayerSprinting { get; set; }

    private readonly float smoothTime = 0.2f;
    private float currentDampVelocity = 0f;

    // Start is called before the first frame update
    // Update is called once per frame
    void Update() {
        if (VirtualCamera == null) { return; }

        float goalFOV =  IsPlayerSprinting ? MaxSprintFOV : DefaultFOV;

        float fov = Mathf.SmoothDamp(VirtualCamera.m_Lens.FieldOfView, goalFOV, ref currentDampVelocity, smoothTime);

        VirtualCamera.m_Lens.FieldOfView = fov;
    }
}
