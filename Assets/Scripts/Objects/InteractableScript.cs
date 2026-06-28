using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    private const string TutorialBlacksmithPrompt = "Go ahead, approach the monster!";

    public enum InteractableType
    {
        Fountain,
        Sign,
        NPC,
        ShopAtk,
        ShopDef,
    }

    public enum SignType
    {
        None,
        FountainSign,
        BlacksmithSign,
        BountyBoardSign,
        TutorialSign,
    }

    [SerializeField] private SpriteRenderer indicatorRenderer;
    [SerializeField] private Sprite dialogueImage;
    [SerializeField] protected bool allowDuringOpeningTutorial;

    public SignType signType = SignType.None;
    public InteractableType objectType;

    [TextArea(2, 5)]
    public string additionalMessage = "";
    public bool overrideMessage = false;

    public SpriteRenderer IndicatorRenderer => indicatorRenderer;

    protected virtual string DefaultInteractionMessage => string.Empty;

    protected virtual void Awake()
    {
        AutoAssignIndicatorRenderer();
    }

    public virtual bool CanInteract =>
        !TutorialManager.IsOpeningTutorialActive ||
        allowDuringOpeningTutorial;

    public virtual void TryInteract(PlayerClass player)
    {
        if (!CanInteract)
        {
            return;
        }

        if (TryHandleTutorialBlacksmithOverride())
        {
            return;
        }

        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.InteractWithObject(this, player);
        }

        if (GameManager.IsGameplayLocked)
        {
            return;
        }

        string message = GetInteractionMessage();
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        UIManager.Instance?.HideMessage();
        UIManager.Instance?.HideBountyBoard();
        DialogueManager.Instance.StartMessage(message, dialogueImage, animateText: false);
    }

    public virtual string GetInteractionMessage()
    {
        string baseMessage = DefaultInteractionMessage;

        switch (objectType)
        {
            case InteractableType.Fountain:
                baseMessage = $"Respawns all monsters. ({GetFountainCost()} GP)";
                break;

            case InteractableType.Sign:
                baseMessage = GetSignMessage();
                break;

            case InteractableType.ShopAtk:
                baseMessage = $"Sharpen your weapon? +1 ATK for {GetShopCost(ShopManager.ShopType.ATK)} GP.";
                break;

            case InteractableType.ShopDef:
                baseMessage = $"Reinforce your armor? +5 DEF for {GetShopCost(ShopManager.ShopType.DEF)} GP.";
                break;
        }

        if (overrideMessage && !string.IsNullOrWhiteSpace(additionalMessage))
        {
            return additionalMessage;
        }

        if (!string.IsNullOrWhiteSpace(additionalMessage) && !string.IsNullOrWhiteSpace(baseMessage))
        {
            return baseMessage + "\n" + additionalMessage;
        }

        return string.IsNullOrWhiteSpace(additionalMessage) ? baseMessage : additionalMessage;
    }

    public string GetSignMessage()
    {
        string baseMessage = "";

        switch (signType)
        {
            case SignType.FountainSign:
                baseMessage = $"Respawns all monsters. ({GetFountainCost()} GP)";
                break;

            case SignType.BlacksmithSign:
                int atkCost = GetShopCost(ShopManager.ShopType.ATK);
                int defCost = GetShopCost(ShopManager.ShopType.DEF);
                baseMessage = $"+1 ATK: {atkCost} GP.\n+5 DEF: {defCost} GP.";
                break;

            case SignType.BountyBoardSign:
                baseMessage = GetBountyBoardMessage();
                break;

            case SignType.TutorialSign:
                baseMessage = "Play tutorial?";
                break;

            default:
                baseMessage = "Nothing interesting here.";
                break;
        }

        return baseMessage;
    }

    private bool TryHandleTutorialBlacksmithOverride()
    {
        if (signType != SignType.BlacksmithSign || !TutorialManager.IsOpeningTutorialActive)
        {
            return false;
        }

        UIManager.Instance?.HideMessage();
        UIManager.Instance?.HideBountyBoard();
        DialogueManager.Instance.StartMessage(TutorialBlacksmithPrompt, dialogueImage, animateText: false);
        return true;
    }

    private int GetFountainCost()
    {
        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        return gameManager == null ? 0 : gameManager.GetCurrentFountainCost();
    }

    protected int GetShopCost(ShopManager.ShopType type)
    {
        return ShopManager.Instance != null ? ShopManager.Instance.GetCurrentCost(type) : 0;
    }

    private string GetBountyBoardMessage()
    {
        return
            "Bounty Board\n" +
            "-----------------------------\n" +
            "Name     ATK  DEF  Bounty\n" +
            "Rat       1    1    1 GP\n" +
            "Spider    2    2    3 GP\n" +
            "Rat2      2    5    6 GP\n" +
            "Crab      1    5    5 GP\n" +
            "Ghost     4    2    8 GP\n" +
            "Cyclops  10   20   50 GP\n";
    }

    protected void AutoAssignIndicatorRenderer()
    {
        if (indicatorRenderer != null)
        {
            return;
        }

        Transform indicator = transform.Find("SelectIcon");
        if (indicator != null)
        {
            indicatorRenderer = indicator.GetComponent<SpriteRenderer>();
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        AutoAssignIndicatorRenderer();
    }
#endif
}
