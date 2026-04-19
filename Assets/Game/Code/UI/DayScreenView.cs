using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DayScreenView : MonoBehaviour
{
    private const float UpgradeItemSpacing = 12f;
    private const float UpgradeItemHeight = 78f;
    private const float ContentPanelHorizontalMargin = 56f;
    private const float ContentPanelVerticalMargin = 40f;
    private const float ContentPanelMaxWidth = 1080f;
    private const float ContentPanelMaxHeight = 620f;
    private const float SectionPadding = 24f;
    private const float SummaryColumnWidthNormalized = 0.34f;
    private const float BottomButtonHeight = 56f;

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

    private void Awake()
    {
        EnsureInitialized();
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

        RebuildUpgradeItems(upgradeItems);
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

        var itemStep = UpgradeItemHeight + UpgradeItemSpacing;
        for (var i = 0; i < upgradeItems.Count; i++)
        {
            var itemView = Instantiate(upgradeItemTemplate, upgradeItemsContainer, false);
            var itemRect = itemView.transform as RectTransform;
            if (itemRect != null)
            {
                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(1f, 1f);
                itemRect.pivot = new Vector2(0.5f, 1f);
                itemRect.sizeDelta = new Vector2(0f, UpgradeItemHeight);
                itemRect.anchoredPosition = new Vector2(0f, -(itemStep * i));
            }

            itemView.gameObject.name = $"UpgradeItem_{i}";
            itemView.BuyRequested -= HandleUpgradeBuyRequested;
            itemView.BuyRequested += HandleUpgradeBuyRequested;
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
        if (runState.LastDayReward == null || runState.LastDayReward.SourceNightIndex <= 0)
        {
            return
                $"<size=26><b>Day {runState.CurrentDay}</b></size>\n" +
                "<color=#d7c7a8>The cemetery is quiet. Prepare the next signal.</color>\n\n" +
                "<size=18><b>State</b></size>\n" +
                $"Faith reserve: <color=#f0ead6>{runState.Faith}</color>\n" +
                $"Gold: <color=#f4c96b>{runState.Gold}</color>\n" +
                $"Cemetery: <color=#d7e3d1>{runState.CemeteryState}/{runState.CemeteryMaxState}</color>";
        }

        var summary =
            $"<size=26><b>Day {runState.CurrentDay}</b></size>\n" +
            $"<color=#d7c7a8>Night {runState.LastDayReward.SourceNightIndex} survived. Spend your gold before dusk.</color>\n\n";

        if (runState.LastDayReward.HasAnyReward)
        {
            summary += $"<size=18><b>Last Reward</b></size>\n{BuildRewardSummary(runState.LastDayReward)}\n\n";
        }

        summary +=
            "<size=18><b>State</b></size>\n" +
            $"Faith reserve: <color=#f0ead6>{runState.Faith}</color>\n" +
            $"Gold: <color=#f4c96b>{runState.Gold}</color>\n" +
            $"Cemetery: <color=#d7e3d1>{runState.CemeteryState}/{runState.CemeteryMaxState}</color>";

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
        if (rootRect == null || contentPanelRect == null || summaryPanelRect == null || upgradesPanelRect == null)
        {
            return;
        }

        var targetWidth = Mathf.Clamp(
            rootRect.rect.width - ContentPanelHorizontalMargin * 2f,
            720f,
            ContentPanelMaxWidth);
        var targetHeight = Mathf.Clamp(
            rootRect.rect.height - ContentPanelVerticalMargin * 2f,
            440f,
            ContentPanelMaxHeight);

        contentPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentPanelRect.pivot = new Vector2(0.5f, 0.5f);
        contentPanelRect.sizeDelta = new Vector2(targetWidth, targetHeight);
        contentPanelRect.anchoredPosition = Vector2.zero;

        summaryPanelRect.anchorMin = new Vector2(0f, 0f);
        summaryPanelRect.anchorMax = new Vector2(SummaryColumnWidthNormalized, 1f);
        summaryPanelRect.offsetMin = new Vector2(SectionPadding, SectionPadding);
        summaryPanelRect.offsetMax = new Vector2(-SectionPadding * 0.5f, -SectionPadding);

        upgradesPanelRect.anchorMin = new Vector2(SummaryColumnWidthNormalized, 0f);
        upgradesPanelRect.anchorMax = new Vector2(1f, 1f);
        upgradesPanelRect.offsetMin = new Vector2(SectionPadding * 0.5f, SectionPadding);
        upgradesPanelRect.offsetMax = new Vector2(-SectionPadding, -SectionPadding);

        if (summaryText != null)
        {
            var summaryRect = summaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0f, 0.26f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.offsetMin = new Vector2(22f, 20f);
            summaryRect.offsetMax = new Vector2(-22f, -20f);
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin = 16f;
            summaryText.fontSizeMax = 30f;
            summaryText.color = new Color(0.95f, 0.94f, 0.9f, 1f);
        }

        if (startNightButton != null)
        {
            var buttonRect = startNightButton.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 0f);
                buttonRect.anchorMax = new Vector2(1f, 0f);
                buttonRect.offsetMin = new Vector2(22f, 22f);
                buttonRect.offsetMax = new Vector2(-22f, 22f + BottomButtonHeight);
            }
        }

        if (upgradeItemsContainer != null)
        {
            upgradeItemsContainer.anchorMin = new Vector2(0f, 0f);
            upgradeItemsContainer.anchorMax = new Vector2(1f, 1f);
            upgradeItemsContainer.offsetMin = new Vector2(18f, 18f);
            upgradeItemsContainer.offsetMax = new Vector2(-18f, -18f);
            upgradeItemsContainer.pivot = new Vector2(0.5f, 1f);
        }
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
