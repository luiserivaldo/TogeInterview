using System;
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
        audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
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
                PromptFountainConfirmation(obj, player);
                break;

            case InteractableObject.InteractableType.ShopAtk:
                PromptShopConfirmation(obj, player, ShopManager.ShopType.ATK);
                break;

            case InteractableObject.InteractableType.ShopDef:
                PromptShopConfirmation(obj, player, ShopManager.ShopType.DEF);
                break;

            case InteractableObject.InteractableType.Sign:
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


    private void PromptFountainConfirmation(InteractableObject obj, PlayerClass player)
    {
        ShowInteractionConfirmation(obj, "Use fountain?", () =>
        {
            bool success = HandleFountain(player);
            if (!success)
            {
                ShowInteractionFeedback($"You need {GetCurrentFountainCost()} GP to use the fountain.");
            }
        });
    }

    private void PromptShopConfirmation(InteractableObject obj, PlayerClass player, ShopManager.ShopType type)
    {
        int currentCost = ShopManager.Instance != null ? ShopManager.Instance.GetCurrentCost(type) : 0;
        string fallbackPrompt = type == ShopManager.ShopType.ATK
            ? $"Buy +1 ATK for {currentCost} GP?"
            : $"Buy +5 DEF for {currentCost} GP?";

        ShowInteractionConfirmation(obj, fallbackPrompt, () =>
        {
            bool success = HandleShop(player, type);
            if (!success)
            {
                ShowInteractionFeedback($"You need {currentCost} GP for that upgrade.");
            }
        });
    }

    private void ShowInteractionConfirmation(InteractableObject obj, string fallbackPrompt, Action confirmedAction)
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        string prompt = BuildConfirmationPrompt(obj, fallbackPrompt);
        SetGameplayLocked(true);

        if (UIManager.Instance.IsBoardUiAvailable)
        {
            UIManager.Instance.ShowBoardChoice(
                prompt,
                () => ResolveInteractionConfirmation(confirmedAction),
                CancelInteractionConfirmation,
                "Yes",
                "No");
            return;
        }

        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();
        UIManager.Instance.ShowDialogueUI();
        UIManager.Instance.SetDialogueImage(null);
        UIManager.Instance.SetDialogueText(prompt);
        UIManager.Instance.SetDialogueChoicesVisible(true, "Yes", "No");
        UIManager.Instance.BindDialogueChoiceHandlers(
            () => ResolveInteractionConfirmation(confirmedAction),
            CancelInteractionConfirmation);
        UIManager.Instance.SelectDialogueDefaultAction();
    }

    private static string BuildConfirmationPrompt(InteractableObject obj, string fallbackPrompt)
    {
        string prompt = obj != null ? obj.GetInteractionMessage() : string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return fallbackPrompt;
        }

        return prompt + "\n\nConfirm?";
    }

    private void ResolveInteractionConfirmation(Action confirmedAction)
    {
        CancelInteractionConfirmation();
        confirmedAction?.Invoke();
        UIManager.Instance?.UpdateUI();
    }

    private void CancelInteractionConfirmation()
    {
        UIManager.Instance?.SetDialogueChoicesVisible(false);
        UIManager.Instance?.HideDialogueUI();
        UIManager.Instance?.HideBoardUI();
        SetGameplayLocked(false);
    }

    private void ShowInteractionFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || DialogueManager.Instance == null)
        {
            return;
        }

        DialogueManager.Instance.StartMessage(message, animateText: false);
    }

    private bool HandleFountain(PlayerClass player)
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.GetComponent<PlayerClass>() : null;
        }

        if (player == null)
        {
            return false;
        }

        int currentCost = GetCurrentFountainCost();

        if (player.money >= currentCost)
        {
            player.money -= currentCost;
            MonsterManager.Instance.RespawnAllMonsters();
            fountainUseCount++;
            PlaySFX("fountain");
            Debug.Log($"Fountain used. Cost: {currentCost}. Next use count: {fountainUseCount}");
            return true;
        }

        Debug.Log("Not enough gold to use the fountain.");
        return false;
    }

    private bool HandleShop(PlayerClass player, ShopManager.ShopType type)
    {
        if (player == null || ShopManager.Instance == null)
        {
            return false;
        }

        return ShopManager.Instance.TryPurchase(player, type);
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
            audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        }

        return audioManager;
    }
}
