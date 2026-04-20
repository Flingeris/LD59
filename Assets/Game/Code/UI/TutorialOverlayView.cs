using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialOverlayView : MonoBehaviour
{
    private const float TypeCharacterInterval = 0.022f;
    private const float IntroFontSize = 14f;
    private const float GameplayFontSize = 10f;

    [SerializeField] private Image inputBlockerImage;
    [SerializeField] private Image backdropImage;
    [SerializeField] private RectTransform messagePanelRect;
    [SerializeField] private Image messagePanelImage;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private RectTransform markerRootRect;
    [SerializeField] private RectTransform markerVisualRect;
    [SerializeField] private Image markerBackgroundImage;
    [SerializeField] private TMP_Text markerText;
    [SerializeField] private SineWaveTextAnimation messageWaveAnimation;

    private RectTransform rootRect;
    private Canvas ownerCanvas;
    private Transform markerTarget;
    private Tween markerTween;

    public static TutorialOverlayView CreateUnder(Transform parent, TMP_Text textTemplate = null)
    {
        if (parent == null)
        {
            return null;
        }

        var layer = parent.gameObject.layer;
        var rootObject = new GameObject(
            "TutorialOverlayView",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(TutorialOverlayView));
        rootObject.layer = layer;
        rootObject.transform.SetParent(parent, false);

        var rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var blockerImage = rootObject.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = false;

        var view = rootObject.GetComponent<TutorialOverlayView>();
        view.inputBlockerImage = blockerImage;
        view.backdropImage = CreateFullscreenImage(
            rootObject.transform,
            "Backdrop",
            new Color(0.02f, 0.02f, 0.03f, 0f),
            false);
        view.messagePanelRect = CreatePanel(rootObject.transform, "MessagePanel");
        view.messagePanelImage = view.messagePanelRect.GetComponent<Image>();
        view.messageText = CreateText(
            view.messagePanelRect,
            "MessageText",
            textTemplate,
            GameplayFontSize,
            TextAlignmentOptions.Center,
            new Color(0.95f, 0.95f, 0.92f, 1f));
        view.messageWaveAnimation = view.messageText.GetComponent<SineWaveTextAnimation>();
        if (view.messageWaveAnimation == null)
        {
            view.messageWaveAnimation = view.messageText.gameObject.AddComponent<SineWaveTextAnimation>();
        }

        view.messageWaveAnimation.textMesh = view.messageText;
        view.messageWaveAnimation.frequency = 3.5f;
        view.messageWaveAnimation.amplitude = 1.4f;
        view.markerRootRect = CreateContainer(rootObject.transform, "MarkerRoot");
        view.markerVisualRect = CreatePanel(view.markerRootRect, "MarkerVisual", new Vector2(128f, 24f));
        view.markerBackgroundImage = view.markerVisualRect.GetComponent<Image>();
        view.markerBackgroundImage.color = new Color(0.1f, 0.13f, 0.18f, 0.92f);
        view.markerText = CreateText(
            view.markerVisualRect,
            "MarkerText",
            textTemplate,
            8f,
            TextAlignmentOptions.Center,
            new Color(0.98f, 0.96f, 0.9f, 1f));
        view.HideImmediate();
        return view;
    }

    private void Awake()
    {
        rootRect = transform as RectTransform;
        ownerCanvas = GetComponentInParent<Canvas>();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        markerTween?.Kill();
        markerTween = null;
    }

    private void LateUpdate()
    {
        UpdateMarkerPosition();
    }

    public void ShowBlackBackdropImmediate()
    {
        EnsureReferences();
        ShowRoot();
        SetBackdropAlpha(1f);
    }

    public IEnumerator FadeBackdropTo(float targetAlpha, float duration)
    {
        EnsureReferences();
        ShowRoot();

        var currentColor = backdropImage.color;
        var fromAlpha = currentColor.a;
        if (duration <= 0f)
        {
            SetBackdropAlpha(targetAlpha);
            UpdateVisibility();
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            SetBackdropAlpha(Mathf.Lerp(fromAlpha, targetAlpha, progress));
            yield return null;
        }

        SetBackdropAlpha(targetAlpha);
        UpdateVisibility();
    }

    public IEnumerator PlayTypedMessage(string text, bool centered)
    {
        EnsureReferences();
        ShowRoot();
        ConfigureMessageLayout(centered);
        messagePanelRect.gameObject.SetActive(true);
        inputBlockerImage.raycastTarget = true;
        messageText.text = string.Empty;

        yield return WaitForInputRelease();

        var safeText = text ?? string.Empty;
        var visibleLength = TextUtils.GetVisibleLength(safeText);
        for (var i = 1; i <= visibleLength; i++)
        {
            if (IsAdvancePressed())
            {
                messageText.text = safeText;
                break;
            }

            messageText.text = TextUtils.CutSmart(safeText, i);
            NotifyTextRevealDelta(safeText, i);
            yield return new WaitForSecondsRealtime(TypeCharacterInterval);
        }

        if (messageText.text != safeText)
        {
            messageText.text = safeText;
        }

        yield return WaitForInputRelease();
        while (!IsAdvancePressed())
        {
            yield return null;
        }

        yield return WaitForInputRelease();
        HideMessage();
    }

    public void HideMessage()
    {
        EnsureReferences();
        messagePanelRect.gameObject.SetActive(false);
        inputBlockerImage.raycastTarget = false;
        UpdateVisibility();
    }

    public void ShowWorldMarker(Transform target, string label, Color color)
    {
        EnsureReferences();
        markerTarget = target;
        markerText.text = label ?? string.Empty;
        markerText.color = color;
        markerBackgroundImage.color = new Color(color.r * 0.18f, color.g * 0.18f, color.b * 0.2f, 0.94f);
        markerRootRect.gameObject.SetActive(true);
        ShowRoot();
        markerTween?.Kill();
        markerVisualRect.localScale = Vector3.one;
        markerTween = markerVisualRect
            .DOScale(1.08f, 0.55f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
        UpdateMarkerPosition();
        UpdateVisibility();
    }

    public void HideWorldMarker()
    {
        EnsureReferences();
        markerTween?.Kill();
        markerTween = null;
        markerTarget = null;
        markerVisualRect.localScale = Vector3.one;
        markerRootRect.gameObject.SetActive(false);
        UpdateVisibility();
    }

    public void HideImmediate()
    {
        EnsureReferences();
        markerTween?.Kill();
        markerTween = null;
        markerTarget = null;
        inputBlockerImage.raycastTarget = false;
        messagePanelRect.gameObject.SetActive(false);
        markerRootRect.gameObject.SetActive(false);
        SetBackdropAlpha(0f);
        gameObject.SetActive(false);
    }

    private void EnsureReferences()
    {
        rootRect ??= transform as RectTransform;
        ownerCanvas ??= GetComponentInParent<Canvas>();
    }

    private void ConfigureMessageLayout(bool centered)
    {
        if (centered)
        {
            messagePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            messagePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            messagePanelRect.pivot = new Vector2(0.5f, 0.5f);
            messagePanelRect.anchoredPosition = Vector2.zero;
            messagePanelRect.sizeDelta = new Vector2(340f, 82f);
            messagePanelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.9f);
            messageText.fontSize = IntroFontSize;
        }
        else
        {
            messagePanelRect.anchorMin = new Vector2(0.5f, 0f);
            messagePanelRect.anchorMax = new Vector2(0.5f, 0f);
            messagePanelRect.pivot = new Vector2(0.5f, 0f);
            messagePanelRect.anchoredPosition = new Vector2(0f, 26f);
            messagePanelRect.sizeDelta = new Vector2(360f, 56f);
            messagePanelImage.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            messageText.fontSize = GameplayFontSize;
        }

        messageText.alignment = TextAlignmentOptions.Center;
    }

    private void UpdateMarkerPosition()
    {
        if (markerTarget == null || markerRootRect == null || !markerRootRect.gameObject.activeSelf)
        {
            return;
        }

        var renderCamera = GetRenderCamera();
        if (renderCamera == null)
        {
            return;
        }

        var worldPosition = markerTarget.position + Vector3.up * 0.95f;
        var viewportPoint = renderCamera.WorldToViewportPoint(worldPosition);
        if (viewportPoint.z < 0f)
        {
            markerRootRect.gameObject.SetActive(false);
            return;
        }

        var screenPoint = RectTransformUtility.WorldToScreenPoint(renderCamera, worldPosition);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect,
                screenPoint,
                GetUiCamera(),
                out var localPoint))
        {
            return;
        }

        markerRootRect.gameObject.SetActive(true);
        markerRootRect.anchoredPosition = localPoint;
    }

    private void ShowRoot()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        transform.SetAsLastSibling();
    }

    private void UpdateVisibility()
    {
        var shouldStayVisible =
            messagePanelRect.gameObject.activeSelf ||
            markerRootRect.gameObject.activeSelf ||
            backdropImage.color.a > 0.001f;

        if (shouldStayVisible)
        {
            ShowRoot();
            return;
        }

        gameObject.SetActive(false);
    }

    private void SetBackdropAlpha(float alpha)
    {
        var color = backdropImage.color;
        color.a = Mathf.Clamp01(alpha);
        backdropImage.color = color;
    }

    private void NotifyTextRevealDelta(string fullText, int visibleCharIndex)
    {
        if (messageText == null || string.IsNullOrEmpty(fullText) || visibleCharIndex <= 0)
        {
            return;
        }

        messageText.gameObject.BroadcastMessage(
            "TextWriterDelta",
            TextUtils.CharSmart(fullText, visibleCharIndex),
            SendMessageOptions.DontRequireReceiver);
    }

    private static IEnumerator WaitForInputRelease()
    {
        yield return null;

        while (Input.GetMouseButton(0) || HasActiveTouch())
        {
            yield return null;
        }
    }

    private static bool IsAdvancePressed()
    {
        return Input.GetMouseButtonDown(0) || HasTouchBegan();
    }

    private static bool HasTouchBegan()
    {
        for (var i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasActiveTouch()
    {
        for (var i = 0; i < Input.touchCount; i++)
        {
            var phase = Input.GetTouch(i).phase;
            if (phase != TouchPhase.Canceled && phase != TouchPhase.Ended)
            {
                return true;
            }
        }

        return false;
    }

    private Camera GetRenderCamera()
    {
        return ownerCanvas != null && ownerCanvas.worldCamera != null
            ? ownerCanvas.worldCamera
            : Camera.main;
    }

    private Camera GetUiCamera()
    {
        if (ownerCanvas == null || ownerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return ownerCanvas.worldCamera != null ? ownerCanvas.worldCamera : Camera.main;
    }

    private static Image CreateFullscreenImage(Transform parent, string objectName, Color color, bool raycastTarget)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        var rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static RectTransform CreatePanel(Transform parent, string objectName, Vector2? size = null)
    {
        var panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = parent.gameObject.layer;
        panelObject.transform.SetParent(parent, false);

        var rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size ?? new Vector2(320f, 56f);

        var image = panelObject.GetComponent<Image>();
        image.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
        image.raycastTarget = false;
        return rectTransform;
    }

    private static RectTransform CreateContainer(Transform parent, string objectName)
    {
        var containerObject = new GameObject(objectName, typeof(RectTransform));
        containerObject.layer = parent.gameObject.layer;
        containerObject.transform.SetParent(parent, false);

        var rectTransform = containerObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        return rectTransform;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        TMP_Text textTemplate,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(10f, 8f);
        rectTransform.offsetMax = new Vector2(-10f, -8f);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        if (textTemplate != null)
        {
            text.font = textTemplate.font;
            text.fontSharedMaterial = textTemplate.fontSharedMaterial;
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.richText = true;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = string.Empty;
        return text;
    }
}
