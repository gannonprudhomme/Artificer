using UnityEngine;

#nullable enable

// we might want this? So it triggers Collider triggers
// [RequireComponent(typeof(RigidBody))]
public abstract class Enemy : Entity {
    [Header("Enemy (inherited)")]
    [Tooltip("Where the NavMeshAgent is going to navigate to. Should be the player")]
    public Target? Target;

    [Tooltip("Reference to the UIFollowPlayer component for the Enemy Health & Status Bar. Needed so we can set the target on the health bar (so it looks at the player)")]
    // public EnemyHealthBar healthBar;
    public UIFollowPlayer? HealthAndStatusBarFollowPlayer;

    private EnemyManager? enemyManager;

    // the Director is going to set this when we spawn the enemy
    public int ExperienceGrantedOnDeath { get; set; }
    public int GoldGrantedOnDeath => ExperienceGrantedOnDeath * 2;

    public abstract string EnemyIdentifier { get; }

    protected override void Start() {
        base.Start();

        enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager == null) {
            return;
		}

        enemyManager.AddEnemy(this);

        health!.OnDeath += OnDeath;

        if (Target) {
            HealthAndStatusBarFollowPlayer!.Target = Target!.Camera!.transform;
		}
    }

    protected virtual void OnDeath() {
        enemyManager!.RemoveEnemy(this);

        // Grant the Target experience I guess
        // There's gotta be a better way to do this
        if (Target!.TryGetComponent(out Experience experience)) {
            // We gotta calculate this somehow
            experience.GainExperience(ExperienceGrantedOnDeath);
        } else {
            Debug.LogError("Couldn't find Target's Experience");
        }

        if (Target!.TryGetComponent(out GoldWallet goldWallet)) {
            goldWallet.GainGold(GoldGrantedOnDeath);
        } else {
            Debug.LogError("Couldn't find Target's GoldWallet");
        }

        Destroy(this.gameObject);
    }
}
