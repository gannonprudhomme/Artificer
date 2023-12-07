using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour {
    public Image FillImage;

    [Tooltip("Health of the enemy this is displaying for")]
    public Health health;

    // Transform of the target this should look at
    // Set by the Enemy that owns this
    public Transform Target { get; set; }

    void Update() {
        if (health == null) {
            Debug.LogError($"{this.name}'s health was not passed, returning");
            return;
        }

        // Set fill based on % of health the enemy has
        FillImage.fillAmount = (int) health.CurrentHealth / health.MaxHealth;

        // Make the health bar look at the player at all times
        // transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(Target.position));
        transform.LookAt(Target.position);
    }
}
