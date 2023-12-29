using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

// Switches shortcut presets depending on if we're in play mode or edit mode
// From: https://forum.unity.com/threads/unwanted-editor-hotkeys-in-game-mode.182073/
[InitializeOnLoad]
public static class EnterPlayModeShortcuts {
    private static const playModeID = "Play Mode";
    private static const editModeID = "Default copy";

    static EnterPlayModeShortcuts() {
        EditorApplication.playModeStateChanged += ModeChanged;
        EditorApplication.quitting += Quitting;
    }

    private static void ModeChanged(PlayModeStateChange playModeState) {
        if (playModeState == PlayModeStateChange.EnteredPlayMode)
            ShortcutManager.instance.activeProfileId = playModeID;
        else if (playModeState == PlayModeStateChange.EnteredEditMode)
            ShortcutManager.instance.activeProfileId = editModeID;
    }

    private static void Quitting() {
        ShortcutManager.instance.activeProfileId = editModeID;
    }
}
