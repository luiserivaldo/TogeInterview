using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CutsceneEvent : MonoBehaviour
{
    [Serializable]
    public class CutsceneStep
    {
        public enum SceneType
        {
            ShowText,
            MoveActor,
            ShowChoice
        }

        public CutscenePointer pointer;
        public SceneType sceneType = SceneType.ShowText;

        [TextArea(2, 5)]
        public string dialogueText;

        public bool requireInput = true;
        public float textSpeed = 45f;
        public float nextTextDelay = 0.5f;
        public Transform moveTo;
        public string choiceA = "A";
        public string choiceB = "B";
    }

    public enum TriggerType
    {
        None,
        SceneStart,
        TriggerEnter2D,
        External
    }

    [Header("Event")]
    [SerializeField] private string eventId = "Event1";
    [SerializeField] private GameObject actor;
    [SerializeField] private TriggerType triggerType = TriggerType.None;
    [SerializeField] private string requiredTriggerTag = "Player";
    [SerializeField] private List<CutsceneStep> steps = new();

    public string EventId => eventId;
    public GameObject Actor => actor;
    public TriggerType EventTriggerType => triggerType;
    public string RequiredTriggerTag => requiredTriggerTag;
    public IReadOnlyList<CutsceneStep> Steps => steps;

    private void OnTriggerEnter2D(Collider2D other)
    {
        CutsceneManager manager = GetComponentInParent<CutsceneManager>();
        if (manager != null)
        {
            manager.TryStartEvent(this, other);
        }
    }

#if UNITY_EDITOR
    public void EditorSetEventId(string newEventId)
    {
        eventId = newEventId;
        RenameChildren();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorAddStep(CutsceneManager manager)
    {
        UnityEditor.Undo.RecordObject(this, "Add Cutscene Step");

        CutsceneStep step = new CutsceneStep
        {
            sceneType = CutsceneStep.SceneType.ShowText,
            requireInput = true,
            textSpeed = 45f,
            nextTextDelay = 0.5f
        };

        steps.Add(step);
        RenameChildren();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorRemoveStep(int stepIndex, CutsceneManager manager)
    {
        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Remove Cutscene Step");

        CutsceneStep step = steps[stepIndex];
        Transform moveTarget = step?.moveTo;

        if (step?.pointer != null)
        {
            UnityEditor.Undo.DestroyObjectImmediate(step.pointer.gameObject);
        }

        if (step?.sceneType == CutsceneStep.SceneType.MoveActor &&
            moveTarget != null &&
            moveTarget.TryGetComponent<CutsceneMovePointer>(out _))
        {
            UnityEditor.Undo.DestroyObjectImmediate(moveTarget.gameObject);
        }

        steps.RemoveAt(stepIndex);

        RenameChildren();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorEnsureMovePointer(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            return;
        }

        CutsceneStep step = steps[stepIndex];
        if (step == null || step.sceneType != CutsceneStep.SceneType.MoveActor || step.moveTo != null)
        {
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Create Cutscene Move Pointer");
        CutsceneMovePointer movePointer = CreateMovePointerChild(stepIndex);
        step.moveTo = movePointer.transform;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorSyncNames()
    {
        RenameChildren();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private CutsceneMovePointer CreateMovePointerChild(int stepIndex)
    {
        GameObject pointerObject = new GameObject($"CutsceneMovePointer (step{stepIndex + 1})");
        UnityEditor.Undo.RegisterCreatedObjectUndo(pointerObject, "Create Cutscene Move Pointer");

        pointerObject.transform.SetParent(transform, true);

        Vector3 basePosition = actor != null
        ? actor.transform.position
        : transform.position;

        // Place the destination one world-grid unit beside the actor.
        pointerObject.transform.position = basePosition + Vector3.right;

        CutsceneMovePointer movePointer = pointerObject.AddComponent<CutsceneMovePointer>();
        AssignBlueIcon(pointerObject);
        return movePointer;
    }

    private void RenameChildren()
    {
        gameObject.name = string.IsNullOrWhiteSpace(eventId) ? "CutsceneEvent" : eventId;

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i]?.moveTo != null &&
                steps[i].moveTo.TryGetComponent<CutsceneMovePointer>(out CutsceneMovePointer movePointer))
            {
                movePointer.name = $"CutsceneMovePointer (step{i + 1})";
                AssignBlueIcon(movePointer.gameObject);
            }
        }
    }

    private static void AssignBlueIcon(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        Texture2D icon = UnityEditor.EditorGUIUtility.IconContent("sv_icon_dot3_pix16_gizmo").image as Texture2D;
        if (icon == null)
        {
            return;
        }

        MethodInfo setIconMethod = typeof(UnityEditor.EditorGUIUtility).GetMethod(
            "SetIconForObject",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(UnityEngine.Object), typeof(Texture2D) },
            null);

        if (setIconMethod == null)
        {
            return;
        }

        setIconMethod.Invoke(null, new object[] { targetObject, icon });
        UnityEditor.EditorUtility.SetDirty(targetObject);
        UnityEditor.SceneView.RepaintAll();
    }
#endif
}
