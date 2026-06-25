using System.Collections;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    private static CutsceneManager instance;

    public static CutsceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<CutsceneManager>();

                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(CutsceneManager));
                    instance = managerObject.AddComponent<CutsceneManager>();
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
        }
    }

    public IEnumerator PlayScript(DialogueScript_ScriptableObject script, Sprite defaultImage = null)
    {
        DialogueManager.Instance.StartScript(script, defaultImage);
        while (DialogueManager.Instance.IsDialogueRunning)
        {
            yield return null;
        }
    }
}
