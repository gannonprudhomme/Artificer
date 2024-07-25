using System.Collections.Generic;
using UnityEngine;

#nullable enable

// Container ScriptableObject so we can find all of the objects we have
[CreateAssetMenu(menuName = "ScriptableObjects/AllItems")]
public class AllItems : ScriptableObject {
    // We shouldn't even need to do this - we could just filter this by Priority
    public List<Item> CommonItems = new();
}
