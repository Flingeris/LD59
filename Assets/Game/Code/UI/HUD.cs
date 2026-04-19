using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HUD : MonoBehaviour
{
    [System.Serializable]
    private class BellButtonBinding
    {
        public string bellId;
        public Button button;
        public TMP_Text label;
        [HideInInspector] public UnityAction clickAction;
    }

    [SerializeField] private TMP_Text faithText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text cemeteryStateText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private DayScreenView dayScreenView;
    [SerializeField] private TMP_Text bellFeedbackText;
    [SerializeField] private BellButtonBinding[] bellButtons;

    public event Action DayScreenStartNightRequested;
    public event Action<string> DayScreenUpgradePurchaseRequested;

    private bool missingMainWarningShown;

    private void Awake()
    {
        G.HUD = this;
        BindBellButtons();
        BindDayScreenView();
        RefreshBellButtonLabels();
    }

    private void OnDestroy()
    {
        UnbindBellButtons();
        UnbindDayScreenView();

        if (G.HUD == this)
        {
            G.HUD = null;
        }
    }

    private void Update()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        if (G.main == null || G.main.RunState == null)
        {
            if (!missingMainWarningShown)
            {
                Debug.LogWarning("HUD: Main or RunState not found");
                missingMainWarningShown = true;
            }

            return;
        }

        missingMainWarningShown = false;

        var runState = G.main.RunState;

        if (faithText != null)
        {
            faithText.text = $"Faith: {runState.Faith}";
        }

        if (goldText != null)
        {
            goldText.text = $"Gold: {runState.Gold}";
        }

        if (cemeteryStateText != null)
        {
            cemeteryStateText.text = $"Cemetery: {runState.CemeteryState}";
        }

        if (phaseText != null)
        {
            phaseText.text = $"Phase: {runState.CurrentPhase}";
        }
    }

    public void ShowDayScreen(RunState runState, IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.Show(runState, upgradeItems);
    }

    public void HideDayScreen()
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.Hide();
    }

    public void RefreshDayScreen(RunState runState, IReadOnlyList<DayUpgradeItemData> upgradeItems)
    {
        if (dayScreenView == null || !dayScreenView.gameObject.activeSelf)
        {
            return;
        }

        dayScreenView.Refresh(runState, upgradeItems);
    }

    private void BindBellButtons()
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            var bellId = binding.bellId;
            binding.clickAction = () => OnBellButtonPressed(bellId);
            binding.button.onClick.AddListener(binding.clickAction);
        }
    }

    private void UnbindBellButtons()
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            if (binding.clickAction != null)
            {
            binding.button.onClick.RemoveListener(binding.clickAction);
            }
        }
    }

    private void BindDayScreenView()
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.StartNightRequested -= HandleDayScreenStartNightRequested;
        dayScreenView.StartNightRequested += HandleDayScreenStartNightRequested;
        dayScreenView.UpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
        dayScreenView.UpgradePurchaseRequested += HandleDayScreenUpgradePurchaseRequested;
    }

    private void UnbindDayScreenView()
    {
        if (dayScreenView == null)
        {
            return;
        }

        dayScreenView.StartNightRequested -= HandleDayScreenStartNightRequested;
        dayScreenView.UpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
    }

    private void RefreshBellButtonLabels()
    {
        if (bellButtons == null)
        {
            return;
        }

        for (var i = 0; i < bellButtons.Length; i++)
        {
            var binding = bellButtons[i];
            if (binding == null || binding.label == null)
            {
                continue;
            }

            var bellDef = CMS.Get<BellDef>(binding.bellId);
            if (bellDef == null)
            {
                binding.label.text = binding.bellId;
                continue;
            }

            binding.label.text = $"{bellDef.DisplayName} ({bellDef.FaithCost})";
        }
    }

    private void OnBellButtonPressed(string bellId)
    {

        var result = G.main.TryRingBell(bellId);
        if (result.IsSuccess && result.SpawnResult != null && result.SpawnResult.IsSuccess)
        {
            SetBellFeedback($"Bell: {result.BellDef.DisplayName}");
            return;
        }

        if (!result.IsSuccess)
        {
            SetBellFeedback($"Bell failed: {result.FailureReason}");
            return;
        }

        if (result.SpawnResult != null && !result.SpawnResult.IsSuccess)
        {
            SetBellFeedback($"Spawn failed: {result.SpawnResult.FailureReason}");
        }
    }

    private void SetBellFeedback(string message)
    {
        if (bellFeedbackText != null)
        {
            bellFeedbackText.text = message;
        }
    }

    private void HandleDayScreenStartNightRequested()
    {
        DayScreenStartNightRequested?.Invoke();
    }

    private void HandleDayScreenUpgradePurchaseRequested(string upgradeId)
    {
        DayScreenUpgradePurchaseRequested?.Invoke(upgradeId);
    }

}
