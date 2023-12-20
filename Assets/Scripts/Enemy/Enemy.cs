using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : Entity {
    private EnemyManager enemyManager;

    protected override void Start() {
        base.Start();

        enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager == null) {
            return;
		}

        enemyManager.AddEnemy(this);

        health.OnDeath += OnDeath;
    }

    private void OnDeath() {
        enemyManager.RemoveEnemy(this);

        Destroy(this.gameObject);
    }
}
