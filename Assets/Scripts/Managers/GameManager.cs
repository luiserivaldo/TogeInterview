using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool IsGameplayLocked { get; private set; }

    private AudioManager audioManager;
    private int fountainUseCount = 0;
    public List<int> fountainCosts = new List<int> { 5, 10, 15, 20, 25, 30 };

    private void Awake()
    {
        audioManager = Object.FindFirstObjectByType<AudioManager>();
    }

    public void StartCombat(PlayerClass player, MonsterClass monster)
    {
        if (player == null || monster == null)
        {
            return;
        }

        if (player.attack >= monster.currentHp)
        {
            monster.TakeDamage(player.attack);
        }
        else
        {
            player.TakeDamage(monster.attack);
            monster.TakeDamage(player.attack);
            PlaySFX("combat");
        }

        if (monster.IsDefeated)
        {
            MonsterKilled(monster);
            monster.gameObject.SetActive(false);
        }

        if (player.IsDefeated)
        {
            GameOver();
        }
    }

    public void MonsterKilled(MonsterClass monster)
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject.TryGetComponent(out PlayerClass player))
        {
            player.AddMoney(monster.money);
            PlaySFX("kill");
        }
    }

    public void InteractWithObject(InteractableObject obj)
    {
        if (IsGameplayLocked || obj == null)
        {
            return;
        }

        switch (obj.objectType)
        {
            case InteractableObject.InteractableType.Fountain:
                HandleFountain(obj);
                break;

            case InteractableObject.InteractableType.Sign:
                HandleSign(obj);
                break;

            case InteractableObject.InteractableType.NPC:
                break;
        }
    }

    public int GetCurrentFountainCost()
    {
        return (fountainUseCount < fountainCosts.Count)
            ? fountainCosts[fountainUseCount]
            : fountainCosts[fountainCosts.Count - 1];
    }

    private void HandleFountain(InteractableObject fountain)
    {
        PlayerClass player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerClass>();
        int currentCost = GetCurrentFountainCost();

        if (player.money >= currentCost)
        {
            player.money -= currentCost;
            MonsterManager.Instance.RespawnAllMonsters();
            fountainUseCount++;
            PlaySFX("fountain");
            Debug.Log($"Fountain used. Cost: {currentCost}. Next use count: {fountainUseCount}");
        }
        else
        {
            Debug.Log("Not enough gold to use the fountain.");
        }

        player.GetComponent<PlayerController>().BumpBack();
    }

    private void HandleSign(InteractableObject sign)
    {
        if (sign != null && sign.signType == InteractableObject.SignType.TutorialSign)
        {
            DialogueManager.Instance.StartTutorialPrompt(sign);
            return;
        }

        Debug.Log("This is a Sign.");
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        PlaySFX("gameOver");
        UIManager.Instance.ShowGameOver();
        DisablePlayerInput();
    }

    private void DisablePlayerInput()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject.TryGetComponent(out PlayerController controller))
        {
            controller.enabled = false;
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void SetGameplayLocked(bool locked)
    {
        IsGameplayLocked = locked;
    }

    private void PlaySFX(string key)
    {
        AudioManager manager = GetAudioManager();

        if (manager != null)
        {
            manager.PlaySFX(key);
        }
    }

    private AudioManager GetAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = Object.FindFirstObjectByType<AudioManager>();
        }

        return audioManager;
    }
}
