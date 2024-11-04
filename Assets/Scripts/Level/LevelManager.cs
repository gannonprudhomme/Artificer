using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable


// Maybe GameManager?
//
// This handles switching between levels
// Will need a reference to the player, as the player should be in the Level scene?
// well, it has to be b/c of the InteractablesDirector for one
// 
// Need to figure out EnemyManager
public class LevelManager : MonoBehaviour {
    [Tooltip("Player prefab which is used to create the player if it doesn't exist")]
    public GameObject? PlayerPrefab;

    [Tooltip("Reference to the HUD manager, which we enable once the scene is loaded")]
    public GameObject? HUDManager;

    [Tooltip("Reference to the camera objects *component*, which we enable once the scene is loaded")]
    public CameraObjects? CameraObjects;
    
    // Ideally these wouldn't be nullable, but fucking unity
    private Scene? currentLevelScene;
    private Level? currentLevel;
    
    // We need access to the player so we can move it around between scenes
    //
    // This is intentionally not a PlayerController type to indicate we only treat is as a generic GameObject
    private GameObject? player;

    private void Awake() {
        // Load the current level (if it's not already loaded?)
        // Ensure other levels aren't loaded
        StartCoroutine(LoadIfNeeded());
    }
    
    public IEnumerator LoadIfNeeded() {
        if (SceneManager.sceneCount > 2) {
            Debug.LogError("We have more than 2 scenes! wat");
            yield break;
        }

        // This might change in the future, but for now we only have 1 persistent scene + 1 (changing) level scene
        // We can assume only the Main scene is active, so create the level scene
        if (SceneManager.sceneCount == 1) {
            // This is intentionally blocking, as we don't have a loading screen yet
            AsyncOperation? loadOperation = SceneManager.LoadSceneAsync("Distant Roost", LoadSceneMode.Additive);

            if (loadOperation == null) {
                Debug.LogError("Scene couldn't be found?");
                yield break;
            }
            
            // Wait until it's done
            while (!loadOperation.isDone) {
                yield return null;
            }
        }
        
        // Find a scene that isn't the main scene & store it
        // This is either the level scene (i.e. for the 
        var activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            var scene = SceneManager.GetSceneAt(i);

            if (scene != activeScene) {
                currentLevelScene = scene;
                SceneManager.SetActiveScene(scene);
                
                // Find the Level component in the scene and store it here
                // as we need the spawn position from it to know where to place the player
            }
        }

        currentLevel = FindLevelComponentInScene(currentLevelScene!.Value);

        // We need to create the player in this case
        // at least I assume we do
        if (player == null) {
            var findObject = FindObjectOfType<PlayerController>();
            
            if (findObject != null) {
                Debug.Log("We found the player!");
                player = findObject.gameObject;
            }
            
            // If it's still null, create it
            // how do we make sure what scene it's in? I guess it's always the scene of this monobehaviour
            // which we actually don't want (since this will be in the Main scene)
            // so we need to move the player to the level scene
            if (player == null) {
                Debug.Log("Creating player!");
                Transform spawnPoint = currentLevel!.PlayerSpawnPoint!;
                
                player = Instantiate(PlayerPrefab!, spawnPoint.position, spawnPoint.rotation);
            } else {
                Debug.Log("Not creating player!");
            }
        }
        
        SceneManager.MoveGameObjectToScene(player, currentLevelScene!.Value);

        
        HUDManager!.SetActive(true);
        CameraObjects!.gameObject.SetActive(true);
    }
    

    private static Level? FindLevelComponentInScene(Scene scene) {
        Debug.Log($"Scene {scene.name} has rootCount {scene.rootCount}, is loaded: {scene.isLoaded}");
        
        foreach(GameObject rootGameObject in scene.GetRootGameObjects()) {
            Debug.Log($"Checking for {rootGameObject.name}");
            if (rootGameObject.TryGetComponent(out Level level)) {
                return level;
            }
        }
        
        Debug.LogError($"Couldn't find Level component in scene '{scene.name}'");

        return null;
    }
}
