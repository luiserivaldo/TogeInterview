using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;

    [SerializeField] private float charactersPerSecond = 45f;

    private Coroutine dialogueRoutine;
    private DialogueScript_ScriptableObject activeScript;
    private Dictionary<string, int> labelLookup = new();
    private GameObject activeParticleInstance;
    private Sprite defaultDialogueImage;
    private Action onDialogueComplete;
    private int lastChoiceIndex = -1;
    private string lastChoiceKey = string.Empty;

    public bool IsDialogueRunning { get; private set; }
    public int LastChoiceIndex => lastChoiceIndex;
    public string LastChoiceKey => lastChoiceKey;

    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindFirstObjectByType<DialogueManager>();

                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(DialogueManager));
                    instance = managerObject.AddComponent<DialogueManager>();
                }
            }

            return instance;
        }
    }

    private void Awake()
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

        HideDialogueUI();
    }

    public void StartTutorialPrompt(InteractableObject interactable)
    {
        if (interactable == null)
        {
            return;
        }

        StopDialogue(false);
        dialogueRoutine = StartCoroutine(RunTutorialPromptRoutine(interactable));
    }

    public void StartScript(DialogueScript_ScriptableObject script, Sprite fallbackImage = null, Action onComplete = null)
    {
        if (script == null)
        {
            Debug.LogWarning("DialogueManager.StartScript called with no script.");
            return;
        }

        StopDialogue(false);
        dialogueRoutine = StartCoroutine(RunScriptRoutine(script, fallbackImage, onComplete));
    }

    public void StopDialogue(bool unlockGameplay = true)
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        CleanupParticle();
        ResetDialogueState();
        HideDialogueUI();

        if (unlockGameplay)
        {
            GameManager.SetGameplayLocked(false);
        }
    }

    private IEnumerator RunTutorialPromptRoutine(InteractableObject interactable)
    {
        BeginDialogue(interactable != null ? interactable.dialogueImage : null, null);

        int selectedChoice = -1;

        UIManager.Instance.SetDialogueText("Play tutorial?");
        UIManager.Instance.SetDialogueChoicesVisible(true, "Yes", "No");
        UIManager.Instance.BindDialogueChoiceHandlers(
            () => selectedChoice = 0,
            () => selectedChoice = 1);
        UIManager.Instance.SelectDialogueDefaultAction();

        while (selectedChoice < 0)
        {
            yield return null;
        }

        UIManager.Instance.SetDialogueChoicesVisible(false);

        if (selectedChoice == 0 && interactable != null && interactable.tutorialDialogueScript != null)
        {
            yield return StartCoroutine(ExecuteScript(interactable.tutorialDialogueScript, interactable.dialogueImage));
        }

        EndDialogue();
    }

    private IEnumerator RunScriptRoutine(DialogueScript_ScriptableObject script, Sprite fallbackImage, Action onComplete)
    {
        BeginDialogue(fallbackImage, onComplete);
        yield return StartCoroutine(ExecuteScript(script, fallbackImage));
        EndDialogue();
    }

    private void BeginDialogue(Sprite fallbackImage, Action onComplete)
    {
        activeScript = null;
        defaultDialogueImage = fallbackImage;
        onDialogueComplete = onComplete;
        lastChoiceIndex = -1;
        lastChoiceKey = string.Empty;
        IsDialogueRunning = true;

        GameManager.SetGameplayLocked(true);
        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();
        UIManager.Instance.ShowDialogueUI();
        UIManager.Instance.SetDialogueChoicesVisible(false);
        UIManager.Instance.SetDialogueText(string.Empty);
        UIManager.Instance.SetDialogueImage(defaultDialogueImage);
    }

    private void EndDialogue()
    {
        Action completion = onDialogueComplete;
        StopDialogue(true);
        completion?.Invoke();
    }

    private IEnumerator ExecuteScript(DialogueScript_ScriptableObject script, Sprite fallbackImage)
    {
        activeScript = script;
        defaultDialogueImage = fallbackImage;
        BuildLabelLookup(script);

        int commandIndex = 0;
        IReadOnlyList<DialogueCommandData> commands = script.Commands;

        while (commandIndex < commands.Count)
        {
            DialogueCommandData command = commands[commandIndex];

            if (command == null)
            {
                commandIndex++;
                continue;
            }

            switch (command.commandType)
            {
                case DialogueCommandType.ShowText:
                    yield return StartCoroutine(ExecuteShowText(command));
                    commandIndex++;
                    break;

                case DialogueCommandType.ShowChoice:
                    yield return StartCoroutine(ExecuteShowChoice(command));
                    commandIndex++;
                    break;

                case DialogueCommandType.Label:
                    commandIndex++;
                    break;

                case DialogueCommandType.JumpToLabel:
                    commandIndex = ResolveJumpIndex(command.targetLabel, commandIndex + 1);
                    break;

                case DialogueCommandType.ForkFromLastChoice:
                    commandIndex = ResolveForkIndex(command, commandIndex + 1);
                    break;

                case DialogueCommandType.PlaySfx:
                    ExecutePlaySfx(command);
                    commandIndex++;
                    break;

                case DialogueCommandType.ShowImage:
                    ExecuteShowImage(command, fallbackImage);
                    commandIndex++;
                    break;

                case DialogueCommandType.ShowParticle:
                    yield return StartCoroutine(ExecuteShowParticle(command));
                    commandIndex++;
                    break;

                default:
                    commandIndex++;
                    break;
            }
        }
    }

    private IEnumerator ExecuteShowText(DialogueCommandData command)
    {
        UIManager.Instance.SetDialogueChoicesVisible(false);
        UIManager.Instance.SetDialogueImage(command.image != null ? command.image : defaultDialogueImage);

        string text = command.text ?? string.Empty;
        if (!command.animateText || charactersPerSecond <= 0f)
        {
            UIManager.Instance.SetDialogueText(text);
        }
        else
        {
            float secondsPerCharacter = 1f / charactersPerSecond;

            for (int i = 1; i <= text.Length; i++)
            {
                if (IsDialogueSubmitPressed())
                {
                    UIManager.Instance.SetDialogueText(text);
                    break;
                }

                UIManager.Instance.SetDialogueText(text.Substring(0, i));
                yield return new WaitForSeconds(secondsPerCharacter);
            }
        }

        yield return new WaitUntil(IsDialogueSubmitPressed);
    }

    private IEnumerator ExecuteShowChoice(DialogueCommandData command)
    {
        string leftLabel = command.options.Count > 0 ? command.options[0].optionLabel : "Yes";
        string rightLabel = command.options.Count > 1 ? command.options[1].optionLabel : "No";

        int resolvedChoice = -1;
        UIManager.Instance.SetDialogueChoicesVisible(true, leftLabel, rightLabel);
        UIManager.Instance.BindDialogueChoiceHandlers(
            () => resolvedChoice = 0,
            () => resolvedChoice = 1);
        UIManager.Instance.SelectDialogueDefaultAction();

        while (resolvedChoice < 0)
        {
            if (IsDialogueSubmitPressed())
            {
                Button selectedButton = EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>();
                if (selectedButton != null && selectedButton.IsActive() && selectedButton.interactable)
                {
                    selectedButton.onClick.Invoke();
                }
            }

            yield return null;
        }

        lastChoiceIndex = resolvedChoice;
        lastChoiceKey = resolvedChoice >= 0 && resolvedChoice < command.options.Count
            ? command.options[resolvedChoice].optionKey
            : string.Empty;

        UIManager.Instance.SetDialogueChoicesVisible(false);
    }

    private void ExecutePlaySfx(DialogueCommandData command)
    {
        if (string.IsNullOrWhiteSpace(command.sfxKey))
        {
            return;
        }

        AudioManager audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlaySFX(command.sfxKey);
        }
    }

    private void ExecuteShowImage(DialogueCommandData command, Sprite fallbackImage)
    {
        UIManager.Instance.SetDialogueImage(command.image != null ? command.image : fallbackImage);
    }

    private IEnumerator ExecuteShowParticle(DialogueCommandData command)
    {
        CleanupParticle();

        if (command.particlePrefab == null)
        {
            yield break;
        }

        Transform effectAnchor = UIManager.Instance.GetDialogueEffectAnchor();
        activeParticleInstance = effectAnchor != null
            ? Instantiate(command.particlePrefab, effectAnchor, false)
            : Instantiate(command.particlePrefab);

        if (!command.waitForParticleToFinish)
        {
            yield break;
        }

        ParticleSystem particleSystem = activeParticleInstance.GetComponentInChildren<ParticleSystem>();
        if (particleSystem == null)
        {
            yield break;
        }

        float waitDuration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
        if (waitDuration > 0f)
        {
            yield return new WaitForSeconds(waitDuration);
        }
    }

    private int ResolveJumpIndex(string targetLabel, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(targetLabel) && labelLookup.TryGetValue(targetLabel, out int jumpIndex))
        {
            return jumpIndex + 1;
        }

        return fallbackIndex;
    }

    private int ResolveForkIndex(DialogueCommandData command, int fallbackIndex)
    {
        if (lastChoiceIndex >= 0 && lastChoiceIndex < command.options.Count)
        {
            string targetLabel = command.options[lastChoiceIndex].targetLabel;
            return ResolveJumpIndex(targetLabel, fallbackIndex);
        }

        return fallbackIndex;
    }

    private void BuildLabelLookup(DialogueScript_ScriptableObject script)
    {
        labelLookup.Clear();

        IReadOnlyList<DialogueCommandData> commands = script.Commands;
        for (int i = 0; i < commands.Count; i++)
        {
            DialogueCommandData command = commands[i];
            if (command == null || command.commandType != DialogueCommandType.Label || string.IsNullOrWhiteSpace(command.label))
            {
                continue;
            }

            labelLookup[command.label] = i;
        }
    }

    private void HideDialogueUI()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.HideDialogueUI();
    }

    private void ResetDialogueState()
    {
        activeScript = null;
        defaultDialogueImage = null;
        onDialogueComplete = null;
        lastChoiceIndex = -1;
        lastChoiceKey = string.Empty;
        IsDialogueRunning = false;
    }

    private void CleanupParticle()
    {
        if (activeParticleInstance != null)
        {
            Destroy(activeParticleInstance);
            activeParticleInstance = null;
        }
    }

    private bool IsDialogueSubmitPressed()
    {
        return Input.GetKeyDown(KeyCode.Z) || Input.GetButtonDown("Submit");
    }
}
