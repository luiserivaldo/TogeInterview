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

    public void InteractWithObject(InteractableObject obj, PlayerClass player = null)
    {
        if (IsGameplayLocked || obj == null)
        {
            return;
        }

        switch (obj.objectType)
        {
            case InteractableObject.InteractableType.Fountain:
                HandleFountain(player);
                break;

            case InteractableObject.InteractableType.ShopAtk:
                HandleShop(player, ShopManager.ShopType.ATK);
                break;

            case InteractableObject.InteractableType.ShopDef:
                HandleShop(player, ShopManager.ShopType.DEF);
                break;

            case InteractableObject.InteractableType.Sign:
            case InteractableObject.InteractableType.NPC:
                HandleSign(obj);
                break;
        }
    }

    public int GetCurrentFountainCost()
    {
        return (fountainUseCount < fountainCosts.Count)
            ? fountainCosts[fountainUseCount]
            : fountainCosts[fountainCosts.Count - 1];
    }

    private void HandleFountain(PlayerClass player)
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.GetComponent<PlayerClass>() : null;
        }

        if (player == null)
        {
            return;
        }

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
    }

    private void HandleShop(PlayerClass player, ShopManager.ShopType type)
    {
        if (player == null || ShopManager.Instance == null)
        {
            return;
        }

        ShopManager.Instance.TryPurchase(player, type);
    }

    private void HandleSign(InteractableObject sign)
    {
        Debug.Log($"Interacted with {sign.name}.");
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
