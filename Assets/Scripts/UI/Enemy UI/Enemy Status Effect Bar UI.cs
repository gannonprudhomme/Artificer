using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This gets all of the status effects UI for the Enemy it belongs to
// and adds / removes them all
public class EnemyStatusEffectBarUI : MonoBehaviour {
    [Tooltip("Reference to the Entity component for the enemy we're displaying this for")]
    public Entity Entity; 

    [Tooltip("The prefab of the EnemyStatusEffectUI we'll use to make new ones")]
    public EnemyStatusEffectUI EnemyStatusEffectUIPrefab;

    // Maintains a mapping between the BaseStatusEffect.name: Instance of EnemyStatusEffectUI
    private Dictionary<string, EnemyStatusEffectUI> statusEffectUIDict = new();

    void Start() {
        Entity.OnStatusEffectAdded += OnStatusEffectAdded;
        Entity.OnStatusEffectRemoved += OnStatusEffectRemoved;

        Entity.health.OnDeath += OnDeath;
    }

    private void OnStatusEffectAdded(BaseStatusEffect effect) {
        // Some effects won't display anything, so don't do anything
        if (effect.ImageName == null) {
            return;
        }

        // Create an instance
        EnemyStatusEffectUI uiInstance = Instantiate(EnemyStatusEffectUIPrefab, transform);
        uiInstance.StatusEffect = effect;

        // Add it to the dictionary
        statusEffectUIDict[effect.Name] = uiInstance;
    }

    private void OnStatusEffectRemoved(BaseStatusEffect effect) {
        if (effect.ImageName == null) {
            return;
        }

        // Remove it, and destroy the instance
        if(statusEffectUIDict.TryGetValue(effect.Name, out EnemyStatusEffectUI uiInstance)) {
            Destroy(uiInstance.gameObject);
        } else {
            Debug.LogError($"Failed to delete status effect UI instance of {effect.Name} on {this.gameObject}");
        }
    }

    private void OnDeath() {
        Entity.OnStatusEffectAdded -= OnStatusEffectAdded;
        Entity.OnStatusEffectRemoved -= OnStatusEffectRemoved;

        Destroy(this.gameObject);
    }
}
