using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Akin to Health, but for experience / the player's level
// Only the Player has one of these
//
// Each subsequent level requires 1.55 times as much experience as the previous one
public class Experience : MonoBehaviour {
    public int totalExperience { get; private set; } 

    public UnityAction<int> OnLevelUp;

    public int currentLevel {
        get {
            return (int) Mathf.Log(1 + 0.0275f * totalExperience, 1.55f) + 1;
        }
    }

    void Start() {
        totalExperience = 0;
    }

    public void GainExperience(int numExperience) {
        int prevLevel = currentLevel;
        totalExperience += numExperience;

        if (currentLevel > prevLevel) {
            // We leveled up! Notify
            OnLevelUp?.Invoke(currentLevel);
        }
    }

    public static int GetExperienceRequiredToLevel(int goalLevel) {
        return (int) ((Mathf.Pow(1.55f, goalLevel - 1) - 1) / 0.0257f);
        // return (int) (-4f / (0.11f  * (1 - Mathf.Pow(1.55f, goalLevel - 1))));
    }
}
