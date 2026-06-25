using UnityEngine;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
{
    private PlayerClass currentPlayer;
    private MonsterClass currentMonster;
    private GameObject panelRoot;
    private Button attackButton;
    private Button itemButton;
    private Button runButton;
    private Text statusText;
    private bool combatInputEnabled;
    private bool isCombatActive;
    private static Sprite cachedSprite;

    public bool IsCombatActive => isCombatActive;
    public RectTransform AttackButtonTransform => attackButton != null ? attackButton.GetComponent<RectTransform>() : null;
    public RectTransform ItemButtonTransform => itemButton != null ? itemButton.GetComponent<RectTransform>() : null;
    public RectTransform RunButtonTransform => runButton != null ? runButton.GetComponent<RectTransform>() : null;

    public void BeginCombat(PlayerClass player, MonsterClass monster)
    {
        if (player == null || monster == null || isCombatActive)
        {
            return;
        }

        EnsureUi();

        currentPlayer = player;
        currentMonster = monster;
        isCombatActive = true;
        panelRoot.SetActive(true);
        SetInputEnabled(true);
        SetStatus(monster.isTutorialMonster ? "The guide watches closely..." : $"A wild {monster.name} appears.");

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetControlMode(PlayerController.ControlMode.Locked);
        }

        TutorialDirector.Instance?.HandleCombatStarted(this, monster);
    }

    public void PauseForTutorial(string statusMessage)
    {
        SetInputEnabled(false);
        SetStatus(statusMessage);
    }

    public void ResumeAfterTutorial(string statusMessage)
    {
        SetInputEnabled(true);
        SetStatus(statusMessage);
    }

    public void CancelCombat()
    {
        if (!isCombatActive)
        {
            return;
        }

        panelRoot.SetActive(false);
        currentPlayer = null;
        currentMonster = null;
        isCombatActive = false;
        combatInputEnabled = false;
    }

    private void EnsureUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Runtime Combat Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        panelRoot = CreateUiObject("Combat Panel", canvas.transform);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-40f, 40f);
        panelRect.sizeDelta = new Vector2(360f, 260f);

        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.sprite = GetRuntimeSprite();
        panelImage.color = new Color(0.07f, 0.09f, 0.15f, 0.95f);

        GameObject titleObject = CreateUiObject("Combat Title", panelRoot.transform);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(20f, -48f);
        titleRect.offsetMax = new Vector2(-20f, -8f);
        Text titleText = titleObject.AddComponent<Text>();
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 26;
        titleText.text = "Battle Commands";
        titleText.color = Color.white;

        GameObject statusObject = CreateUiObject("Combat Status", panelRoot.transform);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(20f, -120f);
        statusRect.offsetMax = new Vector2(-20f, -60f);
        statusText = statusObject.AddComponent<Text>();
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 18;
        statusText.color = new Color(0.94f, 0.94f, 0.96f, 1f);

        attackButton = CreateButton(panelRoot.transform, "Attack", new Vector2(0f, 0f), HandleAttack);
        itemButton = CreateButton(panelRoot.transform, "Item", new Vector2(0f, -62f), HandleItem);
        runButton = CreateButton(panelRoot.transform, "Run", new Vector2(0f, -124f), HandleRun);

        panelRoot.SetActive(false);
    }

    private Button CreateButton(Transform parent, string label, Vector2 offset, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(label + " Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f + (-offset.y));
        rect.sizeDelta = new Vector2(280f, 50f);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = GetRuntimeSprite();
        image.color = new Color(0.18f, 0.27f, 0.43f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.27f, 0.43f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.39f, 0.61f, 1f);
        colors.pressedColor = new Color(0.11f, 0.18f, 0.29f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        GameObject textObject = CreateUiObject(label + " Text", buttonObject.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.text = label;
        text.color = Color.white;

        return button;
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Sprite GetRuntimeSprite()
    {
        if (cachedSprite == null)
        {
            cachedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return cachedSprite;
    }

    private void SetInputEnabled(bool enabled)
    {
        combatInputEnabled = enabled;
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

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void HandleAttack()
    {
        if (!isCombatActive || !combatInputEnabled)
        {
            return;
        }

        GameManager.Instance.ResolveCombatRound(currentPlayer, currentMonster);

        if (currentMonster == null || !currentMonster.gameObject.activeSelf || currentMonster.currentDef <= 0)
        {
            EndCombat(true);
            return;
        }

        if (currentPlayer == null || currentPlayer.defense <= 0)
        {
            CancelCombat();
            return;
        }

        SetStatus($"{currentMonster.name} still stands. Choose again.");
    }

    private void HandleItem()
    {
        if (!isCombatActive || !combatInputEnabled)
        {
            return;
        }

        SetStatus("You do not have any usable items yet.");
    }

    private void HandleRun()
    {
        if (!isCombatActive || !combatInputEnabled)
        {
            return;
        }

        if (currentMonster != null && currentMonster.isTutorialMonster)
        {
            SetStatus("The guide wants you to finish this fight yourself.");
            return;
        }

        EndCombat(false);
    }

    private void EndCombat(bool monsterDefeated)
    {
        MonsterClass finishedMonster = currentMonster;
        PlayerClass finishedPlayer = currentPlayer;

        panelRoot.SetActive(false);
        currentMonster = null;
        currentPlayer = null;
        isCombatActive = false;
        combatInputEnabled = false;

        bool keepLocked = TutorialDirector.Instance != null && TutorialDirector.Instance.ShouldKeepPlayerLockedAfterCombat(finishedMonster);
        if (!keepLocked && finishedPlayer != null)
        {
            PlayerController controller = finishedPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetControlMode(PlayerController.ControlMode.Player);
            }
        }

        TutorialDirector.Instance?.HandleCombatEnded(monsterDefeated, finishedMonster);
    }
}
