using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class DayScreenView : MonoBehaviour
{
    private const string GeneratedRootAssetPath = "Assets/Game/Resources/GeneratedUi/GeneratedDayScreenView.prefab";
    private const string GeneratedCardAssetPath = "Assets/Game/Resources/GeneratedUi/GeneratedDayUpgradeCard.prefab";
    private const string GeneratedCardResourcePath = "GeneratedUi/GeneratedDayUpgradeCard";

    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private RectTransform upgradeItemsContainer;
    [SerializeField] private DayUpgradeItemView upgradeItemTemplate;
    [SerializeField] private Button startNightButton;

    public event Action StartNightRequested;
    public event Action<string> UpgradePurchaseRequested;

    private readonly List<DayUpgradeItemView> spawnedUpgradeItems = new();

    private bool isInitialized;
    private bool hasSavedGeneratedPrefabs;
    private bool isApplyingLayout;

    private RunState currentRunState;
    private IReadOnlyList<DayUpgradeItemData> currentUpgradeItems = Array.Empty<DayUpgradeItemData>();

    private RectTransform rootRect;
    private RectTransform contentRootRect;
    private RectTransform summaryPanelRect;
    private RectTransform cardsPanelRect;
    private RectTransform footerPanelRect;

    private Image contentRootImage;
    private Image summaryPanelImage;
    private Image cardsPanelImage;
    private Image footerPanelImage;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (startNightButton != null)
        {
            startNightButton.onClick.RemoveListener(HandleStartNightPressed);
        }

        ClearUpgradeItems();
        isInitialized = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || isApplyingLayout)
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        EnsureInitialized();
        ApplyRuntimeLayout();

        if (currentRunState != null)
        {
            RefreshSummary(currentRunState);
        }

        RebuildUpgradeItems(currentUpgradeItems);
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
        RefreshSummary(runState);
        RefreshStartNightButton(runState);
        RebuildUpgradeItems(currentUpgradeItems);
    }

    private void EnsureInitialized()
    {
        rootRect ??= transform as RectTransform;
        if (rootRect == null)
        {
            return;
        }

        MakeRootNonBlocking();
        EnsureRuntimeStructure();

        if (!isInitialized)
        {
            if (startNightButton != null)
            {
                startNightButton.onClick.RemoveListener(HandleStartNightPressed);
                startNightButton.onClick.AddListener(HandleStartNightPressed);
            }

            isInitialized = true;
        }

        ApplyRuntimeLayout();
        TrySaveGeneratedPrefabs();
    }

    private void MakeRootNonBlocking()
    {
        if (TryGetComponent<Image>(out var rootImage))
        {
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = false;
        }
    }

    private void EnsureRuntimeStructure()
    {
        if (contentRootRect == null)
        {
            contentRootRect = CreatePanel("DayScreenContentRoot", rootRect, out contentRootImage);
            contentRootImage.color = new Color(0f, 0f, 0f, 0f);
            contentRootImage.raycastTarget = false;
        }

        if (summaryPanelRect == null)
        {
            summaryPanelRect = CreatePanel("DaySummaryPanel", contentRootRect, out summaryPanelImage);
            summaryPanelImage.color = new Color(0.10f, 0.12f, 0.15f, 0.94f);
        }

        if (cardsPanelRect == null)
        {
            cardsPanelRect = CreatePanel("DayCardsPanel", contentRootRect, out cardsPanelImage);
            cardsPanelImage.color = new Color(0f, 0f, 0f, 0f);
            cardsPanelImage.raycastTarget = false;
        }

        if (footerPanelRect == null)
        {
            footerPanelRect = CreatePanel("DayFooterPanel", contentRootRect, out footerPanelImage);
            footerPanelImage.color = new Color(0f, 0f, 0f, 0f);
            footerPanelImage.raycastTarget = false;
        }

        if (summaryText == null)
        {
            summaryText = CreateText("SummaryText", summaryPanelRect, string.Empty);
        }
        else if (summaryText.rectTransform.parent != summaryPanelRect)
        {
            summaryText.rectTransform.SetParent(summaryPanelRect, false);
        }

        if (startNightButton == null)
        {
            startNightButton = CreateButton("StartNightButton", footerPanelRect, "Start Night");
        }
        else if ((startNightButton.transform as RectTransform)?.parent != footerPanelRect)
        {
            (startNightButton.transform as RectTransform)?.SetParent(footerPanelRect, false);
        }

        EnsureUpgradeTemplate();
        EnsureUpgradeContainer();
    }

    private void EnsureUpgradeTemplate()
    {
        if (upgradeItemTemplate == null)
        {
            var generatedPrefab = Resources.Load<GameObject>(GeneratedCardResourcePath);
            if (generatedPrefab != null)
            {
                var instance = Instantiate(generatedPrefab, cardsPanelRect, false);
                instance.name = generatedPrefab.name;
                upgradeItemTemplate = instance.GetComponent<DayUpgradeItemView>();
            }
        }

        if (upgradeItemTemplate == null)
        {
            var templateObject = new GameObject(
                "DayUpgradeItemTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(DayUpgradeItemView));

            var templateRect = templateObject.GetComponent<RectTransform>();
            templateRect.SetParent(cardsPanelRect, false);
            templateRect.sizeDelta = new Vector2(92f, 74f);

            upgradeItemTemplate = templateObject.GetComponent<DayUpgradeItemView>();
        }

        upgradeItemTemplate.gameObject.SetActive(false);
    }

    private void EnsureUpgradeContainer()
    {
        if (upgradeItemsContainer == null)
        {
            var containerObject = new GameObject("UpgradeItemsContainer", typeof(RectTransform));
            upgradeItemsContainer = containerObject.GetComponent<RectTransform>();
            upgradeItemsContainer.SetParent(cardsPanelRect, false);
        }
        else if (upgradeItemsContainer.parent != cardsPanelRect)
        {
            upgradeItemsContainer.SetParent(cardsPanelRect, false);
        }
    }

    private void RefreshSummary(RunState runState)
    {
        if (summaryText == null || runState == null)
        {
            return;
        }

        summaryText.text = BuildSummary(runState);
    }

    private void RefreshStartNightButton(RunState runState)
    {
        if (startNightButton == null || runState == null)
        {
            return;
        }

        startNightButton.interactable = runState.CurrentPhase == GamePhase.Day;
    }

    private void HandleStartNightPressed()
    {
        StartNightRequested?.Invoke();
    }

    private string BuildSummary(RunState runState)
    {
        var hasLastNight = runState.LastDayReward != null && runState.LastDayReward.SourceNightIndex > 0;

        var leadLine = hasLastNight
            ? $"Night {runState.LastDayReward.SourceNightIndex} survived."
            : "Prepare for the next night.";

        return
            $"<b>Day {runState.CurrentDay}</b>\n" +
            $"<color=#d7c7a8>{leadLine}</color>\n" +
            $"Start Faith: <color=#f0ead6>{Mathf.Max(0, runState.StartingNightFaith)}</color>\n" +
            $"Gold: <color=#f4c96b>{Mathf.Max(0, runState.Gold)}</color>\n" +
            $"Cemetery: <color=#d7e3d1>{Mathf.Max(0, runState.CemeteryState)}/{Mathf.Max(1, runState.CemeteryMaxState)}</color>";
    }

    private void RebuildUpgradeItems(IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        ClearUpgradeItems();

        if (upgradeItemsContainer == null || upgradeItemTemplate == null)
        {
            return;
        }

        const int slotCount = 3;

        var availableWidth = Mathf.Max(280f, upgradeItemsContainer.rect.width);
        var availableHeight = Mathf.Max(70f, upgradeItemsContainer.rect.height);
        const float spacing = 4f;

        var cardWidth = Mathf.Floor((availableWidth - spacing * 2f) / 3f);
        var cardHeight = Mathf.Clamp(availableHeight, 70f, 82f);

        var usedWidth = slotCount * cardWidth + (slotCount - 1) * spacing;
        var startX = Mathf.Floor((availableWidth - usedWidth) * 0.5f);

        for (var i = 0; i < slotCount; i++)
        {
            DayUpgradeItemData itemData = null;
            if (upgradeItems != null && i < upgradeItems.Count)
            {
                itemData = upgradeItems[i];
            }

            var itemView = Instantiate(upgradeItemTemplate, upgradeItemsContainer, false);
            itemView.gameObject.name = $"UpgradeItem_{i + 1}";
            itemView.gameObject.SetActive(true);

            if (itemView.transform is RectTransform itemRect)
            {
                itemRect.anchorMin = new Vector2(0f, 0.5f);
                itemRect.anchorMax = new Vector2(0f, 0.5f);
                itemRect.pivot = new Vector2(0f, 0.5f);
                itemRect.sizeDelta = new Vector2(cardWidth, cardHeight);
                itemRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0f);
            }

            itemView.BuyRequested -= HandleUpgradeBuyRequested;
            itemView.BuyRequested += HandleUpgradeBuyRequested;
            itemView.ApplyLayout(true, cardWidth, cardHeight);

            var isEmptySlot = itemData == null || string.IsNullOrWhiteSpace(itemData.UpgradeId);
            if (isEmptySlot)
            {
                itemView.gameObject.SetActive(false);
            }
            else
            {
                itemView.Bind(itemData);
            }

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
            Destroy(itemView.gameObject);
        }

        spawnedUpgradeItems.Clear();
    }

    private void HandleUpgradeBuyRequested(string upgradeId)
    {
        UpgradePurchaseRequested?.Invoke(upgradeId);
    }

    private void ApplyRuntimeLayout()
    {
        if (rootRect == null || contentRootRect == null || summaryPanelRect == null || cardsPanelRect == null ||
            footerPanelRect == null)
        {
            return;
        }

        if (isApplyingLayout)
        {
            return;
        }

        isApplyingLayout = true;

        try
        {
            // canvas target: 320x180
            var rootWidth = Mathf.Max(320f, rootRect.rect.width);
            var rootHeight = Mathf.Max(180f, rootRect.rect.height);

            var contentWidth = 312f;
            var headerHeight = 46f;
            var cardsHeight = 76f;
            var footerHeight = 24f;
            var topMargin = 4f;
            var verticalGap = 4f;
            var totalHeight = headerHeight + verticalGap + cardsHeight + verticalGap + footerHeight;

            contentRootRect.anchorMin = new Vector2(0.5f, 1f);
            contentRootRect.anchorMax = new Vector2(0.5f, 1f);
            contentRootRect.pivot = new Vector2(0.5f, 1f);
            contentRootRect.sizeDelta = new Vector2(contentWidth, totalHeight);
            contentRootRect.anchoredPosition = new Vector2(0f, -topMargin);

            summaryPanelRect.anchorMin = new Vector2(0f, 1f);
            summaryPanelRect.anchorMax = new Vector2(0f, 1f);
            summaryPanelRect.pivot = new Vector2(0f, 1f);
            summaryPanelRect.sizeDelta = new Vector2(140f, headerHeight);
            summaryPanelRect.anchoredPosition = Vector2.zero;

            cardsPanelRect.anchorMin = new Vector2(0f, 1f);
            cardsPanelRect.anchorMax = new Vector2(1f, 1f);
            cardsPanelRect.pivot = new Vector2(0.5f, 1f);
            cardsPanelRect.sizeDelta = new Vector2(0f, cardsHeight);
            cardsPanelRect.anchoredPosition = new Vector2(0f, -(headerHeight + verticalGap));

            footerPanelRect.anchorMin = new Vector2(0f, 1f);
            footerPanelRect.anchorMax = new Vector2(1f, 1f);
            footerPanelRect.pivot = new Vector2(0.5f, 1f);
            footerPanelRect.sizeDelta = new Vector2(0f, footerHeight);
            footerPanelRect.anchoredPosition =
                new Vector2(0f, -(headerHeight + verticalGap + cardsHeight + verticalGap));

            if (summaryText != null)
            {
                var summaryRect = summaryText.rectTransform;
                summaryRect.anchorMin = Vector2.zero;
                summaryRect.anchorMax = Vector2.one;
                summaryRect.offsetMin = new Vector2(5f, 4f);
                summaryRect.offsetMax = new Vector2(-5f, -4f);

                summaryText.alignment = TextAlignmentOptions.TopLeft;
                summaryText.enableAutoSizing = true;
                summaryText.fontSizeMin = 5f;
                summaryText.fontSizeMax = 10f;
                summaryText.lineSpacing = -10f;
                summaryText.color = new Color(0.95f, 0.94f, 0.90f, 1f);
            }

            if (upgradeItemsContainer != null)
            {
                upgradeItemsContainer.anchorMin = Vector2.zero;
                upgradeItemsContainer.anchorMax = Vector2.one;
                upgradeItemsContainer.offsetMin = Vector2.zero;
                upgradeItemsContainer.offsetMax = Vector2.zero;
                upgradeItemsContainer.pivot = new Vector2(0.5f, 0.5f);
            }

            if (startNightButton != null && startNightButton.transform is RectTransform buttonRect)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.sizeDelta = new Vector2(120f, 20f);
                buttonRect.anchoredPosition = Vector2.zero;

                if (startNightButton.TryGetComponent<Image>(out var buttonImage))
                {
                    buttonImage.color = new Color(0.91f, 0.86f, 0.75f, 1f);
                    buttonImage.type = Image.Type.Sliced;
                }

                var buttonText = startNightButton.GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null)
                {
                    buttonText.alignment = TextAlignmentOptions.Center;
                    buttonText.enableAutoSizing = true;
                    buttonText.fontSizeMin = 6f;
                    buttonText.fontSizeMax = 10f;
                    buttonText.fontStyle = FontStyles.Bold;
                    buttonText.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                    buttonText.text = "Start Night";
                }
            }
        }
        finally
        {
            isApplyingLayout = false;
        }
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, out Image image)
    {
        var panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);

        image = panelObject.GetComponent<Image>();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        return panelRect;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string textValue)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
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

        var labelObject =
            new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonObject.transform, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        return button;
    }

    [Conditional("UNITY_EDITOR")]
    private void TrySaveGeneratedPrefabs()
    {
#if UNITY_EDITOR
        if (hasSavedGeneratedPrefabs)
        {
            return;
        }

        try
        {
            var resourcesDirectoryAbsolutePath = Path.Combine(Application.dataPath, "Game/Resources/GeneratedUi");
            Directory.CreateDirectory(resourcesDirectoryAbsolutePath);
            AssetDatabase.Refresh();

            if (upgradeItemTemplate != null)
            {
                PrefabUtility.SaveAsPrefabAsset(upgradeItemTemplate.gameObject, GeneratedCardAssetPath);
            }

            PrefabUtility.SaveAsPrefabAsset(gameObject, GeneratedRootAssetPath);
            hasSavedGeneratedPrefabs = true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DayScreenView: failed to save generated prefab assets. {exception.Message}");
        }
#endif
    }
}