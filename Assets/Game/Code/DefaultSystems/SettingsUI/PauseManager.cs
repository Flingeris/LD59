using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.08f, 0.09f, 0.13f, 0.92f);
    private static readonly Color PanelOutlineColor = new(0.63f, 0.72f, 0.95f, 0.85f);
    private static readonly Color ButtonColor = new(0.17f, 0.2f, 0.28f, 0.96f);
    private static readonly Color SliderBackgroundColor = new(0.19f, 0.2f, 0.28f, 1f);
    private static readonly Color SliderFillColor = new(0.72f, 0.81f, 1f, 1f);
    private static readonly Color ToggleBackgroundColor = new(0.19f, 0.2f, 0.28f, 1f);
    private static readonly Color ToggleCheckColor = new(0.78f, 0.88f, 1f, 1f);
    private static readonly Color TextColor = new(0.95f, 0.97f, 1f, 1f);

    [SerializeField] private Slider slider_SFX;
    [SerializeField] private Slider slider_Music;
    [SerializeField] private Toggle toggle_PostFx;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Camera targetCamera;

    public bool IsOpen { get; private set; } = false;

    private void Awake()
    {
        EnsureUiBuilt();
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        EnsureUiBuilt();

        if (slider_Music != null)
        {
            slider_Music.SetValueWithoutNotify(G.audioSystem != null ? G.audioSystem.MusicVolume : 1f);
        }

        if (slider_SFX != null)
        {
            slider_SFX.SetValueWithoutNotify(G.audioSystem != null ? G.audioSystem.SfxVolume : 1f);
        }

        if (toggle_PostFx != null)
        {
            toggle_PostFx.SetIsOnWithoutNotify(IsPostFxEnabled());
        }

        SetToggle(false);
    }

    public void SetMusicVolume(float volume)
    {
        if (G.audioSystem == null) return;
        G.audioSystem.SetMusicVolume(volume);
        G.audioSystem.SetAmbientVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (G.audioSystem == null) return;
        G.audioSystem.SetSFXVolume(volume);
    }

    public void SetPostFxEnabled(bool enabled)
    {
        var cameraToConfigure = ResolveTargetCamera();
        if (cameraToConfigure == null)
        {
            return;
        }

        var additionalCameraData = cameraToConfigure.GetUniversalAdditionalCameraData();
        additionalCameraData.renderPostProcessing = enabled;
    }

    public void SetToggle(bool toggle)
    {
        IsOpen = toggle;
        if (pausePanel != null)
        {
            pausePanel.SetActive(IsOpen);
        }

        //if (IsOpen)
        //{
        //    Time.timeScale = 0f;
        //}
        //else
        //{
        //    Time.timeScale = 1f;
        //}
    }

    public void Toggle()
    {
        bool b = !IsOpen;
        SetToggle(b);
    }

    private void EnsureUiBuilt()
    {
        ResolveFontAsset();

        if (toggleButton == null)
        {
            toggleButton = CreateMenuButton();
        }

        if (pausePanel == null)
        {
            pausePanel = CreatePanel();
        }

        BindGeneratedReferences();
    }

    private void ResolveFontAsset()
    {
        if (fontAsset != null)
        {
            return;
        }

        var sampleText = GetComponentInChildren<TMP_Text>(true);
        if (sampleText != null)
        {
            fontAsset = sampleText.font;
        }
    }

    private void BindGeneratedReferences()
    {
        if (pausePanel == null)
        {
            return;
        }

        if (slider_Music == null)
        {
            slider_Music = pausePanel.transform.Find("MusicRow/Slider")?.GetComponent<Slider>();
        }

        if (slider_SFX == null)
        {
            slider_SFX = pausePanel.transform.Find("SfxRow/Slider")?.GetComponent<Slider>();
        }

        if (toggle_PostFx == null)
        {
            toggle_PostFx = pausePanel.transform.Find("PostFxRow/Toggle")?.GetComponent<Toggle>();
        }

        if (slider_Music != null)
        {
            slider_Music.onValueChanged.RemoveListener(SetMusicVolume);
            slider_Music.onValueChanged.AddListener(SetMusicVolume);
        }

        if (slider_SFX != null)
        {
            slider_SFX.onValueChanged.RemoveListener(SetSFXVolume);
            slider_SFX.onValueChanged.AddListener(SetSFXVolume);
        }

        if (toggle_PostFx != null)
        {
            toggle_PostFx.onValueChanged.RemoveListener(SetPostFxEnabled);
            toggle_PostFx.onValueChanged.AddListener(SetPostFxEnabled);
        }
    }

    private Button CreateMenuButton()
    {
        var buttonRoot = CreateUiObject("SettingsButton", transform);
        var buttonRect = buttonRoot.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-6f, -6f);
        buttonRect.sizeDelta = new Vector2(24f, 16f);

        var image = buttonRoot.AddComponent<Image>();
        image.color = ButtonColor;
        AddOutline(buttonRoot, PanelOutlineColor, 1f);

        var button = buttonRoot.AddComponent<Button>();
        button.onClick.AddListener(Toggle);

        var label = CreateText("Label", buttonRoot.transform, "SET", 5f, TextAlignmentOptions.Center);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private GameObject CreatePanel()
    {
        var panelRoot = CreateUiObject("SettingsPanel", transform);
        var panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-6f, -26f);
        panelRect.sizeDelta = new Vector2(114f, 72f);

        var background = panelRoot.AddComponent<Image>();
        background.color = PanelColor;
        AddOutline(panelRoot, PanelOutlineColor, 1f);

        CreateText("Title", panelRoot.transform, "SETTINGS", 5f, TextAlignmentOptions.Center);
        var titleRect = panelRoot.transform.Find("Title").GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -6f);
        titleRect.sizeDelta = new Vector2(96f, 10f);

        CreateSliderRow(panelRoot.transform, "MusicRow", "MUSIC", new Vector2(8f, -22f));
        CreateSliderRow(panelRoot.transform, "SfxRow", "SFX", new Vector2(8f, -40f));
        CreateToggleRow(panelRoot.transform, "PostFxRow", "POST FX", new Vector2(8f, -58f));

        panelRoot.SetActive(false);
        return panelRoot;
    }

    private void CreateSliderRow(Transform parent, string rowName, string labelText, Vector2 anchoredPosition)
    {
        var row = CreateUiObject(rowName, parent);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(98f, 14f);

        var label = CreateText("Label", row.transform, labelText, 4f, TextAlignmentOptions.MidlineLeft);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(38f, 12f);

        var sliderObject = CreateUiObject("Slider", row.transform);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(1f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(1f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, 0f);
        sliderRect.sizeDelta = new Vector2(56f, 12f);

        var slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        var background = CreateUiObject("Background", sliderObject.transform);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.offsetMin = new Vector2(0f, -2f);
        backgroundRect.offsetMax = new Vector2(0f, 2f);
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = SliderBackgroundColor;

        var fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(1f, 0f);
        fillAreaRect.offsetMax = new Vector2(-1f, 0f);

        var fill = CreateUiObject("Fill", fillArea.transform);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = SliderFillColor;

        var handle = CreateUiObject("Handle", sliderObject.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(5f, 10f);
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = TextColor;
        AddOutline(handle, PanelOutlineColor, 1f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
    }

    private void CreateToggleRow(Transform parent, string rowName, string labelText, Vector2 anchoredPosition)
    {
        var row = CreateUiObject(rowName, parent);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(98f, 12f);

        var label = CreateText("Label", row.transform, labelText, 4f, TextAlignmentOptions.MidlineLeft);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(48f, 12f);

        var toggleObject = CreateUiObject("Toggle", row.transform);
        var toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(-2f, 0f);
        toggleRect.sizeDelta = new Vector2(12f, 12f);

        var toggle = toggleObject.AddComponent<Toggle>();

        var background = CreateUiObject("Background", toggleObject.transform);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(10f, 10f);
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = ToggleBackgroundColor;
        AddOutline(background, PanelOutlineColor, 1f);

        var checkmark = CreateUiObject("Checkmark", background.transform);
        var checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchoredPosition = Vector2.zero;
        checkmarkRect.sizeDelta = new Vector2(6f, 6f);
        var checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.color = ToggleCheckColor;

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string textValue,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        var textObject = CreateUiObject(objectName, parent);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = TextColor;
        text.raycastTarget = false;
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        return text;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static void AddOutline(GameObject targetObject, Color outlineColor, float effectDistance)
    {
        var outline = targetObject.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(effectDistance, -effectDistance);
    }

    private Camera ResolveTargetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }

        return targetCamera;
    }

    private bool IsPostFxEnabled()
    {
        var cameraToCheck = ResolveTargetCamera();
        if (cameraToCheck == null)
        {
            return false;
        }

        return cameraToCheck.GetUniversalAdditionalCameraData().renderPostProcessing;
    }
}
