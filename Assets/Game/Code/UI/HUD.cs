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
    [SerializeField] private WaveProgressView waveProgressView;
    [SerializeField] private DayScreenView dayScreenView;
    [SerializeField] private DefeatScreenView defeatScreenView;
    [SerializeField] private WinScreenView winScreenView;
    [SerializeField] private PhaseTransitionView phaseTransitionView;
    [SerializeField] private TMP_Text bellFeedbackText;
    [SerializeField] private BellButtonBinding[] bellButtons;

    public event Action DayScreenStartNightRequested;
    public event Action<string> DayScreenUpgradePurchaseRequested;
    public event Action DefeatScreenRestartRequested;
    public event Action WinScreenRestartRequested;

    private bool missingMainWarningShown;

    private void Awake()
    {
        G.HUD = this;
        BindDayScreenView();
        BindDefeatScreenView();
        BindWinScreenView();
        RefreshBellButtonLabels();
        RefreshBellButtonInteractivity(null);
    }

    private void OnDestroy()
    {
        UnbindDayScreenView();
        UnbindDefeatScreenView();
        UnbindWinScreenView();

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
            RefreshBellButtonInteractivity(null);
            waveProgressView?.SetInactive();

            if (!missingMainWarningShown)
            {
                Debug.LogWarning("HUD: Main or RunState not found");
                missingMainWarningShown = true;
            }

            return;
        }

        missingMainWarningShown = false;

        var runState = G.main.RunState;
        var showRuntimeHud = runState.CurrentPhase == GamePhase.Night;
        SetRuntimeHudVisible(showRuntimeHud);
        RefreshBellButtonInteractivity(runState);

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

        waveProgressView?.RefreshView();
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

    public void ShowDefeatScreen(RunState runState)
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.Show(runState);
    }

    public void HideDefeatScreen()
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.Hide();
    }

    public void ShowWinScreen(RunState runState)
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.Show(runState);
    }

    public void HideWinScreen()
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.Hide();
    }

    public void ShowPhaseTransition(GamePhase phase)
    {
        if (phaseTransitionView == null)
        {
            return;
        }

        phaseTransitionView.Play(phase);
    }

    public void HidePhaseTransition()
    {
        if (phaseTransitionView == null)
        {
            return;
        }

        phaseTransitionView.Hide();
    }

    public void ShowBellFeedback(string message)
    {
        SetBellFeedback(message);
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

    private void BindDefeatScreenView()
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.RestartRequested -= HandleDefeatScreenRestartRequested;
        defeatScreenView.RestartRequested += HandleDefeatScreenRestartRequested;
    }

    private void UnbindDefeatScreenView()
    {
        if (defeatScreenView == null)
        {
            return;
        }

        defeatScreenView.RestartRequested -= HandleDefeatScreenRestartRequested;
    }

    private void BindWinScreenView()
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.RestartRequested -= HandleWinScreenRestartRequested;
        winScreenView.RestartRequested += HandleWinScreenRestartRequested;
    }

    private void UnbindWinScreenView()
    {
        if (winScreenView == null)
        {
            return;
        }

        winScreenView.RestartRequested -= HandleWinScreenRestartRequested;
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

    private void RefreshBellButtonInteractivity(RunState runState)
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

            binding.button.interactable = false;
        }
    }

    private void OnBellButtonPressed(string bellId)
    {
        SetBellFeedback("Use bells in the world");
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

    private void HandleDefeatScreenRestartRequested()
    {
        DefeatScreenRestartRequested?.Invoke();
    }

    private void HandleWinScreenRestartRequested()
    {
        WinScreenRestartRequested?.Invoke();
    }

    private void SetRuntimeHudVisible(bool visible)
    {
        SetViewVisible(faithText, visible);
        SetViewVisible(goldText, visible);
        SetViewVisible(cemeteryStateText, visible);
        SetViewVisible(phaseText, visible);
        SetViewVisible(bellFeedbackText, visible);

        if (waveProgressView != null && waveProgressView.gameObject.activeSelf != visible)
        {
            waveProgressView.gameObject.SetActive(visible);
        }
    }

    private static void SetViewVisible(Component component, bool visible)
    {
        if (component == null)
        {
            return;
        }

        if (component.gameObject.activeSelf != visible)
        {
            component.gameObject.SetActive(visible);
        }
    }

}
