using UnityEngine;
using UnityEngine.VFX;

#nullable enable

// Maybe call it Experience Emitter?
// This is what Enemies spawn when they die so it can grant the player experience
[RequireComponent(typeof(VisualEffect))]
public class ExperienceGranter : MonoBehaviour {
    private VisualEffect? experienceVFX;

    private Transform? goalTransform;

    // The player's experience object;
    private Experience? targetExperience;

    private int numBallsGranted = 0;
    private int totalExperienceToGrant;
    private float timeOfEmitStart;

    private int totalExperienceBalls => (int) Mathf.Ceil((float) totalExperienceToGrant / (float) experiencePerBall);
    private readonly int experiencePerBall = 5;

    // The individual lifetime of an experience ball
    // aka how long it takes for the ball to hit the player from emit time
    private readonly float vfxBallLifetime = 1f;
    private readonly float delayBetweenBalls = 0.1f;

    void Start() {
        experienceVFX = GetComponent<VisualEffect>();
    }

    void Update() {
        if (IsDoneEmittingVFX()) {
            experienceVFX!.Stop();
        }

        if (numBallsGranted >= totalExperienceBalls) {
            Destroy(this.gameObject);
            return;
        }

        experienceVFX!.SetVector3("Goal Position", goalTransform!.position);

        // Start emitting / granting it to the player
        // we time it with the Visual Effect so once it hits the player the experience is granted
        // Note that the first one is spawned immediately, and there's always at least one
        if (ShouldGrantExperience()) {
            // check if it's the last one so we can give the remainder
            int experienceToGrant = experiencePerBall;
            if (numBallsGranted == totalExperienceBalls - 1) {
                experienceToGrant = totalExperienceToGrant - (experiencePerBall * numBallsGranted);
            }

            numBallsGranted++;
            targetExperience!.GainExperience(experienceToGrant);
        }
    }

    public void Emit(
        int experienceToGrant,
        Experience targetExperience,
        Transform goalTransform
    ) {
        totalExperienceToGrant = experienceToGrant;
        this.targetExperience = targetExperience;
        timeOfEmitStart = Time.time;
        this.goalTransform = goalTransform;

        // Since this is called before Start() is called
        if (experienceVFX == null) {
            experienceVFX = GetComponent<VisualEffect>();
        }

        // Determine how many we should emit based on experienceToGrant
        experienceVFX!.SetFloat("Lifetime", vfxBallLifetime);
        experienceVFX!.SetInt("Num Balls", totalExperienceBalls);
        experienceVFX!.SetFloat("Delay Between Balls", delayBetweenBalls);

        experienceVFX!.Play();
    }

    private bool ShouldGrantExperience() {
        float timeSinceEmitStart = Time.time - timeOfEmitStart;
        // We want this to be 0 for the first one since it fires immediately
        float delay = delayBetweenBalls * numBallsGranted;

        float lifetime = vfxBallLifetime;

        return (Time.time - timeOfEmitStart) >= delay + lifetime;
    }

    // We should really just stop it the next frame? Since Play() will make all of the ones we want to be emitted
    // but w/e
    private bool IsDoneEmittingVFX() {
        return (Time.time - timeOfEmitStart) > delayBetweenBalls * totalExperienceBalls;
    }
}
