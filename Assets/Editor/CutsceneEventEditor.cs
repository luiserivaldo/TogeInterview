using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CutsceneEvent))]
public class CutsceneEventEditor : Editor
{
    private SerializedProperty eventIdProperty;
    private SerializedProperty actorProperty;
    private SerializedProperty triggerTypeProperty;
    private SerializedProperty requiredTriggerTagProperty;
    private SerializedProperty stepsProperty;

    private void OnEnable()
    {
        eventIdProperty = serializedObject.FindProperty("eventId");
        actorProperty = serializedObject.FindProperty("actor");
        triggerTypeProperty = serializedObject.FindProperty("triggerType");
        requiredTriggerTagProperty = serializedObject.FindProperty("requiredTriggerTag");
        stepsProperty = serializedObject.FindProperty("steps");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(eventIdProperty);
        EditorGUILayout.PropertyField(actorProperty);
        EditorGUILayout.PropertyField(triggerTypeProperty);

        if ((CutsceneEvent.TriggerType)triggerTypeProperty.enumValueIndex == CutsceneEvent.TriggerType.TriggerEnter2D)
        {
            EditorGUILayout.PropertyField(requiredTriggerTagProperty);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

        CutsceneEvent cutsceneEvent = (CutsceneEvent)target;
        CutsceneManager manager = cutsceneEvent.GetComponentInParent<CutsceneManager>();

        for (int i = 0; i < stepsProperty.arraySize; i++)
        {
            SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(i);
            SerializedProperty pointerProperty = stepProperty.FindPropertyRelative("pointer");
            SerializedProperty sceneTypeProperty = stepProperty.FindPropertyRelative("sceneType");
            SerializedProperty dialogueTextProperty = stepProperty.FindPropertyRelative("dialogueText");
            SerializedProperty requireInputProperty = stepProperty.FindPropertyRelative("requireInput");
            SerializedProperty textSpeedProperty = stepProperty.FindPropertyRelative("textSpeed");
            SerializedProperty nextTextDelayProperty = stepProperty.FindPropertyRelative("nextTextDelay");
            SerializedProperty moveToProperty = stepProperty.FindPropertyRelative("moveTo");
            SerializedProperty choiceAProperty = stepProperty.FindPropertyRelative("choiceA");
            SerializedProperty choiceBProperty = stepProperty.FindPropertyRelative("choiceB");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Step {i + 1}", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(pointerProperty);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(sceneTypeProperty, new GUIContent("Scene Type"));
            bool sceneTypeChanged = EditorGUI.EndChangeCheck();

            CutsceneEvent.CutsceneStep.SceneType sceneType = (CutsceneEvent.CutsceneStep.SceneType)sceneTypeProperty.enumValueIndex;

            switch (sceneType)
            {
                case CutsceneEvent.CutsceneStep.SceneType.ShowText:
                    EditorGUILayout.PropertyField(dialogueTextProperty, new GUIContent("Dialogue Text"));
                    EditorGUILayout.PropertyField(requireInputProperty, new GUIContent("Require Input"));
                    EditorGUILayout.PropertyField(textSpeedProperty, new GUIContent("Text Speed"));
                    EditorGUILayout.PropertyField(nextTextDelayProperty, new GUIContent("Next Text Delay"));
                    break;

                case CutsceneEvent.CutsceneStep.SceneType.MoveActor:
                    EditorGUILayout.PropertyField(moveToProperty, new GUIContent("Move To"));
                    break;

                case CutsceneEvent.CutsceneStep.SceneType.ShowChoice:
                    EditorGUILayout.PropertyField(dialogueTextProperty, new GUIContent("Dialogue Text"));
                    EditorGUILayout.PropertyField(choiceAProperty, new GUIContent("Choice A"));
                    EditorGUILayout.PropertyField(choiceBProperty, new GUIContent("Choice B"));
                    break;
            }

            EditorGUILayout.EndVertical();

            if (sceneTypeChanged)
            {
                serializedObject.ApplyModifiedProperties();
                if (sceneType == CutsceneEvent.CutsceneStep.SceneType.MoveActor)
                {
                    cutsceneEvent.EditorEnsureMovePointer(i);
                }
                serializedObject.Update();
            }
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Add Step"))
        {
            cutsceneEvent.EditorAddStep(manager);
        }

        if (GUI.changed)
        {
            cutsceneEvent.EditorSyncNames();
        }
    }
}
