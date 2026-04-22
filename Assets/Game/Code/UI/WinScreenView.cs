using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WinScreenView : MonoBehaviour
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
        EnsureSummaryWaveAnimation();

        if (summaryText != null)
        {
            summaryText.text = BuildSummary(runState);
        }

        if (restartButton != null)
        {
            restartButton.interactable = runState != null && runState.CurrentPhase == GamePhase.Win;
        }
    }

    private void EnsureInitialized()
    {
        EnsureSummaryWaveAnimation();

        if (isInitialized || restartButton == null)
        {
            return;
        }

        restartButton.onClick.AddListener(HandleRestartPressed);
        isInitialized = true;
    }

    private void EnsureSummaryWaveAnimation()
    {
        if (summaryText == null)
        {
            return;
        }

        var waveAnimation = summaryText.GetComponent<SineWaveTextAnimation>();
        if (waveAnimation == null)
        {
            waveAnimation = summaryText.gameObject.AddComponent<SineWaveTextAnimation>();
        }

        waveAnimation.textMesh = summaryText;
        waveAnimation.frequency = 3.5f;
        waveAnimation.amplitude = 1.4f;
    }

    private void HandleRestartPressed()
    {
        RestartRequested?.Invoke();
    }

    private static string BuildSummary(RunState runState)
    {
        if (runState == null)
        {
            return "Victory\nYou survived the graveyard watch.\n\n<link=\"wave\">THANKS FOR PLAYING!</link>";
        }

        return
            "Victory\n" +
            "The cemetery endured the night watch.\n" +
            $"Nights survived: {runState.CurrentNight}\n\n" +
            "<link=\"wave\">THANKS FOR PLAYING!</link>";
    }
}
