using UnityEngine;

public class KeeperActor : MonoBehaviour
{
    private const float FacingThreshold = 0.001f;
    private const int SortingPrecision = 100;

    private SpriteRenderer spriteRenderer;

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public Vector2 GetWorldPosition()
    {
        var worldPosition = transform.position;
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    public void SetWorldPosition(Vector2 worldPosition)
    {
        var currentPosition = transform.position;
        UpdateFacing(new Vector2(worldPosition.x - currentPosition.x, worldPosition.y - currentPosition.y));
        transform.position = new Vector3(worldPosition.x, worldPosition.y, currentPosition.z);
    }

    private void UpdateFacing(Vector2 movementOffset)
    {
        if (Mathf.Abs(movementOffset.x) <= FacingThreshold)
        {
            return;
        }

        var targetRenderer = GetSpriteRenderer();
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.flipX = movementOffset.x < 0f;
    }

    private void UpdateSortingOrder()
    {
        var targetRenderer = GetSpriteRenderer();
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * SortingPrecision);
    }

    private SpriteRenderer GetSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        return spriteRenderer;
    }
}
