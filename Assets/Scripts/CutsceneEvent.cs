using System;
using System.Collections.Generic;
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

    public void BindPointers(CutsceneManager manager)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i]?.pointer != null)
            {
                steps[i].pointer.Bind(this, manager);
            }
        }
    }

#if UNITY_EDITOR
    public void EditorAddStep(CutsceneManager manager)
    {
        if (manager == null)
        {
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Add Cutscene Step");

        CutscenePointer pointer = CreatePointerChild(steps.Count);
        CutsceneStep step = new CutsceneStep
        {
            pointer = pointer,
            sceneType = CutsceneStep.SceneType.ShowText,
            requireInput = true,
            textSpeed = 45f,
            nextTextDelay = 0.5f
        };

        steps.Add(step);
        BindPointers(manager);
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

        if (manager != null)
        {
            BindPointers(manager);
        }

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

    private CutscenePointer CreatePointerChild(int stepIndex)
    {
        GameObject pointerObject = new GameObject($"CutscenePointer_{stepIndex + 1}");
        UnityEditor.Undo.RegisterCreatedObjectUndo(pointerObject, "Create Cutscene Pointer");
        pointerObject.transform.SetParent(transform, false);
        return pointerObject.AddComponent<CutscenePointer>();
    }

    private CutsceneMovePointer CreateMovePointerChild(int stepIndex)
    {
        GameObject pointerObject = new GameObject($"CutsceneMovePointer_{stepIndex + 1}");
        UnityEditor.Undo.RegisterCreatedObjectUndo(pointerObject, "Create Cutscene Move Pointer");
        pointerObject.transform.SetParent(transform, false);
        return pointerObject.AddComponent<CutsceneMovePointer>();
    }

    private void RenameChildren()
    {
        gameObject.name = string.IsNullOrWhiteSpace(eventId) ? "CutsceneEvent" : eventId;

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i]?.pointer != null)
            {
                steps[i].pointer.name = $"CutscenePointer_{i + 1}";
            }
        }
    }
#endif
}
