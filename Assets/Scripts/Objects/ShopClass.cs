using UnityEngine;

public class ShopClass : InteractableObject
{
    [SerializeField] private ShopManager.ShopType shopType;

    public ShopManager.ShopType ShopType => shopType;

    protected override string DefaultInteractionMessage => shopType == ShopManager.ShopType.ATK
        ? $"Sharpen your weapon? +1 ATK for {GetShopCost(ShopManager.ShopType.ATK)} GP."
        : $"Reinforce your armor? +5 DEF for {GetShopCost(ShopManager.ShopType.DEF)} GP.";

#if UNITY_EDITOR
    private void OnValidate()
    {
        objectType = shopType == ShopManager.ShopType.ATK
            ? InteractableType.ShopAtk
            : InteractableType.ShopDef;
    }
#endif
}
