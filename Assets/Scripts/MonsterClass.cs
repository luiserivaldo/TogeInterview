using UnityEngine;
using UnityEngine.Serialization;

public class MonsterClass : MonoBehaviour
{
    public string displayName;
    public MonsterStat_ScriptableObject statsData;
    public int attack = 1;
    [FormerlySerializedAs("defense")]
    [FormerlySerializedAs("maxDef")]
    public int maxHp = 1;
    public int currentHp;
    public int maxDef;
    public int currentDef;
    public int money = 1;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public bool IsDefeated => currentHp <= 0;

    private void Start()
    {
        InitializeFromStats();

        if (maxHp <= 0)
        {
            maxHp = maxDef > 0 ? maxDef : 1;
        }

        if (maxDef <= 0)
        {
            maxDef = maxHp;
        }

        currentHp = maxHp;
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
        maxHp = statsData.baseDefense;

        if (maxDef <= 0)
        {
            maxDef = statsData.baseDefense;
        }

        money = statsData.moneyValue;
    }

    public int ApplyDamage(int amount)
    {
        int damage = Mathf.Max(0, Mathf.Min(amount, currentHp));
        currentHp = Mathf.Max(0, currentHp - damage);
        return damage;
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount);
    }

    public void Respawn()
    {
        currentHp = maxHp;
        currentDef = maxDef;
        gameObject.SetActive(true);
    }
}
