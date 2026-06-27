using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public enum InteractIndicatorState
    {
        Hidden,
        PlayerMultiTarget,
        Discoverable,
        Selected,
        Cancel,
    }

    private const string HeroDisplayName = "Hero";
    private const int MaxCombatLogLines = 3;

    private readonly Queue<string> combatLogEntries = new();
    private PlayerClass player;
    public static UIManager Instance;

    [Header("UI Roots")]
    [SerializeField] private GameObject overworldUIRoot;
    [SerializeField] private GameObject battleUIRoot;
    [SerializeField] private GameObject cutsceneUIRoot;

    [Header("UI References")]
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI moneyText;

    [Header("Sign Elements")]
    public GameObject signTextBox;
    public GameObject bountyBoardPanel;
    public TextMeshProUGUI messageText;

    [Header("Game Over Elements")]
    public GameObject gameOverPanel;

    [Header("Battle Elements")]
    [SerializeField] private BattleUnitUI heroUI;
    [SerializeField] private BattleUnitUI enemyUI;
    [SerializeField] private TextMeshProUGUI combatLogText;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button runButton;

    [Header("Interaction Indicators")]
    [SerializeField] private Sprite playerMultiTargetIndicatorSprite;
    [SerializeField] private Sprite interactableDiscoverableIndicatorSprite;
    [SerializeField] private Sprite interactableSelectedIndicatorSprite;
    [SerializeField] private Sprite cancelIndicatorSprite;
    [SerializeField] private Color playerMultiTargetIndicatorColor = Color.white;
    [SerializeField] private Color interactableDiscoverableIndicatorColor = new(0.4f, 0.4f, 0.4f, 0.9f);
    [SerializeField] private Color interactableSelectedIndicatorColor = Color.white;
    [SerializeField] private Color cancelIndicatorColor = new(1f, 0.82f, 0.82f, 1f);

    [Header("Cutscene Elements")]
    [SerializeField] private Image cutsceneAvatarImage;
    [SerializeField] private TextMeshProUGUI cutsceneDialogueText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TextMeshProUGUI yesButtonText;
    [SerializeField] private TextMeshProUGUI noButtonText;
    [SerializeField] private Transform dialogueEffectAnchor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AutoWireSceneReferences();
        ShowOverworldUI();
        ConfigureBattleActions();
    }

    private void Start()
    {
        player = Object.FindFirstObjectByType<PlayerClass>();
        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (player != null)
        {
            atkText.text = player.attack.ToString();
            defText.text = player.currentDef.ToString();
            moneyText.text = player.money.ToString();
        }
    }

    public void ShowMessage(string msg)
    {
        signTextBox.SetActive(true);
        messageText.text = msg;
    }

    public void HideMessage()
    {
        signTextBox.SetActive(false);
    }

    public void ShowBountyBoard()
    {
        bountyBoardPanel.SetActive(true);
    }

    public void HideBountyBoard()
    {
        bountyBoardPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    public void ShowOverworldUI()
    {
        ClearSelectedUI();

        if (overworldUIRoot != null)
        {
            overworldUIRoot.SetActive(true);
        }

        if (battleUIRoot != null)
        {
            battleUIRoot.SetActive(false);
        }

        if (cutsceneUIRoot != null)
        {
            cutsceneUIRoot.SetActive(false);
        }
    }

    public void ShowBattleUI()
    {
        if (overworldUIRoot != null)
        {
            overworldUIRoot.SetActive(false);
        }

        if (battleUIRoot != null)
        {
            battleUIRoot.SetActive(true);
        }

        if (cutsceneUIRoot != null)
        {
            cutsceneUIRoot.SetActive(false);
        }
    }

    public void ShowCutsceneUI()
    {
        if (overworldUIRoot != null)
        {
            overworldUIRoot.SetActive(true);
        }

        if (battleUIRoot != null)
        {
            battleUIRoot.SetActive(false);
        }

        if (cutsceneUIRoot != null)
        {
            cutsceneUIRoot.SetActive(true);
        }
    }

    public void HideCutsceneUI()
    {
        SetCutsceneChoicesVisible(false);
        SetCutsceneText(string.Empty);
        SetCutsceneAvatar(null);
        ClearSelectedUI();

        if (cutsceneUIRoot != null)
        {
            cutsceneUIRoot.SetActive(false);
        }
    }

    public void SetCutsceneText(string text)
    {
        if (cutsceneDialogueText != null)
        {
            cutsceneDialogueText.text = text ?? string.Empty;
        }
    }

    public void SetCutsceneAvatar(Sprite avatarSprite)
    {
        if (cutsceneAvatarImage == null)
        {
            return;
        }

        cutsceneAvatarImage.sprite = avatarSprite;
        cutsceneAvatarImage.enabled = avatarSprite != null;
    }

    public void SetCutsceneChoicesVisible(bool visible, string yesLabel = "Yes", string noLabel = "No")
    {
        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(visible);
        }

        if (noButton != null)
        {
            noButton.gameObject.SetActive(visible);
        }

        if (yesButtonText != null)
        {
            yesButtonText.text = string.IsNullOrWhiteSpace(yesLabel) ? "Yes" : yesLabel;
        }

        if (noButtonText != null)
        {
            noButtonText.text = string.IsNullOrWhiteSpace(noLabel) ? "No" : noLabel;
        }
    }

    public void BindCutsceneChoiceHandlers(UnityAction yesAction, UnityAction noAction)
    {
        BindButton(yesButton, yesAction, true);
        BindButton(noButton, noAction, true);
    }

    public void SelectCutsceneDefaultAction()
    {
        if (yesButton != null && yesButton.gameObject.activeInHierarchy && yesButton.interactable)
        {
            SelectBattleAction(yesButton);
            return;
        }

        ClearSelectedUI();
    }

    public void ShowDialogueUI()
    {
        ShowCutsceneUI();
    }

    public void HideDialogueUI()
    {
        HideCutsceneUI();
    }

    public void SetDialogueText(string text)
    {
        SetCutsceneText(text);
    }

    public void SetDialogueImage(Sprite image)
    {
        SetCutsceneAvatar(image);
    }

    public void SetDialogueChoicesVisible(bool visible, string yesLabel = "Yes", string noLabel = "No")
    {
        SetCutsceneChoicesVisible(visible, yesLabel, noLabel);
    }

    public void BindDialogueChoiceHandlers(UnityAction yesAction, UnityAction noAction)
    {
        BindCutsceneChoiceHandlers(yesAction, noAction);
    }

    public void SelectDialogueDefaultAction()
    {
        SelectCutsceneDefaultAction();
    }

    public Transform GetDialogueEffectAnchor()
    {
        return dialogueEffectAnchor != null ? dialogueEffectAnchor : cutsceneUIRoot != null ? cutsceneUIRoot.transform : null;
    }

    public void SelectBattleDefaultAction()
    {
        SelectBattleAction(attackButton);
    }

    public Button AttackButton => attackButton;
    public Button RunButton => runButton;

    public void SelectAttackAction()
    {
        SelectBattleAction(attackButton);
    }

    public void SelectItemAction()
    {
        SelectBattleAction(itemButton);
    }

    public void BindBattle(PlayerClass playerUnit, MonsterClass monsterUnit, bool isPlayerTurn = true, int heroMaxHp = -1)
    {
        if (playerUnit == null || monsterUnit == null)
        {
            return;
        }

        Sprite heroSprite = GetUnitSprite(playerUnit.gameObject);
        Sprite enemySprite = GetUnitSprite(monsterUnit.gameObject);
        int resolvedHeroMaxHp = heroMaxHp > 0 ? heroMaxHp : Mathf.Max(1, playerUnit.maxHp);

        heroUI?.SetUnit(HeroDisplayName, playerUnit.currentHp, resolvedHeroMaxHp, playerUnit.attack, playerUnit.currentDef, heroSprite, isPlayerTurn);
        enemyUI?.SetUnit(monsterUnit.DisplayName, monsterUnit.currentHp, monsterUnit.maxHp, monsterUnit.attack, monsterUnit.currentDef, enemySprite, !isPlayerTurn);
    }

    public void SetCombatLog(string message)
    {
        combatLogEntries.Clear();

        if (!string.IsNullOrWhiteSpace(message))
        {
            combatLogEntries.Enqueue(message);
        }

        RefreshCombatLogText();
    }

    public void AppendCombatLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        combatLogEntries.Enqueue(message);

        while (combatLogEntries.Count > MaxCombatLogLines)
        {
            combatLogEntries.Dequeue();
        }

        RefreshCombatLogText();
    }

    public void SetBattleButtonsInteractable(bool enabled)
    {
        if (attackButton != null)
        {
            attackButton.interactable = enabled;
        }

        if (itemButton != null)
        {
            itemButton.interactable = false;
        }

        if (runButton != null)
        {
            runButton.interactable = enabled;
        }
    }

    private void SelectBattleAction(Button button)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SelectBattleActionRoutine(button));
    }

    private IEnumerator SelectBattleActionRoutine(Button button)
    {
        yield return null;

        if (button == null || !button.IsActive() || !button.interactable)
        {
            yield break;
        }

        ClearSelectedUI();
        button.Select();
        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }

    private void ClearSelectedUI()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void RefreshCombatLogText()
    {
        if (combatLogText == null)
        {
            return;
        }

        combatLogText.text = combatLogEntries.Count == 0
            ? string.Empty
            : string.Join("\n", combatLogEntries);
    }

    private void ConfigureBattleActions()
    {
        BindButton(attackButton, BattleManager.Instance.OnAttackPressed);
        BindButton(itemButton, BattleManager.Instance.OnItemPressed);
        BindButton(runButton, BattleManager.Instance.OnRunPressed);
    }

    private Sprite GetUnitSprite(GameObject unitObject)
    {
        if (unitObject == null)
        {
            return null;
        }

        SpriteRenderer spriteRenderer = unitObject.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private void BindButton(Button button, UnityAction action, bool clearExisting = false)
    {
        if (button == null)
        {
            return;
        }

        if (clearExisting)
        {
            button.onClick.RemoveAllListeners();
        }
        else
        {
            button.onClick.RemoveListener(action);
        }

        if (action != null)
        {
            button.onClick.AddListener(action);
        }
    }

    public void ApplyIndicatorState(SpriteRenderer indicator, InteractIndicatorState state)
    {
        if (indicator == null)
        {
            return;
        }

        switch (state)
        {
            case InteractIndicatorState.Hidden:
                indicator.gameObject.SetActive(false);
                break;

            case InteractIndicatorState.PlayerMultiTarget:
                SetIndicatorVisual(indicator, playerMultiTargetIndicatorSprite, playerMultiTargetIndicatorColor);
                break;

            case InteractIndicatorState.Discoverable:
                SetIndicatorVisual(indicator, interactableDiscoverableIndicatorSprite, interactableDiscoverableIndicatorColor);
                break;

            case InteractIndicatorState.Selected:
                SetIndicatorVisual(indicator, interactableSelectedIndicatorSprite, interactableSelectedIndicatorColor);
                break;

            case InteractIndicatorState.Cancel:
                SetIndicatorVisual(indicator, cancelIndicatorSprite != null ? cancelIndicatorSprite : playerMultiTargetIndicatorSprite, cancelIndicatorColor);
                break;
        }
    }

    private static void SetIndicatorVisual(SpriteRenderer indicator, Sprite sprite, Color color)
    {
        if (indicator == null)
        {
            return;
        }

        indicator.sprite = sprite;
        indicator.color = color;
        indicator.gameObject.SetActive(sprite != null);
    }

    private void AutoWireSceneReferences()
    {
        overworldUIRoot ??= FindSceneObject("OverworldUI");
        battleUIRoot ??= FindSceneObject("BattleUI");
        cutsceneUIRoot ??= FindSceneObject("CutsceneUI");

        if (battleUIRoot != null)
        {
            attackButton ??= FindComponentInChildren<Button>(battleUIRoot.transform, "AttackButton");
            itemButton ??= FindComponentInChildren<Button>(battleUIRoot.transform, "ItemButton");
            runButton ??= FindComponentInChildren<Button>(battleUIRoot.transform, "RunButton");
            combatLogText ??= FindComponentInChildren<TextMeshProUGUI>(battleUIRoot.transform, "BattleText");

            heroUI ??= SetupBattleUnitUI("HeroUISection");
            enemyUI ??= SetupBattleUnitUI("EnemyUISection");
        }

        if (cutsceneUIRoot != null)
        {
            cutsceneAvatarImage ??= FindComponentInChildren<Image>(cutsceneUIRoot.transform, "SpeakerAvatar");
            cutsceneDialogueText ??= FindComponentInChildren<TextMeshProUGUI>(cutsceneUIRoot.transform, "DialogueTextBox");
            yesButton ??= FindComponentInChildren<Button>(cutsceneUIRoot.transform, "YesButton");
            noButton ??= FindComponentInChildren<Button>(cutsceneUIRoot.transform, "NoButton");
            yesButtonText ??= yesButton != null ? yesButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            noButtonText ??= noButton != null ? noButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            dialogueEffectAnchor ??= cutsceneUIRoot.transform;
            ConfigureCutsceneNavigation();
        }
    }

    private void ConfigureCutsceneNavigation()
    {
        if (yesButton == null || noButton == null)
        {
            return;
        }

        Navigation yesNavigation = yesButton.navigation;
        yesNavigation.mode = Navigation.Mode.Explicit;
        yesNavigation.selectOnRight = noButton;
        yesNavigation.selectOnLeft = noButton;
        yesButton.navigation = yesNavigation;

        Navigation noNavigation = noButton.navigation;
        noNavigation.mode = Navigation.Mode.Explicit;
        noNavigation.selectOnLeft = yesButton;
        noNavigation.selectOnRight = yesButton;
        noButton.navigation = noNavigation;
    }

    private BattleUnitUI SetupBattleUnitUI(string objectName)
    {
        GameObject targetObject = FindSceneObject(objectName);
        if (targetObject == null)
        {
            return null;
        }

        BattleUnitUI unitUI = targetObject.GetComponent<BattleUnitUI>();
        if (unitUI == null)
        {
            unitUI = targetObject.AddComponent<BattleUnitUI>();
        }

        unitUI.AutoBind();
        return unitUI;
    }

    private T FindComponentInChildren<T>(Transform root, string objectName) where T : Component
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<T>();
    }

    private GameObject FindSceneObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject rootObject in activeScene.GetRootGameObjects())
        {
            Transform match = FindChildRecursive(rootObject.transform, objectName);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindChildRecursive(parent.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
