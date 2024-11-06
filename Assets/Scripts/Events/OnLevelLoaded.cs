using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

// This might be called upon launch when we load the first level, or when the level changes
// Though the form matter more.
//
// This isn't really used currently, but it might be in the future (lol) so I'm too lazy to remove it
[CreateAssetMenu(menuName = "ScriptableObjects/Events/OnLevelLoaded")]
public class OnLevelLoaded : ScriptableObject {
    public UnityAction? Event;
}
