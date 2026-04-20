using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private TMP_Text clickToStartText;

    private Tween clickToStartTween;
    private TutorialOverlayView tutorialOverlayView;

    public Image TitleScreenImage
    {
        get
        {
            ResolveReferences();
            return titleScreen != null ? titleScreen.GetComponent<Image>() : null;
        }
    }

    public static UI EnsureInstance()
    {
        var existingUi = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
        if (existingUi != null)
        {
            existingUi.ResolveReferences();
            if (G.ui != existingUi)
            {
                G.ui = existingUi;
            }

            return existingUi;
        }

        var host = FindSceneObject("UI_Canvas");
        if (host == null)
        {
            var discoveredTitleScreen = FindSceneObject("TitleScreen");
            host = discoveredTitleScreen != null && discoveredTitleScreen.transform.parent != null
                ? discoveredTitleScreen.transform.parent.gameObject
                : null;
        }

        if (host == null)
        {
            return null;
        }

        var createdUi = host.AddComponent<UI>();
        createdUi.ResolveReferences();
        G.ui = createdUi;
        return createdUi;
    }

    private void Awake()
    {
        G.ui = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        StopTitlePromptPulse();

        if (G.ui == this)
        {
            G.ui = null;
        }
    }

    public void ToggleTitle(bool visible)
    {
        ResolveReferences();
        if (titleScreen == null)
        {
            return;
        }

        if (visible)
        {
            ResetTitleScreenAlpha();
            titleScreen.SetActive(true);
            PlayTitlePromptPulse();
            titleScreen.transform.SetAsLastSibling();
            return;
        }

        StopTitlePromptPulse();
        titleScreen.SetActive(false);
    }

    public void PlayTitlePromptPulse()
    {
        ResolveReferences();
        if (clickToStartText == null)
        {
            return;
        }

        StopTitlePromptPulse();
        clickToStartText.gameObject.SetActive(true);
        clickToStartText.rectTransform.localScale = Vector3.one;
        SetClickPromptAlpha(1f);

        clickToStartTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(clickToStartText.rectTransform.DOScale(1.08f, 0.75f).SetEase(Ease.InOutSine))
            .Join(clickToStartText.DOFade(0.45f, 0.75f).SetEase(Ease.InOutSine))
            .Append(clickToStartText.rectTransform.DOScale(1f, 0.75f).SetEase(Ease.InOutSine))
            .Join(clickToStartText.DOFade(1f, 0.75f).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopTitlePromptPulse()
    {
        if (clickToStartTween != null && clickToStartTween.IsActive())
        {
            clickToStartTween.Kill();
        }

        clickToStartTween = null;

        if (clickToStartText == null)
        {
            return;
        }

        clickToStartText.rectTransform.localScale = Vector3.one;
        SetClickPromptAlpha(1f);
    }

    public TutorialOverlayView EnsureTutorialOverlay()
    {
        if (tutorialOverlayView != null)
        {
            return tutorialOverlayView;
        }

        tutorialOverlayView = GetComponentInChildren<TutorialOverlayView>(true);
        if (tutorialOverlayView != null)
        {
            return tutorialOverlayView;
        }

        tutorialOverlayView = TutorialOverlayView.CreateUnder(transform, GetTextTemplate());
        return tutorialOverlayView;
    }

    private void ResolveReferences()
    {
        if (titleScreen == null)
        {
            titleScreen = FindSceneObject("TitleScreen");
        }

        if (clickToStartText != null || titleScreen == null)
        {
            return;
        }

        var texts = titleScreen.GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var candidate = texts[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.text.IndexOf("click", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                clickToStartText = candidate;
                break;
            }
        }

        if (clickToStartText == null && texts.Length > 0)
        {
            clickToStartText = texts[0];
        }
    }

    private void ResetTitleScreenAlpha()
    {
        var titleImage = TitleScreenImage;
        if (titleImage == null)
        {
            return;
        }

        var color = titleImage.color;
        color.a = 1f;
        titleImage.color = color;
    }

    private void SetClickPromptAlpha(float alpha)
    {
        if (clickToStartText == null)
        {
            return;
        }

        var color = clickToStartText.color;
        color.a = alpha;
        clickToStartText.color = color;
    }

    private TMP_Text GetTextTemplate()
    {
        if (clickToStartText != null)
        {
            return clickToStartText;
        }

        return GetComponentInChildren<TMP_Text>(true);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (var i = 0; i < transforms.Length; i++)
        {
            var candidate = transforms[i];
            if (candidate == null ||
                candidate.hideFlags != HideFlags.None ||
                !candidate.gameObject.scene.IsValid() ||
                !string.Equals(candidate.name, objectName, StringComparison.Ordinal))
            {
                continue;
            }

            return candidate.gameObject;
        }

        return null;
    }
}
