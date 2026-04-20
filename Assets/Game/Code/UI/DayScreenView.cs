using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DayScreenView : MonoBehaviour
{
    private const float CompactCanvasWidthThreshold = 420f;
    private const float CompactCanvasHeightThreshold = 240f;
    private const float CompactContentHorizontalMargin = 6f;
    private const float CompactContentVerticalMargin = 6f;
    private const float CompactSectionPadding = 6f;
    private const float CompactBottomButtonHeight = 18f;
    private const float CompactUpgradeItemSpacing = 4f;
    private const float CompactPreferredUpgradeItemHeight = 24f;
    private const float CompactMinimumUpgradeItemHeight = 18f;
    private const float RegularContentHorizontalMargin = 24f;
    private const float RegularContentVerticalMargin = 18f;
    private const float RegularContentMaxWidth = 680f;
    private const float RegularContentMaxHeight = 420f;
    private const float RegularSectionPadding = 16f;
    private const float RegularSummaryColumnWidthNormalized = 0.36f;
    private const float RegularBottomButtonHeight = 40f;
    private const float RegularUpgradeItemSpacing = 8f;
    private const float RegularPreferredUpgradeItemHeight = 52f;
    private const float RegularMinimumUpgradeItemHeight = 40f;

    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private RectTransform upgradeItemsContainer;
    [SerializeField] private DayUpgradeItemView upgradeItemTemplate;
    [SerializeField] private Button startNightButton;

    public event Action StartNightRequested;
    public event Action<string> UpgradePurchaseRequested;

    private bool isInitialized;
    private readonly List<DayUpgradeItemView> spawnedUpgradeItems = new();
    private RectTransform rootRect;
    private RectTransform contentPanelRect;
    private RectTransform summaryPanelRect;
    private RectTransform upgradesPanelRect;
    private Image contentPanelImage;
    private Image summaryPanelImage;
    private Image upgradesPanelImage;
    private RunState currentRunState;
    private IReadOnlyList<DayUpgradeItemData> currentUpgradeItems = Array.Empty<DayUpgradeItemData>();
    private LayoutMetrics currentLayout;
    private bool isApplyingLayout;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || isApplyingLayout)
        {
            return;
        }

        EnsureInitialized();
        ApplyRuntimeLayout();

        if (currentRunState != null && summaryText != null)
        {
            summaryText.text = BuildSummary(currentRunState);
        }

        if (currentUpgradeItems != null)
        {
            RebuildUpgradeItems(currentUpgradeItems);
        }
    }

    private void OnDestroy()
    {
        if (!isInitialized || startNightButton == null)
        {
            ClearUpgradeItems();
            return;
        }

        startNightButton.onClick.RemoveListener(HandleStartNightPressed);
        isInitialized = false;
        ClearUpgradeItems();
    }

    public void Show(RunState runState, IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        Refresh(runState, upgradeItems);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh(RunState runState, IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        if (runState == null)
        {
            return;
        }

        currentRunState = runState;
        currentUpgradeItems = upgradeItems ?? Array.Empty<DayUpgradeItemData>();
        EnsureInitialized();
        ApplyRuntimeLayout();

        if (summaryText != null)
        {
            summaryText.text = BuildSummary(runState);
        }

        if (startNightButton != null)
        {
            startNightButton.interactable = runState.CurrentPhase == GamePhase.Day;
        }

        RebuildUpgradeItems(currentUpgradeItems);
    }

    private void EnsureInitialized()
    {
        rootRect ??= transform as RectTransform;

        if (upgradeItemTemplate != null)
        {
            upgradeItemTemplate.gameObject.SetActive(false);
        }

        if (isInitialized || startNightButton == null)
        {
            return;
        }

        startNightButton.onClick.AddListener(HandleStartNightPressed);
        isInitialized = true;
        EnsureRuntimePanels();
        ApplyRuntimeLayout();
    }

    private void HandleStartNightPressed()
    {
        StartNightRequested?.Invoke();
    }

    private void RebuildUpgradeItems(IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        ClearUpgradeItems();

        if (upgradeItemsContainer == null || upgradeItemTemplate == null || upgradeItems == null)
        {
            return;
        }

        var templateRect = upgradeItemTemplate.transform as RectTransform;
        if (templateRect == null)
        {
            return;
        }

        var columnCount = Mathf.Max(1, currentLayout.UpgradeColumnCount);
        var spacing = currentLayout.UpgradeItemSpacing;
        var availableWidth = upgradeItemsContainer.rect.width;
        if (availableWidth <= 1f)
        {
            availableWidth = templateRect.rect.width;
        }

        if (currentLayout.IsCompact && availableWidth < 150f)
        {
            columnCount = 1;
        }

        var rowCount = Mathf.CeilToInt(upgradeItems.Count / (float)columnCount);
        var availableHeight = upgradeItemsContainer.rect.height;
        var itemHeight = currentLayout.PreferredUpgradeItemHeight;
        if (rowCount > 0 && availableHeight > 0f)
        {
            var fittedHeight = (availableHeight - spacing * Mathf.Max(0, rowCount - 1)) / rowCount;
            itemHeight = Mathf.Clamp(
                fittedHeight,
                currentLayout.MinimumUpgradeItemHeight,
                currentLayout.PreferredUpgradeItemHeight);
        }

        var itemWidth = (availableWidth - spacing * Mathf.Max(0, columnCount - 1)) / columnCount;
        if (itemWidth <= 1f)
        {
            itemWidth = templateRect.rect.width;
        }

        for (var i = 0; i < upgradeItems.Count; i++)
        {
            var itemView = Instantiate(upgradeItemTemplate, upgradeItemsContainer, false);
            var itemRect = itemView.transform as RectTransform;
            if (itemRect != null)
            {
                var row = i / columnCount;
                var column = i % columnCount;
                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(0f, 1f);
                itemRect.pivot = new Vector2(0f, 1f);
                itemRect.sizeDelta = new Vector2(itemWidth, itemHeight);
                itemRect.anchoredPosition = new Vector2(
                    column * (itemWidth + spacing),
                    -(row * (itemHeight + spacing)));
            }

            itemView.gameObject.name = $"UpgradeItem_{i}";
            itemView.BuyRequested -= HandleUpgradeBuyRequested;
            itemView.BuyRequested += HandleUpgradeBuyRequested;
            itemView.ApplyLayout(currentLayout.IsCompact, itemWidth, itemHeight);
            itemView.Bind(upgradeItems[i]);
            itemView.gameObject.SetActive(true);
            spawnedUpgradeItems.Add(itemView);
        }
    }

    private void ClearUpgradeItems()
    {
        for (var i = 0; i < spawnedUpgradeItems.Count; i++)
        {
            var itemView = spawnedUpgradeItems[i];
            if (itemView == null)
            {
                continue;
            }

            itemView.BuyRequested -= HandleUpgradeBuyRequested;
            itemView.gameObject.SetActive(false);
            Destroy(itemView.gameObject);
        }

        spawnedUpgradeItems.Clear();
    }

    private void HandleUpgradeBuyRequested(string upgradeId)
    {
        UpgradePurchaseRequested?.Invoke(upgradeId);
    }

    private string BuildSummary(RunState runState)
    {
        var hasLastNight = runState.LastDayReward != null && runState.LastDayReward.SourceNightIndex > 0;
        var hasReward = runState.LastDayReward != null && runState.LastDayReward.HasAnyReward;
        var offerCount = currentUpgradeItems != null ? currentUpgradeItems.Count : 0;
        var upgradeChoiceLine = offerCount > 0
            ? $"{offerCount} offers available today."
            : "No offers available today.";
        var resourcesLine = currentLayout.IsCompact
            ? $"Faith {runState.Faith}  Gold {runState.Gold}"
            : $"Faith reserve: <color=#f0ead6>{runState.Faith}</color>\nGold: <color=#f4c96b>{runState.Gold}</color>";
        var cemeteryLine = currentLayout.IsCompact
            ? $"Cemetery {runState.CemeteryState}/{runState.CemeteryMaxState}"
            : $"Cemetery: <color=#d7e3d1>{runState.CemeteryState}/{runState.CemeteryMaxState}</color>";

        if (currentLayout.IsCompact)
        {
            var leadLine = hasLastNight
                ? $"Night {runState.LastDayReward.SourceNightIndex} survived."
                : "Prepare for the next night.";
            var followupLine = hasReward
                ? $"{BuildRewardSummary(runState.LastDayReward)}. {upgradeChoiceLine}"
                : upgradeChoiceLine;

            return
                $"<b>Day {runState.CurrentDay}</b>\n" +
                $"<color=#d7c7a8>{leadLine} {followupLine}</color>\n" +
                $"{resourcesLine}\n" +
                cemeteryLine;
        }

        var summary =
            $"<b>Day {runState.CurrentDay}</b>\n" +
            (hasLastNight
                ? $"<color=#d7c7a8>Night {runState.LastDayReward.SourceNightIndex} survived. {upgradeChoiceLine}</color>\n\n"
                : "<color=#d7c7a8>The cemetery is quiet. Prepare the next signal.</color>\n\n");

        if (hasReward)
        {
            summary += $"<b>Last Reward</b>\n{BuildRewardSummary(runState.LastDayReward)}\n\n";
        }

        summary +=
            "<b>State</b>\n" +
            $"{resourcesLine}\n" +
            cemeteryLine;

        return summary;
    }

    private static string BuildRewardSummary(DayRewardData reward)
    {
        if (reward == null || !reward.HasAnyReward)
        {
            return "none";
        }

        if (reward.FaithReward > 0 && reward.GoldReward > 0)
        {
            return $"+{reward.FaithReward} Faith, +{reward.GoldReward} Gold";
        }

        if (reward.FaithReward > 0)
        {
            return $"+{reward.FaithReward} Faith";
        }

        return $"+{reward.GoldReward} Gold";
    }

    private void EnsureRuntimePanels()
    {
        if (rootRect == null)
        {
            return;
        }

        var rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = new Color(0.03f, 0.04f, 0.06f, 0.9f);
            rootImage.type = Image.Type.Sliced;
        }

        if (contentPanelRect == null)
        {
            contentPanelRect = CreatePanel("DayContentPanel", rootRect, new Color(0.09f, 0.11f, 0.15f, 0.94f), out contentPanelImage);
            contentPanelRect.SetSiblingIndex(0);
        }

        if (summaryPanelRect == null)
        {
            summaryPanelRect = CreatePanel("SummaryPanel", contentPanelRect, new Color(0.14f, 0.16f, 0.2f, 0.96f), out summaryPanelImage);
        }

        if (upgradesPanelRect == null)
        {
            upgradesPanelRect = CreatePanel("UpgradesPanel", contentPanelRect, new Color(0.12f, 0.14f, 0.18f, 0.96f), out upgradesPanelImage);
        }

        if (rootImage != null)
        {
            if (contentPanelImage != null && contentPanelImage.sprite == null)
            {
                contentPanelImage.sprite = rootImage.sprite;
            }

            if (summaryPanelImage != null && summaryPanelImage.sprite == null)
            {
                summaryPanelImage.sprite = rootImage.sprite;
            }

            if (upgradesPanelImage != null && upgradesPanelImage.sprite == null)
            {
                upgradesPanelImage.sprite = rootImage.sprite;
            }
        }

        if (summaryText != null && summaryText.rectTransform.parent != summaryPanelRect)
        {
            summaryText.rectTransform.SetParent(summaryPanelRect, false);
        }

        if (startNightButton != null)
        {
            var buttonRect = startNightButton.transform as RectTransform;
            if (buttonRect != null && buttonRect.parent != summaryPanelRect)
            {
                buttonRect.SetParent(summaryPanelRect, false);
            }

            if (startNightButton.TryGetComponent<Image>(out var buttonImage))
            {
                buttonImage.color = new Color(0.92f, 0.88f, 0.78f, 1f);
                buttonImage.type = Image.Type.Sliced;
            }

            var buttonLabel = startNightButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = "Start Night";
                buttonLabel.alignment = TextAlignmentOptions.Center;
                buttonLabel.enableAutoSizing = true;
                buttonLabel.fontSizeMin = 14f;
                buttonLabel.fontSizeMax = 28f;
                buttonLabel.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                buttonLabel.fontStyle = FontStyles.Bold;
            }
        }

        if (upgradeItemsContainer != null && upgradeItemsContainer.parent != upgradesPanelRect)
        {
            upgradeItemsContainer.SetParent(upgradesPanelRect, false);
        }
    }

    private void ApplyRuntimeLayout()
    {
        if (rootRect == null || contentPanelRect == null || summaryPanelRect == null || upgradesPanelRect == null || isApplyingLayout)
        {
            return;
        }

        isApplyingLayout = true;

        try
        {
            currentLayout = CalculateLayoutMetrics();
            var targetWidth = Mathf.Max(160f, rootRect.rect.width - currentLayout.ContentHorizontalMargin * 2f);
            var targetHeight = Mathf.Max(100f, rootRect.rect.height - currentLayout.ContentVerticalMargin * 2f);
            if (!currentLayout.IsCompact)
            {
                targetWidth = Mathf.Min(targetWidth, RegularContentMaxWidth);
                targetHeight = Mathf.Min(targetHeight, RegularContentMaxHeight);
            }

            contentPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentPanelRect.pivot = new Vector2(0.5f, 0.5f);
            contentPanelRect.sizeDelta = new Vector2(targetWidth, targetHeight);
            contentPanelRect.anchoredPosition = Vector2.zero;

            if (currentLayout.IsCompact)
            {
                var summaryHeight = Mathf.Clamp(targetHeight * 0.36f, 50f, 64f);

                summaryPanelRect.anchorMin = new Vector2(0f, 1f);
                summaryPanelRect.anchorMax = new Vector2(1f, 1f);
                summaryPanelRect.pivot = new Vector2(0.5f, 1f);
                summaryPanelRect.offsetMin = new Vector2(currentLayout.SectionPadding, -(currentLayout.SectionPadding + summaryHeight));
                summaryPanelRect.offsetMax = new Vector2(-currentLayout.SectionPadding, -currentLayout.SectionPadding);

                upgradesPanelRect.anchorMin = new Vector2(0f, 0f);
                upgradesPanelRect.anchorMax = new Vector2(1f, 1f);
                upgradesPanelRect.offsetMin = new Vector2(currentLayout.SectionPadding, currentLayout.SectionPadding);
                upgradesPanelRect.offsetMax = new Vector2(-currentLayout.SectionPadding, -(summaryHeight + currentLayout.SectionPadding * 1.5f));
            }
            else
            {
                summaryPanelRect.anchorMin = new Vector2(0f, 0f);
                summaryPanelRect.anchorMax = new Vector2(currentLayout.SummaryColumnWidthNormalized, 1f);
                summaryPanelRect.offsetMin = new Vector2(currentLayout.SectionPadding, currentLayout.SectionPadding);
                summaryPanelRect.offsetMax = new Vector2(-currentLayout.SectionPadding * 0.5f, -currentLayout.SectionPadding);

                upgradesPanelRect.anchorMin = new Vector2(currentLayout.SummaryColumnWidthNormalized, 0f);
                upgradesPanelRect.anchorMax = new Vector2(1f, 1f);
                upgradesPanelRect.offsetMin = new Vector2(currentLayout.SectionPadding * 0.5f, currentLayout.SectionPadding);
                upgradesPanelRect.offsetMax = new Vector2(-currentLayout.SectionPadding, -currentLayout.SectionPadding);
            }

            var textPadding = currentLayout.IsCompact ? 6f : 12f;
            if (summaryText != null)
            {
                var summaryRect = summaryText.rectTransform;
                summaryRect.anchorMin = new Vector2(0f, 0f);
                summaryRect.anchorMax = new Vector2(1f, 1f);
                summaryRect.offsetMin = new Vector2(textPadding, currentLayout.BottomButtonHeight + textPadding + 2f);
                summaryRect.offsetMax = new Vector2(-textPadding, -textPadding);
                summaryText.alignment = TextAlignmentOptions.TopLeft;
                summaryText.enableAutoSizing = true;
                summaryText.fontSizeMin = currentLayout.IsCompact ? 5f : 8f;
                summaryText.fontSizeMax = currentLayout.IsCompact ? 10f : 16f;
                summaryText.color = new Color(0.95f, 0.94f, 0.9f, 1f);
            }

            if (startNightButton != null)
            {
                var buttonRect = startNightButton.transform as RectTransform;
                if (buttonRect != null)
                {
                    buttonRect.anchorMin = new Vector2(0f, 0f);
                    buttonRect.anchorMax = new Vector2(1f, 0f);
                    buttonRect.offsetMin = new Vector2(textPadding, textPadding);
                    buttonRect.offsetMax = new Vector2(-textPadding, textPadding + currentLayout.BottomButtonHeight);
                }

                var buttonLabel = startNightButton.GetComponentInChildren<TMP_Text>(true);
                if (buttonLabel != null)
                {
                    buttonLabel.fontSizeMin = currentLayout.IsCompact ? 5f : 8f;
                    buttonLabel.fontSizeMax = currentLayout.IsCompact ? 10f : 16f;
                }
            }

            if (upgradeItemsContainer != null)
            {
                var listPadding = currentLayout.IsCompact ? 6f : 10f;
                upgradeItemsContainer.anchorMin = new Vector2(0f, 0f);
                upgradeItemsContainer.anchorMax = new Vector2(1f, 1f);
                upgradeItemsContainer.offsetMin = new Vector2(listPadding, listPadding);
                upgradeItemsContainer.offsetMax = new Vector2(-listPadding, -listPadding);
                upgradeItemsContainer.pivot = new Vector2(0f, 1f);
            }
        }
        finally
        {
            isApplyingLayout = false;
        }
    }

    private LayoutMetrics CalculateLayoutMetrics()
    {
        var canvasWidth = Mathf.Max(1f, rootRect.rect.width);
        var canvasHeight = Mathf.Max(1f, rootRect.rect.height);
        var isCompact = canvasWidth <= CompactCanvasWidthThreshold || canvasHeight <= CompactCanvasHeightThreshold;

        return new LayoutMetrics
        {
            IsCompact = isCompact,
            ContentHorizontalMargin = isCompact ? CompactContentHorizontalMargin : RegularContentHorizontalMargin,
            ContentVerticalMargin = isCompact ? CompactContentVerticalMargin : RegularContentVerticalMargin,
            SectionPadding = isCompact ? CompactSectionPadding : RegularSectionPadding,
            SummaryColumnWidthNormalized = RegularSummaryColumnWidthNormalized,
            BottomButtonHeight = isCompact ? CompactBottomButtonHeight : RegularBottomButtonHeight,
            UpgradeItemSpacing = isCompact ? CompactUpgradeItemSpacing : RegularUpgradeItemSpacing,
            PreferredUpgradeItemHeight = isCompact ? CompactPreferredUpgradeItemHeight : RegularPreferredUpgradeItemHeight,
            MinimumUpgradeItemHeight = isCompact ? CompactMinimumUpgradeItemHeight : RegularMinimumUpgradeItemHeight,
            UpgradeColumnCount = isCompact ? 2 : 1
        };
    }

    private struct LayoutMetrics
    {
        public bool IsCompact;
        public float ContentHorizontalMargin;
        public float ContentVerticalMargin;
        public float SectionPadding;
        public float SummaryColumnWidthNormalized;
        public float BottomButtonHeight;
        public float UpgradeItemSpacing;
        public float PreferredUpgradeItemHeight;
        public float MinimumUpgradeItemHeight;
        public int UpgradeColumnCount;
    }

    private static RectTransform CreatePanel(string panelName, RectTransform parent, Color color, out Image panelImage)
    {
        var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var panelRect = panelObject.transform as RectTransform;
        panelRect.SetParent(parent, false);
        panelImage = panelObject.GetComponent<Image>();
        panelImage.color = color;
        panelImage.raycastTarget = false;
        panelImage.type = Image.Type.Sliced;
        return panelRect;
    }
}
