using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BellTooltipView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform root;

    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior")]
    [SerializeField] private bool followMouse = true;

    [SerializeField] private Vector2 mouseOffset = new Vector2(8f, -6f);
    [SerializeField] private bool clampInsideCanvas = true;

    private Canvas parentCanvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            canvasRect = parentCanvas.GetComponent<RectTransform>();

        HideInstant();
    }

    private void Update()
    {
        if (!followMouse)
            return;

        if (canvasGroup == null || canvasGroup.alpha <= 0.001f)
            return;

        UpdatePosition(Input.mousePosition);
    }

    public void Show(string title, string body)
    {
        if (titleText != null)
            titleText.text = title ?? string.Empty;

        if (bodyText != null)
            bodyText.text = body ?? string.Empty;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            gameObject.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        UpdatePosition(Input.mousePosition);
    }

    public void Hide()
    {
        HideInstant();
    }

    private void HideInstant()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (root == null || canvasRect == null || parentCanvas == null)
            return;

        var targetScreenPos = screenPosition + mouseOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreenPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out var localPoint
        );

        var canvasSize = canvasRect.rect.size;

        // localPoint приходит относительно pivot canvasRect.
        // Нам нужна система координат anchoredPosition для tooltip,
        // у которого anchorMin/max = (0,1), pivot = (0,1),
        // то есть координаты от верхнего левого угла.
        var anchored = new Vector2(
            localPoint.x + canvasSize.x * 0.5f,
            localPoint.y - canvasSize.y * 0.5f
        );

        root.anchoredPosition = clampInsideCanvas ? ClampToCanvas(anchored) : anchored;
    }

    private Vector2 ClampToCanvas(Vector2 desired)
    {
        if (root == null || canvasRect == null)
            return desired;

        var canvasSize = canvasRect.rect.size;
        var tooltipSize = root.rect.size;

        float x = Mathf.Clamp(desired.x, 0f, Mathf.Max(0f, canvasSize.x - tooltipSize.x));
        float y = Mathf.Clamp(desired.y, -Mathf.Max(0f, canvasSize.y - tooltipSize.y), 0f);

        return new Vector2(x, y);
    }

    public static string BuildBody(BellDef bellDef, UnitDef unitDef, int faithCost)
    {
        var lines = new List<string>();

        lines.Add($"Cost: {faithCost} Faith");

        if (unitDef != null)
        {
            string summonName;
            switch (unitDef.Id)
            {
                case "skel1":
                    summonName = "Summon skeleton";
                    break;

                case "zombie":
                    summonName = "Summon zombie";
                    break;

                case "vampire":
                    summonName = "Summon vampire";
                    break;

                default:
                    summonName = "Summon undead ally";
                    break;
            }

            lines.Add(summonName);
            lines.Add($"HP {unitDef.Hp}  DMG {unitDef.Damage}");
        }
        else
        {
            lines.Add("Summon undead ally");
        }

        return string.Join("\n", lines);
    }

    public static string BuildTitle(BellDef bellDef, string bellId)
    {
        return Humanize(bellDef != null ? bellDef.DisplayName : null, bellId);
    }

    private static string Humanize(string preferred, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
            return preferred;

        if (string.IsNullOrWhiteSpace(fallback))
            return "Unknown";

        var raw = fallback.Replace('_', ' ').Replace('-', ' ').Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return "Unknown";

        var parts = raw.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
                continue;

            var lower = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", parts);
    }
}