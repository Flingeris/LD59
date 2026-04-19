using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PhaseTransitionView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private string dayLabel = "Day";
    [SerializeField] private string nightLabel = "Night";
    [Min(0f)] [SerializeField] private float fadeInDuration = 0.12f;
    [Min(0f)] [SerializeField] private float holdDuration = 0.45f;
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.2f;

    public void Play(GamePhase phase)
    {
        var phaseLabel = GetPhaseLabel(phase);
        if (string.IsNullOrWhiteSpace(phaseLabel))
        {
            Hide();
            return;
        }

        StopAllCoroutines();

        if (bannerText != null)
        {
            bannerText.text = phaseLabel;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(true);
        StartCoroutine(PlayRoutine());
    }

    public void Hide()
    {
        StopAllCoroutines();
        HideImmediate();
    }

    private IEnumerator PlayRoutine()
    {
        yield return Fade(0f, 1f, fadeInDuration);

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        yield return Fade(1f, 0f, fadeOutDuration);
        HideImmediate();
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = toAlpha;
            yield break;
        }

        var elapsed = 0f;
        canvasGroup.alpha = fromAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = toAlpha;
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    private string GetPhaseLabel(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Day => dayLabel,
            GamePhase.Night => nightLabel,
            _ => string.Empty
        };
    }
}
