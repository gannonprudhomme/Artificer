using UnityEngine;

#nullable enable

[RequireComponent(
    typeof(InstantDirector),
    typeof(FastDirector),
    typeof(SlowDirector)
)]
[RequireComponent(
    typeof(InteractablesDirector),
    typeof(NavOctreeSpace),
    typeof(OctreeManager)
)]
public sealed class Level : MonoBehaviour {
    [Tooltip("Where to spawn the player upon level start")]
    public Transform? PlayerSpawnPoint;
    
    [Tooltip("The name of the string / file. Used to load it.")]
    public string SceneName = "Distant Roost";

    private FastDirector? fastDirector;
    private SlowDirector? slowDirector;
    private InstantDirector? instantDirector;
    private InteractablesDirector? interactablesDirector;

    private void Awake() {
        fastDirector = GetComponent<FastDirector>();
        slowDirector = GetComponent<SlowDirector>();
        instantDirector = GetComponent<InstantDirector>();
        interactablesDirector = GetComponent<InteractablesDirector>();

        // Disable them until loading is completed
        fastDirector.enabled = false;
        slowDirector.enabled = false;
        instantDirector.enabled = false;
        interactablesDirector.enabled = false;
    }

    public void LoadCompleted() {
        fastDirector!.enabled = true;
        slowDirector!.enabled = true;
        instantDirector!.enabled = true;
        interactablesDirector!.enabled = true;
    }
}
