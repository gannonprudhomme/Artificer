using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Contains 8 gold & 4 exp
[RequireComponent(typeof(Animator))]
public class Barrel : SpawnableInteractable {

    private Animator? animator;

    private const string ANIM_IS_OPEN = "IsOpen";

    protected override void Start() {
        base.Start();

        animator = GetComponent<Animator>();
        animator!.SetBool(ANIM_IS_OPEN, false);

        foreach(Material material in GetMaterials()) {
            material.SetInt("_CanPurchase", 1);
        }
    }

    public override void OnSelected(GoldWallet goldWallet, ItemsDelegate _) {
        Debug.Log("Barrel selected!");

        // Give it the money
        goldWallet.GainGold(8);

        foreach(Material material in GetMaterials()) {
            material.SetInt(SHADER_OUTLINE_IS_ENABLED, 0);
        }
        animator!.SetBool(ANIM_IS_OPEN, true);

        hasBeenInteractedWith = true;

        // Also give experience
    }

    public override void OnHover() {
        base.OnHover();

        if (!hasBeenInteractedWith) {
            HoverEvent!.OnHover("Open barrel", null);
        }
    }

    public override Vector3 GetSpawnPositionOffset() {
        return Vector3.up * -1.5f;
    }
    public override Quaternion GetSpawnRotationOffset() {
        float xzRange = 15;

        return Quaternion.Euler(
            x: Random.Range(-xzRange, xzRange),
            y: Random.Range(0, 360),
            z: Random.Range(-xzRange, xzRange) 
        );
    }

}
