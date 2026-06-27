using UnityEngine;

public class ShopClass : MonoBehaviour
{
    [SerializeField] private SpriteRenderer indicatorRenderer;
    [SerializeField] private bool disableDuringOpeningTutorial;

    public ShopManager.ShopType shopType;

    public SpriteRenderer IndicatorRenderer => indicatorRenderer;

    private void Awake()
    {
        AutoAssignIndicatorRenderer();
    }

    // Disable interaction during tutorial sequence
    public bool CanInteract =>
    !disableDuringOpeningTutorial ||
    !TutorialManager.IsOpeningTutorialActive;

    public void TryInteract(PlayerClass player)
    {
        if (!CanInteract || player == null || ShopManager.Instance == null)
        {
            return;
        }

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
