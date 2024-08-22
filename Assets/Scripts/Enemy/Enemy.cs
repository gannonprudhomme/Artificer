using System;
using UnityEngine;
using UnityEngine.VFX;

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

    [Tooltip("The VFX instance that's played when the enemy is stunned")]
    public VisualEffect? StunnedStatusVFXInstance;

    [Tooltip("Where we instantiate the ExperienceGranter & DiedWhileFrozen VFX when the enemy dies")]
    public Transform? VFXSpawnPoint;

    [Tooltip("Prefab of the experience granter. Created when the entity dies")]
    public ExperienceGranter? ExperienceGranterPrefab;

    [Tooltip("VFX prefab which is created when the enemy dies from being frozen")]
    public VisualEffect? DiedWhileFrozenVFXPrefab;

    [Tooltip("Scriptable object event that gets triggered when the enemy dies. Used to propogate the event to Items.")]
    public OnEnemyKilledEvent? OnEnemyKilledEvent;

    protected EnemyManager? enemyManager;

    private bool isStunnedStatusEffectActive = false;

    // the Director is going to set this when we spawn the enemy
    public int ExperienceGrantedOnDeath { get; set; }
    public int GoldGrantedOnDeath => ExperienceGrantedOnDeath * 2;

    // Set by the director when this is spawned
    public float Level = 1;
    
    protected abstract float StartingHealth { get; }
    protected abstract float HealthIncreasePerLevel { get; }
    protected abstract float DamageIncreasePerLevel { get; }
    public float MaxHealth => StartingHealth + (HealthIncreasePerLevel * Level);
    public override float CurrentBaseDamage => StartingBaseDamage + (DamageIncreasePerLevel * Level);

    public abstract string EnemyIdentifier { get; }

    protected override void Start() {
        base.Start();

        enemyManager = FindObjectOfType<EnemyManager>();

        if (enemyManager == null) {
            return;
		}

        enemyManager.AddEnemy(this);

        health!.SetMaxHealth(MaxHealth);
        health!.OnDeath += OnDeath;

        if (Target) {
            HealthAndStatusBarFollowPlayer!.Target = Target!.Camera!.transform;
		}

        StunnedStatusVFXInstance!.Stop();
    }

    protected virtual void OnDeath() {
        enemyManager!.RemoveEnemy(this);

        GrantExperienceAndGold();

        CheckAndPlayDiedWhileFrozenVFX();

        CallAllStatusEffectsOnFinished();

        OnEnemyKilledEvent!.Event?.Invoke(BleedVFXSpawnTransform!.transform.position);

        Destroy(this.gameObject);
    }

    protected void GrantExperienceAndGold() {
        // Grant the Target experience I guess
        // There's gotta be a better way to do this
        if (Target!.TryGetComponent(out Experience experience)) {
            // We gotta calculate this somehow
            ExperienceGranter granter = Instantiate(
                ExperienceGranterPrefab!,
                VFXSpawnPoint!.position,
                Quaternion.identity
            );

            granter.Emit(
                experienceToGrant: ExperienceGrantedOnDeath,
                targetExperience: experience,
                goalTransform: Target!.AimPoint!
            );
        } else {
            Debug.LogError("Couldn't find Target's Experience");
        }

        if (Target!.TryGetComponent(out GoldWallet goldWallet)) {
            goldWallet.GainGold(GoldGrantedOnDeath);
        } else {
            Debug.LogError("Couldn't find Target's GoldWallet");
        }
    }

    // Ideally we'd just have some common (but not sub-classed) Stunned state which would let us do this
    protected void StartStunnedVFXIfNotPlaying() {
        if (isStunnedStatusEffectActive) {
            return;
        }

        isStunnedStatusEffectActive = true;

        StunnedStatusVFXInstance!.Play();
    }

    protected void StopStunnedVFX() {
        if (!isStunnedStatusEffectActive) {
            return;
        }

        isStunnedStatusEffectActive = false;

        StunnedStatusVFXInstance!.Stop();
    }

    protected void CheckAndPlayDiedWhileFrozenVFX() {
        if (!isFrozen) return;

        VisualEffect deathFromFreezeVFX = Instantiate(
            DiedWhileFrozenVFXPrefab!,
            position: VFXSpawnPoint!.transform.position,
            rotation: Quaternion.identity
        );
        deathFromFreezeVFX.gameObject.SetActive(true); // It's disabled for the stone golem (so we can have a custom variant w/o a prefab)
        deathFromFreezeVFX.Play();

        // Destroy it after it's done (guessing on time here)
        Destroy(deathFromFreezeVFX, 2.0f);
    }

    protected void CallAllStatusEffectsOnFinished() {
        foreach(var statusEffect in statusEffects.Values) {
            statusEffect.OnFinished(this);
        }
    }
}
