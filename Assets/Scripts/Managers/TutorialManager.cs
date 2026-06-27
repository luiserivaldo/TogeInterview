using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DefaultExecutionOrder(50)]
public class TutorialManager : MonoBehaviour
{
    [Header("Cutscene Event IDs")]
    [SerializeField] private string introEventId = "Event1";
    [SerializeField] private string monsterApproachEventId = "Event2";
    [SerializeField] private string combatHintEventId = "Event3";
    [SerializeField] private string outroEventId = "Event4";

    [SerializeField] private string tutorialMonsterName = "rat (1)";

    [Header("Battle Guidance")]
    [SerializeField] private Color highlightColor = new(1f, 0.86f, 0.2f, 1f);
    [SerializeField] private float highlightDuration = 1.4f;
    [SerializeField] private float highlightPulseSpeed = 7f;

    private readonly List<MonsterClass> hiddenMonsters = new();
    private readonly List<MonsterClass> activeMonsters = new();

    private CutsceneManager cutsceneManager;
    private BattleManager battleManager;
    private CutsceneEvent introEvent;
    private CutsceneEvent monsterApproachEvent;
    private CutsceneEvent combatHintEvent;
    private CutsceneEvent outroEvent;
    private MonsterClass tutorialMonster;
    private bool battleResolved;
    private BattleManager.BattleResult battleResult;
    private bool introObserved;
    private bool started;
    public static bool IsOpeningTutorialActive { get; private set; }

    private void OnEnable()
    {
        IsOpeningTutorialActive = true;

        battleManager = BattleManager.Instance;
        if (battleManager != null)
        {
            battleManager.BattleEnded += HandleBattleEnded;
        }
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.BattleEnded -= HandleBattleEnded;
        }

        IsOpeningTutorialActive = true;
    }

    private IEnumerator Start()
    {
        yield return null;

        cutsceneManager = CutsceneManager.Instance;
        ResolveTutorialEvents();
        SetupTutorialMonsters();

        started = true;

        if (cutsceneManager != null && !introObserved)
        {
            int waitFrames = 0;
            while (!introObserved && waitFrames < 30)
            {
                if (cutsceneManager.IsSequenceRunning)
                {
                    introObserved = true;
                }

                waitFrames++;
                yield return null;
            }

            if (cutsceneManager.IsSequenceRunning)
            {
                yield return new WaitUntil(() => !cutsceneManager.IsSequenceRunning);
            }
        }

        ActivateTutorialMonster();
        yield return StartCoroutine(PlayExternalEvent(monsterApproachEvent));

        if (battleManager != null)
        {
            battleManager.SetOpeningTutorialMode(true);
        }

        yield return StartCoroutine(PlayExternalEvent(combatHintEvent));
        yield return new WaitUntil(() => battleManager != null && battleManager.IsBattleActive);
        yield return StartCoroutine(ShowBattleHintsRoutine());
        yield return new WaitUntil(() => battleResolved);

        if (tutorialMonster != null && battleResult == BattleManager.BattleResult.RanAway)
        {
            tutorialMonster.gameObject.SetActive(false);
        }

        yield return StartCoroutine(PlayExternalEvent(outroEvent));
        IsOpeningTutorialActive = false;
        RestoreMonsters();
    }

    private void ResolveTutorialEvents()
    {
        if (cutsceneManager == null)
        {
            return;
        }

        introEvent = FindEventById(introEventId);
        monsterApproachEvent = FindEventById(monsterApproachEventId);
        combatHintEvent = FindEventById(combatHintEventId);
        outroEvent = FindEventById(outroEventId);
        introObserved = introEvent == null;
    }

    private CutsceneEvent FindEventById(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId) || cutsceneManager == null)
        {
            return null;
        }

        foreach (CutsceneEvent cutsceneEvent in cutsceneManager.CutsceneEvents)
        {
            if (cutsceneEvent != null && cutsceneEvent.EventId == eventId)
            {
                return cutsceneEvent;
            }
        }

        return null;
    }

    private void SetupTutorialMonsters()
    {
        hiddenMonsters.Clear();
        activeMonsters.Clear();

        MonsterClass[] monsters = Object.FindObjectsByType<MonsterClass>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (monsters.Length == 0)
        {
            return;
        }

        InteractableObject blacksmith = FindBlacksmith();
        Transform blacksmithTransform = blacksmith != null ? blacksmith.transform : null;
        Dictionary<Transform, List<MonsterClass>> groups = new();

        foreach (MonsterClass monster in monsters)
        {
            if (monster == null)
            {
                continue;
            }

            Transform group = FindMonsterGroup(monster.transform);
            if (!groups.TryGetValue(group, out List<MonsterClass> groupMonsters))
            {
                groupMonsters = new List<MonsterClass>();
                groups[group] = groupMonsters;
            }

            groupMonsters.Add(monster);
        }

        tutorialMonster = monsters.FirstOrDefault(monster => monster != null && monster.name == tutorialMonsterName);

        Transform tutorialGroup = tutorialMonster != null
            ? FindMonsterGroup(tutorialMonster.transform)
            : groups.Keys
                .OrderBy(group => blacksmithTransform == null ? 0f : Vector3.Distance(group.position, blacksmithTransform.position))
                .FirstOrDefault();

        if (tutorialMonster == null && tutorialGroup != null && groups.TryGetValue(tutorialGroup, out List<MonsterClass> tutorialGroupMonsters))
        {
            tutorialMonster = tutorialGroupMonsters
                .OrderBy(monster => blacksmithTransform == null ? 0f : Vector3.Distance(monster.transform.position, blacksmithTransform.position))
                .FirstOrDefault();
        }

        foreach (KeyValuePair<Transform, List<MonsterClass>> groupEntry in groups)
        {
            List<MonsterClass> groupMonsters = groupEntry.Value;
            List<MonsterClass> orderedGroup = groupMonsters
                .OrderBy(monster => monster.transform.GetSiblingIndex())
                .ToList();

            MonsterClass fallbackMonster = orderedGroup.FirstOrDefault(monster => monster != tutorialMonster);

            foreach (MonsterClass monster in orderedGroup)
            {
                if (monster == null)
                {
                    continue;
                }

                bool shouldRemainActive = monster == fallbackMonster && monster != tutorialMonster;
                if (shouldRemainActive)
                {
                    activeMonsters.Add(monster);
                    continue;
                }

                HideMonster(monster);
            }
        }
    }

    private void HideMonster(MonsterClass monster)
    {
        if (monster == null)
        {
            return;
        }

        if (!hiddenMonsters.Contains(monster))
        {
            hiddenMonsters.Add(monster);
        }

        if (monster.gameObject.activeSelf)
        {
            monster.gameObject.SetActive(false);
        }
    }

    private void ActivateTutorialMonster()
    {
        if (tutorialMonster == null)
        {
            return;
        }

        tutorialMonster.Respawn();
    }

    private void RestoreMonsters()
    {
        foreach (MonsterClass monster in hiddenMonsters)
        {
            if (monster != null)
            {
                monster.Respawn();
            }
        }

        MonsterManager.Instance?.RespawnAllMonsters();
    }

    private IEnumerator PlayExternalEvent(CutsceneEvent cutsceneEvent)
    {
        if (cutsceneManager == null || cutsceneEvent == null)
        {
            yield break;
        }

        cutsceneManager.TryStartEvent(cutsceneEvent);
        yield return null;

        if (!cutsceneManager.IsSequenceRunning)
        {
            yield break;
        }

        yield return new WaitUntil(() => !cutsceneManager.IsSequenceRunning);
    }

    private IEnumerator ShowBattleHintsRoutine()
    {
        if (UIManager.Instance == null || battleManager == null)
        {
            yield break;
        }

        yield return new WaitUntil(AreTutorialButtonsReady);

        UIManager.Instance.SetBattleButtonsInteractable(false);
        UIManager.Instance.SetCombatLog("Attack is your main damage option. Use it when you want to defeat the enemy.\n\nPress confirm to continue.");
        yield return StartCoroutine(PulseButton(UIManager.Instance.AttackButton, true));

        if (!battleManager.IsBattleActive)
        {
            yield break;
        }

        UIManager.Instance.SetCombatLog("Run lets you leave battle safely. If a fight looks bad, this gets you out.\n\nPress confirm to continue.");
        yield return StartCoroutine(PulseButton(UIManager.Instance.RunButton, true));

        if (!battleManager.IsBattleActive)
        {
            yield break;
        }

        UIManager.Instance.SetCombatLog("Now the battle starts for real.\n\nPress confirm to begin.");
        yield return StartCoroutine(WaitForFreshSubmit());

        if (!battleManager.IsBattleActive)
        {
            yield break;
        }

        battleManager.CompleteOpeningTutorial();
    }

    private IEnumerator PulseButton(Button button, bool waitForSubmit)
    {
        if (button == null)
        {
            yield break;
        }

        Graphic graphic = button.targetGraphic;
        Color baseGraphicColor = graphic != null ? graphic.color : Color.white;
        Vector3 baseScale = button.transform.localScale;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        Color baseLabelColor = label != null ? label.color : Color.white;

        button.Select();

        if (waitForSubmit)
        {
            while (battleManager != null && battleManager.IsBattleActive && !IsTutorialSubmitPressed())
            {
                ApplyPulse(button, graphic, label, baseScale, baseGraphicColor, baseLabelColor);
                yield return null;
            }

            if (battleManager != null && battleManager.IsBattleActive)
            {
                yield return new WaitUntil(() => !IsTutorialSubmitHeld());
            }
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < highlightDuration && battleManager != null && battleManager.IsBattleActive)
            {
                elapsed += Time.deltaTime;
                ApplyPulse(button, graphic, label, baseScale, baseGraphicColor, baseLabelColor);
                yield return null;
            }
        }

        button.transform.localScale = baseScale;
        if (graphic != null)
        {
            graphic.color = baseGraphicColor;
        }

        if (label != null)
        {
            label.color = baseLabelColor;
        }
    }

    private void ApplyPulse(Button button, Graphic graphic, TextMeshProUGUI label, Vector3 baseScale, Color baseGraphicColor, Color baseLabelColor)
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * highlightPulseSpeed) + 1f) * 0.5f;
        button.transform.localScale = Vector3.Lerp(baseScale, baseScale * 1.12f, pulse);

        if (graphic != null)
        {
            graphic.color = Color.Lerp(baseGraphicColor, highlightColor, pulse);
        }

        if (label != null)
        {
            label.color = Color.Lerp(baseLabelColor, highlightColor, pulse);
        }
    }

    private static bool IsTutorialSubmitPressed()
    {
        return Input.GetKeyDown(KeyCode.Z)
            || Input.GetButtonDown("Submit")
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0);
    }

    private static bool IsTutorialSubmitHeld()
    {
        return Input.GetKey(KeyCode.Z)
            || Input.GetButton("Submit")
            || Input.GetKey(KeyCode.Space)
            || Input.GetMouseButton(0);
    }

    private IEnumerator WaitForFreshSubmit()
    {
        while (IsTutorialSubmitHeld())
        {
            yield return null;
        }

        yield return new WaitUntil(IsTutorialSubmitPressed);
        yield return new WaitUntil(() => !IsTutorialSubmitHeld());
    }

    private bool AreTutorialButtonsReady()
    {
        if (UIManager.Instance == null)
        {
            return false;
        }

        Button attackButton = UIManager.Instance.AttackButton;
        Button runButton = UIManager.Instance.RunButton;
        return attackButton != null
            && runButton != null
            && attackButton.gameObject.activeInHierarchy
            && runButton.gameObject.activeInHierarchy;
    }

    private void HandleBattleEnded(BattleManager.BattleResult result, MonsterClass monster)
    {
        if (!started || tutorialMonster == null || monster != tutorialMonster)
        {
            return;
        }

        battleResolved = true;
        battleResult = result;
    }

    private InteractableObject FindBlacksmith()
    {
        InteractableObject[] interactables = Object.FindObjectsByType<InteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (InteractableObject interactable in interactables)
        {
            if (interactable != null && interactable.signType == InteractableObject.SignType.BlacksmithSign)
            {
                return interactable;
            }
        }

        return null;
    }

    private Transform FindMonsterGroup(Transform source)
    {
        Transform current = source;
        while (current != null)
        {
            if (current.name == "Monsters")
            {
                return current;
            }

            current = current.parent;
        }

        return source.parent;
    }
}
