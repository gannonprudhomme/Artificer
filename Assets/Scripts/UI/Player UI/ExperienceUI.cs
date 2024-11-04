using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#nullable enable

public class ExperienceUI : MonoBehaviour {
    [Tooltip("The image used to show how much experience the player has")]
    public Image? ExperienceFillImage;

    [Tooltip("Text used for setting the player's current level in the bottom right")]
    public TextMeshProUGUI? CurrentLevelTextBottomRight;

    [Tooltip("Text used for setting the player's current level")]
    public TextMeshProUGUI? CurrentLevelTextTopRight;

    private Experience? experience;

    // Start is called before the first frame update
    void Start() {
        experience = PlayerController.instance!.experience;
        
        CurrentLevelTextBottomRight!.text = $"{experience!.currentLevel}";
        CurrentLevelTextTopRight!.text = $"Lv. {experience.currentLevel}";
    }

    // Update is called once per frame
    void Update() {
        int experienceNeededForThisLevel = Experience.GetExperienceRequiredToLevel(experience!.currentLevel);
        int experiencedNeededForNextLevel = Experience.GetExperienceRequiredToLevel(experience.currentLevel + 1);

        int experienceBetweenLevels = experiencedNeededForNextLevel - experienceNeededForThisLevel;
        int substracted = experience.totalExperience - experienceNeededForThisLevel;

        float percentage = (float) substracted / (float) experienceBetweenLevels;

        ExperienceFillImage!.fillAmount = percentage;
        CurrentLevelTextBottomRight!.text = $"{experience.currentLevel}";
        CurrentLevelTextTopRight!.text = $"Lv. {experience.currentLevel}";
    }
}
