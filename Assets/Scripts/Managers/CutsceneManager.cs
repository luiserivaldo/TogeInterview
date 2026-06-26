using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    private static CutsceneManager instance;

    [Header("Cutscenes")]
    [SerializeField] private List<CutsceneEvent> cutsceneEvents = new();
    [SerializeField] private bool lockGameplayDuringSequence = true;

    private Coroutine activeSequenceRoutine;
    private string queuedEventId;

    public static CutsceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<CutsceneManager>();

                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(CutsceneManager));
                    instance = managerObject.AddComponent<CutsceneManager>();
                }
            }

            return instance;
        }
    }

    public bool IsSequenceRunning => activeSequenceRoutine != null;
    public IReadOnlyList<CutsceneEvent> CutsceneEvents => cutsceneEvents;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        TryStartSceneEvent();
    }

    public void TryStartEvent(CutsceneEvent cutsceneEvent, CutscenePointer pointer, Collider2D triggeringCollider = null)
    {
        TryStartEvent(cutsceneEvent, triggeringCollider);
    }

    public void TryStartEvent(CutsceneEvent cutsceneEvent, Collider2D triggeringCollider = null)
    {
        if (cutsceneEvent == null || activeSequenceRoutine != null)
        {
            return;
        }

        if (!cutsceneEvents.Contains(cutsceneEvent))
        {
            return;
        }

        if (cutsceneEvent.EventTriggerType == CutsceneEvent.TriggerType.TriggerEnter2D)
        {
            if (triggeringCollider == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(cutsceneEvent.RequiredTriggerTag) && !triggeringCollider.CompareTag(cutsceneEvent.RequiredTriggerTag))
            {
                return;
            }
        }

        activeSequenceRoutine = StartCoroutine(PlayEventSequence(cutsceneEvent, 0));
    }

    public IEnumerator PlayScript(DialogueScript_ScriptableObject script, Sprite defaultImage = null)
    {
        DialogueManager.Instance.StartScript(script, defaultImage);
        while (DialogueManager.Instance.IsDialogueRunning)
        {
            yield return null;
        }
    }

    private IEnumerator PlayEventSequence(CutsceneEvent initialEvent, int startStepIndex)
    {
        GameManager.SetGameplayLocked(lockGameplayDuringSequence);
        HideOverworldPopups();

        CutsceneEvent currentEvent = initialEvent;
        int currentStepIndex = startStepIndex;

        while (currentEvent != null)
        {
            IReadOnlyList<CutsceneEvent.CutsceneStep> steps = currentEvent.Steps;

            for (int stepIndex = currentStepIndex; stepIndex < steps.Count; stepIndex++)
            {
                CutsceneEvent.CutsceneStep step = steps[stepIndex];
                if (step == null)
                {
                    continue;
                }

                yield return StartCoroutine(ExecuteStep(currentEvent, step));

                if (!string.IsNullOrWhiteSpace(queuedEventId))
                {
                    CutsceneEvent nextEvent = FindEventById(queuedEventId);
                    queuedEventId = null;
                    currentEvent = nextEvent;
                    currentStepIndex = 0;
                    goto ContinueSequence;
                }
            }

            break;

ContinueSequence:
            if (currentEvent == null)
            {
                break;
            }
        }

        EndSequence();
    }

    private IEnumerator ExecuteStep(CutsceneEvent cutsceneEvent, CutsceneEvent.CutsceneStep step)
    {
        bool stopStep = false;

        switch (step.sceneType)
        {
            case CutsceneEvent.CutsceneStep.SceneType.ShowText:
                yield return StartCoroutine(ExecuteText(step));
                break;

            case CutsceneEvent.CutsceneStep.SceneType.MoveActor:
                yield return StartCoroutine(ExecuteMove(cutsceneEvent, step));
                break;

            case CutsceneEvent.CutsceneStep.SceneType.ShowChoice:
                yield return StartCoroutine(ExecuteChoice(step));
                stopStep = true;
                break;
        }

        if (step.sceneType == CutsceneEvent.CutsceneStep.SceneType.ShowText && !step.requireInput && step.nextTextDelay > 0f)
        {
            yield return new WaitForSeconds(step.nextTextDelay);
        }

        if (stopStep)
        {
            yield break;
        }
    }

    private IEnumerator ExecuteMove(
        CutsceneEvent cutsceneEvent,
        CutsceneEvent.CutsceneStep step)
    {
        if (cutsceneEvent.Actor == null || step.moveTo == null)
        {
            string stepName = step.moveTo != null
            ? step.moveTo.name
            : step.sceneType.ToString();

            Debug.LogWarning(
                $"Cutscene step '{stepName}' has a move action " +
                "without an actor or move target."
            );

            yield break;
        }

        GridMovement mover = cutsceneEvent.Actor.GetComponent<GridMovement>();

        if (mover == null)
        {
            Debug.LogWarning(
                $"Cutscene actor '{cutsceneEvent.Actor.name}' " +
                "does not have a GridMover component."
            );

            yield break;
        }

        mover.MoveToGridPosition(step.moveTo.position);

        yield return new WaitUntil(() => !mover.IsMoving);
    }

    private IEnumerator ExecuteText(CutsceneEvent.CutsceneStep step)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("CutsceneManager could not show text because no UIManager is active.");
            yield break;
        }

        UIManager.Instance.ShowMessage(step.dialogueText ?? string.Empty);

        if (step.requireInput)
        {
            yield return StartCoroutine(WaitForSubmit());
        }
    }

    private IEnumerator ExecuteChoice(CutsceneEvent.CutsceneStep step)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("CutsceneManager could not show choices because no UIManager is active.");
            yield break;
        }

        UIManager.Instance.ShowDialogueUI();
        UIManager.Instance.SetDialogueImage(null);
        UIManager.Instance.SetDialogueText(step.dialogueText ?? string.Empty);
        UIManager.Instance.SetDialogueChoicesVisible(true, step.choiceA, step.choiceB);
        yield return null;
    }

    private IEnumerator WaitForSubmit()
    {
        yield return null;

        while (IsCutsceneSubmitHeld())
        {
            yield return null;
        }

        yield return new WaitUntil(IsCutsceneSubmitPressed);
        yield return null;
    }

    private static bool IsCutsceneSubmitPressed()
    {
        return Input.GetKeyDown(KeyCode.Z)
            || Input.GetButtonDown("Submit")
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0);
    }

    private static bool IsCutsceneSubmitHeld()
    {
        return Input.GetKey(KeyCode.Z)
            || Input.GetButton("Submit")
            || Input.GetKey(KeyCode.Space)
            || Input.GetMouseButton(0);
    }

    private void TryStartSceneEvent()
    {
        if (activeSequenceRoutine != null)
        {
            return;
        }

        for (int i = 0; i < cutsceneEvents.Count; i++)
        {
            CutsceneEvent cutsceneEvent = cutsceneEvents[i];
            if (cutsceneEvent != null && cutsceneEvent.EventTriggerType == CutsceneEvent.TriggerType.SceneStart && cutsceneEvent.Steps.Count > 0)
            {
                activeSequenceRoutine = StartCoroutine(PlayEventSequence(cutsceneEvent, 0));
                return;
            }
        }
    }

    private void EndSequence()
    {
        activeSequenceRoutine = null;
        queuedEventId = null;
        GameManager.SetGameplayLocked(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideMessage();
            UIManager.Instance.HideDialogueUI();
        }
    }

    private CutsceneEvent FindEventById(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        for (int i = 0; i < cutsceneEvents.Count; i++)
        {
            if (cutsceneEvents[i] != null && string.Equals(cutsceneEvents[i].EventId, eventId, System.StringComparison.OrdinalIgnoreCase))
            {
                return cutsceneEvents[i];
            }
        }

        Debug.LogWarning($"CutsceneManager could not find event '{eventId}'.");
        return null;
    }

#if UNITY_EDITOR
    public void EditorAddCutsceneEvent()
    {
        UnityEditor.Undo.RecordObject(this, "Add Cutscene Event");

        GameObject eventObject = new GameObject();
        UnityEditor.Undo.RegisterCreatedObjectUndo(eventObject, "Create Cutscene Event");
        eventObject.transform.SetParent(transform, false);

        CutsceneEvent cutsceneEvent = eventObject.AddComponent<CutsceneEvent>();
        cutsceneEvents.Add(cutsceneEvent);
        EditorSyncCutsceneEvents();
        cutsceneEvent.EditorAddStep(this);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorRemoveCutsceneEvent(int index)
    {
        if (index < 0 || index >= cutsceneEvents.Count)
        {
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Remove Cutscene Event");

        CutsceneEvent cutsceneEvent = cutsceneEvents[index];
        cutsceneEvents.RemoveAt(index);

        if (cutsceneEvent != null)
        {
            UnityEditor.Undo.DestroyObjectImmediate(cutsceneEvent.gameObject);
        }

        EditorSyncCutsceneEvents();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorSyncCutsceneEvents()
    {
        for (int i = 0; i < cutsceneEvents.Count; i++)
        {
            CutsceneEvent cutsceneEvent = cutsceneEvents[i];
            if (cutsceneEvent == null)
            {
                continue;
            }

            cutsceneEvent.EditorSetEventId($"Event{i + 1}");
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private static void HideOverworldPopups()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();
        UIManager.Instance.HideDialogueUI();
    }
}
