using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

// Switches shortcut presets depending on if we're in play mode or edit mode
// From: https://forum.unity.com/threads/unwanted-editor-hotkeys-in-game-mode.182073/
//
// Go to Edit -> Shortcuts, add a new profile named {playModeID} and a reuse the existing / default one for the editor named {editModeID}.
// Then for the Play Mode profile, remove the shortcut you don't want (in my case it was save for Ctrl+S)
// I also change Enter Play Mode to Ctrl + R
[InitializeOnLoad]
public static class ChangePlayModeShortcuts {
    private static readonly string playModeID = "Play Mode";
    private static readonly string editModeID = "Default copy";

    static ChangePlayModeShortcuts() {
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
