using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(CutsceneEvent))]
public class CutsceneEventEditor : Editor
{
    private SerializedProperty eventIdProperty;
    private SerializedProperty actorProperty;
    private SerializedProperty triggerTypeProperty;
    private SerializedProperty requiredTriggerTagProperty;
    private SerializedProperty stepsProperty;
    private ReorderableList stepsList;
    private CutsceneEvent cutsceneEvent;
    private CutsceneManager manager;
    private readonly List<bool> stepFoldouts = new();

    private void OnEnable()
    {
        eventIdProperty = serializedObject.FindProperty("eventId");
        actorProperty = serializedObject.FindProperty("actor");
        triggerTypeProperty = serializedObject.FindProperty("triggerType");
        requiredTriggerTagProperty = serializedObject.FindProperty("requiredTriggerTag");
        stepsProperty = serializedObject.FindProperty("steps");

        cutsceneEvent = (CutsceneEvent)target;
        manager = cutsceneEvent.GetComponentInParent<CutsceneManager>();

        stepsList = new ReorderableList(serializedObject, stepsProperty, true, true, true, true);

        stepsList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Steps");
        };

        stepsList.elementHeightCallback = index =>
        {
            SyncFoldoutCount();

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float totalHeight = EditorGUIUtility.singleLineHeight + 6f; // foldout row

            if (index < 0 || index >= stepFoldouts.Count)
            {
                return totalHeight;
            }

            if (!stepFoldouts[index])
            {
                return totalHeight;
            }

            SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            SerializedProperty pointerProperty = stepProperty.FindPropertyRelative("pointer");
            SerializedProperty sceneTypeProperty = stepProperty.FindPropertyRelative("sceneType");
            SerializedProperty dialogueTextProperty = stepProperty.FindPropertyRelative("dialogueText");
            SerializedProperty requireInputProperty = stepProperty.FindPropertyRelative("requireInput");
            SerializedProperty textSpeedProperty = stepProperty.FindPropertyRelative("textSpeed");
            SerializedProperty nextTextDelayProperty = stepProperty.FindPropertyRelative("nextTextDelay");
            SerializedProperty moveToProperty = stepProperty.FindPropertyRelative("moveTo");
            SerializedProperty choiceAProperty = stepProperty.FindPropertyRelative("choiceA");
            SerializedProperty choiceBProperty = stepProperty.FindPropertyRelative("choiceB");

            totalHeight += EditorGUI.GetPropertyHeight(pointerProperty, true) + spacing;
            totalHeight += EditorGUI.GetPropertyHeight(sceneTypeProperty, true) + spacing;

            CutsceneEvent.CutsceneStep.SceneType sceneType =
            (CutsceneEvent.CutsceneStep.SceneType)sceneTypeProperty.enumValueIndex;

            switch (sceneType)
            {
                case CutsceneEvent.CutsceneStep.SceneType.ShowText:
                    totalHeight += EditorGUI.GetPropertyHeight(dialogueTextProperty, true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(requireInputProperty, true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(textSpeedProperty, true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(nextTextDelayProperty, true) + spacing;
                    break;

                case CutsceneEvent.CutsceneStep.SceneType.MoveActor:
                    totalHeight += EditorGUI.GetPropertyHeight(moveToProperty, true) + spacing;
                    break;

                case CutsceneEvent.CutsceneStep.SceneType.ShowChoice:
                    totalHeight += EditorGUI.GetPropertyHeight(dialogueTextProperty, true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(choiceAProperty, true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(choiceBProperty, true) + spacing;
                    break;
            }

            return totalHeight + 4f;
        };

        stepsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SyncFoldoutCount();

            SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            SerializedProperty pointerProperty = stepProperty.FindPropertyRelative("pointer");
            SerializedProperty sceneTypeProperty = stepProperty.FindPropertyRelative("sceneType");
            SerializedProperty dialogueTextProperty = stepProperty.FindPropertyRelative("dialogueText");
            SerializedProperty requireInputProperty = stepProperty.FindPropertyRelative("requireInput");
            SerializedProperty textSpeedProperty = stepProperty.FindPropertyRelative("textSpeed");
            SerializedProperty nextTextDelayProperty = stepProperty.FindPropertyRelative("nextTextDelay");
            SerializedProperty moveToProperty = stepProperty.FindPropertyRelative("moveTo");
            SerializedProperty choiceAProperty = stepProperty.FindPropertyRelative("choiceA");
            SerializedProperty choiceBProperty = stepProperty.FindPropertyRelative("choiceB");

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float y = rect.y + 2f;

            // Leave room for the drag handle and a little right padding.
            const float leftInset = 14f;
            const float rightInset = 4f;

            Rect contentRect = new Rect(
                rect.x + leftInset,
                rect.y,
                rect.width - leftInset - rightInset,
                rect.height
            );

            Rect row = new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight);
            stepFoldouts[index] = EditorGUI.Foldout(row, stepFoldouts[index], $"Step {index + 1}", true);
            y += row.height + spacing;

            if (!stepFoldouts[index])
            {
                return;
            }

            float h;

            h = EditorGUI.GetPropertyHeight(pointerProperty, true);
            row = new Rect(contentRect.x, y, contentRect.width, h);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(row, pointerProperty, true);
            }
            y += h + spacing;

            EditorGUI.BeginChangeCheck();
            h = EditorGUI.GetPropertyHeight(sceneTypeProperty, true);
            row = new Rect(contentRect.x, y, contentRect.width, h);
            EditorGUI.PropertyField(row, sceneTypeProperty, new GUIContent("Scene Type"), true);
            bool sceneTypeChanged = EditorGUI.EndChangeCheck();
            y += h + spacing;

            CutsceneEvent.CutsceneStep.SceneType sceneType =
            (CutsceneEvent.CutsceneStep.SceneType)sceneTypeProperty.enumValueIndex;

            switch (sceneType)
            {
                case CutsceneEvent.CutsceneStep.SceneType.ShowText:
                    h = EditorGUI.GetPropertyHeight(dialogueTextProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, dialogueTextProperty, new GUIContent("Dialogue Text"), true);
                    y += h + spacing;

                    h = EditorGUI.GetPropertyHeight(requireInputProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, requireInputProperty, new GUIContent("Require Input"), true);
                    y += h + spacing;

                    h = EditorGUI.GetPropertyHeight(textSpeedProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, textSpeedProperty, new GUIContent("Text Speed"), true);
                    y += h + spacing;

                    h = EditorGUI.GetPropertyHeight(nextTextDelayProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, nextTextDelayProperty, new GUIContent("Next Text Delay"), true);
                    y += h + spacing;
                    break;

                case CutsceneEvent.CutsceneStep.SceneType.MoveActor:
                    h = EditorGUI.GetPropertyHeight(moveToProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, moveToProperty, new GUIContent("Move To"), true);
                    y += h + spacing;
                    break;

                case CutsceneEvent.CutsceneStep.SceneType.ShowChoice:
                    h = EditorGUI.GetPropertyHeight(dialogueTextProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, dialogueTextProperty, new GUIContent("Dialogue Text"), true);
                    y += h + spacing;

                    h = EditorGUI.GetPropertyHeight(choiceAProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, choiceAProperty, new GUIContent("Choice A"), true);
                    y += h + spacing;

                    h = EditorGUI.GetPropertyHeight(choiceBProperty, true);
                    row = new Rect(contentRect.x, y, contentRect.width, h);
                    EditorGUI.PropertyField(row, choiceBProperty, new GUIContent("Choice B"), true);
                    y += h + spacing;
                    break;
            }

            if (sceneTypeChanged)
            {
                serializedObject.ApplyModifiedProperties();

                if (sceneType == CutsceneEvent.CutsceneStep.SceneType.MoveActor)
                {
                    cutsceneEvent.EditorEnsureMovePointer(index);
                }

                serializedObject.Update();
            }
        };

        stepsList.onReorderCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            cutsceneEvent.EditorSyncNames();
            serializedObject.Update();
        };

        stepsList.onAddCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            cutsceneEvent.EditorAddStep(manager);
            serializedObject.Update();
        };

        stepsList.onRemoveCallback = list =>
        {
            serializedObject.ApplyModifiedProperties();
            cutsceneEvent.EditorRemoveStep(list.index, manager);
            serializedObject.Update();
        };
    }

    private void SyncFoldoutCount()
    {
        while (stepFoldouts.Count < stepsProperty.arraySize)
        {
            stepFoldouts.Add(true);
        }

        while (stepFoldouts.Count > stepsProperty.arraySize)
        {
            stepFoldouts.RemoveAt(stepFoldouts.Count - 1);
        }
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
        stepsList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            cutsceneEvent.EditorSyncNames();
        }
    }
}
