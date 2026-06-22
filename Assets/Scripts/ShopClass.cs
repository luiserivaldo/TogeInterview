using UnityEngine;

public class ShopClass : MonoBehaviour
{
    public ShopManager.ShopType shopType;

    public void TryInteract(PlayerClass player)
    {
        ShopManager.Instance.TryPurchase(player, shopType);
    }
}
