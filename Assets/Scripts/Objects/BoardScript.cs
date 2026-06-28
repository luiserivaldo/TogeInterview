using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardScript : InteractableObject
{
    private const int MaxSections = 2;

    [Serializable]
    public class BoardPage
    {
        [TextArea(3, 8)]
        public string content = string.Empty;
    }

    [Serializable]
    public class BoardSection
    {
        public string title = "Info";
        public List<BoardPage> pages = new();
    }

    [Header("Board Content")]
    [SerializeField] private string sectionPrompt = "Would you like some information?";
    [SerializeField] private string previousLabel = "Previous";
    [SerializeField] private string nextLabel = "Next";
    [SerializeField] private string closeLabel = "Close";
    [SerializeField] private List<BoardSection> sections = new();

    public override void TryInteract(PlayerClass player)
    {
        if (!CanInteract)
        {
            return;
        }

        if (UIManager.Instance == null || !UIManager.Instance.IsBoardUiAvailable)
        {
            Debug.LogWarning($"BoardUI is unavailable for board '{name}'.");
            return;
        }

        List<BoardSection> readableSections = GetReadableSections();
        if (readableSections.Count == 0)
        {
            return;
        }

        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();
        GameManager.SetGameplayLocked(true);

        if (readableSections.Count == 1)
        {
            ShowSection(readableSections[0]);
            return;
        }

        UIManager.Instance.ShowBoardChoice(
            GetSectionPrompt(),
            () => ShowSection(readableSections[0]),
            () => ShowSection(readableSections[1]),
            GetSectionTitle(readableSections[0], "Left"),
            GetSectionTitle(readableSections[1], "Right"));
    }

    public override string GetInteractionMessage()
    {
        return GetSectionPrompt();
    }

    private void ShowSection(BoardSection section)
    {
        List<string> pages = GetPages(section);
        if (pages.Count == 0)
        {
            EndBoardInteraction();
            return;
        }

        UIManager.Instance.HideBoardUI();
        UIManager.Instance.ShowBoardPages(pages, EndBoardInteraction, previousLabel, nextLabel, closeLabel);
    }

    private void EndBoardInteraction()
    {
        GameManager.SetGameplayLocked(false);
    }

    private string GetSectionPrompt()
    {
        return string.IsNullOrWhiteSpace(sectionPrompt)
            ? "Would you like some information?"
            : sectionPrompt;
    }

    private List<BoardSection> GetReadableSections()
    {
        List<BoardSection> readableSections = new();
        if (sections == null)
        {
            return readableSections;
        }

        for (int i = 0; i < sections.Count && readableSections.Count < MaxSections; i++)
        {
            BoardSection section = sections[i];
            if (!HasReadablePages(section))
            {
                continue;
            }

            readableSections.Add(section);
        }

        return readableSections;
    }

    private static bool HasReadablePages(BoardSection section)
    {
        if (section == null || section.pages == null)
        {
            return false;
        }

        for (int i = 0; i < section.pages.Count; i++)
        {
            BoardPage page = section.pages[i];
            if (page != null && !string.IsNullOrWhiteSpace(page.content))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> GetPages(BoardSection section)
    {
        List<string> pages = new();
        if (section == null || section.pages == null)
        {
            return pages;
        }

        for (int i = 0; i < section.pages.Count; i++)
        {
            BoardPage page = section.pages[i];
            if (page != null && !string.IsNullOrWhiteSpace(page.content))
            {
                pages.Add(page.content);
            }
        }

        return pages;
    }

    private static string GetSectionTitle(BoardSection section, string fallback)
    {
        if (section == null || string.IsNullOrWhiteSpace(section.title))
        {
            return fallback;
        }

        return section.title.Trim();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        objectType = InteractableType.Sign;

        if (sections == null)
        {
            return;
        }

        while (sections.Count > MaxSections)
        {
            sections.RemoveAt(sections.Count - 1);
        }
    }
#endif
}
