using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// we might want this? So it triggers Collider triggers
// [RequireComponent(typeof(RigidBody))]
public abstract class Enemy : Entity {
    [Header("Enemy (inherited)")]
    [Tooltip("Where the NavMeshAgent is going to navigate to. Should be the player")]
    public Target Target;

    [Tooltip("Reference to the UIFollowPlayer component for the Enemy Health & Status Bar. Needed so we can set the target on the health bar (so it looks at the player)")]
    // public EnemyHealthBar healthBar;
    public UIFollowPlayer HealthAndStatusBarFollowPlayer;

    private EnemyManager enemyManager;

    protected override void Start() {
        base.Start();

        enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager == null) {
            return;
		}

        enemyManager.AddEnemy(this);

        health.OnDeath += OnDeath;

        if (Target) {
            HealthAndStatusBarFollowPlayer.Target = Target.AimPoint;
		}
    }

    private void OnDeath() {
        enemyManager.RemoveEnemy(this);

        Destroy(this.gameObject);
    }
}
