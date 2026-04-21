using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WaveProgressView : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private Image barBackground;
    [SerializeField] private Image barFill;
    [SerializeField] private RectTransform markersRoot;
    [SerializeField] private RectTransform cursor;

    [Header("Marker settings")]
    [SerializeField] private Vector2 markerSize = new(1f, 6f);
    [SerializeField] private Vector2 cursorSize = new(2f, 8f);
    [SerializeField] private bool hidePassedMarkers = false;

    [Header("Optional custom sprites")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite passedMarkerSprite;
    [SerializeField] private Sprite upcomingMarkerSprite;
    [SerializeField] private Sprite cursorSprite;

    [Header("Optional colors")]
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField] private Color passedMarkerColor = Color.white;
    [SerializeField] private Color upcomingMarkerColor = Color.white;
    [SerializeField] private Color cursorColor = Color.white;

    private readonly List<Image> spawnedMarkers = new();

    private void Awake()
    {
        ApplyVisualOverrides();
        ValidateSceneRefs();
    }

    private void OnEnable()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        if (G.main == null || G.main.RunState == null || !G.main.IsNightWaveActive)
        {
            SetInactive();
            return;
        }

        ApplyVisualOverrides();

        var elapsedSeconds = Mathf.Max(0f, G.main.CurrentNightElapsedSeconds);
        var durationSeconds = Mathf.Max(0.01f, G.main.CurrentNightDurationSeconds);
        var clampedElapsedSeconds = Mathf.Clamp(elapsedSeconds, 0f, durationSeconds);
        var normalizedProgress = clampedElapsedSeconds / durationSeconds;

        if (timerText != null)
        {
            timerText.text = $"{FormatSeconds(clampedElapsedSeconds)} / {FormatSeconds(durationSeconds)}";
        }

        if (barFill != null)
        {
            barFill.gameObject.SetActive(true);
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = normalizedProgress;
        }

        RefreshMarkers(durationSeconds, clampedElapsedSeconds);
        RefreshCursor(normalizedProgress);
    }

    public void SetInactive()
    {
        if (timerText != null)
        {
            timerText.text = "--:-- / --:--";
        }

        if (barFill != null)
        {
            barFill.fillAmount = 0f;
            barFill.gameObject.SetActive(true);
        }

        ClearMarkers();

        if (cursor != null)
        {
            cursor.gameObject.SetActive(false);
        }
    }

    private void RefreshMarkers(float durationSeconds, float elapsedSeconds)
    {
        ClearMarkers();

        if (markersRoot == null || barRoot == null || G.main == null)
        {
            return;
        }

        var entries = G.main.CurrentNightWaveEntries;
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        var barWidth = barRoot.rect.width;
        if (barWidth <= 0.01f)
        {
            return;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            var normalized = Mathf.Clamp01(entry.TriggerTime / durationSeconds);
            var markerX = normalized * barWidth;

            var markerGo = new GameObject($"WaveMarker_{i}", typeof(RectTransform), typeof(Image));
            markerGo.transform.SetParent(markersRoot, false);

            var markerRect = markerGo.GetComponent<RectTransform>();
            var markerImage = markerGo.GetComponent<Image>();

            var isPassed = entry.TriggerTime <= elapsedSeconds;

            if (hidePassedMarkers && isPassed)
            {
                markerGo.SetActive(false);
                spawnedMarkers.Add(markerImage);
                continue;
            }

            markerRect.anchorMin = new Vector2(0f, 0.5f);
            markerRect.anchorMax = new Vector2(0f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = new Vector2(markerX, 0f);
            markerRect.sizeDelta = markerSize;

            markerImage.raycastTarget = false;
            markerImage.sprite = isPassed ? passedMarkerSprite : upcomingMarkerSprite;
            markerImage.color = isPassed ? passedMarkerColor : upcomingMarkerColor;
            markerImage.type = Image.Type.Simple;
            markerImage.preserveAspect = false;

            spawnedMarkers.Add(markerImage);
        }
    }

    private void RefreshCursor(float normalizedProgress)
    {
        if (cursor == null || barRoot == null)
        {
            return;
        }

        var barWidth = barRoot.rect.width;
        if (barWidth <= 0.01f)
        {
            cursor.gameObject.SetActive(false);
            return;
        }

        cursor.anchorMin = new Vector2(0f, 0.5f);
        cursor.anchorMax = new Vector2(0f, 0.5f);
        cursor.pivot = new Vector2(0.5f, 0.5f);
        cursor.sizeDelta = cursorSize;
        cursor.anchoredPosition = new Vector2(normalizedProgress * barWidth, 0f);
        cursor.gameObject.SetActive(true);
    }

    private void ApplyVisualOverrides()
    {
        if (barBackground != null)
        {
            if (backgroundSprite != null)
            {
                barBackground.sprite = backgroundSprite;
                barBackground.type = Image.Type.Simple;
            }

            barBackground.color = backgroundColor;
            barBackground.raycastTarget = false;
        }

        if (barFill != null)
        {
            if (fillSprite != null)
            {
                barFill.sprite = fillSprite;
            }

            barFill.color = fillColor;
            barFill.raycastTarget = false;
        }

        if (cursor != null)
        {
            var cursorImage = cursor.GetComponent<Image>();
            if (cursorImage != null)
            {
                if (cursorSprite != null)
                {
                    cursorImage.sprite = cursorSprite;
                    cursorImage.type = Image.Type.Simple;
                }

                cursorImage.color = cursorColor;
                cursorImage.raycastTarget = false;
            }
        }
    }

    private void ValidateSceneRefs()
    {
        if (barRoot == null)
        {
            Debug.LogWarning("WaveProgressView: barRoot is not assigned.", this);
        }

        if (markersRoot == null)
        {
            Debug.LogWarning("WaveProgressView: markersRoot is not assigned.", this);
        }

        if (cursor == null)
        {
            Debug.LogWarning("WaveProgressView: cursor is not assigned.", this);
        }
    }

    private void ClearMarkers()
    {
        for (var i = 0; i < spawnedMarkers.Count; i++)
        {
            if (spawnedMarkers[i] != null)
            {
                Destroy(spawnedMarkers[i].gameObject);
            }
        }

        spawnedMarkers.Clear();
    }

    private static string FormatSeconds(float seconds)
    {
        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        var minutes = totalSeconds / 60;
        var remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private void OnDestroy()
    {
        ClearMarkers();
    }
}