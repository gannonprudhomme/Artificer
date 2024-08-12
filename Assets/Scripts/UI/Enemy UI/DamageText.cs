using TMPro;
using UnityEngine;

#nullable enable

// An individual Damage text instance
// 
// Destroys itself after `Lifetime` is reached. 
// It also randomly determines it's starts and end position (based on a range)
[RequireComponent(typeof(TextMeshProUGUI))]
public class DamageText : MonoBehaviour {
    [Tooltip("The alpha value over time. Normalized to time of [0, 1]")]
    public AnimationCurve AlphaFadeCurve;

    public int Damage { get; set; }

    private TextMeshProUGUI damageText;

    // Used to make this "float" upwards
    private Vector3 startPosition;
    private Vector3 endPosition;

    // How long this lasts
    private const float Lifetime = 1f;

    // The time when this was spawned
    private float startTime;

    private Color textColor = Color.magenta;

    // Start is called before the first frame update
    void Start() {
        damageText = GetComponent<TextMeshProUGUI>();
        // We're assuming this is always going to be set by the time Start() is called
		damageText.color = textColor;

        // damageText.text = $"{Damage}";

        // Random value between [2.0, 4.0]
        float upMove = Random.Range(2f, 4f);

        // Random value between [-2.0, 2.0]
        float horizontalMove = Random.Range(-2f, 2f);

        // Get the end position of this - it should move upwards & horizontally
        startPosition = transform.position;
        endPosition = startPosition + (Vector3.up * upMove) + (Vector3.right * horizontalMove);

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

        damageText.alpha = AlphaFadeCurve.Evaluate((Time.time - startTime) / Lifetime);
    }

    public void SetTextColor(Color color) {
        if (damageText != null) {
			damageText.color = color;
		}

        textColor = color;
    }
}
