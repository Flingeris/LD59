using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private string currentUpgradeId;
    private RectTransform rootRect;
    private RectTransform nameRect;
    private RectTransform priceRect;
    private RectTransform effectRect;
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
        if (!isInitialized || buyButton == null)
        {
            return;
        }

        buyButton.onClick.RemoveListener(HandleBuyPressed);
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

        if (priceText != null)
        {
            priceText.text = itemData?.PriceText ?? string.Empty;
        }

        if (effectText != null)
        {
            effectText.text = itemData?.EffectText ?? string.Empty;
        }

        if (buyButton != null)
        {
            var canBuy = itemData != null
                && itemData.CanBuy
                && !string.IsNullOrWhiteSpace(itemData.UpgradeId);
            buyButton.interactable = canBuy;
            StyleForState(canBuy);
        }
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
        ResolveRuntimeReferences();
        ApplyRuntimeLayout();

        if (isInitialized || buyButton == null)
        {
            return;
        }

        buyButton.onClick.AddListener(HandleBuyPressed);
        isInitialized = true;
    }

    private void HandleBuyPressed()
    {
        if (string.IsNullOrWhiteSpace(currentUpgradeId))
        {
            return;
        }

        BuyRequested?.Invoke(currentUpgradeId);
    }

    private void ResolveRuntimeReferences()
    {
        rootRect ??= transform as RectTransform;
        nameRect ??= nameText != null ? nameText.rectTransform : null;
        priceRect ??= priceText != null ? priceText.rectTransform : null;
        effectRect ??= effectText != null ? effectText.rectTransform : null;
        buyButtonRect ??= buyButton != null ? buyButton.transform as RectTransform : null;
        backgroundImage ??= GetComponent<Image>();
        buyButtonImage ??= buyButton != null ? buyButton.targetGraphic as Image : null;
        buyButtonText ??= buyButton != null ? buyButton.GetComponentInChildren<TMP_Text>(true) : null;

        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }

        if (backgroundImage.sprite == null && buyButtonImage != null)
        {
            backgroundImage.sprite = buyButtonImage.sprite;
        }

        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.raycastTarget = false;
    }

    private void ApplyRuntimeLayout()
    {
        var resolvedWidth = layoutWidth > 0f
            ? layoutWidth
            : rootRect != null && rootRect.rect.width > 0f
                ? rootRect.rect.width
                : 120f;
        var resolvedHeight = layoutHeight > 0f ? layoutHeight : 78f;

        if (rootRect != null)
        {
            rootRect.sizeDelta = new Vector2(resolvedWidth, resolvedHeight);
        }

        var padding = useCompactLayout
            ? Mathf.Clamp(resolvedHeight * 0.18f, 3f, 5f)
            : Mathf.Clamp(resolvedHeight * 0.16f, 6f, 10f);
        var sideColumnWidth = useCompactLayout
            ? Mathf.Clamp(resolvedWidth * 0.34f, 42f, 56f)
            : Mathf.Clamp(resolvedWidth * 0.26f, 74f, 128f);
        sideColumnWidth = Mathf.Min(sideColumnWidth, Mathf.Max(40f, resolvedWidth - padding * 2f - 44f));
        var topLineHeight = Mathf.Clamp(resolvedHeight * (useCompactLayout ? 0.34f : 0.28f), 7f, 18f);
        var buttonHeight = Mathf.Clamp(resolvedHeight * (useCompactLayout ? 0.46f : 0.42f), 10f, 22f);
        var textRightInset = sideColumnWidth + padding * 2f;

        if (nameRect != null)
        {
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(padding, -(padding + topLineHeight));
            nameRect.offsetMax = new Vector2(-textRightInset, -padding);
        }

        if (effectRect != null)
        {
            effectRect.anchorMin = new Vector2(0f, 0f);
            effectRect.anchorMax = new Vector2(1f, 1f);
            effectRect.offsetMin = new Vector2(padding, padding);
            effectRect.offsetMax = new Vector2(-textRightInset, -(padding + topLineHeight + 1f));
        }

        if (priceRect != null)
        {
            priceRect.anchorMin = new Vector2(1f, 1f);
            priceRect.anchorMax = new Vector2(1f, 1f);
            priceRect.pivot = new Vector2(1f, 1f);
            priceRect.anchoredPosition = new Vector2(-padding, -padding);
            priceRect.sizeDelta = new Vector2(sideColumnWidth, topLineHeight);
        }

        if (buyButtonRect != null)
        {
            buyButtonRect.anchorMin = new Vector2(1f, 0f);
            buyButtonRect.anchorMax = new Vector2(1f, 0f);
            buyButtonRect.pivot = new Vector2(1f, 0f);
            buyButtonRect.anchoredPosition = new Vector2(-padding, padding);
            buyButtonRect.sizeDelta = new Vector2(sideColumnWidth, buttonHeight);
        }

        if (nameText != null)
        {
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = useCompactLayout ? 4f : 7f;
            nameText.fontSizeMax = useCompactLayout ? 8f : 14f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = new Color(0.96f, 0.94f, 0.9f, 1f);
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (effectText != null)
        {
            effectText.alignment = TextAlignmentOptions.TopLeft;
            effectText.enableAutoSizing = true;
            effectText.fontSizeMin = useCompactLayout ? 3.5f : 6f;
            effectText.fontSizeMax = useCompactLayout ? 6f : 10f;
            effectText.color = new Color(0.78f, 0.81f, 0.86f, 1f);
            effectText.textWrappingMode = TextWrappingModes.NoWrap;
            effectText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (priceText != null)
        {
            priceText.alignment = TextAlignmentOptions.TopRight;
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = useCompactLayout ? 3.5f : 6f;
            priceText.fontSizeMax = useCompactLayout ? 6.5f : 10f;
            priceText.fontStyle = FontStyles.Bold;
            priceText.textWrappingMode = TextWrappingModes.NoWrap;
            priceText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (buyButtonText != null)
        {
            buyButtonText.alignment = TextAlignmentOptions.Center;
            buyButtonText.enableAutoSizing = true;
            buyButtonText.fontSizeMin = useCompactLayout ? 4f : 6f;
            buyButtonText.fontSizeMax = useCompactLayout ? 6.5f : 10f;
            buyButtonText.fontStyle = FontStyles.Bold;
            buyButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            buyButtonText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (buyButtonImage != null && buyButtonImage.sprite == null)
        {
            buyButtonImage.sprite = backgroundImage != null ? backgroundImage.sprite : null;
        }
    }

    private void StyleForState(bool canBuy)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = canBuy
                ? new Color(0.17f, 0.2f, 0.25f, 0.92f)
                : new Color(0.12f, 0.14f, 0.18f, 0.88f);
        }

        if (priceText != null)
        {
            priceText.color = canBuy
                ? new Color(0.98f, 0.83f, 0.45f, 1f)
                : new Color(0.72f, 0.65f, 0.5f, 1f);
        }

        if (buyButtonImage != null)
        {
            buyButtonImage.color = canBuy
                ? new Color(0.9f, 0.86f, 0.74f, 1f)
                : new Color(0.42f, 0.42f, 0.44f, 0.85f);
            buyButtonImage.type = Image.Type.Sliced;
        }

        if (buyButtonText != null)
        {
            buyButtonText.text = canBuy ? "Buy" : "Locked";
            buyButtonText.color = canBuy
                ? new Color(0.12f, 0.12f, 0.14f, 1f)
                : new Color(0.2f, 0.2f, 0.22f, 0.85f);
        }
    }
}
