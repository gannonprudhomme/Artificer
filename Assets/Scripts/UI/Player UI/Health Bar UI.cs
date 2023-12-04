using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour {
    public Image FillImage;

    [Tooltip("Optional health text. Will display like {CurrHealth}/{MaxHealth}")]
    public TextMeshProUGUI CurrentHealthText;

    public Health health;

    void Update() {
        if (health == null) {
            Debug.LogError($"{this.name}'s health was not passed, returning");
            return;
        }

        FillImage.fillAmount = (int) health.CurrentHealth / health.MaxHealth;

        if (CurrentHealthText != null ) {
            CurrentHealthText.text = $"{(int)health.CurrentHealth} / {(int) health.MaxHealth}";
        }
    }
}
