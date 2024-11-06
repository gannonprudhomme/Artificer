using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#nullable enable

// This handles switching between levels
// Will need a reference to the player, as the player should be in the Level scene?
// well, it has to be b/c of the InteractablesDirector for one
public class LevelManager : MonoBehaviour {
    public static LevelManager? instance { get; private set; }
    
    [Tooltip("Player prefab which is used to create the player if it doesn't exist")]
    public GameObject? PlayerPrefab;

    [Tooltip("Reference to the HUD manager, which we enable once the scene is loaded")]
    public GameObject? HUDManager;

    [Tooltip("Reference to the camera objects *component*, which we enable once the scene is loaded")]
    public CameraObjects? CameraObjects;

    [Tooltip("OnLevelChanged ScriptableObject which we use to notify other objects that the level has changed")]
    public OnLevelLoaded? OnLevelLoaded;

    [SerializeField]
    [Tooltip("The names of the available *level* scenes. Should not include the Main (UI) scene")]
    private string[] SceneNames = {
        "Distant Roost", "Titanic Plains"
    };
    
    // Ideally these wouldn't be nullable, but fucking unity
    private Scene? currentLevelScene;
    private Level? currentLevel;
    
    // We need access to the player so we can move it around between scenes
    //
    // This is intentionally not a PlayerController type to indicate we only treat is as a generic GameObject
    private GameObject? player;

    private void Awake() {
        instance = this;
        
        // Load the current level (if it's not already loaded?)
        // Ensure other levels aren't loaded
        StartCoroutine(LoadIfNeeded());
    }
    
    private IEnumerator LoadIfNeeded() {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        if (SceneManager.sceneCount > 2) {
            Debug.LogError("[LevelManager] We have more than 2 scenes! wat");
            yield break;
        }

        // This might change in the future, but for now we only have 1 persistent scene + 1 (changing) level scene
        // We can assume only the Main scene is active, so create the level scene
        if (SceneManager.sceneCount == 1) {
            Debug.Log("[LevelManager] No scene loaded, loading new one!");
            // This is intentionally blocking, as we don't have a loading screen yet
            string sceneName = SceneNames[0];

            yield return StartCoroutine(LoadScene(sceneName));
        }
        
        yield return StartCoroutine(FindLevelSceneAndSetActive(sceneName: null)); // null indicates we should just choose one that isn't the Main scene
        
        // Needs to be done after we find the current level
        FindOrCreatePlayer();

        HUDManager!.SetActive(true);
        CameraObjects!.gameObject.SetActive(true);
        
        currentLevel!.LoadCompleted();
        OnLevelLoaded!.Event?.Invoke(); // not really used for anything rn

        stopwatch.Stop();
        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;
        Debug.Log($"LoadIfNeeded took {seconds:F2} seconds ({ms:F0}ms)");
    }

    public void ChangeLevel() {
        // If another object calls ChangeLevelCoroutine but it's in the scene that's getting unloaded then the Coroutine will get finished
        // hence we need to start this in something that persists between level changes - the LevelManager!
        StartCoroutine(ChangeLevelCoroutine());
    }
    
    private IEnumerator ChangeLevelCoroutine() {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        if (currentLevel == null) {
            Debug.LogError("[LevelManager] Why is currentLevel null?");
            yield break;
        }
        
        Debug.Log("[LevelManager] Changing levels");
        
        // Move the player to the Main scene, so we can load & unload them at the same time (and not destroy the Player object in the process)
        SceneManager.MoveGameObjectToScene(player, FindMainScene()!.Value);
        
        Scene? oldScene = currentLevelScene;
        
        // Unload the current level
        // Load the next one
        string newSceneName = GetInactiveSceneName();
        
        var load = StartCoroutine(LoadScene(newSceneName));
        var unload = StartCoroutine(UnloadScene(oldScene!.Value));

        yield return unload;
        yield return load;
        
        yield return StartCoroutine(FindLevelSceneAndSetActive(newSceneName));

        // Move the player to the new scene
        SceneManager.MoveGameObjectToScene(player, currentLevelScene!.Value);

        // Reset the position to the new one
        player!.transform.position = currentLevel!.PlayerSpawnPoint!.position;
        player!.transform.rotation = currentLevel!.PlayerSpawnPoint!.rotation;

        currentLevel!.LoadCompleted();
        OnLevelLoaded!.Event!.Invoke();
        
        stopwatch.Stop();
        double ms = ((double)stopwatch.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000d;
        double seconds = ms / 1000d;
        
        Debug.Log($"[LevelManager] ChangeLevel completed in {seconds:F2} seconds ({ms:F0}ms)");
    }

    private static IEnumerator LoadScene(string? sceneName) {
        AsyncOperation? loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (loadOperation == null) {
            Debug.LogError("Scene couldn't be found?");
            yield break;
        }

        // Wait until it's done
        while (!loadOperation.isDone) {
            if (loadOperation.progress >= 0.9f) {
                // The scene might not load correctly if we don't set this (I can't tell)
                loadOperation.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        // Debug.Log($"Loading {sceneName} completed!");
    }

    private static IEnumerator UnloadScene(Scene scene) {
        AsyncOperation? unloadOperation = SceneManager.UnloadSceneAsync(scene);

        if (unloadOperation == null) {
            Debug.LogError("Scene couldn't be found?");
            yield break;
        }
        
        while (!unloadOperation.isDone) {
            yield return null;
        }
        
        Debug.Log($"Unloading scene {scene.name} completed!");
    }
    
    private IEnumerator FindLevelSceneAndSetActive(string? sceneName) {
        // Find a scene that isn't the main scene & store it
        // This is either the level scene (i.e. for the 
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            var scene = SceneManager.GetSceneAt(i);
            
            bool didMatchName = sceneName != null && scene.name == sceneName;
            bool isNotMain = scene.name != "Main";

            // This is basically an enum switch (if only C# had enum's w/ associated values)
            // if sceneName is null, we just want to find a scene that isn't the Main scene
            // which is used for when we already have a scene loaded.
            //
            // if sceneName is not null, we want to find the scene that matches the name
            // which is for when we don't have a scene loaded & need to load it, or we're changing levels
            bool didMatchOrNotMain = sceneName != null ? didMatchName : isNotMain;

            if (didMatchOrNotMain) {
                while (!scene.isLoaded) {
                    // This shouldn't happen? Hopefully
                    Debug.Log($"Scene '{scene.name}' isn't loaded!");
                    yield return null;
                }

                currentLevelScene = scene;
                SceneManager.SetActiveScene(scene);
                
                currentLevel = FindLevelComponentInScene(currentLevelScene!.Value);

                // Find the Level component in the scene and store it here
                // as we need the spawn position from it to know where to place the player
            }
        }
    }

    // The currentLevel must be set & the level's scene must be the active scene for this to work properly
    private void FindOrCreatePlayer() {
        if (player == null) {
            var findObject = FindObjectOfType<PlayerController>();
            
            if (findObject != null) {
                // Debug.Log("[LevelManager] We found the player!");
                player = findObject.gameObject;
            } else {
                // Debug.Log("[LevelManager] Couldn't find the player, creating a new one");
                Transform spawnPoint = currentLevel!.PlayerSpawnPoint!;
                
                player = Instantiate(PlayerPrefab!, spawnPoint.position, spawnPoint.rotation);
            }
        } else {
            // Debug.Log("[LevelManager] We had a player, no need to do anything");
        }
        
        SceneManager.MoveGameObjectToScene(player, currentLevelScene!.Value);
    }

    private static Scene? FindMainScene() {
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == "Main") {
                return scene;
            }
        }

        Debug.LogError("Couldn't find Main scene!");
        return null;
    }

    private static Level? FindLevelComponentInScene(Scene scene) {
        foreach(GameObject rootGameObject in scene.GetRootGameObjects()) {
            if (rootGameObject.TryGetComponent(out Level level)) {
                return level;
            }
        }
        
        Debug.LogError($"Couldn't find Level component in scene '{scene.name}'");

        return null;
    }

    // Gets whichever level we're not currently in
    private string GetInactiveSceneName() {
        string currentSceneName = currentLevelScene!.Value.name;
        
        foreach(string sceneName in SceneNames) {
            if (sceneName != currentSceneName) {
                return sceneName;
            }
        }

        // Not even going to handle this cause it really shouldn't happen
        Debug.LogError("How did this happen");
        return "";
    }
}
