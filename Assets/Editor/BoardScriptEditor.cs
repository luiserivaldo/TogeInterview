using UnityEditor;

[CustomEditor(typeof(BoardScript))]
public class BoardScriptEditor : Editor
{
    private SerializedProperty allowDuringOpeningTutorialProperty;
    private SerializedProperty sectionPromptProperty;
    private SerializedProperty previousLabelProperty;
    private SerializedProperty nextLabelProperty;
    private SerializedProperty closeLabelProperty;
    private SerializedProperty sectionsProperty;

    private void OnEnable()
    {
        allowDuringOpeningTutorialProperty = serializedObject.FindProperty("allowDuringOpeningTutorial");
        sectionPromptProperty = serializedObject.FindProperty("sectionPrompt");
        previousLabelProperty = serializedObject.FindProperty("previousLabel");
        nextLabelProperty = serializedObject.FindProperty("nextLabel");
        closeLabelProperty = serializedObject.FindProperty("closeLabel");
        sectionsProperty = serializedObject.FindProperty("sections");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (allowDuringOpeningTutorialProperty != null)
        {
            EditorGUILayout.PropertyField(allowDuringOpeningTutorialProperty);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(sectionPromptProperty);
        EditorGUILayout.PropertyField(previousLabelProperty);
        EditorGUILayout.PropertyField(nextLabelProperty);
        EditorGUILayout.PropertyField(closeLabelProperty);
        EditorGUILayout.PropertyField(sectionsProperty, true);

        serializedObject.ApplyModifiedProperties();
    }
}
