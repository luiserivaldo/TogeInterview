using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CutsceneManager), true)]
public class CutsceneManagerEditor : Editor
{
    private SerializedProperty cutsceneEventsProperty;
    private SerializedProperty lockGameplayDuringSequenceProperty;
    private ReorderableList cutsceneEventsList;
    private CutsceneManager manager;

    private void OnEnable()
    {
        cutsceneEventsProperty = serializedObject.FindProperty("cutsceneEvents");
        lockGameplayDuringSequenceProperty = serializedObject.FindProperty("lockGameplayDuringSequence");
        manager = (CutsceneManager)target;

        cutsceneEventsList = new ReorderableList(serializedObject, cutsceneEventsProperty, true, true, true, true);
        cutsceneEventsList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Cutscene Events");
        };

        cutsceneEventsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = cutsceneEventsProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            EditorGUI.PropertyField(rect, element, new GUIContent($"Element {index}"));
        };

        cutsceneEventsList.onAddCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            manager.EditorAddCutsceneEvent();
            serializedObject.Update();
        };

        cutsceneEventsList.onRemoveCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            manager.EditorRemoveCutsceneEvent(list.index);
            serializedObject.Update();
        };

        cutsceneEventsList.onReorderCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            manager.EditorSyncCutsceneEvents();
            serializedObject.Update();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        cutsceneEventsList.DoLayoutList();
        EditorGUILayout.PropertyField(lockGameplayDuringSequenceProperty);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            manager.EditorSyncCutsceneEvents();
            serializedObject.Update();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Add Cutscene Event"))
        {
            manager.EditorAddCutsceneEvent();
            serializedObject.Update();
        }
    }
}
