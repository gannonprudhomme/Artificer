using UnityEngine;
using UnityEditor;

[ExecuteAlways]
[CustomEditor(typeof(CombatDirector), editorForChildClasses:true)]
public class CombatDirectorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GUILayout.Space(16);

        CombatDirector director = (CombatDirector)target;

        // Display the director's numCredits as a GUI label
        GUILayout.Label($"Num Credits: {director.GetNumCredits()}");
        GUILayout.Label($"Difficulty Coefficient: {director.GetDifficultyCoefficient()}");
        // GUILayout.Label($"Credits Per Second: {director.GetCreditsPerSecond()}");
        GUILayout.Label($"Current card: {director.GetSelectedCard()?.identifier ?? "null"}");
    }
}
