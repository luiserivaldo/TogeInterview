using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private int fountainUseCount = 0;
    public List<int> fountainCosts = new List<int> { 5, 10, 15, 20, 25, 30 };

    private void Awake()
    {
        // ensure only one GameManager exists on scene
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartCombat(PlayerClass player, MonsterClass monster)
    {
        // If player's attack is enough to kill the monster, no damage taken
        if (player.attack >= monster.currentDef)
        {
            monster.TakeDamage(player.attack);
        }
        else
        {
            // Normal trade of damage
            player.TakeDamage(monster.attack);
            monster.TakeDamage(player.attack);
            AudioManager.Instance.PlaySFX("combat");
        }
    }

    public void MonsterKilled(MonsterClass monster)
    {
        // Grant player money
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject.TryGetComponent(out PlayerClass player))
        {
            player.AddMoney(monster.money);
            AudioManager.Instance.PlaySFX("kill");
        }
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
                // Future: Handle NPC logic
                break;
        }
    }

    private void HandleFountain(InteractableObject fountain)
    {
        PlayerClass player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerClass>();
        int currentCost = (fountainUseCount < fountainCosts.Count)
            ? fountainCosts[fountainUseCount]
            : fountainCosts[fountainCosts.Count - 1]; // Use last value if over list length


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

        // Optional: bump animation or effect here
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
        Destroy(AudioManager.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
