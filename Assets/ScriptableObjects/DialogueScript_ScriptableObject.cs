using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueScript_ScriptableObject", menuName = "TogeInterview/Dialogue Script")]
public class DialogueScript_ScriptableObject : ScriptableObject
{
    [SerializeField] private List<DialogueCommandData> commands = new();

    public IReadOnlyList<DialogueCommandData> Commands => commands;
}

public enum DialogueCommandType
{
    ShowText,
    ShowChoice,
    Label,
    JumpToLabel,
    ForkFromLastChoice,
    PlaySfx,
    ShowImage,
    ShowParticle,
}

[System.Serializable]
public class DialogueCommandData
{
    public DialogueCommandType commandType;
    public string label;

    [TextArea(2, 5)]
    public string text;

    public bool animateText = true;
    public Sprite image;
    public string sfxKey;
    public string targetLabel;
    public GameObject particlePrefab;
    public bool waitForParticleToFinish;
    public List<DialogueChoiceOption> options = new();
}

[System.Serializable]
public class DialogueChoiceOption
{
    public string optionKey;
    public string optionLabel;
    public string targetLabel;
}
