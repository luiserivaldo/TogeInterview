using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Serializable]
    public class BoardPage
    {
        [TextArea(3, 8)]
        public string text = string.Empty;
    }

    [Serializable]
    public class BoardSequence
    {
        public string sequenceId = "default";
        public List<BoardPage> pages = new();
    }

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

    [Header("Board Presentation")]
    [SerializeField] private bool useBoardUi;
    [SerializeField] private string defaultBoardSequenceId = "default";
    [SerializeField] private string boardPreviousLabel = "Previous";
    [SerializeField] private string boardNextLabel = "Next";
    [SerializeField] private string boardCloseLabel = "Close";
    [SerializeField] private List<BoardSequence> boardSequences = new();

    public SignType signType = SignType.None;
    public InteractableType objectType;

    [TextArea(2, 5)]
    public string additionalMessage = "";
    public bool overrideMessage = false;

    public SpriteRenderer IndicatorRenderer => indicatorRenderer;

    protected virtual string DefaultInteractionMessage => string.Empty;

    private void Awake()
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

        if (TryShowBoardSequence())
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

    public IReadOnlyList<string> GetBoardPages(string sequenceId = null)
    {
        List<string> pages = ExtractBoardPages(string.IsNullOrWhiteSpace(sequenceId) ? defaultBoardSequenceId : sequenceId);
        if (pages.Count > 0)
        {
            return pages;
        }

        string fallbackMessage = GetInteractionMessage();
        if (string.IsNullOrWhiteSpace(fallbackMessage))
        {
            return Array.Empty<string>();
        }

        return new[] { fallbackMessage };
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

    private bool TryShowBoardSequence()
    {
        if (!ShouldPresentWithBoardUi() || UIManager.Instance == null || !UIManager.Instance.IsBoardUiAvailable)
        {
            return false;
        }

        IReadOnlyList<string> pages = GetBoardPages();
        if (pages.Count == 0)
        {
            return false;
        }

        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();
        GameManager.SetGameplayLocked(true);
        UIManager.Instance.ShowBoardPages(pages, HandleBoardClosed, boardPreviousLabel, boardNextLabel, boardCloseLabel);
        return true;
    }

    private void HandleBoardClosed()
    {
        GameManager.SetGameplayLocked(false);
    }


    private bool ShouldPresentWithBoardUi()
    {
        return useBoardUi || objectType == InteractableType.Sign || HasBoardPageContent();
    }

    private bool HasBoardPageContent()
    {
        if (boardSequences == null)
        {
            return false;
        }

        for (int i = 0; i < boardSequences.Count; i++)
        {
            BoardSequence sequence = boardSequences[i];
            if (sequence == null || sequence.pages == null)
            {
                continue;
            }

            for (int j = 0; j < sequence.pages.Count; j++)
            {
                BoardPage page = sequence.pages[j];
                if (page != null && !string.IsNullOrWhiteSpace(page.text))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private List<string> ExtractBoardPages(string sequenceId)
    {
        List<string> pages = new();

        BoardSequence matchedSequence = null;
        for (int i = 0; i < boardSequences.Count; i++)
        {
            BoardSequence sequence = boardSequences[i];
            if (sequence == null || sequence.pages == null || sequence.pages.Count == 0)
            {
                continue;
            }

            if (matchedSequence == null)
            {
                matchedSequence = sequence;
            }

            if (!string.IsNullOrWhiteSpace(sequenceId) && string.Equals(sequence.sequenceId, sequenceId, StringComparison.OrdinalIgnoreCase))
            {
                matchedSequence = sequence;
                break;
            }
        }

        if (matchedSequence == null)
        {
            return pages;
        }

        for (int i = 0; i < matchedSequence.pages.Count; i++)
        {
            BoardPage page = matchedSequence.pages[i];
            if (page != null && !string.IsNullOrWhiteSpace(page.text))
            {
                pages.Add(page.text);
            }
        }

        return pages;
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

    private void AutoAssignIndicatorRenderer()
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
    private void OnValidate()
    {
        AutoAssignIndicatorRenderer();
    }
#endif
}
