using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fungus;

public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    private const string TutorialCompletedKey = "tutorial_completed";

    private PlayerController playerController;
    private PlayerClass playerClass;
    private CombatController combatController;
    private TutorialOverlayUI overlayUi;
    private Flowchart tutorialFlowchart;
    private NpcCutsceneMover tutorialNpc;
    private MonsterClass tutorialMonster;
    private SayDialog sayDialog;
    private MenuDialog menuDialog;
    private bool tutorialStarted;
    private bool tutorialAccepted;
    private bool waitingForTutorialCombat;
    private bool tutorialCombatStarted;
    private bool tutorialCombatResolved;
    private Vector3 playerStartPosition;
    private Vector3 npcHomePosition;
    private Vector3 npcTalkPosition;
    private Vector3 monsterPosition;
    private Vector3 playerStagingPosition;
    private Vector3 npcGuidePosition;

    public bool IsTutorialInProgress => tutorialStarted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        if (SceneManager.GetActiveScene().name != "map01")
        {
            yield break;
        }

        if (PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1)
        {
            yield break;
        }

        SetupRuntimeReferences();
        SpawnTutorialActors();
        StartTutorial();
    }

    public void StartTutorial()
    {
        if (tutorialStarted || PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1)
        {
            return;
        }

        tutorialStarted = true;
        StartCoroutine(RunTutorialSequence());
    }

    public void BeginNpcApproach()
    {
        if (tutorialNpc != null)
        {
            tutorialNpc.MoveTo(npcTalkPosition);
        }
    }

    public void GuidePlayerToMonster()
    {
        if (playerController != null)
        {
            waitingForTutorialCombat = true;
            tutorialCombatStarted = false;
            tutorialCombatResolved = false;
            playerController.MoveToPosition(monsterPosition, null, true);
        }
    }

    public void PauseForCombatTutorial()
    {
        combatController?.PauseForTutorial("Listen to the guide before you pick a command.");
    }

    public void ResumeTutorialCombat()
    {
        overlayUi?.Hide();
        combatController?.ResumeAfterTutorial("Your turn. Pick a command.");
    }

    public void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();
        CleanupTutorialState();
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    public void HandleCombatStarted(CombatController controller, MonsterClass monster)
    {
        if (monster == tutorialMonster && waitingForTutorialCombat)
        {
            tutorialCombatStarted = true;
            PauseForCombatTutorial();
        }
    }

    public void HandleCombatEnded(bool monsterDefeated, MonsterClass monster)
    {
        if (monster == tutorialMonster)
        {
            tutorialCombatResolved = monsterDefeated;
        }
    }

    public void HandleMonsterKilled(MonsterClass monster)
    {
        if (monster == tutorialMonster)
        {
            tutorialCombatResolved = true;
        }
    }

    public bool ShouldKeepPlayerLockedAfterCombat(MonsterClass monster)
    {
        return tutorialStarted && monster == tutorialMonster;
    }

    private void SetupRuntimeReferences()
    {
        playerController = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        playerClass = UnityEngine.Object.FindFirstObjectByType<PlayerClass>();
        combatController = GameManager.Instance != null ? GameManager.Instance.CombatController : GetComponent<CombatController>();
        overlayUi = GetComponent<TutorialOverlayUI>();
        if (overlayUi == null)
        {
            overlayUi = gameObject.AddComponent<TutorialOverlayUI>();
        }

        tutorialFlowchart = UnityEngine.Object.FindFirstObjectByType<Flowchart>();
        if (tutorialFlowchart == null)
        {
            GameObject flowchartObject = new GameObject("Tutorial Flowchart");
            tutorialFlowchart = flowchartObject.AddComponent<Flowchart>();
        }

        sayDialog = SayDialog.GetSayDialog();
        menuDialog = MenuDialog.GetMenuDialog();
        if (menuDialog != null)
        {
            menuDialog.Clear();
            menuDialog.SetActive(false);
        }

        playerStartPosition = playerController != null ? playerController.GridPosition : Vector3.zero;
        ComputeStagePositions();
    }

    private void SpawnTutorialActors()
    {
        if (playerController == null)
        {
            return;
        }

        if (tutorialNpc == null)
        {
            SpriteRenderer sourceRenderer = FindVisualSource();
            GameObject npcObject = new GameObject("Tutorial NPC");
            SpriteRenderer npcRenderer = npcObject.AddComponent<SpriteRenderer>();
            if (sourceRenderer != null)
            {
                npcRenderer.sprite = sourceRenderer.sprite;
                npcRenderer.material = sourceRenderer.material;
                npcRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                npcRenderer.sortingOrder = sourceRenderer.sortingOrder;
            }
            npcRenderer.color = new Color(0.82f, 0.96f, 1f, 1f);
            npcObject.transform.position = npcHomePosition;
            tutorialNpc = npcObject.AddComponent<NpcCutsceneMover>();
            tutorialNpc.SetHomePosition(npcHomePosition);
        }

        if (tutorialMonster == null)
        {
            MonsterClass sourceMonster = FindMonsterTemplate();
            if (sourceMonster == null)
            {
                return;
            }

            GameObject monsterObject = Instantiate(sourceMonster.gameObject, monsterPosition, Quaternion.identity);
            monsterObject.name = "Tutorial Monster";
            tutorialMonster = monsterObject.GetComponent<MonsterClass>();
            tutorialMonster.attack = 1;
            tutorialMonster.maxDef = 1;
            tutorialMonster.currentDef = 1;
            tutorialMonster.money = 1;
            tutorialMonster.isTutorialMonster = true;
        }
    }

    private IEnumerator RunTutorialSequence()
    {
        if (playerController == null)
        {
            yield break;
        }

        playerController.SetControlMode(PlayerController.ControlMode.Locked);

        yield return MoveNpcTo(npcTalkPosition);

        bool accepted = false;
        yield return AskForHelp(result => accepted = result);
        tutorialAccepted = accepted;

        if (!accepted)
        {
            yield return SpeakGuideLine("Well best of luck to you.");
            yield return MoveNpcHome();
            SkipTutorial();
            yield break;
        }

        yield return SpeakGuideLine("Lets get you started.");
        yield return MoveNpcTo(npcGuidePosition);
        yield return SpeakGuideLine("Walk into it.");

        GuidePlayerToMonster();
        yield return new WaitUntil(() => tutorialCombatStarted);

        yield return HighlightControl(combatController.AttackButtonTransform, "This is the attack button. Use it to attack.");
        yield return HighlightControl(combatController.ItemButtonTransform, "This is the item button. It will be useful later.");
        yield return HighlightControl(combatController.RunButtonTransform, "If you feel like you cant win, theres no shame in running away.");
        yield return HighlightControl(combatController.AttackButtonTransform, "Why dont you try handling this one yourself?");
        ResumeTutorialCombat();

        yield return new WaitUntil(() => tutorialCombatResolved);

        yield return SpeakGuideLine("Well best of luck to you.");
        yield return MoveNpcHome();
        CompleteTutorial();
    }

    private IEnumerator AskForHelp(Action<bool> onChoice)
    {
        bool? selection = null;
        yield return ShowPromptWithoutAdvance("You look new, do you need help?");

        menuDialog.Clear();
        menuDialog.SetActive(true);

        UnityEngine.UI.Button[] buttons = menuDialog.CachedButtons;
        ConfigureMenuButton(buttons, 0, "YES", () => selection = true);
        ConfigureMenuButton(buttons, 1, "NO", () => selection = false);

        yield return new WaitUntil(() => selection.HasValue);
        menuDialog.Clear();
        menuDialog.SetActive(false);
        onChoice?.Invoke(selection.Value);
    }

    private IEnumerator HighlightControl(RectTransform target, string line)
    {
        overlayUi.ShowHighlight(target, line);
        yield return SpeakGuideLine(line);
    }

    private void ConfigureMenuButton(UnityEngine.UI.Button[] buttons, int index, string label, Action onClick)
    {
        if (buttons == null || index < 0 || index >= buttons.Length)
        {
            return;
        }

        UnityEngine.UI.Button button = buttons[index];
        button.gameObject.SetActive(true);
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        UnityEngine.UI.Text text = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }


    private IEnumerator SpeakGuideLine(string line)
    {
        bool finished = false;
        PrepareGuideDialog();
        sayDialog.Say(line, true, true, false, true, false, null, () => finished = true);
        yield return new WaitUntil(() => finished);
    }

    private IEnumerator ShowPromptWithoutAdvance(string line)
    {
        bool finished = false;
        PrepareGuideDialog();
        sayDialog.Say(line, true, false, false, true, false, null, () => finished = true);
        yield return new WaitUntil(() => finished);
    }

    private void PrepareGuideDialog()
    {
        if (sayDialog == null)
        {
            sayDialog = SayDialog.GetSayDialog();
        }

        sayDialog.SetActive(true);
        sayDialog.SetCharacter(null);
        sayDialog.SetCharacterImage(null);
        sayDialog.SetCharacterName("Guide", Color.white);
    }

    private IEnumerator MoveNpcTo(Vector3 target)
    {
        bool completed = false;
        tutorialNpc.MoveTo(target, () => completed = true);
        yield return new WaitUntil(() => completed);
    }

    private IEnumerator MoveNpcHome()
    {
        bool completed = false;
        tutorialNpc.ReturnHome(() => completed = true);
        yield return new WaitUntil(() => completed);
    }

    private void CleanupTutorialState()
    {
        tutorialStarted = false;
        waitingForTutorialCombat = false;
        tutorialCombatStarted = false;
        tutorialCombatResolved = false;

        if (overlayUi != null)
        {
            overlayUi.Hide();
        }

        if (menuDialog != null)
        {
            menuDialog.Clear();
            menuDialog.SetActive(false);
        }

        if (sayDialog != null)
        {
            sayDialog.SetActive(false);
        }

        if (tutorialMonster != null && tutorialMonster.gameObject.activeSelf)
        {
            tutorialMonster.gameObject.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.SetControlMode(PlayerController.ControlMode.Player);
        }
    }

    private void ComputeStagePositions()
    {
        Vector3[][] plans = new Vector3[][]
        {
            new [] { playerStartPosition + Vector3.left * 3f, playerStartPosition + Vector3.left, playerStartPosition + Vector3.right * 3f, playerStartPosition + Vector3.right * 2f, playerStartPosition + Vector3.right },
            new [] { playerStartPosition + Vector3.right * 3f, playerStartPosition + Vector3.right, playerStartPosition + Vector3.left * 3f, playerStartPosition + Vector3.left * 2f, playerStartPosition + Vector3.left },
            new [] { playerStartPosition + Vector3.down * 3f, playerStartPosition + Vector3.down, playerStartPosition + Vector3.up * 3f, playerStartPosition + Vector3.up * 2f, playerStartPosition + Vector3.up },
            new [] { playerStartPosition + Vector3.up * 3f, playerStartPosition + Vector3.up, playerStartPosition + Vector3.down * 3f, playerStartPosition + Vector3.down * 2f, playerStartPosition + Vector3.down },
        };

        for (int i = 0; i < plans.Length; i++)
        {
            Vector3 candidateNpcHome = plans[i][0];
            Vector3 candidateNpcTalk = plans[i][1];
            Vector3 candidateMonster = plans[i][2];
            Vector3 candidatePlayerStage = plans[i][3];
            Vector3 candidateNpcGuide = plans[i][4];

            if (playerController.CanOccupy(candidateNpcHome, true) &&
                playerController.CanOccupy(candidateNpcTalk, true) &&
                playerController.CanOccupy(candidatePlayerStage, true) &&
                playerController.CanOccupy(candidateMonster, true) &&
                playerController.CanOccupy(candidateNpcGuide, true))
            {
                npcHomePosition = candidateNpcHome;
                npcTalkPosition = candidateNpcTalk;
                monsterPosition = candidateMonster;
                playerStagingPosition = candidatePlayerStage;
                npcGuidePosition = candidateNpcGuide;
                return;
            }
        }

        npcHomePosition = playerStartPosition + Vector3.left * 3f;
        npcTalkPosition = playerStartPosition + Vector3.left;
        monsterPosition = playerStartPosition + Vector3.right * 3f;
        playerStagingPosition = playerStartPosition + Vector3.right * 2f;
        npcGuidePosition = playerStartPosition + Vector3.right;
    }

    private SpriteRenderer FindVisualSource()
    {
        InteractableObject[] interactables = UnityEngine.Object.FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i].signType == InteractableObject.SignType.BlacksmithSign)
            {
                return interactables[i].GetComponent<SpriteRenderer>();
            }
        }

        return playerController != null ? playerController.GetComponent<SpriteRenderer>() : null;
    }

    private MonsterClass FindMonsterTemplate()
    {
        MonsterClass[] monsters = UnityEngine.Object.FindObjectsByType<MonsterClass>(FindObjectsSortMode.None);
        MonsterClass nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterClass candidate = monsters[i];
            if (candidate == null || !candidate.gameObject.activeSelf || candidate.isTutorialMonster)
            {
                continue;
            }

            float distance = Vector3.Distance(candidate.transform.position, playerStartPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
