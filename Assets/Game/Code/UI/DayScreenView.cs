using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DayScreenView : MonoBehaviour
{
    private const float UpgradeItemSpacing = 12f;

    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private RectTransform upgradeItemsContainer;
    [SerializeField] private DayUpgradeItemView upgradeItemTemplate;
    [SerializeField] private Button startNightButton;

    public event Action StartNightRequested;
    public event Action<string> UpgradePurchaseRequested;

    private bool isInitialized;
    private readonly List<DayUpgradeItemView> spawnedUpgradeItems = new();

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

        var itemStep = templateRect.sizeDelta.y + UpgradeItemSpacing;
        for (var i = 0; i < upgradeItems.Count; i++)
        {
            var itemView = Instantiate(upgradeItemTemplate, upgradeItemsContainer, false);
            var itemRect = itemView.transform as RectTransform;
            if (itemRect != null)
            {
                itemRect.anchorMin = templateRect.anchorMin;
                itemRect.anchorMax = templateRect.anchorMax;
                itemRect.pivot = templateRect.pivot;
                itemRect.sizeDelta = templateRect.sizeDelta;
                itemRect.anchoredPosition = templateRect.anchoredPosition - new Vector2(0f, itemStep * i);
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
                $"Day {runState.CurrentDay}\n" +
                "The cemetery is quiet.\n" +
                $"Faith: {runState.Faith}\n" +
                $"Gold: {runState.Gold}\n" +
                $"Cemetery: {runState.CemeteryState}";
        }

        return
            $"Day {runState.CurrentDay}\n" +
            $"Night {runState.LastDayReward.SourceNightIndex} completed\n" +
            $"Reward: {BuildRewardSummary(runState.LastDayReward)}\n" +
            $"Faith: {runState.Faith}\n" +
            $"Gold: {runState.Gold}\n" +
            $"Cemetery: {runState.CemeteryState}";
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
}
