using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class DebugTeleporter : Interactable {
    public override void OnSelected(GoldWallet _, Experience __, Transform ___, ItemsDelegate ____) {
        LevelManager.instance.ChangeLevel();
    }
}

// something
