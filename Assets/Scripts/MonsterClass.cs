using UnityEngine;
using UnityEngine.Serialization;

public class MonsterClass : MonoBehaviour
{
    public string displayName;
    public MonsterStat_ScriptableObject statsData;
    public int attack = 1;
    [FormerlySerializedAs("defense")]
    public int maxDef = 1;
    public int currentDef = 1;
    public int money = 1;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

    private void Start()
    {
        InitializeFromStats();
        currentDef = maxDef;
        MonsterManager.Instance.RegisterMonster(this);
    }

    private void InitializeFromStats()
    {
        if (statsData == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(statsData.displayName))
        {
            displayName = statsData.displayName;
        }

        attack = statsData.baseAttack;
        maxDef = statsData.baseDefense;
        money = statsData.moneyValue;
    }

    public void TakeDamage(int amount)
    {
        currentDef -= amount;
        if (currentDef <= 0)
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();

            if (gameManager != null)
            {
                gameManager.MonsterKilled(this);
            }

            gameObject.SetActive(false);
        }
    }

    public void Respawn()
    {
        currentDef = maxDef;
        gameObject.SetActive(true);
    }
}
