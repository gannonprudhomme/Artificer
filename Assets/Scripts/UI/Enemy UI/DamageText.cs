using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// An individual Damage text instance
// 
// Destroys itself after `Lifetime` is reached. 
// It also randomly determines it's starts and end position (based on a range)
[RequireComponent(typeof(TextMeshProUGUI))]
public class DamageText : MonoBehaviour {
    public int Damage { get; set; }

    private TextMeshProUGUI damageText;

    // Used to make this "float" upwards
    private Vector3 startPosition;
    private Vector3 endPosition;

    // How long this lasts
    private const float Lifetime = 2f;

    // The time when this was spawned
    private float startTime;

    // Start is called before the first frame update
    void Start() {
        damageText = GetComponent<TextMeshProUGUI>();
        // damageText.text = $"{Damage}";

        // Random value between [2.0, 4.0]
        float upMove = (Random.value * 2f) + 2f;

        // Random value between [-2.0, 2.0]
        float horizontalMove = (Random.value * 4f) - 2f;

        Vector3 modifiedStartPos = transform.position;
        modifiedStartPos.x += (Random.value * 2f) - 1f; // Random value between [-1f, 1f]
        modifiedStartPos.y += (Random.value - 0.5f); // Random value between [-0.5f, 0.5f]

        // Get the end position of this - it should move upwards & horizontally
        endPosition = modifiedStartPos + (Vector3.up * upMove) + (Vector3.right * horizontalMove);

        startPosition = modifiedStartPos;
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update() {
        damageText.text = $"{Damage}";

        transform.rotation = Camera.main.transform.rotation;

        float timeLeft = Time.time - startTime;

        // Update the position
        if (timeLeft >= Lifetime) {
            Destroy(this.gameObject);
            return;
        }

        transform.position = Vector3.Lerp(
            startPosition,
            endPosition,
            timeLeft / Lifetime
        );

        // We only want to fade it out in the last X% of the "animation"

        const float percentToAnimateAlpha = 0.2f;

        if (timeLeft / Lifetime <= percentToAnimateAlpha) {
            // Need to convert [0.0f, 0.2f] to [0.0f, 1.0f]

            damageText.alpha = Mathf.Lerp(
                1f,
                0f,
                // [0.0f, {percentToAnimateAlpha}] -> [0.0f, 1.0f]
                timeLeft / Lifetime * (percentToAnimateAlpha / 1f)
            );
        }

        /*
        damageText.alpha = Mathf.Lerp(
            1f,
            0f,
            (Time.time - startTime) / Lifetime
        );
        */
    }
}