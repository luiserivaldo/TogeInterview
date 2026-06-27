using UnityEngine;

public class ShopClass : MonoBehaviour
{
    [SerializeField] private SpriteRenderer indicatorRenderer;

    public ShopManager.ShopType shopType;

    public SpriteRenderer IndicatorRenderer => indicatorRenderer;

    private void Awake()
    {
        AutoAssignIndicatorRenderer();
    }

    public void TryInteract(PlayerClass player)
    {
        ShopManager.Instance.TryPurchase(player, shopType);
    }

    private void AutoAssignIndicatorRenderer()
    {
        if (indicatorRenderer != null)
        {
            return;
        }

        Transform indicator = transform.Find("SelectIcon");
        if (indicator != null)
        {
            indicatorRenderer = indicator.GetComponent<SpriteRenderer>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignIndicatorRenderer();
    }
#endif
}
