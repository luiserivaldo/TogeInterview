using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Sound Effects")]
    public AudioClip sfxCombat;
    public AudioClip sfxDamage;
    public AudioClip sfxEnemyDefeat;
    public AudioClip sfxKill;
    public AudioClip sfxUpgrade;
    public AudioClip fountainBuy;
    public AudioClip gameOver;

    private Dictionary<string, AudioClip> sfxClips;

    void Awake()
    {
        EnsureAudioSources();

        // Build SFX dictionary
        RebuildSfxDictionary();
    }

    void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmSource && backgroundMusic)
        {
            if (bgmSource.isPlaying && bgmSource.clip == backgroundMusic)
            {
                return;
            }

            bgmSource.clip = backgroundMusic;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.Play();
        }
    }

    public void PlaySFX(string key)
    {
        if (sfxClips == null || sfxClips.Count == 0)
        {
            RebuildSfxDictionary();
        }

        if (HasSfx(key))
        {
            sfxSource.PlayOneShot(sfxClips[key]);
        }
    }

    public bool HasSfx(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (sfxClips == null || sfxClips.Count == 0)
        {
            RebuildSfxDictionary();
        }

        return sfxClips.TryGetValue(key, out AudioClip clip) && clip != null;
    }

    private void EnsureAudioSources()
    {
        if (!sfxSource)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (!sfxSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (!bgmSource)
        {
            bgmSource = GetComponentInChildren<AudioSource>(true);
        }

        if (!bgmSource || bgmSource == sfxSource)
        {
            Transform bgmPlayer = transform.Find("BGM Player");

            if (bgmPlayer == null)
            {
                GameObject bgmChild = new GameObject("BGM Player");
                bgmChild.transform.SetParent(transform);
                bgmChild.transform.localPosition = Vector3.zero;
                bgmChild.transform.localRotation = Quaternion.identity;
                bgmChild.transform.localScale = Vector3.one;
                bgmPlayer = bgmChild.transform;
            }

            bgmSource = bgmPlayer.GetComponent<AudioSource>();

            if (!bgmSource)
            {
                bgmSource = bgmPlayer.gameObject.AddComponent<AudioSource>();
            }
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    private void RebuildSfxDictionary()
    {
        sfxClips = new Dictionary<string, AudioClip>
        {
            { "combat", sfxCombat },
            { "damage", sfxDamage },
            { "enemyDefeat", sfxEnemyDefeat },
            { "kill", sfxKill },
            { "upgrade", sfxUpgrade },
            { "fountain", fountainBuy },
            { "gameOver", gameOver }
        };
    }
}
