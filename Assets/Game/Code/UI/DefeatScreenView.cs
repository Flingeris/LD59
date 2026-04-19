using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DefeatScreenView : MonoBehaviour
{
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button restartButton;

    public event Action RestartRequested;

    private bool isInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (!isInitialized || restartButton == null)
        {
            return;
        }

        restartButton.onClick.RemoveListener(HandleRestartPressed);
        isInitialized = false;
    }

    public void Show(RunState runState)
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        Refresh(runState);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh(RunState runState)
    {
        if (summaryText != null)
        {
            summaryText.text = BuildSummary(runState);
        }

        if (restartButton != null)
        {
            restartButton.interactable = runState != null && runState.CurrentPhase == GamePhase.Defeat;
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized || restartButton == null)
        {
            return;
        }

        restartButton.onClick.AddListener(HandleRestartPressed);
        isInitialized = true;
    }

    private void HandleRestartPressed()
    {
        RestartRequested?.Invoke();
    }

    private static string BuildSummary(RunState runState)
    {
        if (runState == null)
        {
            return "Defeat\nThe cemetery has fallen.";
        }

        var reachedDayCount = GetReachedDayCount(runState);

        return
            "Defeat\n" +
            "The cemetery has fallen.\n" +
            $"Day reached: {reachedDayCount}\n" +
            $"Night reached: {runState.CurrentNight}";
    }

    private static int GetReachedDayCount(RunState runState)
    {
        if (runState == null)
        {
            return 0;
        }

        return runState.CurrentNight > runState.CurrentDay
            ? runState.CurrentDay + 1
            : runState.CurrentDay;
    }
}
