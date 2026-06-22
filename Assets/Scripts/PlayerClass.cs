using UnityEngine;

public class PlayerClass : MonoBehaviour
{
    public int attack = 1;
    public int defense = 5;
    public int money = 0;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out MonsterClass monster))
        {
            GameManager.Instance.StartCombat(this, monster);
        }
    }

    public void TakeDamage(int amount)
    {
        defense -= amount;
        if (defense <= 0)
        {
            GameManager.Instance.GameOver();
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }
}
