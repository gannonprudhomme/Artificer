using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour {
    [Tooltip("Image to be used for the health fill %")]
    public Image FillImage;

    [Tooltip("Reference to the parent game object which contains the frozen outline image that displays over the health bar when the enemy is frozen")]
    public GameObject FrozenOutline;
    
    [Tooltip("The entity (enemy) this is displaying for")]
    public Entity entity;

    private void Start() {
        entity.health.OnDeath += OnDeath;
    }

    void Update() {
        if (entity.health == null) {
            Debug.LogError($"{this.name}'s health was not passed, returning");
            return;
        }

        // Set fill based on % of health the enemy has
        FillImage.fillAmount = (int) entity.health.CurrentHealth / entity.health.MaxHealth;

        // Hide / show the health bar frozen "outline" if the enemy is frozen
        FrozenOutline.SetActive(entity.GetIsFrozen());
    }

    private void OnDeath() {
        Destroy(this.gameObject);
    }
}
