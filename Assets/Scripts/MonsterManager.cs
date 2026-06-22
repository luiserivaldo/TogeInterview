using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance;
    private List<MonsterClass> allMonsters = new List<MonsterClass>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterMonster(MonsterClass monster)
    {
        if (!allMonsters.Contains(monster))
            allMonsters.Add(monster);
    }

    public void RespawnAllMonsters()
    {
        foreach (var monster in allMonsters)
        {
            monster.Respawn();
        }
    }
}
