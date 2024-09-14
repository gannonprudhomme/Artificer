using UnityEngine;

// General attack
// Attacks should be "dumb" since the Enemy will be controlling between diff attacks
// Might not need this: attacks aren't reusable by other enemies (I don't think - there base logic might)
// and each enemy will only have like 1-4 attacks which we'll manually balance (not iterate over)
public abstract class EnemyAttack {
    public bool canAttack = true;

    public virtual void OnUpdate(float entityBaseDamage) { }
}

// I don't really think this should be a MonoBehaviour? It's not going to be a component or anything
// And the settings for the attacks we'll probably just set on the Stone Golem
// But it will be nice to have a container for this attack

// This needs to:
// - Control the shader / LineRenderer for this
// - Deal with the AoE damage (or have some DamageArea thing that does that)
// - Control the timing of this once we're trying to attack
//
// The laser shouldn't be perfect - if the player strafes left/right the player should be able to avoid it
//
// From the wiki:
// - Tracks the player for 3 seconds
// - Deals 250% of base damage (20, +4 per level)
// - Must be within 10-45m of the target, and have line of sight
// - Has a cooldown of 5 seconds
public class GolemLaserAttack: EnemyAttack {
    private const float DamageCoefficient = 3.0f; // 300%

    // This should really be damage area
    // We should get the damage from the Stone Golem
    private float entityBaseDamage;

    private AnimationCurve firingSizeCurve;
    private LineRenderer lineRenderer;
    // Where on the stone golem we're aiming from
    // the start of the line
    private Transform startAimPoint;
    // What we're aiming at (the player)
    // The end of the line
    private Transform target;

    private DamageArea damageArea;

    // AudioClip that plays when the laser begins to charge
    private AudioClip chargingSfx;
    // AudioClip that plays when the laser fires
    private AudioClip fireSfx;

    // How far we can move (per second?) to try to aim towards the target
    // Probably going to be divided by Time.deltaTime, but idk
    // private const float aimSpeed = 1f;
    public float aimSpeed = 1f;
    // Where the end of the laser / line was last frame
    // though we can just read this from the line renderer really
    // Only public so we can do OnDrawGizmos() in Stone Golem
    public Vector3 lastEndAimPoint = Vector3.positiveInfinity; // TODO: Find a better name 

    // TODO: Put this shit below into structs we have too many

    // Are we in the middle of charging the laser (aka about to fire)?
    private bool isCharging;
    // Have we finished charging & we're in the middle of firing?
    private bool isFiring;

    // When we started firing the attack (after we finished charging)
    private float timeOfFireStart = Mathf.NegativeInfinity;
    // How long we play the "firing" animation for(?), before we start recharging again?
    // When timeOfFireStart > fireTime, we hide the line renderer
    private const float fireDuration = 0.25f;
    // When we started charging this attack
    private float timeOfChargeStart = Mathf.NegativeInfinity;
    // Charge the laser for 3 seconds before firing
    private const float chargeDuration = 3.0f;
    // Start flashing the lazer 2.5 seconds into the charge
    private const float aboutToFireFlashDuration = 1.5f;

    private const float maxFiringDistance = 45f * 1.5f;

    // The Time.time of when we last fired
    private float timeOfLastFire = Mathf.NegativeInfinity;
    private const float cooldown = 5.0f; // In seconds

    private const string SHADER_IS_FIRING = "_IsFiring";
    private const string SHADER_SHOULD_CHARGE_FLASH = "_ShouldChargeFlash";
    private const string SHADER_FIRING_LASER_SIZE = "_FiringLaserSize";

    // Expect this to be called in Start() (or Awake()?) in the Stone Golem
    public GolemLaserAttack(
        LineRenderer lineRenderer,
        Transform aimPoint,
        Transform target,
        DamageArea damageArea,
        AnimationCurve firingSizeCurve,
        AudioClip chargingSfx,
        AudioClip fireSfx
    ) {
        this.lineRenderer = lineRenderer;
        this.startAimPoint = aimPoint;
        this.target = target;
        this.firingSizeCurve = firingSizeCurve;
        this.chargingSfx = chargingSfx;
        this.fireSfx = fireSfx;
        this.damageArea = damageArea;

        this.lineRenderer.enabled = true;
        this.lineRenderer.useWorldSpace = true;
        this.lineRenderer.positionCount = 2;

        // We need to set the width to the _max_ possible value so the firing animation can scale up as it needs to
        // We could theoretically change it in code depending on the is charging vs is firing state,
        // but I think doing it all in teh shader is going to be easier.
        this.lineRenderer.startWidth = 1.0f;
        this.lineRenderer.endWidth = 1.0f;
    }

    // Called in Update() in Stone Golem
    public override void OnUpdate(float entityBaseDamage) {
        if (!canAttack) {
			// We do this a ton of unnecessary times doing it this way
			// But it's probably fine? For now at least (don't optimize early and shit)
            ResetAttack();
            return;
        }

	    this.entityBaseDamage = entityBaseDamage;

        // boolean checks are just to prevent unnecessary checking
        // we don't need to check if we can attack if we're currently attacking
        if (!isCharging && !isFiring && CanAttack()) {
            StartCharging();
        }

        if (isCharging) {
            HandleChargingAttack();
        } else if (isFiring) {
            // We should do this in another if block really
            // as this should run when HandleChargingAttack() sets isFiring to true (within the same frame)
            HandleFiringAttack();
        } else { // Not doing either
            return; // return early
        }

        // Runs whenever we're charging or firing
        AimAtTarget();
    }

    // The laser is getting ready to fire (apply damage)
    // this is when the line is appearing but is the thinest
    private void HandleChargingAttack() {
        float secondsIntoCharge = Time.time - timeOfChargeStart;
        bool readyToFire = secondsIntoCharge >= chargeDuration;
        if (readyToFire) { // TODO: I'm not really sure if this is where we should do this
            StartFiring();
            return;
        }

        lineRenderer.material.SetInt(SHADER_IS_FIRING, 0);
        
        if (secondsIntoCharge >= aboutToFireFlashDuration) {
            // Tell the shader (material) to start flashing
            lineRenderer.material.SetInt(SHADER_SHOULD_CHARGE_FLASH, 1);
        } else { // We're charging normally (not flashing) so animate accordingly?
            lineRenderer.material.SetInt(SHADER_SHOULD_CHARGE_FLASH, 0);
        }
    }

    // We're in the middle of firing - handle it
    private void HandleFiringAttack() {
        // We're charging
        // Probably pass secondsIntoCharge into the
        float secondsIntoFiring = Time.time - timeOfFireStart;

        if (secondsIntoFiring < fireDuration) { // Are we in the middle of the firing (animation)
            float percentIntoFiring = secondsIntoFiring / fireDuration;
            float size = firingSizeCurve.Evaluate(percentIntoFiring);
            lineRenderer.material.SetFloat(SHADER_FIRING_LASER_SIZE, size);
            
        } else { // we're done, wrap up
            ResetAttack();
        }
    }

    // TODO: This still sucks, I want to improve it
    private void AimAtTarget() {
        // Attempt to move as much as we can this frame to aim at the target

        // We don't want to lerp,
        // we want to mvoe the lastEndAimPoint in the direction of the player at a certain speed (Time.deltaTime * aimSpeed);
        // This way it's a constant movement (Lerp will make it dependent on player speed)
        Vector3 directionToPlayer = (target.position - lastEndAimPoint).normalized;
        float distanceToPlayer = Vector3.Distance(target.position, lastEndAimPoint);

        // Ensure we don't over-shoot the player by doing Min with the distanceToPlayer
        float aimSpeedPerFrame = Mathf.Min(Time.deltaTime * aimSpeed, distanceToPlayer);
        
        lastEndAimPoint += directionToPlayer * aimSpeedPerFrame;

        SetLineRendererPositions();
    }

    // This shouldn't modify anything
    private bool CanAttack() {
        float timeSinceLastFire = Time.time - timeOfLastFire;

        if (timeSinceLastFire < cooldown) {
            // Can't attack since we haven't cooled down enough
            return false;
        }

        // We should really use the Stone Golem's main transform, not the aim point (it's gonna be farther away from the target?)
        // probs negligible though (REMOVE THIS)
        float distanceToTarget = Vector3.Distance(startAimPoint.position, target.position);
        // Are we within 10 - 45m of the target (lets say 2m for the min for now, or whatever I set the stopping distance to)
        if (distanceToTarget < maxFiringDistance) {
            // Debug.Log("Within distance, can attack!");
            return true;
        }

        return false;
    }

    // In this case, start charging the laser.
    // The StoneGolem is handing over the reigns to the attack at this point
    private void StartCharging() {
        isCharging = true;

        timeOfChargeStart = Time.time;
        lineRenderer.enabled = true;

        // When we start to fire, set the position to the target immediately
        lastEndAimPoint = target.position;

        // Play audioclip
        AudioUtility.shared.CreateSFX(
            chargingSfx,
            startAimPoint.position,
            AudioUtility.AudioGroups.EnemyAttack,
            1f,
            1f
        );
    }

    private void StartFiring() {
        isCharging = false;
        isFiring = true;
        timeOfFireStart = Time.time;
        timeOfLastFire = Time.time; // I suppose we don't need two of these
        // Hide it when this is all over

        lineRenderer.material.SetInt(SHADER_IS_FIRING, 1);

        AudioUtility.shared.CreateSFX(
            fireSfx,
            startAimPoint.position,
            AudioUtility.AudioGroups.EnemyAttack,
            1f,
            1f
        );

        // For now just damage the target, assuming we hit them

        Vector3 fromEyeToEndPointDir = lastEndAimPoint - startAimPoint.position;

        // Do a raycast to see what we hit and trigger the DamageArea there
        // Also spawn the explosion there
        if(Physics.Raycast(
            startAimPoint.position,
            fromEyeToEndPointDir.normalized,
            out RaycastHit hit,
            100.0f, // Same as the one used in SetLineRendererPositions
            -1,
            QueryTriggerInteraction.Ignore
        )) {
            // Trigger the damage area
            damageArea.InflictDamageOverArea(
                entityBaseDamage * DamageCoefficient,
                procCoefficient: 1f,
                hit.point,
                Affiliation.Enemy,
                hit.collider,
                null,
                -1
            );

            // Uncomment this to debug the laser attack's DamageArea
            // lastEndAimPoint = hit.point;

            // TODO: Spawn the explosion VFX
        }
    }

    private void SetLineRendererPositions() {
        lineRenderer.SetPosition(0, startAimPoint.position);

        // Need to calculate the end position each time - it's a raycast
        // it should basically go on for infinity (it's a ray after all), but we'll set the max to like 500 or something
        const float maxDist = 100.0f;
        Vector3 direction = (lastEndAimPoint - startAimPoint.position).normalized;
        if (Physics.Raycast(
            startAimPoint.position,
            direction,
            out RaycastHit hit,
            maxDist,
            -1
        )) { // Ideally we hit a player, but who knows!
            lineRenderer.SetPosition(1, hit.point);
        } else {
            Vector3 endPos = startAimPoint.position + (direction * maxDist);
            lineRenderer.SetPosition(1, endPos);
        }
    }

    private void ResetAttack() {
        isCharging = false;
        isFiring = false;
        lineRenderer.enabled = false;
        lineRenderer.material.SetInt(SHADER_IS_FIRING, 0);
    }
}
