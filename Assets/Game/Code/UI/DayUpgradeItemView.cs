using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DayUpgradeItemData
{
    public string UpgradeId;
    public string NameText;
    public string PriceText;
    public string EffectText;
    public bool CanBuy;
}

[DisallowMultipleComponent]
public class DayUpgradeItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Button buyButton;

    public event Action<string> BuyRequested;

    private bool isInitialized;
    private string currentUpgradeId = string.Empty;

    private RectTransform rootRect;
    private RectTransform nameRect;
    private RectTransform effectRect;
    private RectTransform priceRect;
    private RectTransform buyButtonRect;

    private Image backgroundImage;
    private Image buyButtonImage;
    private TMP_Text buyButtonText;

    private bool useCompactLayout;
    private float layoutWidth = -1f;
    private float layoutHeight = -1f;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyPressed);
        }

        isInitialized = false;
    }

    public void Bind(DayUpgradeItemData itemData)
    {
        EnsureInitialized();

        currentUpgradeId = itemData?.UpgradeId ?? string.Empty;

        if (nameText != null)
        {
            nameText.text = itemData?.NameText ?? string.Empty;
        }

        if (effectText != null)
        {
            effectText.text = itemData?.EffectText ?? string.Empty;
        }

        if (priceText != null)
        {
            priceText.text = itemData?.PriceText ?? string.Empty;
        }

        var canBuy = itemData != null
                     && itemData.CanBuy
                     && !string.IsNullOrWhiteSpace(itemData.UpgradeId);

        if (buyButton != null)
        {
            buyButton.interactable = canBuy;
        }

        ApplyStateStyle(canBuy);
    }

    public void ApplyLayout(bool compactLayout, float itemWidth, float itemHeight)
    {
        useCompactLayout = compactLayout;
        layoutWidth = itemWidth;
        layoutHeight = itemHeight;

        EnsureInitialized();
        ApplyRuntimeLayout();
    }

    private void EnsureInitialized()
    {
        ResolveReferences();
        EnsureRuntimeStructure();
        ApplyRuntimeLayout();

        if (isInitialized)
        {
            return;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyPressed);
            buyButton.onClick.AddListener(HandleBuyPressed);
        }

        isInitialized = true;
    }

    private void ResolveReferences()
    {
        rootRect ??= transform as RectTransform;
        backgroundImage ??= GetComponent<Image>();
    }

    private void EnsureRuntimeStructure()
    {
        if (rootRect == null)
        {
            return;
        }

        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }

        backgroundImage.type = Image.Type.Sliced;

        if (nameText == null)
        {
            nameText = CreateText("NameText", transform, TextAlignmentOptions.TopLeft);
        }

        if (effectText == null)
        {
            effectText = CreateText("EffectText", transform, TextAlignmentOptions.TopLeft);
        }

        if (priceText == null)
        {
            priceText = CreateText("PriceText", transform, TextAlignmentOptions.Center);
        }

        if (buyButton == null)
        {
            buyButton = CreateButton("BuyButton", transform, "Buy");
        }

        nameRect ??= nameText.rectTransform;
        effectRect ??= effectText.rectTransform;
        priceRect ??= priceText.rectTransform;
        buyButtonRect ??= buyButton.transform as RectTransform;
        buyButtonImage ??= buyButton.GetComponent<Image>();
        buyButtonText ??= buyButton.GetComponentInChildren<TMP_Text>(true);

        if (buyButtonImage != null)
        {
            buyButtonImage.type = Image.Type.Sliced;
        }
    }

    private void ApplyRuntimeLayout()
    {
        if (rootRect == null)
        {
            return;
        }

        var width = layoutWidth > 0f ? layoutWidth : 100f;
        var height = layoutHeight > 0f ? layoutHeight : 76f;

        rootRect.sizeDelta = new Vector2(width, height);

        var padding = useCompactLayout ? 3f : 8f;
        var buttonHeight = useCompactLayout ? 14f : 28f;
        var priceHeight = useCompactLayout ? 10f : 20f;
        var titleHeight = useCompactLayout ? 12f : 24f;

        if (nameRect != null)
        {
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(padding, -(padding + titleHeight));
            nameRect.offsetMax = new Vector2(-padding, -padding);
        }

        if (effectRect != null)
        {
            effectRect.anchorMin = new Vector2(0f, 0f);
            effectRect.anchorMax = new Vector2(1f, 1f);
            effectRect.offsetMin = new Vector2(padding, padding + buttonHeight + priceHeight + 2f);
            effectRect.offsetMax = new Vector2(-padding, -(padding + titleHeight + 2f));
        }

        if (priceRect != null)
        {
            priceRect.anchorMin = new Vector2(0f, 0f);
            priceRect.anchorMax = new Vector2(1f, 0f);
            priceRect.pivot = new Vector2(0.5f, 0f);
            priceRect.offsetMin = new Vector2(padding, padding + buttonHeight + 1f);
            priceRect.offsetMax = new Vector2(-padding, padding + buttonHeight + 1f + priceHeight);
        }

        if (buyButtonRect != null)
        {
            buyButtonRect.anchorMin = new Vector2(0f, 0f);
            buyButtonRect.anchorMax = new Vector2(1f, 0f);
            buyButtonRect.pivot = new Vector2(0.5f, 0f);
            buyButtonRect.offsetMin = new Vector2(padding, padding);
            buyButtonRect.offsetMax = new Vector2(-padding, padding + buttonHeight);
        }

        ConfigureTextStyles();
    }

    private void ConfigureTextStyles()
    {
        if (nameText != null)
        {
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = useCompactLayout ? 4f : 8f;
            nameText.fontSizeMax = useCompactLayout ? 7f : 14f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = new Color(0.96f, 0.94f, 0.90f, 1f);
            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.lineSpacing = -15f;
            nameText.raycastTarget = false;
        }

        if (effectText != null)
        {
            effectText.enableAutoSizing = true;
            effectText.fontSizeMin = useCompactLayout ? 3f : 7f;
            effectText.fontSizeMax = useCompactLayout ? 6f : 12f;
            effectText.color = new Color(0.78f, 0.81f, 0.86f, 1f);
            effectText.textWrappingMode = TextWrappingModes.Normal;
            effectText.overflowMode = TextOverflowModes.Ellipsis;
            effectText.alignment = TextAlignmentOptions.TopLeft;
            effectText.lineSpacing = -20f;
            effectText.raycastTarget = false;
        }

        if (priceText != null)
        {
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = useCompactLayout ? 4f : 7f;
            priceText.fontSizeMax = useCompactLayout ? 6f : 11f;
            priceText.fontStyle = FontStyles.Bold;
            priceText.alignment = TextAlignmentOptions.Center;
            priceText.lineSpacing = -15f;
            priceText.raycastTarget = false;
        }

        if (buyButtonText != null)
        {
            buyButtonText.enableAutoSizing = true;
            buyButtonText.fontSizeMin = useCompactLayout ? 4f : 7f;
            buyButtonText.fontSizeMax = useCompactLayout ? 6f : 11f;
            buyButtonText.fontStyle = FontStyles.Bold;
            buyButtonText.alignment = TextAlignmentOptions.Center;
            buyButtonText.lineSpacing = -15f;
            buyButtonText.raycastTarget = false;
        }
    }

    private void ApplyStateStyle(bool canBuy)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = canBuy
                ? new Color(0.14f, 0.17f, 0.21f, 0.96f)
                : new Color(0.10f, 0.12f, 0.15f, 0.92f);
        }

        if (priceText != null)
        {
            priceText.color = canBuy
                ? new Color(0.98f, 0.83f, 0.45f, 1f)
                : new Color(0.66f, 0.61f, 0.52f, 1f);
        }

        if (buyButtonImage != null)
        {
            buyButtonImage.color = canBuy
                ? new Color(0.90f, 0.86f, 0.74f, 1f)
                : new Color(0.38f, 0.38f, 0.42f, 0.90f);
        }

        if (buyButtonText != null)
        {
            buyButtonText.text = canBuy ? "Buy" : "Locked";
            buyButtonText.color = canBuy
                ? new Color(0.12f, 0.12f, 0.14f, 1f)
                : new Color(0.20f, 0.20f, 0.22f, 0.85f);
        }
    }

    private void HandleBuyPressed()
    {
        if (string.IsNullOrWhiteSpace(currentUpgradeId))
        {
            return;
        }

        BuyRequested?.Invoke(currentUpgradeId);
    }

    private static TMP_Text CreateText(string objectName, Transform parent, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.raycastTarget = false;

        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button));
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);

        var image = buttonObject.GetComponent<Image>();
        image.type = Image.Type.Sliced;

        var button = buttonObject.GetComponent<Button>();

        var textObject =
            new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }
}