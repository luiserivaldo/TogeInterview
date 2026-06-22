using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Sound Effects")]
    public AudioClip sfxCombat;
    public AudioClip sfxKill;
    public AudioClip sfxUpgrade;
    public AudioClip fountainBuy;
    public AudioClip gameOver;

    private Dictionary<string, AudioClip> sfxClips;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Build SFX dictionary
        sfxClips = new Dictionary<string, AudioClip>
        {
            { "combat", sfxCombat },
            { "kill", sfxKill },
            { "upgrade", sfxUpgrade },
            { "fountain", fountainBuy },
            { "gameOver", gameOver }
        };
    }

    void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmSource && backgroundMusic)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void PlaySFX(string key)
    {
        if (sfxClips.ContainsKey(key) && sfxClips[key] != null)
        {
            sfxSource.PlayOneShot(sfxClips[key]);
        }
    }
    /* public void ReloadScene()
    {
        Destroy(AudioManager.Instance.gameObject); // Destroy before reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    } */
}
