using UnityEngine;

public class MonsterClass : MonoBehaviour
{
    public int attack = 1;
    public int maxDef = 1;
    public int currentDef = 1;
    public int money = 1;

    private void Start()
    {
        currentDef = maxDef;
        MonsterManager.Instance.RegisterMonster(this);
    }

    public void TakeDamage(int amount)
    {
        currentDef -= amount;
        if (currentDef <= 0)
        {
            GameManager.Instance.MonsterKilled(this);
            gameObject.SetActive(false); // Disable prefab on death; previously destroy
        }
    }

    public void Respawn()
    {
        currentDef = maxDef;
        gameObject.SetActive(true);
    }
}