using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

public class EnemyHealthBar : MonoBehaviour {
    [Tooltip("Image to be used for the health fill %")]
    public Image? FillImage;

    [Tooltip("Reference to the parent game object which contains the frozen outline image that displays over the health bar when the enemy is frozen")]
    public GameObject? FrozenOutline;

    [Tooltip("Canvas that contains the healthbar. Used to hide / show it when the enemy is at full health")]
    public Canvas? CanvasContainer; 
    
    [Tooltip("The entity (enemy) this is displaying for")]
    public Entity? entity;

    private void Start() {
        entity!.health!.OnDeath += OnDeath;
    }

    void Update() {
        if (entity!.health == null) {
            Debug.LogError($"{this.name}'s health was not passed, returning");
            return;
        }

        float percent = entity.health.CurrentHealth / entity.health.MaxHealth;
        if (percent >= 0.9999f) {
            CanvasContainer!.enabled = false;
        } else {
            CanvasContainer!.enabled = true;
        }

        // Set fill based on % of health the enemy has
        FillImage!.fillAmount = ((int) entity.health.CurrentHealth) / entity.health.MaxHealth;

        // Hide / show the health bar frozen "outline" if the enemy is frozen
        FrozenOutline!.SetActive(entity.GetIsFrozen());
    }

    private void OnDeath() {
        Destroy(this.gameObject);
    }
}
