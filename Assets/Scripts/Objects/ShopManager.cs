using UnityEngine;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    // Ascending costs for each purchase
    public int[] upgradeCosts = { 5, 10, 20, 40, 80, 150 };
    public int atkUpgradeIndex = 0;
    public int defUpgradeIndex = 0;
    public int atkBonusPerUpgrade = 1;
    public int defBonusPerUpgrade = 5;
    public enum ShopType { ATK, DEF }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool TryPurchase(PlayerClass player, ShopType type)
    {
        int cost = GetCurrentCost(type);
        if (player.money < cost)
        {
            Debug.Log($"Not enough gold! Needed: {cost}, Have: {player.money}");
            return false;
        }

        player.money -= cost;

        if (type == ShopType.ATK)
        {
            player.attack += atkBonusPerUpgrade;
            atkUpgradeIndex++;
            Debug.Log("Purchased ATK upgrade!");
            PlayUpgradeSFX();
        }
        else
        {
            player.maxDef += defBonusPerUpgrade;
            player.currentDef += defBonusPerUpgrade;
            defUpgradeIndex++;
            Debug.Log("Purchased DEF upgrade!");
            PlayUpgradeSFX();
        }

        return true;
    }

    public int GetCurrentCost(ShopType type)
    {
        int index = (type == ShopType.ATK) ? atkUpgradeIndex : defUpgradeIndex;
        index = Mathf.Clamp(index, 0, upgradeCosts.Length - 1);
        return upgradeCosts[index];
    }

    private void PlayUpgradeSFX()
    {
        AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>();

        if (audioManager != null)
        {
            audioManager.PlaySFX("upgrade");
        }
    }
}
