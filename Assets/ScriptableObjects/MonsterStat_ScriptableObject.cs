using UnityEngine;

[CreateAssetMenu(fileName = "MonsterStats", menuName = "Scriptable Objects/MonsterStats")]
public class MonsterStat_ScriptableObject : ScriptableObject
{
    public string displayName;
    public GameObject visualPrefab;
    public int baseAttack;
    public int baseDefense;
    public int moneyValue;
}
