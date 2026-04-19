using UnityEngine;
using UnityEngine.EventSystems;

public class NightPointOfInterest : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string id;
    [SerializeField] private NightPoiType type;
    [Min(0f)] [SerializeField] private float interactionRadius = 0.6f;

    public string Id => id;
    public NightPoiType Type => type;
    public float InteractionRadius => Mathf.Max(0f, interactionRadius);

    public Vector2 GetWorldPosition()
    {
        var worldPosition = transform.position;
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    public bool IsKeeperInInteractionRange(Vector2 keeperPosition)
    {
        var interactionRadiusValue = InteractionRadius;
        if (interactionRadiusValue <= 0f)
        {
            return false;
        }

        return Vector2.Distance(GetWorldPosition(), keeperPosition) <= interactionRadiusValue;
    }

    public bool TryGetWorldInteractionValidationError(out string validationError)
    {
        if (!TryGetComponent<SpriteRenderer>(out var spriteRenderer) || spriteRenderer == null)
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

        if (InteractionRadius <= 0f)
        {
            validationError = "invalid interaction radius";
            return true;
        }

        validationError = string.Empty;
        return false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (G.main == null)
        {
            Debug.LogWarning($"Night POI click ignored for '{id}': Main is missing");
            return;
        }

        G.main.TryMoveKeeperToPoi(id);
    }
}
