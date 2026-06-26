using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CutsceneManager), true)]
public class CutsceneManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        CutsceneManager manager = (CutsceneManager)target;

        if (GUILayout.Button("Add Cutscene Event"))
        {
            manager.EditorAddCutsceneEvent();
        }
    }
}
