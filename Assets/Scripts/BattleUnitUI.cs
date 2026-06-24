using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitUI : MonoBehaviour
{
    [SerializeField] private Image turnIndicator;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text defText;

    public void AutoBind()
    {
        nameText ??= FindText(transform, "PlayerName", "EnemyName");
        hpSlider ??= GetComponentInChildren<Slider>(true);
        hpText ??= FindText(hpSlider != null ? hpSlider.transform : transform, "HPText");
        atkText ??= FindText(transform, "attack txt");
        defText ??= FindText(transform, "def text");
        turnIndicator ??= FindImage(transform, "TurnIndicator");

        EnsureTurnIndicator();
        EnsureHpText();
        EnsureAtkText();
        EnsureDefText();
    }

    public void SetUnit(string displayName, int currentHp, int maxHp, int attack, int defense, Sprite unitSprite, bool isTurnOwner)
    {
        AutoBind();

        if (nameText != null)
        {
            nameText.text = displayName;
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = Mathf.Max(1, maxHp);
            hpSlider.value = Mathf.Clamp(currentHp, 0, hpSlider.maxValue);
        }

        if (hpText != null)
        {
            hpText.text = currentHp.ToString();
        }

        if (atkText != null)
        {
            atkText.text = attack.ToString();
        }

        if (defText != null)
        {
            defText.text = defense.ToString();
        }

        if (turnIndicator != null)
        {
            turnIndicator.sprite = unitSprite;
            turnIndicator.preserveAspect = true;
            turnIndicator.enabled = unitSprite != null;
            turnIndicator.color = isTurnOwner ? Color.white : new Color(1f, 1f, 1f, 0.75f);
        }
    }

    private void EnsureTurnIndicator()
    {
        if (turnIndicator != null)
        {
            return;
        }

        GameObject indicatorObject = new GameObject("TurnIndicator", typeof(RectTransform), typeof(Image));
        indicatorObject.transform.SetParent(transform, false);

        RectTransform rectTransform = indicatorObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(-32f, 20f);
        rectTransform.sizeDelta = new Vector2(20f, 20f);

        turnIndicator = indicatorObject.GetComponent<Image>();
        turnIndicator.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        turnIndicator.enabled = false;
    }

    private void EnsureHpText()
    {
        if (hpText != null)
        {
            return;
        }

        Transform parent = hpSlider != null ? hpSlider.transform : transform;
        hpText = CreateText("HPText", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 40f), 20f);
        hpText.alignment = TextAlignmentOptions.Center;
    }

    private void EnsureAtkText()
    {
        if (atkText != null)
        {
            return;
        }

        atkText = CreateText("ATKText", transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(220f, 30f), 22f);
        atkText.alignment = TextAlignmentOptions.Center;
    }

    private void EnsureDefText()
    {
        if (defText != null)
        {
            return;
        }

        defText = CreateText("DEFText", transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(220f, 30f), 22f);
        defText.alignment = TextAlignmentOptions.Center;
    }

    private TMP_Text FindText(Transform root, params string[] names)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            foreach (string name in names)
            {
                if (text.gameObject.name == name)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private Image FindImage(Transform root, params string[] names)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            foreach (string name in names)
            {
                if (image.gameObject.name == name)
                {
                    return image;
                }
            }
        }

        return null;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }
}
