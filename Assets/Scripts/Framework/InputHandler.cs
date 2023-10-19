using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class InputConstants {
    public const string AxisNameVertical = "Vertical";
    public const string AxisNameHorizontal = "Horizontal";
    public const string MouseAxisNameVertical = "Mouse Y";
    public const string MouseAxisNameHorizontal = "Mouse X";
    public const string AxisNameJoystickLookVertical = "Look Y";
    public const string AxisNameJoystickLookHorizontal = "Look X";

    public const string Aim = "Aim"; // Aka zoom
    public const string Sprint = "Sprint";
    public const string Jump = "Jump";
    public const string Dash = "Jump";
    public const string AttackPrimary = "AttackPrimary";
    public const string AttackSecondary = "AttackSecondary";
}

// Note that the previous code had a bunch of checks for CanProcessInput()
// which is presumably for ignoring the inputs when we e.g. pause the game
public class InputHandler : MonoBehaviour {
    [Tooltip("Sensitivity multiplier for moving the camera around")]
    public float LookSensitivity = 1f;

    [Tooltip("Limit to consider an input when using a trigger on a controller")]
    public float TriggerAxisThreshold = 0.4f;

    // bool fireInputWasHeld;
    bool attackPrimaryInputWasHeld;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Wtf is this
    void LateUpdate() {
        attackPrimaryInputWasHeld = GetAttackPrimaryInputHeld();
    }

    public bool CanProcessInput() {
        // return true;
        return Cursor.lockState == CursorLockMode.Locked; // &&!gameFlowManager.GameIsEnding
    }

    public Vector3 GetMoveInput() {
        if (CanProcessInput()) {
            var x = Input.GetAxisRaw(InputConstants.AxisNameHorizontal);
            var z = Input.GetAxisRaw(InputConstants.AxisNameVertical);
            Vector3 move = new(x, 0, z);

            // constrain move input to a maximum mangitude of 1, otherwise diagonal movemenet
            // might exceed the max move speed defined
            move = Vector3.ClampMagnitude(move, 1);
            print(move);

            return move;
        }

        return Vector3.zero;
    }

    public float GetLookInputsHorizontal() {
        return GetMouseOrStickLookAxis(
            InputConstants.MouseAxisNameHorizontal
        );
    }

    public float GetLookInputsVertical() {
        return GetMouseOrStickLookAxis(
            InputConstants.MouseAxisNameVertical
        );
    }

    public bool GetJumpInputDown() {
        return Input.GetButtonDown(InputConstants.Jump);
    }

    public bool GetJumpInputHeld() {
        return Input.GetButton(InputConstants.Jump);
    }

    public bool GetAttackPrimaryInputDown() {
        return GetAttackPrimaryInputHeld() && !attackPrimaryInputWasHeld;
    }

    public bool GetAttackPrimaryInputReleased() {
        return !GetAttackPrimaryInputHeld() && attackPrimaryInputWasHeld;
    }

    public bool GetAttackPrimaryInputHeld() {
        return Input.GetButton(InputConstants.AttackPrimary);
    }

    public bool GetAimInputHeld() {
        return Input.GetButton(InputConstants.Aim);
    }

    public bool GetSprintInputHeld() {
        return Input.GetButton(InputConstants.Sprint);
    }

    public int GetSelectWeaponInput() {
        if (CanProcessInput()) {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                return 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                return 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                return 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                return 4;
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                return 5;
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                return 6;
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                return 7;
            else if (Input.GetKeyDown(KeyCode.Alpha8))
                return 8;
            else if (Input.GetKeyDown(KeyCode.Alpha9))
                return 9;
            else
                return 0;
        }

        return 0;
    }


    float GetMouseOrStickLookAxis(string mouseInputName) {
        if (CanProcessInput()) {
            bool isGamepad = false;
            float i = Input.GetAxisRaw(mouseInputName);

            // invert y axis
            i *= LookSensitivity;

            if (!isGamepad) {
                // reduce mouse input amonut to be equivalent to stick movement??
                i *= 0.01f;

                // handle webgl mouse accelaration sensitivity;
            }

            return i;
        }

        return 0;
    }
}
