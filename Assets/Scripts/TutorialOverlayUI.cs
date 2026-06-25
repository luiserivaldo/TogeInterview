using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlayUI : MonoBehaviour
{
    private GameObject overlayRoot;
    private RectTransform overlayRect;
    private Image dimmer;
    private Image highlight;
    private Text messageText;
    private static Sprite cachedSprite;

    private void Awake()
    {
        EnsureUi();
        Hide();
    }

    public void ShowHighlight(RectTransform target, string message)
    {
        EnsureUi();
        overlayRoot.SetActive(true);
        messageText.text = message;

        if (target == null)
        {
            highlight.gameObject.SetActive(false);
            return;
        }

        highlight.gameObject.SetActive(true);
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];
        Vector3 center = (min + max) * 0.5f;

        Vector2 localCenter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, RectTransformUtility.WorldToScreenPoint(null, center), null, out localCenter);
        RectTransform highlightRect = highlight.rectTransform;
        highlightRect.anchoredPosition = localCenter;
        highlightRect.sizeDelta = new Vector2((max.x - min.x) + 28f, (max.y - min.y) + 24f);
    }

    public void Hide()
    {
        EnsureUi();
        overlayRoot.SetActive(false);
    }

    private void EnsureUi()
    {
        if (overlayRoot != null)
        {
            return;
        }

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Runtime Tutorial Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        overlayRoot = new GameObject("Tutorial Overlay", typeof(RectTransform), typeof(CanvasGroup));
        overlayRoot.transform.SetParent(canvas.transform, false);
        overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        CanvasGroup group = overlayRoot.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject dimObject = CreateUiObject("Dimmer", overlayRoot.transform);
        RectTransform dimRect = dimObject.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        dimmer = dimObject.AddComponent<Image>();
        dimmer.sprite = GetRuntimeSprite();
        dimmer.color = new Color(0f, 0f, 0f, 0.55f);
        dimmer.raycastTarget = false;

        GameObject highlightObject = CreateUiObject("Highlight", overlayRoot.transform);
        highlight = highlightObject.AddComponent<Image>();
        highlight.sprite = GetRuntimeSprite();
        highlight.color = new Color(1f, 0.83f, 0.16f, 0.28f);
        highlight.raycastTarget = false;

        Outline outline = highlightObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.94f, 0.44f, 1f);
        outline.effectDistance = new Vector2(4f, 4f);

        GameObject messageObject = CreateUiObject("Overlay Message", overlayRoot.transform);
        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0f);
        messageRect.anchorMax = new Vector2(0.5f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.anchoredPosition = new Vector2(0f, 40f);
        messageRect.sizeDelta = new Vector2(700f, 90f);
        Image messageBg = messageObject.AddComponent<Image>();
        messageBg.sprite = GetRuntimeSprite();
        messageBg.color = new Color(0.1f, 0.14f, 0.22f, 0.92f);
        messageBg.raycastTarget = false;

        GameObject textObject = CreateUiObject("Overlay Text", messageObject.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 14f);
        textRect.offsetMax = new Vector2(-20f, -14f);
        messageText = textObject.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 22;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Sprite GetRuntimeSprite()
    {
        if (cachedSprite == null)
        {
            cachedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return cachedSprite;
    }
}
