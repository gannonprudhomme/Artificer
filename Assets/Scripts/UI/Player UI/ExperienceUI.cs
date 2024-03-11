using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ExperienceUI : MonoBehaviour {
    [Tooltip("The image used to show how much experience the player has")]
    public Image ExperienceFillImage;

    [Tooltip("Text used for setting the player's current level")]
    public TextMeshProUGUI CurrentLevelText;

    [Tooltip("Reference to the experience component of the player")]
    public Experience Experience;

    // Start is called before the first frame update
    void Start() {
        CurrentLevelText.text = $"{Experience.currentLevel}";
    }

    // Update is called once per frame
    void Update() {
        int experienceNeededForThisLevel = Experience.GetExperienceRequiredToLevel(Experience.currentLevel);
        int experiencedNeededForNextLevel = Experience.GetExperienceRequiredToLevel(Experience.currentLevel + 1);

        int experienceBetweenLevels = experiencedNeededForNextLevel - experienceNeededForThisLevel;
        int substracted = Experience.totalExperience - experienceNeededForThisLevel;

        float percentage = (float) substracted / (float) experienceBetweenLevels;

        ExperienceFillImage.fillAmount = percentage;
        CurrentLevelText.text = $"{Experience.currentLevel}";
    }
}
