using UnityEngine;
using UnityEngine.EventSystems;

public class BellWorldObject : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string bellId;
    [SerializeField] private WorldBarFillShrink cooldownBar;
    [Min(0f)] [SerializeField] private float popupVerticalPadding = 0.28f;

    private float cooldownRemainingSeconds;
    private float cooldownDurationSeconds;
    private SpriteRenderer spriteRenderer;

    public string BellId => bellId;
    public float CooldownRemainingSeconds => Mathf.Max(0f, cooldownRemainingSeconds);
    public bool IsOnCooldown => CooldownRemainingSeconds > 0f;

    private void Awake()
    {
        EnsureReferences();
        cooldownBar?.SetVisible(false);
    }

    private void Update()
    {
        if (cooldownRemainingSeconds <= 0f)
        {
            cooldownBar?.SetVisible(false);
            return;
        }

        cooldownRemainingSeconds = Mathf.Max(0f, cooldownRemainingSeconds - Time.deltaTime);
        RefreshCooldownPresentation();
    }

    public bool TryGetWorldInteractionValidationError(out string validationError)
    {
        EnsureReferences();

        if (string.IsNullOrWhiteSpace(bellId))
        {
            validationError = "missing bell id";
            return true;
        }

        if (CMS.Get<BellDef>(bellId) == null)
        {
            validationError = "missing BellDef";
            return true;
        }

        if (spriteRenderer == null)
        {
            validationError = "missing SpriteRenderer";
            return true;
        }

        if (!TryGetComponent<Collider2D>(out var collider2D) || collider2D == null)
        {
            validationError = "missing Collider2D";
            return true;
        }

        if (!collider2D.enabled)
        {
            validationError = "disabled Collider2D";
            return true;
        }

        validationError = string.Empty;
        return false;
    }

    public void ShowFeedbackPopup(string message, Color color)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureReferences();
        Popups.TextAbove(transform, GetPopupOffsetY(), message, color);
    }

    public void InitializeRuntime(string runtimeBellId)
    {
        bellId = runtimeBellId;
        cooldownRemainingSeconds = 0f;
        cooldownDurationSeconds = 0f;
        EnsureReferences();
        cooldownBar?.SetVisible(false);
    }

    public void StartCooldown(float cooldownSeconds)
    {
        cooldownDurationSeconds = Mathf.Max(0f, cooldownSeconds);
        cooldownRemainingSeconds = cooldownDurationSeconds;
        RefreshCooldownPresentation();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (G.main == null)
        {
            Debug.LogWarning($"Bell click ignored for '{bellId}': Main is missing");
            return;
        }

        G.main.TryInteractWithBell(bellId);
    }

    private void RefreshCooldownPresentation()
    {
        if (cooldownBar == null)
            return;

        if (cooldownRemainingSeconds <= 0f || cooldownDurationSeconds <= 0f)
        {
            cooldownBar.SetVisible(false);
            return;
        }

        var remainingNormalized = cooldownRemainingSeconds / Mathf.Max(0.01f, cooldownDurationSeconds);
        cooldownBar.SetVisible(true);
        cooldownBar.SetNormalized(remainingNormalized);
    }

    private void EnsureReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (cooldownBar == null)
            cooldownBar = GetComponentInChildren<WorldBarFillShrink>();
    }

    private float GetPopupOffsetY()
    {
        var spriteHeight = spriteRenderer != null ? spriteRenderer.bounds.size.y : 0.4f;
        return spriteHeight * 0.6f + popupVerticalPadding;
    }
}
