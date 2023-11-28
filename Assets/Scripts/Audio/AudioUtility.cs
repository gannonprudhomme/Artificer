using System;
using System.Collections;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public class AudioUtility {
    public static AudioUtility shared = new();

    private static AudioManager s_AudioManager;

    public enum AudioGroups {
        DamageTick,
        Impact,
        EnemyDetection,
        Pickup,
        WeaponShoot,
        WeaponOverheat,
        WeaponChargeBuildup,
        WeaponChargeLoop,
        HUDVictory,
        HUDObjective,
        EnemyAttack
    }

    AudioUtility() {
        // is there a better way to pass this to this?
        s_AudioManager = GameObject.FindObjectOfType<AudioManager>();
    }

    public void CreateSFX(
        AudioClip clip,
        Vector3 position,
        AudioGroups audioGroup,
        float spatialBlend, // I think I always want this to be 1? Idk 2D vs 3D audio
        float rolloffDistanceMin = 1f
    ) {
        GameObject sfxInstance = new();
        sfxInstance.transform.position = position;
        AudioSource source = sfxInstance.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = spatialBlend;
        source.minDistance = rolloffDistanceMin;
        source.Play();

        source.outputAudioMixerGroup = GetAudioGroup(audioGroup);

        GameObject.Destroy(sfxInstance, clip.length); // Destroy the game object after it's done playing
    }

    public AudioMixerGroup GetAudioGroup(AudioGroups group) {
        if (s_AudioManager == null)
            s_AudioManager = GameObject.FindObjectOfType<AudioManager>();

        var groups = s_AudioManager.FindMatchingGroups(group.ToString());

        if (groups != null && groups.Length == 0) { // guard
            Debug.LogWarning("Didn't find audio group for " + group.ToString());
            return null;
        }

        return groups[0];
    }

    public void SetMasterVolume(float value) {
        if (s_AudioManager == null)
            s_AudioManager = GameObject.FindObjectOfType<AudioManager>();

        if (value <= 0)
            value = 0.001f; // Why not 0?

        float valueInDb = Mathf.Log10(value) * 20;

        s_AudioManager.SetFloat("MasterVolume", valueInDb);
    }

    public float GetMasterVolume() {
        if (s_AudioManager == null)
            s_AudioManager = GameObject.FindObjectOfType<AudioManager>();

        s_AudioManager.GetFloat("MasterVolume", out var valueInDb);
        return Mathf.Pow(10f, valueInDb / 20.0f);
    }
}
