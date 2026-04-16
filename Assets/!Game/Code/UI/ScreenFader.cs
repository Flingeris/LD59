using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeTime = 0.5f;
    public float FadeTime => fadeTime;

    private Sequence currentSequence;

    private void OnValidate()
    {
        if (fadeImage == null) fadeImage = GetComponent<Image>();
        else if (fadeImage != null)
        {
            Color panelColor = Color.black;
            panelColor.a = 0f;
            fadeImage.color = panelColor;
        }
    }

    private void Awake()
    {
        if (G.ScreenFader != null && G.ScreenFader != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);

        G.ScreenFader = this;
        fadeImage.gameObject.SetActive(false);
    }

    public void FadeIn(float time = 0f, Action onComplete = null)
    {
        if (time == 0) time = fadeTime;
        KillCurrentSequence();

        fadeImage.gameObject.SetActive(true);

        currentSequence = DOTween.Sequence();
        currentSequence.SetUpdate(true);

        Color startColor = fadeImage.color;
        startColor.a = 0f;
        fadeImage.color = startColor;
        currentSequence.Append(fadeImage.DOFade(1f, time).SetEase(Ease.InOutSine));

        if (onComplete != null)
        {
            currentSequence.OnComplete(() => onComplete.Invoke());
        }
    }

    public void FadeOut(float time = 0f, Action onComplete = null)
    {
        if (time == 0) time = fadeTime;
        KillCurrentSequence();

        fadeImage.gameObject.SetActive(true);

        currentSequence = DOTween.Sequence();
        currentSequence.SetUpdate(true);

        Color startColor = fadeImage.color;
        startColor.a = 1f;
        fadeImage.color = startColor;

        currentSequence.Append(fadeImage.DOFade(0f, time).SetEase(Ease.InOutSine));

        if (onComplete != null)
        {
            currentSequence.OnComplete(() =>
            {
                fadeImage.gameObject.SetActive(false);
                onComplete.Invoke();
            });
        }
        else
        {
            currentSequence.OnComplete(() =>
            {
                fadeImage.gameObject.SetActive(false);
            });
        }
    }

    public void FadeOutCustom(Image screen, float time = 0f, Action onComplete = null)
    {
        if (time == 0) time = fadeTime;
        KillCurrentSequence();

        screen.gameObject.SetActive(true);

        currentSequence = DOTween.Sequence();
        currentSequence.SetUpdate(true);

        Color startColor = screen.color;
        startColor.a = 1f;
        screen.color = startColor;

        currentSequence.Append(screen.DOFade(0f, time).SetEase(Ease.InOutSine));

        if (onComplete != null)
        {
            currentSequence.OnComplete(() =>
            {
                screen.gameObject.SetActive(false);
                onComplete.Invoke();
            });
        }
        else
        {
            currentSequence.OnComplete(() =>
            {
                screen.gameObject.SetActive(false);
            });
        }
    }

    public void TransitionFade(float time = 0f, Action action = null)
    {
        FadeIn(time, () =>
        {
            action?.Invoke();
            FadeOut();
        });
    }

    private void KillCurrentSequence()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
            currentSequence = null;
        }
    }

    public void StartLevelTransition()
    {
        if (currentSequence != null) return;
        StartCoroutine(LevelTransition());
    }

    public IEnumerator LevelTransition()
    {
        G.ScreenFader.FadeIn(1f);
        yield return new WaitForSeconds(0.5f);
        G.audioSystem.Play(SoundId.SFX_LevelTransiton);
        yield return new WaitForSeconds(1f);
        G.ScreenFader.FadeOut(1f);
    }
}