using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private int fountainUseCount = 0;
    public List<int> fountainCosts = new List<int> { 5, 10, 15, 20, 25, 30 };
    private CombatController combatController;
    private TutorialDirector tutorialDirector;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        combatController = GetComponent<CombatController>();
        if (combatController == null)
            combatController = gameObject.AddComponent<CombatController>();

        tutorialDirector = GetComponent<TutorialDirector>();
        if (tutorialDirector == null)
            tutorialDirector = gameObject.AddComponent<TutorialDirector>();
    }

    public void StartCombat(PlayerClass player, MonsterClass monster)
    {
        if (combatController == null || player == null || monster == null)
        {
            return;
        }

        if (combatController.IsCombatActive)
        {
            return;
        }

        combatController.BeginCombat(player, monster);
    }

    public void ResolveCombatRound(PlayerClass player, MonsterClass monster)
    {
        if (player == null || monster == null)
        {
            return;
        }

        if (player.attack >= monster.currentDef)
        {
            monster.TakeDamage(player.attack);
        }
        else
        {
            player.TakeDamage(monster.attack);
            monster.TakeDamage(player.attack);
            AudioManager.Instance.PlaySFX("combat");
        }
    }

    public void MonsterKilled(MonsterClass monster)
    {
        PlayerClass player = Object.FindFirstObjectByType<PlayerClass>();
        if (player != null)
        {
            player.AddMoney(monster.money);
            AudioManager.Instance.PlaySFX("kill");
        }

        tutorialDirector?.HandleMonsterKilled(monster);
    }

    public void InteractWithObject(InteractableObject obj)
    {
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

    private void HandleFountain(InteractableObject fountain)
    {
        PlayerClass player = Object.FindFirstObjectByType<PlayerClass>();
        if (player == null)
        {
            return;
        }

        int currentCost = (fountainUseCount < fountainCosts.Count)
            ? fountainCosts[fountainUseCount]
            : fountainCosts[fountainCosts.Count - 1];

        if (player.money >= currentCost)
        {
            player.money -= currentCost;
            MonsterManager.Instance.RespawnAllMonsters();
            fountainUseCount++;
            AudioManager.Instance.PlaySFX("fountain");
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
        Debug.Log("This is a Sign.");
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        AudioManager.Instance.PlaySFX("gameOver");
        UIManager.Instance.ShowGameOver();
        combatController?.CancelCombat();
        DisablePlayerInput();
    }

    private void DisablePlayerInput()
    {
        PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    public CombatController CombatController => combatController;
    public bool IsCombatActive => combatController != null && combatController.IsCombatActive;

    public void ReloadScene()
    {
        Destroy(AudioManager.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
