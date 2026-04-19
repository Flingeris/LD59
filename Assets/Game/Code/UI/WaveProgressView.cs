using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveProgressView : MonoBehaviour
{
    [SerializeField] private TMP_Text nightTimerText;
    [SerializeField] private TMP_Text nextWaveText;
    [SerializeField] private Image nightProgressFillImage;
    [SerializeField] private Image nextWaveProgressFillImage;

    public void RefreshView()
    {
        if (G.main == null || G.main.RunState == null || !G.main.IsNightWaveActive)
        {
            SetInactive();
            return;
        }

        var elapsedSeconds = Mathf.Max(0f, G.main.CurrentNightElapsedSeconds);
        var durationSeconds = Mathf.Max(0f, G.main.CurrentNightDurationSeconds);
        var clampedElapsedSeconds = durationSeconds > 0f
            ? Mathf.Min(elapsedSeconds, durationSeconds)
            : elapsedSeconds;

        if (nightTimerText != null)
        {
            nightTimerText.text =
                $"Night {FormatSeconds(clampedElapsedSeconds)} / {FormatSeconds(durationSeconds)}";
        }

        if (nightProgressFillImage != null)
        {
            nightProgressFillImage.fillAmount = durationSeconds <= 0f ? 0f : clampedElapsedSeconds / durationSeconds;
            nightProgressFillImage.gameObject.SetActive(true);
        }

        if (!G.main.HasUpcomingNightWave)
        {
            if (nextWaveText != null)
            {
                nextWaveText.text = "Next wave: cleared";
            }

            if (nextWaveProgressFillImage != null)
            {
                nextWaveProgressFillImage.fillAmount = 1f;
                nextWaveProgressFillImage.gameObject.SetActive(true);
            }

            return;
        }

        var nextWaveTriggerTime = Mathf.Max(0f, G.main.NextNightWaveTriggerTime);
        var previousWaveTriggerTime = Mathf.Max(0f, G.main.PreviousNightWaveTriggerTime);
        var currentWaveWindowDuration = Mathf.Max(0.01f, nextWaveTriggerTime - previousWaveTriggerTime);
        var waveWindowElapsed = Mathf.Clamp(elapsedSeconds - previousWaveTriggerTime, 0f, currentWaveWindowDuration);
        var secondsUntilWave = Mathf.Max(0f, nextWaveTriggerTime - elapsedSeconds);

        if (nextWaveText != null)
        {
            nextWaveText.text = $"Next wave in {FormatSeconds(secondsUntilWave)}";
        }

        if (nextWaveProgressFillImage != null)
        {
            nextWaveProgressFillImage.fillAmount = waveWindowElapsed / currentWaveWindowDuration;
            nextWaveProgressFillImage.gameObject.SetActive(true);
        }
    }

    public void SetInactive()
    {
        if (nightTimerText != null)
        {
            nightTimerText.text = string.Empty;
        }

        if (nextWaveText != null)
        {
            nextWaveText.text = string.Empty;
        }

        if (nightProgressFillImage != null)
        {
            nightProgressFillImage.fillAmount = 0f;
            nightProgressFillImage.gameObject.SetActive(false);
        }

        if (nextWaveProgressFillImage != null)
        {
            nextWaveProgressFillImage.fillAmount = 0f;
            nextWaveProgressFillImage.gameObject.SetActive(false);
        }
    }

    private static string FormatSeconds(float seconds)
    {
        var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        var minutes = totalSeconds / 60;
        var remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
