using UnityEngine;
using UnityEngine.Serialization;

public class PlayerClass : MonoBehaviour
{
    public int attack = 1;
    [FormerlySerializedAs("defense")]
    public int maxHp = 5;
    public int currentHp;
    public int maxDef;
    public int currentDef;
    public int money = 0;

    public bool IsDefeated => currentHp <= 0;

    private void Awake()
    {
        if (maxHp <= 0)
        {
            maxHp = 1;
        }

        if (maxDef <= 0)
        {
            maxDef = maxHp;
        }

        currentHp = currentHp <= 0 ? maxHp : Mathf.Clamp(currentHp, 0, maxHp);
        currentDef = currentDef <= 0 ? maxDef : Mathf.Clamp(currentDef, 0, maxDef);
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

    public void AddMoney(int amount)
    {
        money += amount;
    }
}
