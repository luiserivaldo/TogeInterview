using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private const string HeroDisplayName = "Hero";

    private PlayerClass player;
    public static UIManager Instance;

    [Header("UI Roots")]
    [SerializeField] private GameObject overworldUIRoot;
    [SerializeField] private GameObject battleUIRoot;

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
    }

    public void SelectBattleDefaultAction()
    {
        SelectBattleAction(attackButton);
    }

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
        if (combatLogText != null)
        {
            combatLogText.text = message;
        }
    }

    public void AppendCombatLog(string message)
    {
        if (combatLogText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        combatLogText.text = string.IsNullOrWhiteSpace(combatLogText.text)
            ? message
            : combatLogText.text + "\n" + message;
    }

    public void SetBattleButtonsInteractable(bool enabled)
    {
        if (attackButton != null)
        {
            attackButton.interactable = enabled;
        }

        if (itemButton != null)
        {
            itemButton.interactable = enabled;
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

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void AutoWireSceneReferences()
    {
        overworldUIRoot ??= FindSceneObject("OverworldUI");
        battleUIRoot ??= FindSceneObject("BattleUI");

        if (battleUIRoot != null)
        {
            attackButton ??= FindComponentInChildren<Button>(battleUIRoot.transform, "AttackButton");
            itemButton ??= FindComponentInChildren<Button>(battleUIRoot.transform, "ItemButton");
            runButton ??= FindComponentInChildren<Button>(battleUIRoot.transform, "RunButton");
            combatLogText ??= FindComponentInChildren<TextMeshProUGUI>(battleUIRoot.transform, "BattleText");

            heroUI ??= SetupBattleUnitUI("HeroUISection");
            enemyUI ??= SetupBattleUnitUI("EnemyUISection");
        }
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
