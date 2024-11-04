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
}
